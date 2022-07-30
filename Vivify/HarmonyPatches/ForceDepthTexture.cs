using HarmonyLib;
using Heck;
using UnityEngine;

namespace Vivify.HarmonyPatches
{
    [HeckPatch]
    [HarmonyPatch(typeof(VisualEffectsController))]
    internal static class ForceDepthTexture
    {
        [HarmonyPrefix]
        [HarmonyPatch("HandleDepthTextureEnabledDidChange")]
        private static bool ForceDepth(Camera ____camera)
        {
            ____camera.depthTextureMode = DepthTextureMode.Depth;
            return false;
        }
    }
}
