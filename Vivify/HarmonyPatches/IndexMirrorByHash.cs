using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Heck;
using UnityEngine;

namespace Vivify.HarmonyPatches
{
    [HeckPatch(PatchType.Features)]
    [HarmonyPatch(typeof(MirrorRendererSO))]
    internal static class IndexMirrorByHash
    {
        private static readonly MethodInfo _fieldOfViewGetter = AccessTools.PropertyGetter(typeof(Camera), nameof(Camera.fieldOfView));
        private static readonly MethodInfo _getFloatHash = AccessTools.Method(typeof(IndexMirrorByHash), nameof(GetFloatHash));

        [HarmonyTranspiler]
#if LATEST
        [HarmonyPatch(nameof(MirrorRendererSO.RenderMirrorTexture))]
#else
        [HarmonyPatch(nameof(MirrorRendererSO.GetMirrorTexture))]
#endif
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, _fieldOfViewGetter))
                .Set(OpCodes.Call, _getFloatHash)
                .InstructionEnumeration();
        }

        private static float GetFloatHash(Camera camera)
        {
            // Base game uses field of view to distuingish between cameras, we use hash code
            return camera.GetHashCode();
        }
    }
}
