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

    [UsedImplicitly]
    private SetGlobalProperty(
        AssetBundleManager assetBundleManager,
        [Inject(Id = ID)] DeserializedData deserializedData,
        IAudioTimeSource audioTimeSource,
        IBpmController bpmController,
        CoroutineDummy coroutineDummy)
    {
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
            MaterialPropertyType type = property.Type;
            object value = property.Value;
            bool noDuration = duration == 0 || startTime + duration < _audioTimeSource.songTime;
            switch (property.Id)
            {
                case int propertyId:
                    switch (type)
                    {
                        case MaterialPropertyType.Texture:
                            string texValue = Convert.ToString(value);
                            if (_assetBundleManager.TryGetAsset(texValue, out Texture? texture))
                            {
                                Shader.SetGlobalTexture(propertyId, texture);
                            }

                            continue;

                        case MaterialPropertyType.Color:
                            if (property is AnimatedMaterialProperty<Vector4> colorAnimated)
                            {
                                if (noDuration)
                                {
                                    Shader.SetGlobalColor(propertyId, colorAnimated.PointDefinition.Interpolate(1));
                                }
                                else
                                {
                                    StartCoroutine(
                                        colorAnimated.PointDefinition,
                                        propertyId,
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
                                    propertyId,
                                    new Color(color[0], color[1], color[2], color.Count > 3 ? color[3] : 1));
                            }

                            continue;

                        case MaterialPropertyType.Float:
                            if (property is AnimatedMaterialProperty<float> floatAnimated)
                            {
                                if (noDuration)
                                {
                                    Shader.SetGlobalFloat(propertyId, floatAnimated.PointDefinition.Interpolate(1));
                                }
                                else
                                {
                                    StartCoroutine(
                                        floatAnimated.PointDefinition,
                                        propertyId,
                                        MaterialPropertyType.Float,
                                        duration,
                                        startTime,
                                        easing);
                                }
                            }
                            else
                            {
                                Shader.SetGlobalFloat(propertyId, Convert.ToSingle(value));
                            }

                            continue;

                        case MaterialPropertyType.Vector:
                            if (property is AnimatedMaterialProperty<Vector4> vectorAnimated)
                            {
                                if (noDuration)
                                {
                                    Shader.SetGlobalVector(propertyId, vectorAnimated.PointDefinition.Interpolate(1));
                                }
                                else
                                {
                                    StartCoroutine(
                                        vectorAnimated.PointDefinition,
                                        propertyId,
                                        MaterialPropertyType.Vector,
                                        duration,
                                        startTime,
                                        easing);
                                }
                            }
                            else
                            {
                                List<float> vector = ((List<object>)value).Select(Convert.ToSingle).ToList();
                                Shader.SetGlobalVector(propertyId, new Vector4(vector[0], vector[1], vector[2], vector[3]));
                            }

                            continue;
                    }

                    break;

                case string name:
                    switch (type)
                    {
                        case MaterialPropertyType.Keyword:
                            if (property is AnimatedMaterialProperty<float> keywordAnimated)
                            {
                                if (noDuration)
                                {
                                    SetGlobalKeyword(name, keywordAnimated.PointDefinition.Interpolate(1) >= 1);
                                }
                                else
                                {
                                    StartCoroutine(
                                        keywordAnimated.PointDefinition,
                                        name,
                                        MaterialPropertyType.Float,
                                        duration,
                                        startTime,
                                        easing);
                                }
                            }
                            else
                            {
                                SetGlobalKeyword(name, (bool)value);
                            }

                            continue;
                    }

                    break;
            }

            throw new ArgumentOutOfRangeException(nameof(type), type, "Type not currently supported.");
        }
    }

    private static void SetGlobalKeyword(string keyword, bool value)
    {
        if (value)
        {
            Shader.EnableKeyword(keyword);
        }
        else
        {
            Shader.DisableKeyword(keyword);
        }
    }

    private IEnumerator AnimateGlobalPropertyCoroutine<T>(
        PointDefinition<T> points,
        object id,
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
                switch (id)
                {
                    case int propertyId:
                        switch (type)
                        {
                            case MaterialPropertyType.Color:
                                Shader.SetGlobalColor(propertyId, (points as PointDefinition<Vector4>)!.Interpolate(time));
                                break;

                            case MaterialPropertyType.Float:
                                Shader.SetGlobalFloat(propertyId, (points as PointDefinition<float>)!.Interpolate(time));
                                break;

                            case MaterialPropertyType.Vector:
                                Shader.SetGlobalVector(propertyId, (points as PointDefinition<Vector4>)!.Interpolate(time));
                                break;
                        }

                        break;

                    case string name:
                        switch (type)
                        {
                            case MaterialPropertyType.Keyword:
                                SetGlobalKeyword(name, (points as PointDefinition<float>)!.Interpolate(time) >= 1);
                                break;
                        }

                        break;
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
        object id,
        MaterialPropertyType type,
        float duration,
        float startTime,
        Functions easing)
        where T : struct
    {
        _coroutineDummy.StartCoroutine(AnimateGlobalPropertyCoroutine(points, id, type, duration, startTime, easing));
    }
}
