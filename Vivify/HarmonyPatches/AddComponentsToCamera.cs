using HarmonyLib;
using Heck;
using UnityEngine;
using Vivify.PostProcessing;

namespace Vivify.HarmonyPatches
{
    [HeckPatch]
    [HarmonyPatch(typeof(MainEffectController))]
    internal static class AddComponentsToCamera
    {
        private static void YeetComponent<T>(GameObject gameObject)
            where T : Component
        {
            T[] existing = gameObject.GetComponents<T>();
            foreach (T postProcessingController in existing)
            {
                Object.Destroy(postProcessingController);
            }
        }

        private static void SafeAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            YeetComponent<T>(gameObject);
            gameObject.AddComponent<T>();
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnEnable")]
        private static void AddComponents(MainEffectController __instance)
        {
            GameObject gameObject = __instance.gameObject;
            SafeAddComponent<PostProcessingController>(gameObject);
            SafeAddComponent<CameraPropertyController>(gameObject);
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnDisable")]
        private static void Yeet(MainEffectController __instance)
        {
            GameObject gameObject = __instance.gameObject;
            YeetComponent<PostProcessingController>(gameObject);
            YeetComponent<CameraPropertyController>(gameObject);
        }
    }
}
