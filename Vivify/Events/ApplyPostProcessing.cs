using System;
using System.Collections;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Deserialize;
using Heck.Event;
using SiraUtil.Logging;
using UnityEngine;
using Vivify.Extras;
using Vivify.HarmonyPatches;
using Vivify.Managers;
using Vivify.PostProcessing;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events;

[CustomEvent(APPLY_POST_PROCESSING)]
internal class ApplyPostProcessing : ICustomEvent
{
    private readonly SiraLog _log;
    private readonly AssetBundleManager _assetBundleManager;
    private readonly DeserializedData _deserializedData;
    private readonly BeatmapCallbacksController _beatmapCallbacksController;
    private readonly IBpmController _bpmController;
    private readonly SetMaterialProperty _setMaterialProperty;
    private readonly CameraEffectApplier _cameraEffectApplier;
    private readonly CoroutineDummy _coroutineDummy;

    private ApplyPostProcessing(
        SiraLog log,
        AssetBundleManager assetBundleManager,
        [Inject(Id = ID)] DeserializedData deserializedData,
        BeatmapCallbacksController beatmapCallbacksController,
        IBpmController bpmController,
        SetMaterialProperty setMaterialProperty,
        CameraEffectApplier cameraEffectApplier,
        CoroutineDummy coroutineDummy)
    {
        _log = log;
        _assetBundleManager = assetBundleManager;
        _deserializedData = deserializedData;
        _beatmapCallbacksController = beatmapCallbacksController;
        _bpmController = bpmController;
        _setMaterialProperty = setMaterialProperty;
        _cameraEffectApplier = cameraEffectApplier;
        _coroutineDummy = coroutineDummy;
    }

    public void Callback(CustomEventData customEventData)
    {
        if (!_deserializedData.Resolve(customEventData, out ApplyPostProcessingData? data))
        {
            return;
        }

        float duration = (60f * data.Duration) / _bpmController.currentBpm; // Convert to real time;
        string? assetName = data.Asset;
        Material? material = null;
        if (assetName != null)
        {
            if (!_assetBundleManager.TryGetAsset(assetName, out material))
            {
                return;
            }

            List<MaterialProperty>? properties = data.Properties;
            if (properties != null)
            {
                _setMaterialProperty.SetMaterialProperties(
                    material,
                    properties,
                    duration,
                    data.Easing,
                    customEventData.time);
            }
        }

        List<MaterialData> effects = data.Order switch
        {
            PostProcessingOrder.BeforeMainEffect => _cameraEffectApplier.PreEffects,
            PostProcessingOrder.AfterMainEffect => _cameraEffectApplier.PostEffects,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (duration == 0)
        {
            effects.InsertIntoSortedList(
                new MaterialData(material, data.Priority, data.Source, data.Target, data.Pass, Time.frameCount));
            _log.Debug($"Applied material [{assetName}] for single frame");
            return;
        }

        if (duration <= 0 || _beatmapCallbacksController.songTime > customEventData.time + duration)
        {
            return;
        }

        MaterialData materialData = new(material, data.Priority, data.Source, data.Target, data.Pass);
        effects.InsertIntoSortedList(materialData);
        _log.Debug($"Applied material [{assetName}] for [{duration}] seconds");
        _coroutineDummy.StartCoroutine(
            KillPostProcessingCoroutine(effects, materialData, duration, customEventData.time));
    }

    internal IEnumerator KillPostProcessingCoroutine(
        List<MaterialData> effects,
        MaterialData data,
        float duration,
        float startTime)
    {
        while (true)
        {
            float elapsedTime = _beatmapCallbacksController.songTime - startTime;
            if (elapsedTime < 0)
            {
                break;
            }

            if (elapsedTime < duration)
            {
                yield return null;
            }
            else
            {
                effects.Remove(data);
                break;
            }
        }
    }
}
