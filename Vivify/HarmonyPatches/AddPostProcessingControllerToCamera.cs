using HarmonyLib;
using Heck;
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
            __instance.gameObject.AddComponent<PostProcessingController>();
        }
    }
}
