using System;
using System.Collections.Generic;
using Heck.Animation;
using Vivify.ObjectPrefab.Managers;
using Vivify.ObjectPrefab.Pools;

namespace Vivify.ObjectPrefab.Collections;

internal class PrefabDictionary : IPrefabCollection
{
    private readonly Dictionary<Track, HashSet<PrefabPool?>> _dictionary = new();

    internal event Action<Track>? Changed;

    internal bool AddPrefabPool(Track key, PrefabPool? prefabPool, LoadMode loadMode)
    {
        if (!_dictionary.TryGetValue(key, out HashSet<PrefabPool?> prefabPools))
        {
            _dictionary[key] = prefabPools = [null];
        }

        if (loadMode == LoadMode.Single)
        {
            prefabPools.Clear();
        }

        bool result = prefabPools.Add(prefabPool);
        Changed?.Invoke(key);
        return result;
    }

    internal void Clear()
    {
        _dictionary.Clear();
    }

    internal bool TryGetValue(Track key, out HashSet<PrefabPool?> value)
    {
        return _dictionary.TryGetValue(key, out value);
    }
}
