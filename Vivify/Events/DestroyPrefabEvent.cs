using System;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using UnityEngine;
using Logger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

namespace Vivify.Events
{
    internal static class DestroyPrefabEvent
    {
        internal static void Callback(CustomEventData customEventData)
        {
            if (customEventData.type != "DestroyPrefab")
            {
                return;
            }

            string id = customEventData.data.Get<string>("_id") ?? throw new InvalidOperationException("Id not found.");

            if (AssetBundleController.InstantiatedPrefabs.ContainsKey(id))
            {
                Log.Logger.Log($"Destroying [{id}].");

                GameObject gameObject = AssetBundleController.InstantiatedPrefabs[id];
                Object.Destroy(gameObject);
                AssetBundleController.InstantiatedPrefabs.Remove(id);
            }
            else
            {
                Log.Logger.Log($"No prefab with id [{id}] detected.", Logger.Level.Error);
            }
        }
    }
}
