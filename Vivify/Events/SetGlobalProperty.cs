using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Animation;
using Heck.Deserialize;
using Heck.Event;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;
using Vivify.Managers;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events;

[CustomEvent(SET_GLOBAL_PROPERTY)]
internal class SetGlobalProperty : ICustomEvent
{
    private readonly AssetBundleManager _assetBundleManager;
    private readonly IAudioTimeSource _audioTimeSource;
    private readonly IBpmController _bpmController;
    private readonly CoroutineDummy _coroutineDummy;
    private readonly DeserializedData _deserializedData;
    private readonly SiraLog _log;

    [UsedImplicitly]
    private SetGlobalProperty(
        SiraLog log,
        AssetBundleManager assetBundleManager,
        [Inject(Id = ID)] DeserializedData deserializedData,
        IAudioTimeSource audioTimeSource,
        IBpmController bpmController,
        CoroutineDummy coroutineDummy)
    {
        _log = log;
        _assetBundleManager = assetBundleManager;
        _deserializedData = deserializedData;
        _audioTimeSource = audioTimeSource;
        _bpmController = bpmController;
        _coroutineDummy = coroutineDummy;
    }

    public void Callback(CustomEventData customEventData)
    {
        if (!_deserializedData.Resolve(customEventData, out SetGlobalPropertyData? data))
        {
            return;
        }

        float duration = data.Duration;
        duration = (60f * duration) / _bpmController.currentBpm; // Convert to real time;

        List<MaterialProperty> properties = data.Properties;
        Functions easing = data.Easing;
        float startTime = customEventData.time;

        foreach (MaterialProperty property in properties)
        {
            int name = property.Name;
            MaterialPropertyType type = property.Type;
            object value = property.Value;
            bool noDuration = duration == 0 || startTime + duration < _audioTimeSource.songTime;
            switch (type)
            {
                case MaterialPropertyType.Texture:
                    string texValue = Convert.ToString(value);
                    if (_assetBundleManager.TryGetAsset(texValue, out Texture? texture))
                    {
                        Shader.SetGlobalTexture(name, texture);
                    }

                    break;

                case MaterialPropertyType.Color:
                    if (property is AnimatedMaterialProperty<Vector4> colorAnimated)
                    {
                        if (noDuration)
                        {
                            Shader.SetGlobalColor(name, colorAnimated.PointDefinition.Interpolate(1));
                        }
                        else
                        {
                            StartCoroutine(
                                colorAnimated.PointDefinition,
                                name,
                                MaterialPropertyType.Color,
                                duration,
                                startTime,
                                easing);
                        }
                    }
                    else
                    {
                        List<float> color = ((List<object>)value).Select(Convert.ToSingle).ToList();
                        Shader.SetGlobalColor(
                            name,
                            new Color(color[0], color[1], color[2], color.Count > 3 ? color[3] : 1));
                    }

                    break;

                case MaterialPropertyType.Float:
                    if (property is AnimatedMaterialProperty<float> floatAnimated)
                    {
                        if (noDuration)
                        {
                            Shader.SetGlobalFloat(name, floatAnimated.PointDefinition.Interpolate(1));
                        }
                        else
                        {
                            StartCoroutine(
                                floatAnimated.PointDefinition,
                                name,
                                MaterialPropertyType.Float,
                                duration,
                                startTime,
                                easing);
                        }
                    }
                    else
                    {
                        Shader.SetGlobalFloat(name, Convert.ToSingle(value));
                    }

                    break;

                case MaterialPropertyType.Vector:
                    if (property is AnimatedMaterialProperty<Vector4> vectorAnimated)
                    {
                        if (noDuration)
                        {
                            Shader.SetGlobalVector(name, vectorAnimated.PointDefinition.Interpolate(1));
                        }
                        else
                        {
                            StartCoroutine(
                                vectorAnimated.PointDefinition,
                                name,
                                MaterialPropertyType.Vector,
                                duration,
                                startTime,
                                easing);
                        }
                    }
                    else
                    {
                        List<float> vector = ((List<object>)value).Select(Convert.ToSingle).ToList();
                        Shader.SetGlobalVector(name, new Vector4(vector[0], vector[1], vector[2], vector[3]));
                    }

                    break;

                default:
                    _log.Warn($"{type} not currently supported");
                    break;
            }
        }
    }

    private IEnumerator AnimateGlobalPropertyCoroutine<T>(
        PointDefinition<T> points,
        int name,
        MaterialPropertyType type,
        float duration,
        float startTime,
        Functions easing)
        where T : struct
    {
        while (true)
        {
            float elapsedTime = _audioTimeSource.songTime - startTime;

            if (elapsedTime < duration)
            {
                float time = Easings.Interpolate(Mathf.Min(elapsedTime / duration, 1f), easing);
                switch (type)
                {
                    case MaterialPropertyType.Color:
                        Shader.SetGlobalColor(name, (points as PointDefinition<Vector4>)!.Interpolate(time));
                        break;

                    case MaterialPropertyType.Float:
                        Shader.SetGlobalFloat(name, (points as PointDefinition<float>)!.Interpolate(time));
                        break;

                    case MaterialPropertyType.Vector:
                        Shader.SetGlobalVector(name, (points as PointDefinition<Vector4>)!.Interpolate(time));
                        break;

                    default:
                        _log.Warn($"[{type.ToString()}] not supported yet");
                        yield break;
                }

                yield return null;
            }
            else
            {
                break;
            }
        }
    }

    private void StartCoroutine<T>(
        PointDefinition<T> points,
        int name,
        MaterialPropertyType type,
        float duration,
        float startTime,
        Functions easing)
        where T : struct
    {
        _coroutineDummy.StartCoroutine(AnimateGlobalPropertyCoroutine(points, name, type, duration, startTime, easing));
    }
}
