using System;
using System.Collections;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Deserialize;
using Heck.Event;
using Heck.ReLoad;
using SiraUtil.Logging;
using UnityEngine;
using Vivify.Managers;
using Vivify.PostProcessing;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events;

[CustomEvent(APPLY_POST_PROCESSING)]
internal class ApplyPostProcessing : ICustomEvent, IDisposable
{
    private readonly AssetBundleManager _assetBundleManager;
    private readonly IAudioTimeSource _audioTimeSource;
    private readonly IBpmController _bpmController;
    private readonly CoroutineDummy _coroutineDummy;
    private readonly DeserializedData _deserializedData;
    private readonly SiraLog _log;
    private readonly ReLoader? _reLoader;
    private readonly SetMaterialProperty _setMaterialProperty;

    private ApplyPostProcessing(
        SiraLog log,
        AssetBundleManager assetBundleManager,
        [Inject(Id = ID)] DeserializedData deserializedData,
        IAudioTimeSource audioTimeSource,
        IBpmController bpmController,
        SetMaterialProperty setMaterialProperty,
        CoroutineDummy coroutineDummy,
        [Inject(Id = HeckController.LEFT_HANDED_ID)]
        bool leftHanded,
        [InjectOptional] ReLoader? reLoader)
    {
        _log = log;
        _assetBundleManager = assetBundleManager;
        _deserializedData = deserializedData;
        _audioTimeSource = audioTimeSource;
        _bpmController = bpmController;
        _setMaterialProperty = setMaterialProperty;
        _coroutineDummy = coroutineDummy;
        _reLoader = reLoader;
        if (reLoader != null)
        {
            reLoader.Rewinded += PostProcessingController.ResetMaterial;
        }
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

        if (duration == 0)
        {
            PostProcessingController.PostProcessingMaterial.Add(
                new MaterialData(material, data.Priority, data.Source, data.Target, data.Pass, Time.frameCount));
            _log.Debug($"Applied material [{assetName}] for single frame");
            return;
        }

        if (duration <= 0 || _audioTimeSource.songTime > customEventData.time + duration)
        {
            return;
        }

        MaterialData materialData = new(material, data.Priority, data.Source, data.Target, data.Pass);
        PostProcessingController.PostProcessingMaterial.Add(materialData);
        _log.Debug($"Applied material [{assetName}] for [{duration}] seconds");
        _coroutineDummy.StartCoroutine(KillPostProcessingCoroutine(materialData, duration, customEventData.time));
    }

    public void Dispose()
    {
        if (_reLoader != null)
        {
            _reLoader.Rewinded -= PostProcessingController.ResetMaterial;
        }
    }

    internal IEnumerator KillPostProcessingCoroutine(MaterialData data, float duration, float startTime)
    {
        while (true)
        {
            float elapsedTime = _audioTimeSource.songTime - startTime;
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
                PostProcessingController.PostProcessingMaterial.Remove(data);
                break;
            }
        }
    }
}
