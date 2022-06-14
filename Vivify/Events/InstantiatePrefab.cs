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

            Vector3? position = heckData.Position;
            if (position.HasValue)
            {
                transform.position = position.Value;
            }

            Vector3? rotation = heckData.Rotation;
            if (rotation.HasValue)
            {
                transform.rotation = Quaternion.Euler(rotation.Value);
            }

            Vector3? scale = heckData.Scale;
            if (scale.HasValue)
            {
                transform.localScale = scale.Value;
            }

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
