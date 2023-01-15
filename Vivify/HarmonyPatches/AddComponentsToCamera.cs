using HarmonyLib;
using Heck;
using UnityEngine;
using Vivify.PostProcessing;

namespace Vivify.HarmonyPatches
{
    [HeckPatch]
    [HarmonyPatch(typeof(MainCamera))]
    internal static class AddComponentsToCamera
    {
        private static void SafeAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T[] existing = gameObject.GetComponents<T>();
            foreach (T postProcessingController in existing)
            {
                Object.Destroy(postProcessingController);
            }

            gameObject.AddComponent<T>();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(MainCamera.Awake))]
        private static void Postfix(MainCamera __instance)
        {
            GameObject gameObject = __instance.gameObject;
            SafeAddComponent<PostProcessingController>(gameObject);
            SafeAddComponent<CameraPropertyController>(gameObject);
        }
    }
}
