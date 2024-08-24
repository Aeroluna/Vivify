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
using UnityEngine.Rendering;
using Vivify.Managers;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events;

[CustomEvent(SET_RENDER_SETTING)]
internal class SetRenderSetting : ICustomEvent, IInitializable, IDisposable
{
    private readonly IAudioTimeSource _audioTimeSource;
    private readonly IBpmController _bpmController;
    private readonly CoroutineDummy _coroutineDummy;
    private readonly SiraLog _log;
    private readonly DeserializedData _deserializedData;

    private readonly Dictionary<string, ISettingHandler> _settings = new()
    {
        {
            "ambientEquatorColor",
            new StructSettingHandler<Vector4, Color>(
                new RenderColorCapturedSetting(nameof(RenderSettings.ambientEquatorColor)))
        },
        {
            "ambientGroundColor",
            new StructSettingHandler<Vector4, Color>(
                new RenderColorCapturedSetting(nameof(RenderSettings.ambientEquatorColor)))
        },
        {
            "ambientIntensity",
            new StructSettingHandler<float, float>(
                new RenderFloatCapturedSetting(nameof(RenderSettings.ambientIntensity)))
        },
        {
            "ambientLight",
            new StructSettingHandler<Vector4, Color>(
                new RenderColorCapturedSetting(nameof(RenderSettings.ambientLight)))
        },
        {
            "ambientMode",
            new StructSettingHandler<float, AmbientMode>(
                new RenderEnumCapturedSetting<AmbientMode>(nameof(RenderSettings.ambientMode)))
        },
        {
            "ambientSkyColor",
            new StructSettingHandler<Vector4, Color>(
                new RenderColorCapturedSetting(nameof(RenderSettings.ambientSkyColor)))
        },
        {
            "defaultReflectionMode",
            new StructSettingHandler<float, DefaultReflectionMode>(
                new RenderEnumCapturedSetting<DefaultReflectionMode>(nameof(RenderSettings.defaultReflectionMode)))
        },
        {
            "defaultReflectionResolution",
            new StructSettingHandler<float, int>(
                new RenderIntCapturedSetting(nameof(RenderSettings.defaultReflectionResolution)))
        },
        {
            "flareFadeSpeed",
            new StructSettingHandler<float, float>(
                new RenderFloatCapturedSetting(nameof(RenderSettings.flareFadeSpeed)))
        },
        {
            "flareStrength",
            new StructSettingHandler<float, float>(new RenderFloatCapturedSetting(nameof(RenderSettings.flareStrength)))
        },
        {
            "fog",
            new StructSettingHandler<float, bool>(
                new CapturedSetting<RenderSettings, bool>(nameof(RenderSettings.fog), n => Convert.ToBoolean((int)n)))
        },
        {
            "fogColor",
            new StructSettingHandler<Vector4, Color>(new RenderColorCapturedSetting(nameof(RenderSettings.fogColor)))
        },
        {
            "fogDensity",
            new StructSettingHandler<float, float>(new RenderFloatCapturedSetting(nameof(RenderSettings.fogDensity)))
        },
        {
            "fogEndDistance",
            new StructSettingHandler<float, float>(
                new RenderFloatCapturedSetting(nameof(RenderSettings.fogEndDistance)))
        },
        {
            "fogMode",
            new StructSettingHandler<float, FogMode>(
                new RenderEnumCapturedSetting<FogMode>(nameof(RenderSettings.fogMode)))
        },
        {
            "fogStartDistance",
            new StructSettingHandler<float, float>(
                new RenderFloatCapturedSetting(nameof(RenderSettings.fogStartDistance)))
        },
        {
            "haloStrength",
            new StructSettingHandler<float, float>(new RenderFloatCapturedSetting(nameof(RenderSettings.haloStrength)))
        },
        {
            "reflectionBounces",
            new StructSettingHandler<float, int>(new RenderIntCapturedSetting(nameof(RenderSettings.reflectionBounces)))
        },
        {
            "reflectionIntensity",
            new StructSettingHandler<float, float>(
                new RenderFloatCapturedSetting(nameof(RenderSettings.reflectionIntensity)))
        },
        {
            "subtractiveShadowColor",
            new StructSettingHandler<Vector4, Color>(
                new RenderColorCapturedSetting(nameof(RenderSettings.subtractiveShadowColor)))
        }
    };

    private SetRenderSetting(
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
            SetRenderSetting instance,
            RenderSettingProperty property,
            bool noDuration,
            float duration,
            Functions easing,
            float startTime);

        public void Reset();
    }

    public void Callback(CustomEventData customEventData)
    {
        if (!_deserializedData.Resolve(customEventData, out SetRenderSettingData? data))
        {
            return;
        }

        float duration = data.Duration;
        duration = (60f * duration) / _bpmController.currentBpm; // Convert to real time;
        List<RenderSettingProperty> properties = data.Properties;
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
        List<RenderSettingProperty> properties,
        float duration,
        Functions easing,
        float startTime)
    {
        foreach (RenderSettingProperty property in properties)
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

    private class StructSettingHandler<THandled, TProperty> : ISettingHandler
        where THandled : struct
    {
        private readonly CapturedSetting<RenderSettings, TProperty> _capturedSetting;

        internal StructSettingHandler(CapturedSetting<RenderSettings, TProperty> capturedSetting)
        {
            _capturedSetting = capturedSetting;
        }

        public void Capture()
        {
            _capturedSetting.Capture();
        }

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
                    instance.StartCoroutine(
                        animated.PointDefinition,
                        _capturedSetting.Set,
                        duration,
                        startTime,
                        easing);
                    break;
                case RenderSettingProperty<THandled> value:
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
        private readonly CapturedSetting<RenderSettings, TProperty> _capturedSetting;

        internal ClassSettingHandler(CapturedSetting<RenderSettings, TProperty> capturedSetting)
        {
            _capturedSetting = capturedSetting;
        }

        public void Capture()
        {
            _capturedSetting.Capture();
        }

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
                case RenderSettingProperty<THandled> value:
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

    private class RenderEnumCapturedSetting<TEnum> : CapturedSetting<RenderSettings, TEnum>
        where TEnum : struct, Enum
    {
        internal RenderEnumCapturedSetting(string property)
            : base(property, Convert)
        {
        }

        private static TEnum Convert(object obj)
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), (int)(float)obj);
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

    private class RenderMaterialCapturedSetting : CapturedSetting<RenderSettings, Material>
    {
        internal RenderMaterialCapturedSetting(string property, AssetBundleManager assetBundleManager)
            : base(
                property,
                obj => assetBundleManager.TryGetAsset((string)obj, out Material? material) ? material : null)
        {
        }
    }

    private class RenderLightCapturedSetting : CapturedSetting<RenderSettings, Light>
    {
        internal RenderLightCapturedSetting(string property, PrefabManager prefabManager)
            : base(
                property,
                obj => prefabManager.TryGetPrefab((string)obj, out InstantiatedPrefab? prefab)
                    ? prefab.GameObject.GetComponents<Light>().FirstOrDefault(n => n.type == LightType.Directional)
                    : null)
        {
        }
    }
}
