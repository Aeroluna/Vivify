using CustomJSONData.CustomBeatmap;

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
            _prefabManager.Destroy(id);
        }
    }
}
