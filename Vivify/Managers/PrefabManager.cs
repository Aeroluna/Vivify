using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Heck;
using Heck.Animation;
using IPA.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;
using Logger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

namespace Vivify.Managers
{
    internal class PrefabManager : IDisposable
    {
        private readonly Dictionary<string, InstantiatedPrefab> _prefabs = new();
        private readonly ReLoader? _reLoader;

        [UsedImplicitly]
        private PrefabManager([InjectOptional] ReLoader? reLoader)
        {
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

        internal void Add(string id, GameObject prefab, Track? track)
        {
            _prefabs.Add(id, new InstantiatedPrefab(prefab, track));
        }

        internal void Destroy(string id)
        {
            if (!TryGetPrefab(id, out InstantiatedPrefab? prefab))
            {
                return;
            }

            Log.Logger.Log($"Destroying [{id}].");

            prefab.Track?.RemoveGameObject(prefab.GameObject);

            Object.Destroy(prefab.GameObject);
            _prefabs.Remove(id);
        }

        internal bool TryGetPrefab(string id, [NotNullWhen(true)] out InstantiatedPrefab? prefab)
        {
            bool result = _prefabs.TryGetValue(id, out prefab);
            if (!result)
            {
                Log.Logger.Log($"No prefab with id [{id}] detected.", Logger.Level.Error);
            }

            return result;
        }

        private void DestroyAllPrefabs()
        {
            foreach ((string _, InstantiatedPrefab prefab) in _prefabs)
            {
                prefab.Track?.RemoveGameObject(prefab.GameObject);
                Object.Destroy(prefab.GameObject);
            }

            _prefabs.Clear();
        }
    }

    internal class InstantiatedPrefab
    {
        internal InstantiatedPrefab(GameObject gameObject, Track? track)
        {
            GameObject = gameObject;
            Track = track;
            Animators = gameObject.GetComponentsInChildren<Animator>();
        }

        internal GameObject GameObject { get; }

        internal Track? Track { get; }

        internal Animator[] Animators { get; }
    }
}
