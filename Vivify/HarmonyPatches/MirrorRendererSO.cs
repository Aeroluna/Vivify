using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Heck;
using JetBrains.Annotations;
using UnityEngine;

namespace Vivify.HarmonyPatches
{
    [HeckPatch(typeof(MirrorRendererSO))]
    [HeckPatch("GetMirrorTexture")]
    internal static class MirrorRendererSOGetMirrorTexture
    {
        private static readonly MethodInfo _fieldOfViewGetter = AccessTools.PropertyGetter(typeof(Camera), nameof(Camera.fieldOfView));
        private static readonly MethodInfo _getFloatHash = AccessTools.Method(typeof(MirrorRendererSOGetMirrorTexture), nameof(GetFloatHash));

        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Callvirt, _fieldOfViewGetter))
                .Set(OpCodes.Call, _getFloatHash)
                .InstructionEnumeration();
        }

        private static float GetFloatHash(Camera camera)
        {
            return camera.GetHashCode();
        }
    }
}
