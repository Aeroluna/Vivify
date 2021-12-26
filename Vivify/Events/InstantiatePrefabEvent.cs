using System;
using System.Collections.Generic;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vivify.Events
{
    internal static class InstantiatePrefabEvent
    {
        internal static void Callback(CustomEventData customEventData)
        {
            if (customEventData.type != "InstantiatePrefab")
            {
                return;
            }

            string assetName = customEventData.data.Get<string>("_asset") ?? throw new InvalidOperationException("Asset name not found.");
            GameObject? prefab = AssetBundleController.TryGetAsset<GameObject>(assetName);
            if (prefab == null)
            {
                return;
            }

            GameObject gameObject = Object.Instantiate(prefab);

            Transform transform = gameObject.transform;

            Dictionary<string, object?> data = customEventData.data;
            Vector3? position = data.GetVector3("_position");
            if (position.HasValue)
            {
                transform.position = position.Value;
            }

            Vector3? rotation = data.GetVector3("_rotation");
            if (rotation.HasValue)
            {
                transform.rotation = Quaternion.Euler(rotation.Value);
            }

            Vector3? scale = data.GetVector3("_position");
            if (scale.HasValue)
            {
                transform.localScale = scale.Value;
            }

            string? id = customEventData.data.Get<string>("_id");
            if (id != null)
            {
                Log.Logger.Log($"Created [{assetName}] with id [{id}].");
                AssetBundleController.InstantiatedPrefabs.Add(id, gameObject);
            }
            else
            {
                Log.Logger.Log($"Created [{assetName}] without an id.");
            }
        }
    }
}
