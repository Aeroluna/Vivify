using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Heck;
using IPA.Loader;
using JetBrains.Annotations;
using UnityEngine;
using Vivify.PostProcessing;

namespace Vivify.HarmonyPatches;

[HeckPatch]
internal class Camera2Priority
{
    private static readonly HashSet<(string Name, GameObject GameObject)> _nonAllocCams = [];

    private static MethodBase? _switchToCamlist;

    private static IDictionary? _cams;

    [UsedImplicitly]
    [HarmonyPrepare]
    private static bool Prepare()
    {
        Assembly? assembly = PluginManager.GetPluginFromId("Camera2")?.Assembly;

        // ReSharper disable once InvertIf
        if (assembly != null)
        {
            _switchToCamlist = assembly
                .GetType("Camera2.Managers.ScenesManager")
                ?.GetMethod("SwitchToCamlist", AccessTools.all);
            if (_switchToCamlist == null)
            {
                Plugin.Log.Warn("Could not find [Camera2.Managers.ScenesManager].");
                return false;
            }

            _cams = (IDictionary?)assembly
                .GetType("Camera2.Managers.CamManager")
                ?.GetProperty("cams", AccessTools.all)
                ?.GetValue(null);

            // ReSharper disable once InvertIf
            if (_cams == null)
            {
                Plugin.Log.Warn("Could not find [Camera2.Managers.CamManager.cams].");
                return false;
            }
        }

        return _switchToCamlist != null;
    }

    [UsedImplicitly]
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return _switchToCamlist ?? throw new InvalidOperationException();
    }

    [UsedImplicitly]
    [HarmonyPostfix]
    private static void SwapMainCam(List<string>? cams)
    {
        _nonAllocCams.Clear();
        foreach (DictionaryEntry entry in _cams ?? throw new InvalidOperationException())
        {
            _nonAllocCams.Add(((string)entry.Key, ((MonoBehaviour)entry.Value).gameObject));
        }

        string? mainCam = cams?.First(n => _nonAllocCams.Any(m => m.Name == n));
        Plugin.Log.Info($"Switching main Vivify Cam2 to [{mainCam ?? "null"}]");
        foreach ((string Name, GameObject GameObject) cam in _nonAllocCams)
        {
            PostProcessingController postProcessingController = cam.GameObject.GetComponentInChildren<PostProcessingController>();
            if (postProcessingController != null)
            {
                postProcessingController.enabled = cam.Name == mainCam;
            }
        }
    }
}
