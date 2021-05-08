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
                string assetName = Trees.at(customEventData.data, "_asset");
                if (AssetBundleController.Assets.TryGetValue(assetName, out UnityEngine.Object prefab))
                {
                    Vector3 position = Vector3.zero;
                    IEnumerable<float> positionraw = ((List<object>)Trees.at(customEventData.data, "_position"))?.Select(n => Convert.ToSingle(n));
                    if (positionraw != null)
                    {
                        position = new Vector3(positionraw.ElementAt(0), positionraw.ElementAt(1), positionraw.ElementAt(2));
                    }

                    Quaternion rotation = Quaternion.identity;
                    IEnumerable<float> rotraw = ((List<object>)Trees.at(customEventData.data, "_rotation"))?.Select(n => Convert.ToSingle(n));
                    if (rotraw != null)
                    {
                        rotation = Quaternion.Euler(rotraw.ElementAt(0), rotraw.ElementAt(1), rotraw.ElementAt(2));
                    }

                    UnityEngine.Object.Instantiate(prefab, position, rotation);
                }
                else
                {
                    Plugin.Logger.Log($"Could not find asset {assetName}", IPA.Logging.Logger.Level.Error);
                }
            }
        }
    }
}
