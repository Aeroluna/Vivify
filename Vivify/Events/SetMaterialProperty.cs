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

            Material? material = AssetBundleController.TryGetAsset<Material>(heckData.Asset);
            if (material == null)
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
                PointDefinition? points = property.PointDefinition;
                string name = property.Name;
                MaterialPropertyType type = property.Type;
                object value = property.Value;
                bool noDuration = duration == 0 || startTime + duration < _audioTimeSource.songTime;
                switch (type)
                {
                    case MaterialPropertyType.Texture:
                        string texValue = Convert.ToString(value);
                        Texture? texture = AssetBundleController.TryGetAsset<Texture>(texValue);
                        if (texture != null)
                        {
                            material.SetTexture(name, texture);
                        }

                        break;

                    case MaterialPropertyType.Color:
                        if (value is List<object>)
                        {
                            if (points == null)
                            {
                                Log.Logger.Log("Unable to get point definition.", Logger.Level.Error);
                                return;
                            }

                            if (noDuration)
                            {
                                material.SetColor(name, points.InterpolateVector4(1));
                            }
                            else
                            {
                                StartCoroutine(points, material, name, MaterialPropertyType.Color, duration, startTime, easing);
                            }
                        }
                        else
                        {
                            List<float> color = ((List<object>)value).Select(Convert.ToSingle).ToList();
                            material.SetColor(name, new Color(color[0], color[1], color[2], color.Count > 3 ? color[3] : 1));
                        }

                        break;

                    case MaterialPropertyType.Float:
                        if (value is List<object>)
                        {
                            if (points == null)
                            {
                                Log.Logger.Log("Unable to get point definition.", Logger.Level.Error);
                                return;
                            }

                            if (noDuration)
                            {
                                material.SetFloat(name, points.InterpolateLinear(1));
                            }
                            else
                            {
                                StartCoroutine(points, material, name, MaterialPropertyType.Float, duration, startTime, easing);
                            }
                        }
                        else
                        {
                            material.SetFloat(name, Convert.ToSingle(value));
                        }

                        break;

                    default:
                        // im lazy, shoot me
                        Log.Logger.Log($"{type} not currently supported", Logger.Level.Warning);
                        break;
                }
            }
        }

        private void StartCoroutine(
            PointDefinition points,
            Material material,
            string name,
            MaterialPropertyType type,
            float duration,
            float startTime,
            Functions easing) => _coroutineDummy.StartCoroutine(AnimatePropertyCoroutine(points, material, name, type, duration, startTime, easing));

        private IEnumerator AnimatePropertyCoroutine(PointDefinition points, Material material, string name, MaterialPropertyType type, float duration, float startTime, Functions easing)
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
                            material.SetColor(name, points.InterpolateVector4(time));
                            break;

                        case MaterialPropertyType.Float:
                            material.SetFloat(name, points.InterpolateLinear(time));
                            break;

                        case MaterialPropertyType.Vector:
                            material.SetVector(name, points.InterpolateVector4(time));
                            break;

                        default:
                            Log.Logger.Log($"[{type.ToString()}] not supported yet.");
                            goto notSupported;
                    }

                    yield return null;
                }
                else
                {
                    break;
                }
            }

            notSupported:
            yield return null;
        }
    }
}
