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
internal static class Camera2Priority
{
    private static readonly HashSet<(string Name, MonoBehaviour MonoBehaviour)> _nonAllocCams = [];

    private static MethodBase? _cameraGetter;
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

            _cameraGetter = assembly
                .GetType("Camera2.Behaviours.Cam2")
                ?.GetProperty("UCamera", AccessTools.all)
                ?.GetGetMethod(true);

            // ReSharper disable once InvertIf
            if (_cameraGetter == null)
            {
                Plugin.Log.Warn("Could not find [Camera2.Behaviours.Cam2.UCamera].");
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
            _nonAllocCams.Add(((string)entry.Key, (MonoBehaviour)entry.Value));
        }

        if (_cameraGetter == null)
        {
            throw new InvalidOperationException();
        }

        string[] mainCam = _nonAllocCams
            .Where(n => cams?.Contains(n.Name) ?? false)
            .OrderBy(n => ((Camera)_cameraGetter.Invoke(n.MonoBehaviour, null)).depth)
            .Take(Plugin.Config.MaxCamera2Cams)
            .Select(n => n.Name)
            .ToArray();
        string cameraLog = mainCam.Length > 0 ? string.Join(", ", mainCam) : "null";
        Plugin.Log.Info($"Switching main Vivify Cam2 to [{cameraLog}]");
        foreach ((string Name, MonoBehaviour MonoBehaviour) cam in _nonAllocCams)
        {
            PostProcessingController postProcessingController = cam.MonoBehaviour.GetComponentInChildren<PostProcessingController>();
            if (postProcessingController != null)
            {
                postProcessingController.enabled = mainCam.Contains(cam.Name);
            }
        }
    }
}
