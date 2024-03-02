using System;
using System.Collections;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Animation;
using Heck.Event;
using UnityEngine;
using UnityEngine.Rendering;
using Vivify.Managers;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events
{
    [CustomEvent(SET_RENDER_SETTING)]
    internal class SetRenderSetting : ICustomEvent, IInitializable, IDisposable
    {
        private readonly DeserializedData _deserializedData;
        private readonly IAudioTimeSource _audioTimeSource;
        private readonly IBpmController _bpmController;
        private readonly CoroutineDummy _coroutineDummy;

        private readonly Dictionary<string, ISettingHandler> _settings = new()
        {
            { "ambientEquatorColor", new SettingHandler<Vector4, Color>(new RenderColorCapturedSetting(nameof(RenderSettings.ambientEquatorColor))) },
            { "ambientGroundColor", new SettingHandler<Vector4, Color>(new RenderColorCapturedSetting(nameof(RenderSettings.ambientEquatorColor))) },
            { "ambientIntensity", new SettingHandler<float, float>(new RenderFloatCapturedSetting(nameof(RenderSettings.ambientIntensity))) },
            { "ambientLight", new SettingHandler<Vector4, Color>(new RenderColorCapturedSetting(nameof(RenderSettings.ambientLight))) },
            { "ambientMode", new SettingHandler<float, AmbientMode>(new RenderEnumCapturedSetting<AmbientMode>(nameof(RenderSettings.ambientMode))) },
            { "ambientSkyColor", new SettingHandler<Vector4, Color>(new RenderColorCapturedSetting(nameof(RenderSettings.ambientSkyColor))) },
            { "defaultReflectionMode", new SettingHandler<float, DefaultReflectionMode>(new RenderEnumCapturedSetting<DefaultReflectionMode>(nameof(RenderSettings.defaultReflectionMode))) },
            { "defaultReflectionResolution", new SettingHandler<float, int>(new RenderIntCapturedSetting(nameof(RenderSettings.defaultReflectionResolution))) },
            { "flareFadeSpeed", new SettingHandler<float, float>(new RenderFloatCapturedSetting(nameof(RenderSettings.flareFadeSpeed))) },
            { "flareStrength", new SettingHandler<float, float>(new RenderFloatCapturedSetting(nameof(RenderSettings.flareStrength))) },
            { "fog", new SettingHandler<float, bool>(new CapturedSetting<RenderSettings, bool>(nameof(RenderSettings.fog), n => Convert.ToBoolean((int)n))) },
            { "fogColor", new SettingHandler<Vector4, Color>(new RenderColorCapturedSetting(nameof(RenderSettings.fogColor))) },
            { "fogDensity", new SettingHandler<float, float>(new RenderFloatCapturedSetting(nameof(RenderSettings.fogDensity))) },
            { "fogEndDistance", new SettingHandler<float, float>(new RenderFloatCapturedSetting(nameof(RenderSettings.fogEndDistance))) },
            { "fogMode", new SettingHandler<float, FogMode>(new RenderEnumCapturedSetting<FogMode>(nameof(RenderSettings.fogMode))) },
            { "fogStartDistance", new SettingHandler<float, float>(new RenderFloatCapturedSetting(nameof(RenderSettings.fogStartDistance))) },
            { "haloStrength", new SettingHandler<float, float>(new RenderFloatCapturedSetting(nameof(RenderSettings.haloStrength))) },
            { "reflectionBounces", new SettingHandler<float, int>(new RenderIntCapturedSetting(nameof(RenderSettings.reflectionBounces))) },
            { "reflectionIntensity", new SettingHandler<float, float>(new RenderFloatCapturedSetting(nameof(RenderSettings.reflectionIntensity))) },
            { "subtractiveShadowColor", new SettingHandler<Vector4, Color>(new RenderColorCapturedSetting(nameof(RenderSettings.subtractiveShadowColor))) },
        };

        private SetRenderSetting(
            [Inject(Id = ID)] DeserializedData deserializedData,
            IAudioTimeSource audioTimeSource,
            IBpmController bpmController,
            CoroutineDummy coroutineDummy)
        {
            _deserializedData = deserializedData;
            _audioTimeSource = audioTimeSource;
            _bpmController = bpmController;
            _coroutineDummy = coroutineDummy;
        }

        private interface ISettingHandler
        {
            public void Capture();

            public void Reset();

            public void Handle(
                SetRenderSetting instance,
                RenderSettingProperty property,
                bool noDuration,
                float duration,
                Functions easing,
                float startTime);
        }

        public void Callback(CustomEventData customEventData)
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

        public void Initialize()
        {
            foreach (ISettingHandler settingHandler in _settings.Values)
            {
                settingHandler.Capture();
            }
        }

        public void Dispose()
        {
            foreach (ISettingHandler settingHandler in _settings.Values)
            {
                settingHandler.Reset();
            }
        }

        internal void SetRenderSettings(List<RenderSettingProperty> properties, float duration, Functions easing, float startTime)
        {
            foreach (RenderSettingProperty property in properties)
            {
                string name = property.Name;

                bool noDuration = duration == 0 || startTime + duration < _audioTimeSource.songTime;

                if (_settings.TryGetValue(name, out ISettingHandler settingHandler))
                {
                    settingHandler.Handle(this, property, noDuration, duration, easing, startTime);
                }
            }
        }

        private void StartCoroutine<T>(
            PointDefinition<T> points,
            Action<object> set,
            float duration,
            float startTime,
            Functions easing)
            where T : struct
            => _coroutineDummy.StartCoroutine(AnimatePropertyCoroutine(points, set, duration, startTime, easing));

        private IEnumerator AnimatePropertyCoroutine<T>(PointDefinition<T> points, Action<object> set, float duration, float startTime, Functions easing)
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

        private class SettingHandler<THandled, TProperty> : ISettingHandler
            where THandled : struct
            where TProperty : struct
        {
            private readonly CapturedSetting<RenderSettings, TProperty> _capturedSetting;

            internal SettingHandler(CapturedSetting<RenderSettings, TProperty> capturedSetting)
            {
                _capturedSetting = capturedSetting;
            }

            public void Capture() => _capturedSetting.Capture();

            public void Reset() => _capturedSetting.Reset();

            public void Handle(
                SetRenderSetting instance,
                RenderSettingProperty property,
                bool noDuration,
                float duration,
                Functions easing,
                float startTime)
            {
                switch (property)
                {
                    case AnimatedRenderSettingProperty<THandled> animated when noDuration:
                        _capturedSetting.Set(animated.PointDefinition.Interpolate(1));
                        break;
                    case AnimatedRenderSettingProperty<THandled> animated:
                        instance.StartCoroutine(animated.PointDefinition, _capturedSetting.Set, duration, startTime, easing);
                        break;
                    case RenderSettingProperty<THandled> value:
                        _capturedSetting.Set(value.Value);
                        DynamicGI.UpdateEnvironment();
                        break;
                    default:
                        throw new InvalidOperationException($"Could not handle type [{property.GetType().FullName}]");
                }
            }
        }

        private class RenderEnumCapturedSetting<TEnum> : EnumCapturedSetting<RenderSettings, TEnum>
            where TEnum : struct, Enum
        {
            internal RenderEnumCapturedSetting(string property)
                : base(property)
            {
            }
        }

        private class RenderColorCapturedSetting : ColorCapturedSetting<RenderSettings>
        {
            internal RenderColorCapturedSetting(string property)
                : base(property)
            {
            }
        }

        private class RenderFloatCapturedSetting : FloatCapturedSetting<RenderSettings>
        {
            internal RenderFloatCapturedSetting(string property)
                : base(property)
            {
            }
        }

        private class RenderIntCapturedSetting : IntCapturedSetting<RenderSettings>
        {
            internal RenderIntCapturedSetting(string property)
                : base(property)
            {
            }
        }
    }
}
