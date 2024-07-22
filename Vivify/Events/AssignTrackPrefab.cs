using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Animation;
using Heck.Deserialize;
using Heck.Event;
using SiraUtil.Logging;
using Vivify.Managers;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events
{
    [CustomEvent(ASSIGN_TRACK_PREFAB)]
    internal class AssignTrackPrefab : ICustomEvent
    {
        private readonly SiraLog _log;
        private readonly DeserializedData _deserializedData;
        private readonly BeatmapObjectPrefabManager _beatmapObjectPrefabManager;

        private AssignTrackPrefab(
            SiraLog log,
            [Inject(Id = ID)] DeserializedData deserializedData,
            BeatmapObjectPrefabManager beatmapObjectPrefabManager)
        {
            _log = log;
            _deserializedData = deserializedData;
            _beatmapObjectPrefabManager = beatmapObjectPrefabManager;
        }

        // ReSharper disable InvertIf
        public void Callback(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out AssignTrackPrefabData? data))
            {
                return;
            }

            AddAsset(_beatmapObjectPrefabManager.ColorNotePrefabs, data.ColorNoteAsset);
            AddAsset(_beatmapObjectPrefabManager.BombNotePrefabs, data.BombNoteAsset);
            AddAsset(_beatmapObjectPrefabManager.BurstSliderPrefabs, data.BurstSliderAsset);
            AddAsset(_beatmapObjectPrefabManager.BurstSliderElementPrefabs, data.BurstSliderElementAsset);
            AddAsset(_beatmapObjectPrefabManager.ColorNoteDebrisPrefabs, data.ColorNoteDebrisAsset);
            AddAsset(_beatmapObjectPrefabManager.BurstSliderDebrisPrefabs, data.BurstSliderDebrisAsset);
            AddAsset(_beatmapObjectPrefabManager.BurstSliderElementDebrisPrefabs, data.BurstSliderElementDebrisAsset);

            return;

            void AddAsset(Dictionary<Track, BeatmapObjectPrefabManager.PrefabPool> prefabPoolDictionary, string? field)
            {
                _log.Debug($"Assigned prefab: [{field ?? "null"}]");
                _beatmapObjectPrefabManager.AssignPrefab(prefabPoolDictionary, data.Track, field);
            }
        }
    }
}
