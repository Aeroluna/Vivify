using HarmonyLib;
using JetBrains.Annotations;

namespace Vivify.HarmonyPatches.SceneTransition
{
    // Force disable AssLoader in tutorial scene
    [HarmonyPatch(typeof(TutorialScenesTransitionSetupDataSO))]
    [HarmonyPatch("Init")]
    internal static class TutorialScenesTransitionSetupDataSOInit
    {
        [UsedImplicitly]
        private static void Prefix()
        {
            VivifyController.ToggleVivifyPatches(false);
        }
    }
}
