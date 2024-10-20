using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Heck.Animation;
using Heck.ReLoad;
using IPA.Utilities;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Vivify.Managers;

internal class PrefabManager : IDisposable
{
    private readonly SiraLog _log;
    private readonly Dictionary<string, InstantiatedPrefab> _prefabs = new();
    private readonly ReLoader? _reLoader;

    [UsedImplicitly]
    private PrefabManager(SiraLog log, [InjectOptional] ReLoader? reLoader)
    {
        _log = log;
        _reLoader = reLoader;
        if (reLoader != null)
        {
            reLoader.Rewinded += DestroyAllPrefabs;
        }
    }

    public void Dispose()
    {
        if (_reLoader != null)
        {
            _reLoader.Rewinded -= DestroyAllPrefabs;
        }
    }

    internal void Add(string id, GameObject prefab, List<Track>? track)
    {
        _prefabs.Add(id, new InstantiatedPrefab(prefab, track));
    }

    internal void Destroy(string id)
    {
        if (!TryGetPrefab(id, out InstantiatedPrefab? prefab))
        {
            return;
        }

        _log.Debug($"Destroying [{id}]");

        List<Track>? tracks = prefab.Track;
        if (tracks != null)
        {
            foreach (Track track in tracks)
            {
                track.RemoveGameObject(prefab.GameObject);
            }
        }

        ////prefab.GameObject.SetActive(false);
        Object.Destroy(prefab.GameObject);
        _prefabs.Remove(id);
    }

    internal bool TryGetPrefab(string id, [NotNullWhen(true)] out InstantiatedPrefab? prefab)
    {
        bool result = _prefabs.TryGetValue(id, out prefab);
        if (!result)
        {
            _log.Error($"No prefab with id [{id}] detected");
        }

        return result;
    }

    private void DestroyAllPrefabs()
    {
        foreach ((string _, InstantiatedPrefab prefab) in _prefabs)
        {
            List<Track>? tracks = prefab.Track;
            if (tracks != null)
            {
                foreach (Track track in tracks)
                {
                    track.RemoveGameObject(prefab.GameObject);
                }
            }

            Object.Destroy(prefab.GameObject);
        }

        _prefabs.Clear();
    }
}

internal class InstantiatedPrefab
{
    internal InstantiatedPrefab(GameObject gameObject, List<Track>? track)
    {
        GameObject = gameObject;
        Track = track;
        Animators = gameObject.GetComponentsInChildren<Animator>();
    }

    internal Animator[] Animators { get; }

    internal GameObject GameObject { get; }

    internal List<Track>? Track { get; }
}
