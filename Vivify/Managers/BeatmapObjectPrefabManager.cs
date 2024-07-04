using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Heck.Animation;
using Heck.Deserialize;
using Heck.ReLoad;
using JetBrains.Annotations;
using UnityEngine;
using Vivify.Controllers.Sync;
using Zenject;
using Object = UnityEngine.Object;

namespace Vivify.Managers
{
    internal class BeatmapObjectPrefabManager : IDisposable
    {
        private readonly BeatmapObjectManager _beatmapObjectManager;
        private readonly IInstantiator _instantiator;
        private readonly DeserializedData _deserializedData;
        private readonly ReLoader? _reLoader;
        private readonly Dictionary<Track, PrefabPool> _prefabPools = new();
        private readonly Dictionary<NoteController, PrefabPool> _activePools = new();

        private readonly Dictionary<NoteController, MPBControllerHijacker> _hijackers = new();

        [UsedImplicitly]
        private BeatmapObjectPrefabManager(
            BeatmapObjectManager beatmapObjectManager,
            IInstantiator instantiator,
            [Inject(Id = VivifyController.ID)] DeserializedData deserializedData,
            [InjectOptional] ReLoader? reLoader)
        {
            _beatmapObjectManager = beatmapObjectManager;
            _instantiator = instantiator;
            _deserializedData = deserializedData;
            _reLoader = reLoader;
            if (reLoader != null)
            {
                reLoader.Rewinded += OnRewind;
            }

            beatmapObjectManager.noteWasSpawnedEvent += HandleNoteWasSpawned;
            beatmapObjectManager.noteWasDespawnedEvent += HandleNoteWasDespawned;
        }

        public void Dispose()
        {
            if (_reLoader != null)
            {
                _reLoader.Rewinded -= OnRewind;
            }

            _beatmapObjectManager.noteWasSpawnedEvent -= HandleNoteWasSpawned;
            _beatmapObjectManager.noteWasDespawnedEvent -= HandleNoteWasDespawned;
            _prefabPools.Values.Do(n => n.Dispose());
        }

        internal void Add(Track track, GameObject prefab)
        {
            _prefabPools.Add(track, new PrefabPool(prefab, _instantiator));
        }

        private void HandleNoteWasSpawned(NoteController noteController)
        {
            if (!_deserializedData.Resolve(noteController.noteData, out VivifyObjectData? data) || data.Track == null)
            {
                return;
            }

            foreach (Track track in data.Track)
            {
                if (!_prefabPools.TryGetValue(track, out PrefabPool prefabPool))
                {
                    continue;
                }

                GameObject spawned = prefabPool.Spawn(noteController);
                if (!_hijackers.TryGetValue(noteController, out MPBControllerHijacker hijacker))
                {
                    hijacker = new MPBControllerHijacker(noteController);
                    _hijackers.Add(noteController, hijacker);
                }

                hijacker.Activate(spawned);
                _activePools.Add(noteController, prefabPool);
                return;
            }
        }

        private void HandleNoteWasDespawned(NoteController noteController)
        {
            if (!_activePools.TryGetValue(noteController, out PrefabPool pool))
            {
                return;
            }

            if (_hijackers.TryGetValue(noteController, out MPBControllerHijacker hijacker))
            {
                hijacker.Deactivate();
            }

            pool.Despawn(noteController);
            _activePools.Remove(noteController);
        }

        private void OnRewind()
        {
            _activePools.Clear();
            _prefabPools.Values.Do(n => n.Dispose());
            _prefabPools.Clear();
        }

        private class PrefabPool
        {
            private readonly GameObject _original;
            private readonly IInstantiator _instantiator;

            private readonly Stack<GameObject> _inactive = new();

            private readonly Dictionary<NoteController, GameObject> _active = new();

            internal PrefabPool(GameObject original, IInstantiator instantiator)
            {
                _original = original;
                _instantiator = instantiator;
            }

            internal void Dispose()
            {
                _inactive.Do(Object.Destroy);
                _active.Values.Do(Object.Destroy);
            }

            internal GameObject Spawn(NoteController noteController)
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

                _active.Add(noteController, spawned);

                _instantiator.SongSynchronize(spawned, noteController._noteMovement._floorMovement.startTime);

                Transform transform = noteController.transform;
                transform.GetComponentsInChildren<Renderer>().Do(n => n.enabled = false);
                spawned.transform.SetParent(transform.GetChild(0), false);
                return spawned;
            }

            internal void Despawn(NoteController noteController)
            {
                if (!_active.TryGetValue(noteController, out GameObject spawned))
                {
                    return;
                }

                spawned.SetActive(false);
                _inactive.Push(spawned);
                _active.Remove(noteController);

                spawned.transform.SetParent(null, false);
                noteController.transform.GetComponentsInChildren<Renderer>().Do(n => n.enabled = true);
            }
        }

        private class MPBControllerHijacker
        {
            private readonly MaterialPropertyBlockController _materialPropertyBlockController;

            private Renderer[]? _cachedRenderers;
            private List<int>? _cachedNumberOfMaterialsInRenderers;

            internal MPBControllerHijacker(Component noteController)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (noteController is GameNoteController)
                {
                    _materialPropertyBlockController = noteController.transform.GetChild(0).GetComponent<MaterialPropertyBlockController>();
                }
                else
                {
                    _materialPropertyBlockController = noteController.GetComponent<MaterialPropertyBlockController>();
                }
            }

            internal void Activate(GameObject gameObject)
            {
                if (_materialPropertyBlockController._isInitialized)
                {
                    _cachedNumberOfMaterialsInRenderers = _materialPropertyBlockController._numberOfMaterialsInRenderers;
                    _materialPropertyBlockController._isInitialized = false;
                }

                _cachedRenderers = _materialPropertyBlockController._renderers;
                Renderer[] newRenderers = gameObject.GetComponentsInChildren<Renderer>(true);
                _materialPropertyBlockController._renderers = _cachedRenderers.Concat(newRenderers).ToArray();
                _materialPropertyBlockController.ApplyChanges();
            }

            // ReSharper disable once InvertIf
            internal void Deactivate()
            {
                if (_cachedNumberOfMaterialsInRenderers != null)
                {
                    _materialPropertyBlockController._numberOfMaterialsInRenderers = _cachedNumberOfMaterialsInRenderers;
                    _cachedNumberOfMaterialsInRenderers = null;
                }

                if (_cachedRenderers != null)
                {
                    _materialPropertyBlockController._renderers = _cachedRenderers;
                    _cachedRenderers = null;
                }
            }
        }
    }
}
