using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Deserialize;
using Heck.Event;
using SiraUtil.Logging;
using UnityEngine;
using Vivify.Managers;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events
{
    [CustomEvent(ASSIGN_TRACK_PREFAB)]
    internal class AssignTrackPrefab : ICustomEvent
    {
        private readonly SiraLog _log;
        private readonly AssetBundleManager _assetBundleManager;
        private readonly DeserializedData _deserializedData;
        private readonly BeatmapObjectPrefabManager _beatmapObjectPrefabManager;

        private AssignTrackPrefab(
            SiraLog log,
            AssetBundleManager assetBundleManager,
            [Inject(Id = ID)] DeserializedData deserializedData,
            BeatmapObjectPrefabManager beatmapObjectPrefabManager)
        {
            _log = log;
            _assetBundleManager = assetBundleManager;
            _deserializedData = deserializedData;
            _beatmapObjectPrefabManager = beatmapObjectPrefabManager;
        }

        public void Callback(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out AssignTrackPrefabData? data))
            {
                return;
            }

            string? assetName = data.NoteAsset;
            if (assetName == null || !_assetBundleManager.TryGetAsset(assetName, out GameObject? prefab))
            {
                return;
            }

            _log.Debug($"Assigned note prefab: [{assetName}]");
            _beatmapObjectPrefabManager.Add(data.Track, prefab);
        }
    }
}
