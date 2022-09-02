using System;
using System.Collections.Generic;
using Heck;
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

        internal void Add(string id, GameObject prefab)
        {
            _prefabs.Add(id, new InstantiatedPrefab(prefab));
        }

        internal void Destroy(string id)
        {
            if (!_prefabs.TryGetValue(id, out InstantiatedPrefab? prefab))
            {
                Log.Logger.Log($"No prefab with id [{id}] detected.", Logger.Level.Error);
                return;
            }

            Log.Logger.Log($"Destroying [{id}].");

            Object.Destroy(prefab.GameObject);
            _prefabs.Remove(id);
        }

        private void DestroyAllPrefabs()
        {
            foreach (KeyValuePair<string, InstantiatedPrefab> keyValuePair in _prefabs)
            {
                Object.Destroy(keyValuePair.Value.GameObject);
            }

            _prefabs.Clear();
        }
    }

    internal class InstantiatedPrefab
    {
        internal InstantiatedPrefab(GameObject gameObject)
        {
            GameObject = gameObject;
            Animators = gameObject.GetComponentsInChildren<Animator>();
        }

        internal GameObject GameObject { get; }

        internal Animator[] Animators { get; }
    }
}
