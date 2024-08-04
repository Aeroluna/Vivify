using System;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using Heck.Animation;
using Heck.Deserialize;
using Heck.ReLoad;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;
using Vivify.Controllers.Sync;
using Zenject;
using Object = UnityEngine.Object;

namespace Vivify.Managers
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    internal enum LoadMode
    {
        Single,
        Additive
    }

    internal class BeatmapObjectPrefabManager : IDisposable
    {
        private readonly AssetBundleManager _assetBundleManager;
        private readonly BeatmapObjectManager _beatmapObjectManager;
        private readonly Dictionary<string, Track> _beatmapTracks;
        private readonly DeserializedData _deserializedData;
        private readonly IInstantiator _instantiator;
        private readonly SiraLog _log;

        private readonly Dictionary<string, PrefabPool> _prefabPools = new();
        private readonly ReLoader? _reLoader;

        [UsedImplicitly]
        private BeatmapObjectPrefabManager(
            SiraLog log,
            AssetBundleManager assetBundleManager,
            BeatmapObjectManager beatmapObjectManager,
            IInstantiator instantiator,
            Dictionary<string, Track> beatmapTracks,
            [Inject(Id = VivifyController.ID)] DeserializedData deserializedData,
            [InjectOptional] ReLoader? reLoader)
        {
            _log = log;
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

        internal Dictionary<Component, HashSet<PrefabPool?>> ActivePools { get; } = new();

        internal Dictionary<Track, HashSet<PrefabPool?>> BombNotePrefabs { get; } = new();

        internal Dictionary<Track, HashSet<PrefabPool?>> BurstSliderDebrisPrefabs { get; } = new();

        internal Dictionary<Track, HashSet<PrefabPool?>> BurstSliderElementDebrisPrefabs { get; } = new();

        internal Dictionary<Track, HashSet<PrefabPool?>> BurstSliderElementPrefabs { get; } = new();

        internal Dictionary<Track, HashSet<PrefabPool?>> BurstSliderPrefabs { get; } = new();

        internal Dictionary<Track, HashSet<PrefabPool?>> ColorNoteDebrisPrefabs { get; } = new();

        internal Dictionary<Track, HashSet<PrefabPool?>> ColorNotePrefabs { get; } = new();

        internal Dictionary<Component, MpbControllerHijacker> Hijackers { get; } = new();

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

        internal void AssignPrefab(
            Dictionary<Track, HashSet<PrefabPool?>> prefabDictionary,
            Track track,
            string? assetName,
            LoadMode loadMode)
        {
            PrefabPool? prefabPool;
            if (assetName == null)
            {
                prefabPool = null;
            }
            else
            {
                if (!_prefabPools.TryGetValue(assetName, out prefabPool))
                {
                    if (!_assetBundleManager.TryGetAsset(assetName, out GameObject? prefab))
                    {
                        return;
                    }

                    _prefabPools[assetName] = prefabPool = new PrefabPool(prefab, _instantiator);
                }
            }

            if (!prefabDictionary.TryGetValue(track, out HashSet<PrefabPool?> prefabPools))
            {
                prefabDictionary[track] = prefabPools = new HashSet<PrefabPool?>
                {
                    null
                };
            }

            if (loadMode == LoadMode.Single)
            {
                prefabPools.Clear();
            }

            if (!prefabPools.Add(prefabPool))
            {
                _log.Error($"Could not assign [{assetName}], is already on track");
            }
        }

        internal void Despawn(Component component)
        {
            if (!ActivePools.TryGetValue(component, out HashSet<PrefabPool?> pools))
            {
                return;
            }

            if (Hijackers.TryGetValue(component, out MpbControllerHijacker hijacker))
            {
                hijacker.Deactivate();
            }

            foreach (PrefabPool? n in pools)
            {
                n?.Despawn(component);
            }

            ActivePools.Remove(component);
        }

        internal void Spawn(
            IEnumerable<Track> tracks,
            Dictionary<Track, HashSet<PrefabPool?>> prefabDictionary,
            Component component,
            float startTime)
        {
            MpbControllerHijacker? hijacker = null;
            HashSet<PrefabPool?>? activePool = null;
            List<GameObject>? spawned = null;
            bool hideOriginal = false;
            foreach (Track track in tracks)
            {
                if (!prefabDictionary.TryGetValue(track, out HashSet<PrefabPool?> prefabPools) ||
                    prefabPools.Count == 0)
                {
                    continue;
                }

                if (hijacker == null &&
                    !Hijackers.TryGetValue(component, out hijacker))
                {
                    Hijackers[component] = hijacker = new MpbControllerHijacker(component);
                }

                if (activePool == null &&
                    !ActivePools.TryGetValue(component, out activePool))
                {
                    ActivePools[component] = activePool = new HashSet<PrefabPool?>();
                }

                spawned ??= new List<GameObject>(prefabPools.Count);
                bool hasNull = false;
                foreach (PrefabPool? prefabPool in prefabPools)
                {
                    if (prefabPool == null)
                    {
                        hasNull = true;
                    }
                    else
                    {
                        spawned.Add(prefabPool.Spawn(component, startTime));
                        activePool.Add(prefabPool);
                    }
                }

                if (!hasNull)
                {
                    hideOriginal = true;
                }
            }

            if (spawned != null)
            {
                hijacker?.Activate(spawned, hideOriginal);
            }
        }

        private void HandleNoteWasDespawned(NoteController noteController)
        {
            Despawn(noteController);
        }

        private void HandleNoteWasSpawned(NoteController noteController)
        {
            NoteData noteData = noteController.noteData;
            IEnumerable<Track>? track;
            if (noteData.gameplayType == NoteData.GameplayType.BurstSliderElement)
            {
                // unfortunately burst slider elements are created on the fly so we cant deserialize them before the map begins
                if (noteData is not CustomNoteData customNoteData)
                {
                    return;
                }

                track = customNoteData.customData.GetNullableTrackArray(_beatmapTracks, false);
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

            Dictionary<Track, HashSet<PrefabPool?>>? prefabPoolDictionary =
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

        private void OnRewind()
        {
            foreach (Component activeComponent in ActivePools.Keys)
            {
                Despawn(activeComponent);
            }

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
            private readonly Dictionary<Component, GameObject> _active = new();
            private readonly Stack<GameObject> _inactive = new();
            private readonly IInstantiator _instantiator;
            private readonly GameObject _original;

            internal PrefabPool(GameObject original, IInstantiator instantiator)
            {
                _original = original;
                _instantiator = instantiator;
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
                spawned.transform.SetParent(transform.GetChild(0), false);
                return spawned;
            }
        }

        internal class MpbControllerHijacker
        {
            private readonly Renderer[] _originalRenderers;
            private readonly MaterialPropertyBlockController _materialPropertyBlockController;
            private List<int>? _cachedNumberOfMaterialsInRenderers;
            private Renderer[]? _cachedRenderers;

            internal MpbControllerHijacker(Component component)
            {
                _originalRenderers = component.GetComponentsInChildren<Renderer>();

                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (component is GameNoteController or BurstSliderGameNoteController)
                {
                    _materialPropertyBlockController =
                        component.transform.GetChild(0).GetComponent<MaterialPropertyBlockController>();
                }
                else
                {
                    _materialPropertyBlockController = component.GetComponent<MaterialPropertyBlockController>();
                }
            }

            internal void Activate(IEnumerable<GameObject> gameObjects, bool hideOriginal)
            {
                if (_materialPropertyBlockController._isInitialized)
                {
                    _cachedNumberOfMaterialsInRenderers =
                        _materialPropertyBlockController._numberOfMaterialsInRenderers;
                    _materialPropertyBlockController._isInitialized = false;
                }

                _cachedRenderers = _materialPropertyBlockController._renderers;
                IEnumerable<Renderer> newRenderers =
                    gameObjects.SelectMany(n => n.GetComponentsInChildren<Renderer>(true));

                if (hideOriginal)
                {
                    foreach (Renderer renderer in _originalRenderers)
                    {
                        renderer.enabled = false;
                    }

                    _materialPropertyBlockController._renderers = newRenderers.ToArray();
                }
                else
                {
                    _materialPropertyBlockController._renderers = _cachedRenderers.Concat(newRenderers).ToArray();
                }

                _materialPropertyBlockController.ApplyChanges();
            }

            // ReSharper disable once InvertIf
            internal void Deactivate()
            {
                if (_cachedNumberOfMaterialsInRenderers != null)
                {
                    _materialPropertyBlockController._numberOfMaterialsInRenderers =
                        _cachedNumberOfMaterialsInRenderers;
                    _cachedNumberOfMaterialsInRenderers = null;
                }

                if (_cachedRenderers != null)
                {
                    _materialPropertyBlockController._renderers = _cachedRenderers;
                    _cachedRenderers = null;
                }

                foreach (Renderer renderer in _originalRenderers)
                {
                    renderer.enabled = true;
                }
            }
        }
    }
}
