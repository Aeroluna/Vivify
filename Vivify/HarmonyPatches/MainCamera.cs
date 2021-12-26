using HarmonyLib;
using JetBrains.Annotations;
using Vivify.PostProcessing;

namespace Vivify.HarmonyPatches
{
    [HarmonyPatch(typeof(MainCamera))]
    [HarmonyPatch("Awake")]
    internal static class MainCameraAwake
    {
        [UsedImplicitly]
        private static void Postfix(MainCamera __instance)
        {
            __instance.gameObject.AddComponent<PostProcessingController>();
        }
    }
}
