namespace Vivify.Events
{
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;
    using UnityEngine;

    public class EventController : MonoBehaviour
    {
        public static EventController Instance { get; private set; }

        public CustomEventCallbackController CustomEventCallbackController { get; private set; }

        public BeatmapObjectSpawnController BeatmapObjectSpawnController => HarmonyPatches.BeatmapObjectSpawnControllerStart.BeatmapObjectSpawnController;

        internal static void CustomEventCallbackInit(CustomEventCallbackController customEventCallbackController)
        {
            if (customEventCallbackController._beatmapData is CustomBeatmapData customBeatmapData && Trees.at(customBeatmapData.customData, "isMultiplayer") != null)
            {
                return;
            }

            if (Instance != null)
            {
                Destroy(Instance);
            }

            Instance = customEventCallbackController.gameObject.AddComponent<EventController>();

            Instance.CustomEventCallbackController = customEventCallbackController;
            Instance.CustomEventCallbackController.AddCustomEventCallback(InstantiatePrefabEvent.Callback);
            Instance.CustomEventCallbackController.AddCustomEventCallback(ApplyPostProcessingEvent.Callback);
            Instance.CustomEventCallbackController.AddCustomEventCallback(SetMaterialPropertyEvent.Callback);
        }
    }
}
