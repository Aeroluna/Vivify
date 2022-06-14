using HarmonyLib;
using Heck;
using JetBrains.Annotations;
using Vivify.PostProcessing;

namespace Vivify.HarmonyPatches
{
    [HeckPatch]
    [HarmonyPatch(typeof(MainCamera), "Awake")]
    internal static class MainCameraAwake
    {
        [UsedImplicitly]
        private static void Postfix(MainCamera __instance)
        {
            __instance.gameObject.AddComponent<PostProcessingController>();
        }
    }
}
