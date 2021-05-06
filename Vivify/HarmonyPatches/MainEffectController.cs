namespace Vivify.HarmonyPatches
{
    using System;
    using HarmonyLib;
    using UnityEngine;

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
