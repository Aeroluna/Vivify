using HarmonyLib;
using Heck;
using UnityEngine;

namespace Vivify.HarmonyPatches;

[HeckPatch(PatchType.Features)]
internal static class MoveAlwaysVisibleQuad
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AlwaysVisibleQuad), nameof(AlwaysVisibleQuad.OnEnable))]
    private static void MoveIt(AlwaysVisibleQuad __instance)
    {
        __instance.transform.position = new Vector3(0, -1000, 0);
    }
}
