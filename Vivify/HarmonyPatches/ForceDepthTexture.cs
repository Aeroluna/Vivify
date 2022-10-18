using HarmonyLib;
using Heck;
using UnityEngine;

namespace Vivify.HarmonyPatches
{
    [HeckPatch]
    [HarmonyPatch(typeof(VisualEffectsController))]
    internal static class ForceDepthTexture
    {
        // TODO: have mapper set what they need
        [HarmonyPrefix]
        [HarmonyPatch("HandleDepthTextureEnabledDidChange")]
        private static bool ForceDepth(Camera ____camera)
        {
            ____camera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
            return false;
        }
    }
}
