namespace Vivify.Events
{
    using System;
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;
    using UnityEngine;

    internal static class DestroyPrefabEvent
    {
        internal static void Callback(CustomEventData customEventData)
        {
            if (customEventData.type == "DestroyPrefab")
            {
                string id = customEventData.data.Get<string>("_id") ?? throw new InvalidOperationException("Id not found.");

                if (AssetBundleController.InstantiatedPrefabs.ContainsKey(id))
                {
                    Plugin.Logger.Log($"Destroying [{id}].");

                    GameObject gameObject = AssetBundleController.InstantiatedPrefabs[id];
                    UnityEngine.Object.Destroy(gameObject);
                    AssetBundleController.InstantiatedPrefabs.Remove(id);
                }
                else
                {
                    Plugin.Logger.Log($"No prefab with id [{id}] detected.", IPA.Logging.Logger.Level.Error);
                }
            }
        }
    }
}
