using CustomJSONData;
using UnityEngine;
using Vivify.HarmonyPatches;

namespace Vivify.Events
{
    public class EventController : MonoBehaviour
    {
        public static EventController Instance { get; private set; } = null!;

        public static BeatmapObjectSpawnController BeatmapObjectSpawnController => BeatmapObjectSpawnControllerStart.BeatmapObjectSpawnController;

        public CustomEventCallbackController CustomEventCallbackController { get; private set; } = null!;

        internal static void CustomEventCallbackInit(CustomEventCallbackController customEventCallbackController)
        {
            if (customEventCallbackController.BeatmapData?.customData.Get<bool>("isMultiplayer") ?? false)
            {
                return;
            }

            if (Instance != null)
            {
                Destroy(Instance);
            }

            Instance = customEventCallbackController.gameObject.AddComponent<EventController>();

            Instance.CustomEventCallbackController = customEventCallbackController;
            customEventCallbackController.AddCustomEventCallback(InstantiatePrefabEvent.Callback);
            customEventCallbackController.AddCustomEventCallback(ApplyPostProcessingEvent.Callback);
            customEventCallbackController.AddCustomEventCallback(SetMaterialPropertyEvent.Callback);
            customEventCallbackController.AddCustomEventCallback(DestroyPrefabEvent.Callback);
            customEventCallbackController.AddCustomEventCallback(DeclareMaskEvent.Callback);
            customEventCallbackController.AddCustomEventCallback(DeclareCullingMaskEvent.Callback);
        }
    }
}
