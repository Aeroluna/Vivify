using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using Heck.Animation;
using UnityEngine;
using Logger = IPA.Logging.Logger;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void SetMaterialProperty(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out SetMaterialPropertyData? heckData))
            {
                return;
            }

            float duration = heckData.Duration;
            duration = 60f * duration / _bpmController.currentBpm; // Convert to real time;

            if (!_assetBundleManager.TryGetAsset(heckData.Asset, out Material? material))
            {
                return;
            }

            List<MaterialProperty> properties = heckData.Properties;
            SetMaterialProperties(material, properties, duration, heckData.Easing, customEventData.time);
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
                            material.SetVector(name, new Color(vector[0], vector[1], vector[2], vector[3]));
                        }

                        break;

                    default:
                        // im lazy, shoot me
                        Log.Logger.Log($"[{type}] not currently supported", Logger.Level.Warning);
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
                            Log.Logger.Log($"[{type.ToString()}] not supported yet.");
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
