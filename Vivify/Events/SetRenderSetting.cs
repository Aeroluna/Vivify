using System;
using System.Collections;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using Heck.Animation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vivify.Events
{
    internal partial class EventController
    {
        // TODO: RESET EVERYthing cause this shit needs to reset on map end
        internal void SetRenderSetting(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out SetRenderSettingData? data))
            {
                return;
            }

            float duration = data.Duration;
            duration = 60f * duration / _bpmController.currentBpm; // Convert to real time;
            List<RenderSettingProperty> properties = data.Properties;
            SetRenderSettings(properties, duration, data.Easing, customEventData.time);
        }

        internal void SetRenderSettings(List<RenderSettingProperty> properties, float duration, Functions easing, float startTime)
        {
            foreach (RenderSettingProperty property in properties)
            {
                string name = property.Name;

                bool noDuration = duration == 0 || startTime + duration < _audioTimeSource.songTime;

                switch (name)
                {
                    case "ambientEquatorColor":
                        Handle<Vector4>(n => RenderSettings.ambientEquatorColor = n);
                        break;

                    case "ambientGroundColor":
                        Handle<Vector4>(n => RenderSettings.ambientGroundColor = n);
                        break;

                    case "ambientIntensity":
                        Handle<float>(n => RenderSettings.ambientIntensity = n);
                        break;

                    case "ambientLight":
                        Handle<Vector4>(n => RenderSettings.ambientLight = n);
                        break;

                    case "ambientMode":
                        Handle<float>(n => RenderSettings.ambientMode = ToEnum<AmbientMode>(n));
                        break;

                    case "ambientSkyColor":
                        Handle<Vector4>(n => RenderSettings.ambientSkyColor = n);
                        break;

                    case "defaultReflectionMode":
                        Handle<float>(n => RenderSettings.defaultReflectionMode = ToEnum<DefaultReflectionMode>(n));
                        break;

                    case "defaultReflectionResolution":
                        Handle<float>(n => RenderSettings.defaultReflectionResolution = (int)n);
                        break;

                    case "flareFadeSpeed":
                        Handle<float>(n => RenderSettings.flareFadeSpeed = n);
                        break;

                    case "flareStrength":
                        Handle<float>(n => RenderSettings.flareStrength = n);
                        break;

                    case "fog":
                        Handle<float>(n => RenderSettings.fog = Convert.ToBoolean((int)n));
                        break;

                    case "fogColor":
                        Handle<Vector4>(n => RenderSettings.fogColor = n);
                        break;

                    case "fogDensity":
                        Handle<float>(n => RenderSettings.fogDensity = n);
                        break;

                    case "fogEndDistance":
                        Handle<float>(n => RenderSettings.fogEndDistance = n);
                        break;

                    case "fogMode":
                        Handle<float>(n => RenderSettings.fogMode = ToEnum<FogMode>(n));
                        break;

                    case "fogStartDistance":
                        Handle<float>(n => RenderSettings.fogStartDistance = n);
                        break;

                    case "haloStrength":
                        Handle<float>(n => RenderSettings.fogStartDistance = n);
                        break;

                    case "reflectionBounces":
                        Handle<float>(n => RenderSettings.reflectionBounces = (int)n);
                        break;

                    case "reflectionIntensity":
                        Handle<float>(n => RenderSettings.reflectionIntensity = n);
                        break;

                    case "subtractiveShadowColor":
                        Handle<Vector4>(n => RenderSettings.subtractiveShadowColor = n);
                        break;
                }

                continue;

                void Handle<T>(Action<T> set)
                    where T : struct
                {
                    switch (property)
                    {
                        case AnimatedRenderSettingProperty<T> animated when noDuration:
                            set(animated.PointDefinition.Interpolate(1));
                            break;
                        case AnimatedRenderSettingProperty<T> animated:
                            StartCoroutine(animated.PointDefinition, set, duration, startTime, easing);
                            break;
                        case RenderSettingProperty<T> value:
                            set(value.Value);
                            DynamicGI.UpdateEnvironment();
                            break;
                        default:
                            throw new InvalidOperationException($"Could not handle type [{property.GetType().FullName}]");
                    }
                }

                T ToEnum<T>(float obj)
                {
                    T enumVal = (T)Enum.ToObject(typeof(T), (int)obj);
                    return enumVal;
                }
            }
        }

        private void StartCoroutine<T>(
            PointDefinition<T> points,
            Action<T> set,
            float duration,
            float startTime,
            Functions easing)
            where T : struct
            => _coroutineDummy.StartCoroutine(AnimatePropertyCoroutine(points, set, duration, startTime, easing));

        private IEnumerator AnimatePropertyCoroutine<T>(PointDefinition<T> points, Action<T> set, float duration, float startTime, Functions easing)
            where T : struct
        {
            while (true)
            {
                float elapsedTime = _audioTimeSource.songTime - startTime;

                if (elapsedTime < duration)
                {
                    float time = Easings.Interpolate(Mathf.Min(elapsedTime / duration, 1f), easing);
                    set(points.Interpolate(time));

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
