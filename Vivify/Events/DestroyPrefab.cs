using CustomJSONData.CustomBeatmap;
using HarmonyLib;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void DestroyPrefab(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out DestroyPrefabData? data))
            {
                return;
            }

            data.Id.Do(_prefabManager.Destroy);
        }
    }
}
