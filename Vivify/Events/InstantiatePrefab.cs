using CustomJSONData.CustomBeatmap;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void InstantiatePrefab(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out InstantiatePrefabData? heckData))
            {
                return;
            }

            string assetName = heckData.Asset;
            GameObject? prefab = AssetBundleController.TryGetAsset<GameObject>(assetName);
            if (prefab == null)
            {
                return;
            }

            GameObject gameObject = Object.Instantiate(prefab);

            Transform transform = gameObject.transform;
            heckData.TransformData.Apply(transform, false);

            string? id = heckData.Id;
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
