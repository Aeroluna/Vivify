using System;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
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
        private readonly AssetBundleManager _assetBundleManager;
        private readonly BeatmapObjectManager _beatmapObjectManager;
        private readonly IInstantiator _instantiator;
        private readonly Dictionary<string, Track> _beatmapTracks;
        private readonly DeserializedData _deserializedData;
        private readonly ReLoader? _reLoader;

        private readonly Dictionary<string, PrefabPool> _prefabPools = new();

        [UsedImplicitly]
        private BeatmapObjectPrefabManager(
            AssetBundleManager assetBundleManager,
            BeatmapObjectManager beatmapObjectManager,
            IInstantiator instantiator,
            Dictionary<string, Track> beatmapTracks,
            [Inject(Id = VivifyController.ID)] DeserializedData deserializedData,
            [InjectOptional] ReLoader? reLoader)
        {
            _assetBundleManager = assetBundleManager;
            _beatmapObjectManager = beatmapObjectManager;
            _instantiator = instantiator;
            _beatmapTracks = beatmapTracks;
            _deserializedData = deserializedData;
            _reLoader = reLoader;
            if (reLoader != null)
            {
                reLoader.Rewinded += OnRewind;
            }

            beatmapObjectManager.noteWasSpawnedEvent += HandleNoteWasSpawned;
            beatmapObjectManager.noteWasDespawnedEvent += HandleNoteWasDespawned;
        }

        internal Dictionary<Track, PrefabPool> ColorNotePrefabs { get; } = new();

        internal Dictionary<Track, PrefabPool> BombNotePrefabs { get; } = new();

        internal Dictionary<Track, PrefabPool> BurstSliderPrefabs { get; } = new();

        internal Dictionary<Track, PrefabPool> BurstSliderElementPrefabs { get; } = new();

        internal Dictionary<Track, PrefabPool> ColorNoteDebrisPrefabs { get; } = new();

        internal Dictionary<Track, PrefabPool> BurstSliderDebrisPrefabs { get; } = new();

        internal Dictionary<Track, PrefabPool> BurstSliderElementDebrisPrefabs { get; } = new();

        internal Dictionary<Component, PrefabPool> ActivePools { get; } = new();

        internal Dictionary<Component, MPBControllerHijacker> Hijackers { get; } = new();

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

        internal void AssignPrefab(Dictionary<Track, PrefabPool> prefabDictionary, Track track, string? assetName)
        {
            if (assetName == null)
            {
                prefabDictionary.Remove(track);
                return;
            }

            if (!_prefabPools.TryGetValue(assetName, out PrefabPool prefabPool))
            {
                if (!_assetBundleManager.TryGetAsset(assetName, out GameObject? prefab))
                {
                    return;
                }

                _prefabPools[assetName] = prefabPool = new PrefabPool(prefab, _instantiator);
            }

            prefabDictionary[track] = prefabPool;
        }

        internal void Spawn(List<Track> tracks, Dictionary<Track, PrefabPool> prefabPools, Component component, float startTime)
        {
            foreach (Track track in tracks)
            {
                if (!prefabPools.TryGetValue(track, out PrefabPool prefabPool))
                {
                    continue;
                }

                GameObject spawned = prefabPool.Spawn(component, startTime);
                if (!Hijackers.TryGetValue(component, out MPBControllerHijacker hijacker))
                {
                    hijacker = new MPBControllerHijacker(component);
                    Hijackers.Add(component, hijacker);
                }

                hijacker.Activate(spawned);
                ActivePools.Add(component, prefabPool);
                return;
            }
        }

        internal void Despawn(Component component)
        {
            if (!ActivePools.TryGetValue(component, out PrefabPool pool))
            {
                return;
            }

            if (Hijackers.TryGetValue(component, out MPBControllerHijacker hijacker))
            {
                hijacker.Deactivate();
            }

            pool.Despawn(component);
            ActivePools.Remove(component);
        }

        private void HandleNoteWasSpawned(NoteController noteController)
        {
            NoteData noteData = noteController.noteData;
            List<Track>? track;
            if (noteData.gameplayType == NoteData.GameplayType.BurstSliderElement)
            {
                // unfortunately burst slider elements are created on the fly so we cant deserialize them before the map begins
                if (noteData is not CustomNoteData customNoteData)
                {
                    return;
                }

                track = customNoteData.customData.GetNullableTrackArray(_beatmapTracks, false)?.ToList();
            }
            else
            {
                if (!_deserializedData.Resolve(noteData, out VivifyObjectData? data) || (track = data.Track) == null)
                {
                    return;
                }

                track = data.Track;
            }

            if (track == null)
            {
                return;
            }

            Dictionary<Track, PrefabPool>? prefabPoolDictionary =
                noteController.noteData.gameplayType switch
                {
                    NoteData.GameplayType.Normal => ColorNotePrefabs,
                    NoteData.GameplayType.Bomb => BombNotePrefabs,
                    NoteData.GameplayType.BurstSliderHead => BurstSliderPrefabs,
                    NoteData.GameplayType.BurstSliderElement => BurstSliderElementPrefabs,
                    _ => null
                };

            if (prefabPoolDictionary == null)
            {
                return;
            }

            Spawn(track, prefabPoolDictionary, noteController, noteController._noteMovement._floorMovement.startTime);
        }

        private void HandleNoteWasDespawned(NoteController noteController)
        {
            Despawn(noteController);
        }

        private void OnRewind()
        {
            ActivePools.Do(n => n.Value.Despawn(n.Key));
            ActivePools.Clear();
            ColorNotePrefabs.Clear();
            BombNotePrefabs.Clear();
            BurstSliderPrefabs.Clear();
            BurstSliderElementPrefabs.Clear();
            ColorNoteDebrisPrefabs.Clear();
            BurstSliderDebrisPrefabs.Clear();
            BurstSliderElementDebrisPrefabs.Clear();
        }

        internal class PrefabPool
        {
            private readonly GameObject _original;
            private readonly IInstantiator _instantiator;

            private readonly Stack<GameObject> _inactive = new();

            private readonly Dictionary<Component, GameObject> _active = new();

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

            internal GameObject Spawn(Component component, float startTime)
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

                _active.Add(component, spawned);

                _instantiator.SongSynchronize(spawned, startTime);

                Transform transform = component.transform;
                transform.GetComponentsInChildren<Renderer>().Do(n => n.enabled = false);
                spawned.transform.SetParent(transform.GetChild(0), false);
                return spawned;
            }

            internal void Despawn(Component component)
            {
                if (!_active.TryGetValue(component, out GameObject spawned))
                {
                    return;
                }

                spawned.SetActive(false);
                _inactive.Push(spawned);
                _active.Remove(component);

                spawned.transform.SetParent(null, false);
                component.transform.GetComponentsInChildren<Renderer>().Do(n => n.enabled = true);
            }
        }

        internal class MPBControllerHijacker
        {
            private readonly MaterialPropertyBlockController _materialPropertyBlockController;

            private Renderer[]? _cachedRenderers;
            private List<int>? _cachedNumberOfMaterialsInRenderers;

            internal MPBControllerHijacker(Component component)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (component is GameNoteController or BurstSliderGameNoteController)
                {
                    _materialPropertyBlockController = component.transform.GetChild(0).GetComponent<MaterialPropertyBlockController>();
                }
                else
                {
                    _materialPropertyBlockController = component.GetComponent<MaterialPropertyBlockController>();
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
