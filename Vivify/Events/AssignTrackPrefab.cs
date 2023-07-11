using CustomJSONData.CustomBeatmap;
using UnityEngine;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void AssignTrackPrefab(CustomEventData customEventData)
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

            Plugin.Log.LogDebug($"Assigned note prefab: [{assetName}]");
            _beatmapObjectPrefabManager.Add(data.Track, prefab);
        }
    }
}
