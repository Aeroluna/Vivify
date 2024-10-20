using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Vivify.Controllers.Sync;
using Zenject;
using Object = UnityEngine.Object;

namespace Vivify.ObjectPrefab.Pools;

internal class PrefabPool : IPrefabPool<GameObject>
{
    private readonly Dictionary<Component, GameObject> _active = new();
    private readonly Stack<GameObject> _inactive = new();
    private readonly IInstantiator _instantiator;
    private readonly GameObject _original;

    internal PrefabPool(GameObject original, IInstantiator instantiator)
    {
        _original = original;
        _instantiator = instantiator;
    }

    public void Despawn(Component component)
    {
        if (!_active.TryGetValue(component, out GameObject spawned))
        {
            return;
        }

        spawned.SetActive(false);
        _inactive.Push(spawned);
        _active.Remove(component);

        spawned.transform.SetParent(null, false);
    }

    public void Dispose()
    {
        _inactive.Do(Object.Destroy);
        _active.Values.Do(Object.Destroy);
    }

    public GameObject Spawn(Component component, float startTime)
    {
        GameObject spawned;
        if (_inactive.Count == 0)
        {
            spawned = Object.Instantiate(_original);
        }
        else
        {
            spawned = _inactive.Pop();
            spawned.SetActive(true);
        }

        Animator[] animators = spawned.GetComponents<Animator>();
        foreach (Animator animator in animators)
        {
            animator.Rebind();
            animator.Update(0.01f);
        }

        _active.Add(component, spawned);

        _instantiator.SongSynchronize(spawned, startTime);
        return spawned;
    }
}
