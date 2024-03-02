using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using Heck;
using Heck.Event;
using Vivify.Managers;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events
{
    [CustomEvent(DESTROY_PREFAB)]
    internal class DestroyPrefab : ICustomEvent
    {
        private readonly PrefabManager _prefabManager;
        private readonly DeserializedData _deserializedData;

        private DestroyPrefab(
            PrefabManager prefabManager,
            [Inject(Id = ID)] DeserializedData deserializedData)
        {
            _prefabManager = prefabManager;
            _deserializedData = deserializedData;
        }

        public void Callback(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out DestroyPrefabData? data))
            {
                return;
            }

            data.Id.Do(_prefabManager.Destroy);
        }
    }
}
