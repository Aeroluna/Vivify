using System;
using System.Collections;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Animation;
using Heck.Deserialize;
using Heck.Event;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using Vivify.Extras;
using Vivify.Managers;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events;

[CustomEvent(SET_RENDERING_SETTINGS)]
internal class SetRenderingSettings : ICustomEvent, IInitializable, IDisposable
{
    private readonly IAudioTimeSource _audioTimeSource;
    private readonly IBpmController _bpmController;
    private readonly CoroutineDummy _coroutineDummy;
    private readonly DeserializedData _deserializedData;
    private readonly SiraLog _log;

    private readonly Dictionary<string, ISettingHandler> _settings = new()
    {
        {
            nameof(RenderSettings.ambientEquatorColor),
            new StructSettingHandler<RenderSettings, Vector4, Color>(
                new RenderColorCapturedSetting(nameof(RenderSettings.ambientEquatorColor)))
        },
        {
            nameof(RenderSettings.ambientGroundColor),
            new StructSettingHandler<RenderSettings, Vector4, Color>(
                new RenderColorCapturedSetting(nameof(RenderSettings.ambientGroundColor)))
        },
        {
            nameof(RenderSettings.ambientIntensity),
            new StructSettingHandler<RenderSettings, float, float>(
                new RenderFloatCapturedSetting(nameof(RenderSettings.ambientIntensity)))
        },
        {
            nameof(RenderSettings.ambientLight),
            new StructSettingHandler<RenderSettings, Vector4, Color>(
                new RenderColorCapturedSetting(nameof(RenderSettings.ambientLight)))
        },
        {
            nameof(RenderSettings.ambientMode),
            new StructSettingHandler<RenderSettings, float, AmbientMode>(
                new RenderEnumCapturedSetting<AmbientMode>(nameof(RenderSettings.ambientMode)))
        },
        {
            nameof(RenderSettings.ambientSkyColor),
            new StructSettingHandler<RenderSettings, Vector4, Color>(
                new RenderColorCapturedSetting(nameof(RenderSettings.ambientSkyColor)))
        },
        {
            nameof(RenderSettings.defaultReflectionMode),
            new StructSettingHandler<RenderSettings, float, DefaultReflectionMode>(
                new RenderEnumCapturedSetting<DefaultReflectionMode>(nameof(RenderSettings.defaultReflectionMode)))
        },
        {
            nameof(RenderSettings.defaultReflectionResolution),
            new StructSettingHandler<RenderSettings, float, int>(
                new RenderIntCapturedSetting(nameof(RenderSettings.defaultReflectionResolution)))
        },
        {
            nameof(RenderSettings.flareFadeSpeed),
            new StructSettingHandler<RenderSettings, float, float>(
                new RenderFloatCapturedSetting(nameof(RenderSettings.flareFadeSpeed)))
        },
        {
            nameof(RenderSettings.flareStrength),
            new StructSettingHandler<RenderSettings, float, float>(
                new RenderFloatCapturedSetting(nameof(RenderSettings.flareStrength)))
        },
        {
            nameof(RenderSettings.fog),
            new StructSettingHandler<RenderSettings, float, bool>(
                new RenderBoolCapturedSetting(nameof(RenderSettings.fog)))
        },
        {
            nameof(RenderSettings.fogColor),
            new StructSettingHandler<RenderSettings, Vector4, Color>(
                new RenderColorCapturedSetting(nameof(RenderSettings.fogColor)))
        },
        {
            nameof(RenderSettings.fogDensity),
            new StructSettingHandler<RenderSettings, float, float>(
                new RenderFloatCapturedSetting(nameof(RenderSettings.fogDensity)))
        },
        {
            nameof(RenderSettings.fogEndDistance),
            new StructSettingHandler<RenderSettings, float, float>(
                new RenderFloatCapturedSetting(nameof(RenderSettings.fogEndDistance)))
        },
        {
            nameof(RenderSettings.fogMode),
            new StructSettingHandler<RenderSettings, float, FogMode>(
                new RenderEnumCapturedSetting<FogMode>(nameof(RenderSettings.fogMode)))
        },
        {
            nameof(RenderSettings.fogStartDistance),
            new StructSettingHandler<RenderSettings, float, float>(
                new RenderFloatCapturedSetting(nameof(RenderSettings.fogStartDistance)))
        },
        {
            nameof(RenderSettings.haloStrength),
            new StructSettingHandler<RenderSettings, float, float>(
                new RenderFloatCapturedSetting(nameof(RenderSettings.haloStrength)))
        },
        {
            nameof(RenderSettings.reflectionBounces),
            new StructSettingHandler<RenderSettings, float, int>(
                new RenderIntCapturedSetting(nameof(RenderSettings.reflectionBounces)))
        },
        {
            nameof(RenderSettings.reflectionIntensity),
            new StructSettingHandler<RenderSettings, float, float>(
                new RenderFloatCapturedSetting(nameof(RenderSettings.reflectionIntensity)))
        },
        {
            nameof(RenderSettings.subtractiveShadowColor),
            new StructSettingHandler<RenderSettings, Vector4, Color>(
                new RenderColorCapturedSetting(nameof(RenderSettings.subtractiveShadowColor)))
        },
        {
            nameof(QualitySettings.anisotropicFiltering),
            new StructSettingHandler<QualitySettings, float, AnisotropicFiltering>(
                new QualityEnumCapturedSetting<AnisotropicFiltering>(nameof(QualitySettings.anisotropicFiltering)))
        },
        {
            nameof(QualitySettings.antiAliasing),
            new StructSettingHandler<QualitySettings, float, int>(
                new QualityIntCapturedSetting(nameof(QualitySettings.antiAliasing)))
        },
        {
            nameof(QualitySettings.pixelLightCount),
            new StructSettingHandler<QualitySettings, float, int>(
                new QualityIntCapturedSetting(nameof(QualitySettings.pixelLightCount)))
        },
        {
            nameof(QualitySettings.realtimeReflectionProbes),
            new StructSettingHandler<QualitySettings, float, bool>(
                new QualityBoolCapturedSetting(nameof(QualitySettings.realtimeReflectionProbes)))
        },
        {
            nameof(QualitySettings.shadowCascades),
            new StructSettingHandler<QualitySettings, float, int>(
                new QualityIntCapturedSetting(nameof(QualitySettings.shadowCascades)))
        },
        {
            nameof(QualitySettings.shadowDistance),
            new StructSettingHandler<QualitySettings, float, float>(
                new QualityFloatCapturedSetting(nameof(QualitySettings.shadowDistance)))
        },
        {
            nameof(QualitySettings.shadowmaskMode),
            new StructSettingHandler<QualitySettings, float, ShadowmaskMode>(
                new QualityEnumCapturedSetting<ShadowmaskMode>(nameof(QualitySettings.shadowmaskMode)))
        },
        {
            nameof(QualitySettings.shadowNearPlaneOffset),
            new StructSettingHandler<QualitySettings, float, float>(
                new QualityFloatCapturedSetting(nameof(QualitySettings.shadowNearPlaneOffset)))
        },
        {
            nameof(QualitySettings.shadowProjection),
            new StructSettingHandler<QualitySettings, float, ShadowProjection>(
                new QualityEnumCapturedSetting<ShadowProjection>(nameof(QualitySettings.shadowProjection)))
        },
        {
            nameof(QualitySettings.shadowResolution),
            new StructSettingHandler<QualitySettings, float, ShadowResolution>(
                new QualityEnumCapturedSetting<ShadowResolution>(nameof(QualitySettings.shadowResolution)))
        },
        {
            nameof(QualitySettings.shadows),
            new StructSettingHandler<QualitySettings, float, ShadowQuality>(
                new QualityEnumCapturedSetting<ShadowQuality>(nameof(QualitySettings.shadows)))
        },
        {
            nameof(QualitySettings.softParticles),
            new StructSettingHandler<QualitySettings, float, bool>(
                new QualityBoolCapturedSetting(nameof(QualitySettings.softParticles)))
        },
        {
            nameof(XRSettings.useOcclusionMesh),
            new StructSettingHandler<XRSettingsSetter, float, bool>(
                new BoolCapturedSetting<XRSettingsSetter>(nameof(XRSettings.useOcclusionMesh)))
        }
    };

    private SetRenderingSettings(
        SiraLog log,
        [Inject(Id = ID)] DeserializedData deserializedData,
        IAudioTimeSource audioTimeSource,
        IBpmController bpmController,
        CoroutineDummy coroutineDummy,
        AssetBundleManager assetBundleManager,
        PrefabManager prefabManager)
    {
        _log = log;
        _deserializedData = deserializedData;
        _audioTimeSource = audioTimeSource;
        _bpmController = bpmController;
        _coroutineDummy = coroutineDummy;
        _settings.Add(
            "skybox",
            new ClassSettingHandler<string, Material>(
                new RenderMaterialCapturedSetting(nameof(RenderSettings.skybox), assetBundleManager)));
        _settings.Add(
            "sun",
            new ClassSettingHandler<string, Light>(
                new RenderLightCapturedSetting(nameof(RenderSettings.sun), prefabManager)));
    }

    private interface ISettingHandler
    {
        public void Capture();

        public void Handle(
            SetRenderingSettings instance,
            RenderingSettingsProperty property,
            bool noDuration,
            float duration,
            Functions easing,
            float startTime);

        public void Reset();
    }

    public void Callback(CustomEventData customEventData)
    {
        if (!_deserializedData.Resolve(customEventData, out SetRenderingSettingsData? data))
        {
            return;
        }

        float duration = data.Duration;
        duration = (60f * duration) / _bpmController.currentBpm; // Convert to real time;
        List<RenderingSettingsProperty> properties = data.Properties;
        SetRenderSettings(properties, duration, data.Easing, customEventData.time);
    }

    public void Dispose()
    {
        foreach (ISettingHandler settingHandler in _settings.Values)
        {
            settingHandler.Reset();
        }
    }

    public void Initialize()
    {
        foreach (ISettingHandler settingHandler in _settings.Values)
        {
            settingHandler.Capture();
        }
    }

    internal void SetRenderSettings(
        List<RenderingSettingsProperty> properties,
        float duration,
        Functions easing,
        float startTime)
    {
        foreach (RenderingSettingsProperty property in properties)
        {
            string name = property.Name;
            _log.Debug($"Setting [{name}]");

            bool noDuration = duration == 0 || startTime + duration < _audioTimeSource.songTime;

            if (_settings.TryGetValue(name, out ISettingHandler settingHandler))
            {
                settingHandler.Handle(this, property, noDuration, duration, easing, startTime);
            }
        }
    }

    private IEnumerator AnimatePropertyCoroutine<T>(
        PointDefinition<T> points,
        Action<object> set,
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
                set(points.Interpolate(time));

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
        Action<object> set,
        float duration,
        float startTime,
        Functions easing)
        where T : struct
    {
        _coroutineDummy.StartCoroutine(AnimatePropertyCoroutine(points, set, duration, startTime, easing));
    }

    private class StructSettingHandler<TSettings, THandled, TProperty> : ISettingHandler
        where TSettings : class
        where THandled : struct
    {
        private readonly CapturedSettings<TSettings, TProperty> _capturedSetting;

        internal StructSettingHandler(CapturedSettings<TSettings, TProperty> capturedSetting)
        {
            _capturedSetting = capturedSetting;
        }

        public void Capture()
        {
            _capturedSetting.Capture();
        }

        public void Handle(
            SetRenderingSettings instance,
            RenderingSettingsProperty property,
            bool noDuration,
            float duration,
            Functions easing,
            float startTime)
        {
            switch (property)
            {
                case AnimatedRenderingSettingsProperty<THandled> animated when noDuration:
                    _capturedSetting.Set(animated.PointDefinition.Interpolate(1));
                    break;
                case AnimatedRenderingSettingsProperty<THandled> animated:
                    instance.StartCoroutine(
                        animated.PointDefinition,
                        _capturedSetting.Set,
                        duration,
                        startTime,
                        easing);
                    break;
                case RenderingSettingsProperty<THandled> value:
                    _capturedSetting.Set(value.Value);
                    DynamicGI.UpdateEnvironment();
                    break;

                default:
                    throw new InvalidOperationException($"Could not handle type [{property.GetType().FullName}].");
            }
        }

        public void Reset()
        {
            _capturedSetting.Reset();
        }
    }

    private class ClassSettingHandler<THandled, TProperty> : ISettingHandler
        where THandled : class
    {
        private readonly CapturedSettings<RenderSettings, TProperty> _capturedSetting;

        internal ClassSettingHandler(CapturedSettings<RenderSettings, TProperty> capturedSetting)
        {
            _capturedSetting = capturedSetting;
        }

        public void Capture()
        {
            _capturedSetting.Capture();
        }

        public void Handle(
            SetRenderingSettings instance,
            RenderingSettingsProperty property,
            bool noDuration,
            float duration,
            Functions easing,
            float startTime)
        {
            switch (property)
            {
                case RenderingSettingsProperty<THandled> value:
                    _capturedSetting.Set(value.Value);
                    DynamicGI.UpdateEnvironment();
                    break;

                default:
                    throw new InvalidOperationException($"Could not handle type [{property.GetType().FullName}].");
            }
        }

        public void Reset()
        {
            _capturedSetting.Reset();
        }
    }
}
