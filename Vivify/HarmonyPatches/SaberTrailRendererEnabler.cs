using HarmonyLib;
using Heck;
using UnityEngine;

namespace Vivify.HarmonyPatches;

[HeckPatch(PatchType.Features)]
internal static class SaberTrailRendererEnabler
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SaberTrailRenderer), nameof(SaberTrailRenderer.OnEnable))]
    [HarmonyPatch(typeof(SaberTrailRenderer), nameof(SaberTrailRenderer.OnDisable))]
    private static bool DisableDisable()
    {
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SaberTrail), nameof(SaberTrail.OnDisable))]
    private static bool DisableFix(
        SaberTrailRenderer ____trailRenderer)
    {
        if (____trailRenderer)
        {
            ____trailRenderer.gameObject.SetActive(false);
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SaberTrail), nameof(SaberTrail.OnEnable))]
    private static bool EnableFix(
        SaberTrail __instance,
        bool ____inited,
        TrailElementCollection ____trailElementCollection,
        Color ____color,
        SaberTrailRenderer ____trailRenderer)
    {
        if (____inited)
        {
            __instance.ResetTrailData();
            ____trailRenderer.UpdateMesh(____trailElementCollection, ____color);
        }

        if (____trailRenderer)
        {
            ____trailRenderer.gameObject.SetActive(true);
        }

        return false;
    }
}
