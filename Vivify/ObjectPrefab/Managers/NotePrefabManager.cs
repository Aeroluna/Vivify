using System;
using System.Linq;
using Heck.Animation;
using Heck.Deserialize;
using Heck.ReLoad;
using JetBrains.Annotations;
using Vivify.ObjectPrefab.Collections;
using Zenject;

namespace Vivify.ObjectPrefab.Managers;

internal class NotePrefabManager : IDisposable
{
    private readonly BasicBeatmapObjectManager? _basicBeatmapObjectManager;
    private readonly BeatmapObjectManager _beatmapObjectManager;
    private readonly DeserializedData _deserializedData;
    private readonly BeatmapObjectPrefabManager _prefabManager;

    private readonly ReLoader? _reLoader;

    [UsedImplicitly]
    private NotePrefabManager(
        BeatmapObjectPrefabManager prefabManager,
        BeatmapObjectManager beatmapObjectManager,
        [Inject(Id = VivifyController.ID)] DeserializedData deserializedData,
        [InjectOptional] ReLoader? reLoader)
    {
        _prefabManager = prefabManager;
        _beatmapObjectManager = beatmapObjectManager;
        _basicBeatmapObjectManager = beatmapObjectManager as BasicBeatmapObjectManager;
        _deserializedData = deserializedData;
        _reLoader = reLoader;
        if (reLoader != null)
        {
            reLoader.Rewinded += OnRewind;
        }

        if (_basicBeatmapObjectManager != null)
        {
            AnyDirectionNotePrefabs.Changed += OnAnyDirectionNotePrefabsChanges;
            BombNotePrefabs.Changed += OnBombNotePrefabsChanged;
            BurstSliderElementPrefabs.Changed += OnBurstSliderElementPrefabsChanged;
            BurstSliderPrefabs.Changed += OnBurstSliderPrefabsChanged;
            ColorNotePrefabs.Changed += OnColorNotePrefabsChanges;
        }

        beatmapObjectManager.noteWasSpawnedEvent += HandleNoteWasSpawned;
        beatmapObjectManager.noteWasDespawnedEvent += HandleNoteWasDespawned;
    }

    internal PrefabDictionary AnyDirectionNotePrefabs { get; } = new();

    internal PrefabDictionary BombNotePrefabs { get; } = new();

    internal PrefabDictionary BurstSliderElementPrefabs { get; } = new();

    internal PrefabDictionary BurstSliderPrefabs { get; } = new();

    internal PrefabDictionary ColorNotePrefabs { get; } = new();

    public void Dispose()
    {
        if (_reLoader != null)
        {
            _reLoader.Rewinded -= OnRewind;
        }

        AnyDirectionNotePrefabs.Changed -= OnAnyDirectionNotePrefabsChanges;
        BombNotePrefabs.Changed -= OnBombNotePrefabsChanged;
        BurstSliderElementPrefabs.Changed -= OnBurstSliderElementPrefabsChanged;
        BurstSliderPrefabs.Changed -= OnBurstSliderPrefabsChanged;
        ColorNotePrefabs.Changed -= OnColorNotePrefabsChanges;

        _beatmapObjectManager.noteWasSpawnedEvent -= HandleNoteWasSpawned;
        _beatmapObjectManager.noteWasDespawnedEvent -= HandleNoteWasDespawned;
    }

    private void HandleNoteWasDespawned(NoteController noteController)
    {
        _prefabManager.Despawn(noteController);
    }

    private void HandleNoteWasSpawned(NoteController noteController)
    {
        NoteData noteData = noteController.noteData;

        if (!_deserializedData.Resolve(noteData, out VivifyObjectData? data) ||
            data.Track == null)
        {
            return;
        }

        PrefabDictionary? prefabDictionary =
            noteData.gameplayType switch
            {
                NoteData.GameplayType.Normal => noteData.cutDirection == NoteCutDirection.Any
                    ? AnyDirectionNotePrefabs
                    : ColorNotePrefabs,
                NoteData.GameplayType.Bomb => BombNotePrefabs,
                NoteData.GameplayType.BurstSliderHead => BurstSliderPrefabs,
                NoteData.GameplayType.BurstSliderElement => BurstSliderElementPrefabs,
                _ => null
            };

        if (prefabDictionary == null)
        {
            return;
        }

        _prefabManager.Spawn(
            data.Track,
            prefabDictionary,
            noteController,
            noteController._noteMovement._floorMovement.startTime);
    }

    private void OnAnyDirectionNotePrefabsChanges(Track track)
    {
        foreach (GameNoteController? noteController in _basicBeatmapObjectManager!._basicGameNotePoolContainer
                     .activeItems)
        {
            if (noteController.noteData.cutDirection == NoteCutDirection.Any)
            {
                RefreshObjects(track, noteController, AnyDirectionNotePrefabs);
            }
        }
    }

    private void OnBombNotePrefabsChanged(Track track)
    {
        foreach (BombNoteController noteController in
                 _basicBeatmapObjectManager!._bombNotePoolContainer.activeItems)
        {
            RefreshObjects(track, noteController, BombNotePrefabs);
        }
    }

    private void OnBurstSliderElementPrefabsChanged(Track track)
    {
        foreach (BurstSliderGameNoteController? noteController in _basicBeatmapObjectManager!
                     ._burstSliderGameNotePoolContainer.activeItems)
        {
            RefreshObjects(track, noteController, BurstSliderElementPrefabs);
        }
    }

    private void OnBurstSliderPrefabsChanged(Track track)
    {
        foreach (GameNoteController? noteController in _basicBeatmapObjectManager!
                     ._burstSliderHeadGameNotePoolContainer.activeItems)
        {
            RefreshObjects(track, noteController, BurstSliderPrefabs);
        }
    }

    private void OnColorNotePrefabsChanges(Track track)
    {
        foreach (GameNoteController? noteController in _basicBeatmapObjectManager!._basicGameNotePoolContainer
                     .activeItems)
        {
            if (noteController.noteData.cutDirection != NoteCutDirection.Any)
            {
                RefreshObjects(track, noteController, ColorNotePrefabs);
            }
        }
    }

    private void OnRewind()
    {
        ColorNotePrefabs.Clear();
        BombNotePrefabs.Clear();
        BurstSliderPrefabs.Clear();
        BurstSliderElementPrefabs.Clear();
    }

    private void RefreshObjects(Track track, NoteController noteController, PrefabDictionary prefabDictionary)
    {
        if (!_deserializedData.Resolve(noteController.noteData, out VivifyObjectData? data) ||
            data.Track == null ||
            !data.Track.Contains(track))
        {
            return;
        }

        _prefabManager.Despawn(noteController);
        _prefabManager.Spawn(
            data.Track,
            prefabDictionary,
            noteController,
            noteController._noteMovement._floorMovement.startTime);
    }
}
