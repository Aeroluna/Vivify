using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using Heck.Animation;
using UnityEngine;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void SetGlobalProperty(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out SetGlobalPropertyData? data))
            {
                return;
            }

            float duration = data.Duration;
            duration = 60f * duration / _bpmController.currentBpm; // Convert to real time;

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
                                StartCoroutine(colorAnimated.PointDefinition, name, MaterialPropertyType.Color, duration, startTime, easing);
                            }
                        }
                        else
                        {
                            List<float> color = ((List<object>)value).Select(Convert.ToSingle).ToList();
                            Shader.SetGlobalColor(name, new Color(color[0], color[1], color[2], color.Count > 3 ? color[3] : 1));
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
                                StartCoroutine(floatAnimated.PointDefinition, name, MaterialPropertyType.Float, duration, startTime, easing);
                            }
                        }
                        else
                        {
                            Shader.SetGlobalFloat(name, Convert.ToSingle(value));
                        }

                        break;

                    default:
                        Plugin.Log.LogWarning($"{type} not currently supported");
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
            => _coroutineDummy.StartCoroutine(AnimateGlobalPropertyCoroutine(points, name, type, duration, startTime, easing));

        private IEnumerator AnimateGlobalPropertyCoroutine<T>(PointDefinition<T> points, int name, MaterialPropertyType type, float duration, float startTime, Functions easing)
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
                            Shader.SetGlobalVector(name, (points as PointDefinition<Vector3>)!.Interpolate(time));
                            break;

                        default:
                            Plugin.Log.LogWarning($"[{type.ToString()}] not supported yet.");
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
