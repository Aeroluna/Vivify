using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Animation;
using Heck.Deserialize;
using Heck.Event;
using SiraUtil.Logging;
using UnityEngine;
using Vivify.Managers;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events
{
    [CustomEvent(SET_MATERIAL_PROPERTY)]
    internal class SetMaterialProperty : ICustomEvent
    {
        private readonly SiraLog _log;
        private readonly AssetBundleManager _assetBundleManager;
        private readonly DeserializedData _deserializedData;
        private readonly IAudioTimeSource _audioTimeSource;
        private readonly IBpmController _bpmController;
        private readonly CoroutineDummy _coroutineDummy;

        private SetMaterialProperty(
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
            if (!_deserializedData.Resolve(customEventData, out SetMaterialPropertyData? data))
            {
                return;
            }

            float duration = data.Duration;
            duration = 60f * duration / _bpmController.currentBpm; // Convert to real time;

            if (!_assetBundleManager.TryGetAsset(data.Asset, out Material? material))
            {
                return;
            }

            List<MaterialProperty> properties = data.Properties;
            SetMaterialProperties(material, properties, duration, data.Easing, customEventData.time);
        }

        internal void SetMaterialProperties(Material material, List<MaterialProperty> properties, float duration, Functions easing, float startTime)
        {
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
                            material.SetTexture(name, texture);
                        }

                        break;

                    case MaterialPropertyType.Color:
                        if (property is AnimatedMaterialProperty<Vector4> colorAnimated)
                        {
                            if (noDuration)
                            {
                                material.SetColor(name, colorAnimated.PointDefinition.Interpolate(1));
                            }
                            else
                            {
                                StartCoroutine(colorAnimated.PointDefinition, material, name, MaterialPropertyType.Color, duration, startTime, easing);
                            }
                        }
                        else
                        {
                            List<float> color = ((List<object>)value).Select(Convert.ToSingle).ToList();
                            material.SetColor(name, new Color(color[0], color[1], color[2], color.Count > 3 ? color[3] : 1));
                        }

                        break;

                    case MaterialPropertyType.Float:
                        if (property is AnimatedMaterialProperty<float> floatAnimated)
                        {
                            if (noDuration)
                            {
                                material.SetFloat(name, floatAnimated.PointDefinition.Interpolate(1));
                            }
                            else
                            {
                                StartCoroutine(floatAnimated.PointDefinition, material, name, MaterialPropertyType.Float, duration, startTime, easing);
                            }
                        }
                        else
                        {
                            material.SetFloat(name, Convert.ToSingle(value));
                        }

                        break;

                    case MaterialPropertyType.Vector:
                        if (property is AnimatedMaterialProperty<Vector4> vectorAnimated)
                        {
                            if (noDuration)
                            {
                                material.SetVector(name, vectorAnimated.PointDefinition.Interpolate(1));
                            }
                            else
                            {
                                StartCoroutine(vectorAnimated.PointDefinition, material, name, MaterialPropertyType.Vector, duration, startTime, easing);
                            }
                        }
                        else
                        {
                            List<float> vector = ((List<object>)value).Select(Convert.ToSingle).ToList();
                            material.SetVector(name, new Vector4(vector[0], vector[1], vector[2], vector[3]));
                        }

                        break;

                    default:
                        // im lazy, shoot me
                        _log.Warn($"[{type}] not currently supported");
                        break;
                }
            }
        }

        private void StartCoroutine<T>(
            PointDefinition<T> points,
            Material material,
            int name,
            MaterialPropertyType type,
            float duration,
            float startTime,
            Functions easing)
            where T : struct
            => _coroutineDummy.StartCoroutine(AnimatePropertyCoroutine(points, material, name, type, duration, startTime, easing));

        private IEnumerator AnimatePropertyCoroutine<T>(PointDefinition<T> points, Material material, int name, MaterialPropertyType type, float duration, float startTime, Functions easing)
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
                            // TODO: i probably should fix this in heck
                            material.SetColor(name, (points as PointDefinition<Vector4>)!.Interpolate(time));
                            break;

                        case MaterialPropertyType.Float:
                            material.SetFloat(name, (points as PointDefinition<float>)!.Interpolate(time));
                            break;

                        case MaterialPropertyType.Vector:
                            material.SetVector(name, (points as PointDefinition<Vector4>)!.Interpolate(time));
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
    }
}
