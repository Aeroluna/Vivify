using System.Collections.Generic;
using Heck.Animation;
using Heck.Deserialize;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using Vivify.Managers;
using Zenject;

namespace Vivify.HarmonyPatches
{
    internal class SpawnDebrisPrefab : IAffinity
    {
        private readonly AudioTimeSyncController _audioTimeSyncController;
        private readonly BeatmapObjectPrefabManager _beatmapObjectPrefabManager;
        private readonly DeserializedData _deserializedData;

        private NoteData? _noteData;

        [UsedImplicitly]
        private SpawnDebrisPrefab(
            BeatmapObjectPrefabManager beatmapObjectPrefabManager,
            AudioTimeSyncController audioTimeSyncController,
            [Inject(Id = VivifyController.ID)] DeserializedData deserializedData)
        {
            _beatmapObjectPrefabManager = beatmapObjectPrefabManager;
            _audioTimeSyncController = audioTimeSyncController;
            _deserializedData = deserializedData;
        }

        [AffinityPostfix]
        [AffinityPatch(typeof(NoteDebrisSpawner), nameof(NoteDebrisSpawner.HandleNoteDebrisDidFinish))]
        private void DespawnPrefab(NoteDebris noteDebris)
        {
            _beatmapObjectPrefabManager.Despawn(noteDebris);
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

            Dictionary<Track, HashSet<BeatmapObjectPrefabManager.PrefabPool?>>? prefabPoolDictionary =
                _noteData.gameplayType switch
                {
                    NoteData.GameplayType.Normal => _beatmapObjectPrefabManager.ColorNoteDebrisPrefabs,
                    NoteData.GameplayType.BurstSliderHead => _beatmapObjectPrefabManager.BurstSliderDebrisPrefabs,
                    NoteData.GameplayType.BurstSliderElement => _beatmapObjectPrefabManager
                        .BurstSliderElementDebrisPrefabs,
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
}
