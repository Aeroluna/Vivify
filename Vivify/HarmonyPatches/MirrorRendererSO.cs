namespace Vivify.HarmonyPatches
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using HarmonyLib;
    using UnityEngine;
    using Vivify.PostProcessing;

    [HarmonyPatch(typeof(MirrorRendererSO))]
    [HarmonyPatch("CreateOrUpdateMirrorCamera")]
    internal static class MirrorRendererSOCreateOrUpdateMirrorCamera
    {
        private static void Postfix(Camera ____mirrorCamera)
        {/*
            ____mirrorCamera.cullingMask &= ~(1 << Plugin.CULLINGLAYER);
            string binary = Convert.ToString(____mirrorCamera.cullingMask, 2);
            binary = new string(binary.ToCharArray().Reverse().ToArray());
            List<int> layers = new List<int>();
            for (int i = 0; i < binary.Length; i++)
            {
                if (binary[i] == '0')
                {
                    layers.Add(i);
                }
            }
            Plugin.Logger.Log($"{____mirrorCamera.gameObject.name}: {binary}");
            Plugin.Logger.Log($"culling: {string.Join(", ", layers)}");
            layers.ForEach(n => Plugin.Logger.Log($"{n}: {LayerMask.LayerToName(n)}"));*/
        }
    }

    [HarmonyPatch(typeof(Mirror))]
    [HarmonyPatch("OnWillRenderObject")]
    internal static class MirrorOnWillRenderObject
    {
        private static void Postfix()
        {/*
            ____mirrorCamera.cullingMask &= ~(1 << Plugin.CULLINGLAYER);
            string binary = Convert.ToString(____mirrorCamera.cullingMask, 2);
            binary = new string(binary.ToCharArray().Reverse().ToArray());
            List<int> layers = new List<int>();
            for (int i = 0; i < binary.Length; i++)
            {
                if (binary[i] == '0')
                {
                    layers.Add(i);
                }
            }
            Plugin.Logger.Log($"{____mirrorCamera.gameObject.name}: {binary}");
            Plugin.Logger.Log($"culling: {string.Join(", ", layers)}");
            layers.ForEach(n => Plugin.Logger.Log($"{n}: {LayerMask.LayerToName(n)}"));*/
        }
    }
}
