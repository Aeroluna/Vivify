namespace Vivify.HarmonyPatches
{
    using HarmonyLib;

    // Force disable AssLoader in tutorial scene
    [HarmonyPatch(typeof(TutorialScenesTransitionSetupDataSO))]
    [HarmonyPatch("Init")]
    internal static class TutorialScenesTransitionSetupDataSOInit
    {
        private static void Prefix()
        {
            VivifyController.ToggleVivifyPatches(false);
        }
    }
}
