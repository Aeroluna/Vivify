using System;
using Heck.Deserialize;
using Heck.ReLoad;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using Vivify.ObjectPrefab.Collections;
using Zenject;

namespace Vivify.ObjectPrefab.Managers;

internal class DebrisPrefabManager : IAffinity, IDisposable
{
    private readonly AudioTimeSyncController _audioTimeSyncController;
    private readonly BeatmapObjectPrefabManager _beatmapObjectPrefabManager;
    private readonly DeserializedData _deserializedData;
    private readonly ReLoader? _reLoader;

    private NoteData? _noteData;

    [UsedImplicitly]
    private DebrisPrefabManager(
        BeatmapObjectPrefabManager beatmapObjectPrefabManager,
        AudioTimeSyncController audioTimeSyncController,
        [Inject(Id = VivifyController.ID)] DeserializedData deserializedData,
        [InjectOptional] ReLoader? reLoader)
    {
        _beatmapObjectPrefabManager = beatmapObjectPrefabManager;
        _audioTimeSyncController = audioTimeSyncController;
        _deserializedData = deserializedData;
        _reLoader = reLoader;
        if (reLoader != null)
        {
            reLoader.Rewinded += OnRewind;
        }
    }

    internal PrefabDictionary BurstSliderDebrisPrefabs { get; } = new();

    internal PrefabDictionary BurstSliderElementDebrisPrefabs { get; } = new();

    internal PrefabDictionary ColorNoteDebrisPrefabs { get; } = new();

    public void Dispose()
    {
        if (_reLoader != null)
        {
            _reLoader.Rewinded -= OnRewind;
        }
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(NoteDebrisSpawner), nameof(NoteDebrisSpawner.HandleNoteDebrisDidFinish))]
    private void DespawnPrefab(NoteDebris noteDebris)
    {
        _beatmapObjectPrefabManager.Despawn(noteDebris);
    }

    private void OnRewind()
    {
        ColorNoteDebrisPrefabs.Clear();
        BurstSliderDebrisPrefabs.Clear();
        BurstSliderElementDebrisPrefabs.Clear();
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(NoteCutCoreEffectsSpawner), nameof(NoteCutCoreEffectsSpawner.SpawnNoteCutEffect))]
    private void SetNoteData(NoteController noteController)
    {
        _noteData = noteController.noteData;
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(NoteDebris), nameof(NoteDebris.Init))]
    private void SpawnPrefab(NoteDebris __instance)
    {
        if (_noteData == null ||
            !_deserializedData.Resolve(_noteData, out VivifyObjectData? data) ||
            data.Track == null)
        {
            return;
        }

        PrefabDictionary? prefabPoolDictionary =
            _noteData.gameplayType switch
            {
                NoteData.GameplayType.Normal => ColorNoteDebrisPrefabs,
                NoteData.GameplayType.BurstSliderHead => BurstSliderDebrisPrefabs,
                NoteData.GameplayType.BurstSliderElement => BurstSliderElementDebrisPrefabs,
                _ => null
            };

        if (prefabPoolDictionary == null)
        {
            return;
        }

        _beatmapObjectPrefabManager.Spawn(
            data.Track,
            prefabPoolDictionary,
            __instance,
            _audioTimeSyncController.songTime);
    }
}
