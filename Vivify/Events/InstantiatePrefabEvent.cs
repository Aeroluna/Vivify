namespace Vivify.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;
    using UnityEngine;

    internal static class InstantiatePrefabEvent
    {
        internal static void Callback(CustomEventData customEventData)
        {
            if (customEventData.type == "InstantiatePrefab")
            {
                string assetName = customEventData.data.Get<string>("_asset") ?? throw new InvalidOperationException("Asset name not found.");
                GameObject? prefab = AssetBundleController.TryGetAsset<GameObject>(assetName);
                if (prefab != null)
                {
                    GameObject gameObject = UnityEngine.Object.Instantiate(prefab);

                    Transform transform = gameObject.transform;

                    IEnumerable<float>? positionraw = customEventData.data.Get<List<object>>("_position")?.Select(n => Convert.ToSingle(n));
                    if (positionraw != null)
                    {
                        transform.position = new Vector3(positionraw.ElementAt(0), positionraw.ElementAt(1), positionraw.ElementAt(2));
                    }

                    IEnumerable<float>? rotraw = customEventData.data.Get<List<object>>("_rotation")?.Select(n => Convert.ToSingle(n));
                    if (rotraw != null)
                    {
                        transform.rotation = Quaternion.Euler(rotraw.ElementAt(0), rotraw.ElementAt(1), rotraw.ElementAt(2));
                    }

                    IEnumerable<float>? scaleraw = customEventData.data.Get<List<object>>("_scale")?.Select(n => Convert.ToSingle(n));
                    if (scaleraw != null)
                    {
                        transform.localScale = new Vector3(rotraw.ElementAt(0), rotraw.ElementAt(1), rotraw.ElementAt(2));
                    }

                    string? id = customEventData.data.Get<string>("_id");
                    if (id != null)
                    {
                        Plugin.Logger.Log($"Created [{assetName}] with id [{id}].");
                        AssetBundleController.InstantiatedPrefabs.Add(id, gameObject);
                    }
                    else
                    {
                        Plugin.Logger.Log($"Created [{assetName}] without id.");
                    }
                }
            }
        }
    }
}
