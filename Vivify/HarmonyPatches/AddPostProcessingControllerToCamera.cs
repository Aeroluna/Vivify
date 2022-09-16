using HarmonyLib;
using Heck;
using UnityEngine;
using Vivify.PostProcessing;

namespace Vivify.HarmonyPatches
{
    [HeckPatch]
    [HarmonyPatch(typeof(MainCamera))]
    internal static class AddPostProcessingControllerToCamera
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MainCamera.Awake))]
        private static void Postfix(MainCamera __instance)
        {
            PostProcessingController[] existing = __instance.gameObject.GetComponents<PostProcessingController>();
            foreach (PostProcessingController postProcessingController in existing)
            {
                Object.Destroy(postProcessingController);
            }

            __instance.gameObject.AddComponent<PostProcessingController>();
        }
    }
}
