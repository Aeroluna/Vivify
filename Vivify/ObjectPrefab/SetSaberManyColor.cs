using HarmonyLib;
using Heck;
using SiraUtil.Extras;
using UnityEngine;

namespace Vivify.ObjectPrefab
{
    [HeckPatch(PatchType.Features)]
    internal class SetSaberManyColor : SetSaberGlowColor
    {
        // maybe add other properties? they don't seem important
        private static readonly int _color = Shader.PropertyToID("_Color");

        private Renderer[] _renderers = null!;

        private static bool SetAllRendererColors(SetSaberGlowColor setSaberGlowColor, Color color)
        {
            if (setSaberGlowColor is not SetSaberManyColor setSaberManyColor)
            {
                return true;
            }

            MaterialPropertyBlock? materialPropertyBlock = setSaberGlowColor._materialPropertyBlock;
            if (materialPropertyBlock == null)
            {
                setSaberGlowColor._materialPropertyBlock = materialPropertyBlock = new MaterialPropertyBlock();
            }

            materialPropertyBlock.SetColor(_color, color);
            foreach (Renderer renderer in setSaberManyColor._renderers)
            {
                renderer.SetPropertyBlock(materialPropertyBlock);
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SetSaberGlowColor), nameof(SetColors))]
        private static bool SetSaberGlowColorOverride(SetSaberGlowColor __instance)
        {
            Color a = __instance._colorManager.ColorForSaberType(__instance._saberType);
            return SetAllRendererColors(__instance, a);
        }

        [HarmonyPrefix]
        [HarmonyPatch(
            typeof(SaberExtensions),
            nameof(SaberExtensions.SetColors),
            typeof(SetSaberGlowColor),
            typeof(Color))]
        private static bool SiraSetColorsOverride(SetSaberGlowColor setSaberGlowColor, Color color)
        {
            return SetAllRendererColors(setSaberGlowColor, color);
        }

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
        }
    }
}
