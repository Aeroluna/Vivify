using HarmonyLib;
using Heck;
using UnityEngine;
using Vivify.Controllers;
using Vivify.PostProcessing;

namespace Vivify.HarmonyPatches
{
    [HeckPatch]
    [HarmonyPatch(typeof(MainEffectController))]
    internal static class AddComponentsToCamera
    {
        private static void SafeAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            if (gameObject.GetComponent<T>() == null)
            {
                gameObject.AddComponent<T>();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(MainEffectController.LazySetupImageEffectController))]
        private static void AddComponents(MainEffectController __instance)
        {
            GameObject gameObject = __instance.gameObject;
            SafeAddComponent<PostProcessingController>(gameObject);
            SafeAddComponent<CameraPropertyController>(gameObject);
        }
    }
}
