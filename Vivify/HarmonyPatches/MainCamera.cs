namespace Vivify.HarmonyPatches
{
    using HarmonyLib;
    using Vivify.PostProcessing;

    [HarmonyPatch(typeof(MainCamera))]
    [HarmonyPatch("Awake")]
    internal static class MainCameraAwake
    {
        private static void Postfix(MainCamera __instance)
        {
            __instance.gameObject.AddComponent<PostProcessingController>();
        }
    }
}
