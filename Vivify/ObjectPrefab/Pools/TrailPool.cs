using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Vivify.ObjectPrefab.Pools;

internal class TrailPool : IPrefabPool<FollowedSaberTrail>
{
    private readonly Dictionary<Component, FollowedSaberTrail> _active = new();
    private readonly Stack<FollowedSaberTrail> _inactive = new();
    private readonly Material _material;
    private readonly TrailProperties _trailProperties;
    private readonly IInstantiator _instantiator;

    internal TrailPool(Material material, TrailProperties trailProperties, IInstantiator instantiator)
    {
        _material = material;
        _trailProperties = trailProperties;
        _instantiator = instantiator;
    }

    public void Despawn(Component component)
    {
        if (!_active.TryGetValue(component, out FollowedSaberTrail spawned))
        {
            return;
        }

        spawned.gameObject.SetActive(false);
        _inactive.Push(spawned);
        _active.Remove(component);

        spawned.transform.SetParent(null, false);
    }

    public void Dispose()
    {
        _inactive.Do(Object.Destroy);
        _active.Values.Do(Object.Destroy);
    }

    public FollowedSaberTrail Spawn(Component component, float _)
    {
        if (_active.TryGetValue(component, out FollowedSaberTrail spawned))
        {
            return spawned;
        }

        if (_inactive.Count == 0)
        {
            GameObject gameObject = new("FollowedSaberTrail");
            spawned = _instantiator.InstantiateComponent<FollowedSaberTrail>(gameObject);
            spawned.Material = _material;
        }
        else
        {
            spawned = _inactive.Pop();
            spawned.gameObject.SetActive(true);
        }

        spawned.InitProperties(_trailProperties);
        _active.Add(component, spawned);
        return spawned;
    }
}
