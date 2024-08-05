using System;
using UnityEngine;

namespace Vivify.ObjectPrefab.Pools;

internal interface IPrefabPool : IDisposable
{
    public void Despawn(Component component);
}

internal interface IPrefabPool<out T> : IPrefabPool
{
    public T Spawn(Component component, float startTime);
}
