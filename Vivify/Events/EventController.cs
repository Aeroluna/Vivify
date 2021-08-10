namespace Vivify.Events
{
    using CustomJSONData;
    using UnityEngine;

    public class EventController : MonoBehaviour
    {
        public static EventController? Instance { get; private set; }

        public CustomEventCallbackController? CustomEventCallbackController { get; private set; }

        public BeatmapObjectSpawnController? BeatmapObjectSpawnController => HarmonyPatches.BeatmapObjectSpawnControllerStart.BeatmapObjectSpawnController;

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
