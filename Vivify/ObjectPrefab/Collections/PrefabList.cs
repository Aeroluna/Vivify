using System;
using System.Collections.Generic;
using Vivify.ObjectPrefab.Managers;
using Vivify.ObjectPrefab.Pools;

namespace Vivify.ObjectPrefab.Collections;

internal abstract class PrefabList<T> : IPrefabCollection
    where T : class
{
    internal event Action<float>? Changed;

    internal HashSet<T?> HashSet { get; } = [null];

    internal bool AddPool(T? pool, LoadMode loadMode, float time)
    {
        if (loadMode == LoadMode.Single)
        {
            HashSet.Clear();
        }

        bool result = HashSet.Add(pool);
        Changed?.Invoke(time);
        return result;
    }

    internal void Clear()
    {
        HashSet.Clear();
    }
}

internal class PrefabList : PrefabList<PrefabPool>;

internal class TrailList : PrefabList<TrailPool>;
