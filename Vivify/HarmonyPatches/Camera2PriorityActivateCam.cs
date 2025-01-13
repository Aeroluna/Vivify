using System;
using System.Reflection;
using HarmonyLib;
using Heck;
using IPA.Loader;
using JetBrains.Annotations;

namespace Vivify.HarmonyPatches;

[HeckPatch]
internal static class Camera2PriorityActivateCam
{
    private static MethodBase? _setCameraActive;

    [UsedImplicitly]
    [HarmonyPrepare]
    private static bool Prepare()
    {
        Assembly? assembly = PluginManager.GetPluginFromId("Camera2")?.Assembly;

        // ReSharper disable once InvertIf
        if (assembly != null)
        {
            _setCameraActive = assembly
                .GetType("Camera2.SDK.Cameras")
                ?.GetMethod("SetCameraActive", AccessTools.all);
            if (_setCameraActive != null)
            {
                return true;
            }

            Plugin.Log.Warn("Could not find [Camera2.SDK.Cameras.SetCameraActive].");
            return false;
        }

        return _setCameraActive != null;
    }

    [UsedImplicitly]
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return _setCameraActive ?? throw new InvalidOperationException();
    }

    [UsedImplicitly]
    [HarmonyPostfix]
    private static void ActivateCam(string cameraName, bool active)
    {
        if (active)
        {
            if (!Camera2PriorityScene.ActiveCams.Add(cameraName))
            {
                return;
            }
        }
        else
        {
            if (!Camera2PriorityScene.ActiveCams.Remove(cameraName))
            {
                return;
            }
        }

        Camera2PriorityScene.UpdateMainCam();
    }
}
