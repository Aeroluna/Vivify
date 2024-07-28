using System;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Animation;
using Heck.Deserialize;
using Heck.Event;
using IPA.Utilities;
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

            foreach ((string? key, string? value) in data.Assets)
            {
                Dictionary<Track, BeatmapObjectPrefabManager.PrefabPool> prefabPoolDictionary = key switch
                {
                    NOTE_PREFAB => _beatmapObjectPrefabManager.ColorNotePrefabs,
                    BOMB_PREFAB => _beatmapObjectPrefabManager.BombNotePrefabs,
                    CHAIN_PREFAB => _beatmapObjectPrefabManager.BurstSliderPrefabs,
                    CHAIN_ELEMENT_PREFAB => _beatmapObjectPrefabManager.BurstSliderElementPrefabs,
                    NOTE_DEBRIS_PREFAB => _beatmapObjectPrefabManager.ColorNoteDebrisPrefabs,
                    CHAIN_DEBRIS_PREFAB => _beatmapObjectPrefabManager.BurstSliderDebrisPrefabs,
                    CHAIN_ELEMENT_DEBRIS_PREFAB => _beatmapObjectPrefabManager.BurstSliderElementDebrisPrefabs,
                    _ => throw new ArgumentOutOfRangeException()
                };

                _log.Debug($"Assigned prefab: [{value ?? "null"}]");
                _beatmapObjectPrefabManager.AssignPrefab(prefabPoolDictionary, data.Track, value);
            }
        }
    }
}
