using CustomJSONData.CustomBeatmap;
using UnityEngine;
using Logger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void DestroyPrefab(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out DestroyPrefabData? heckData))
            {
                return;
            }

            string id = heckData.Id;

            if (!_assetBundleController.InstantiatedPrefabs.ContainsKey(id))
            {
                Log.Logger.Log($"No prefab with id [{id}] detected.", Logger.Level.Error);
                return;
            }

            Log.Logger.Log($"Destroying [{id}].");

            GameObject gameObject = _assetBundleController.InstantiatedPrefabs[id];
            Object.Destroy(gameObject);
            _assetBundleController.InstantiatedPrefabs.Remove(id);
        }
    }
}
