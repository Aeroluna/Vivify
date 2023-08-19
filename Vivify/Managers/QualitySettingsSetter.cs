using System;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using IPA.Utilities;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace Vivify.Managers
{
    internal class QualitySettingsManager : IInitializable, IDisposable
    {
        private readonly Dictionary<string, QualitySetting> _settings = new()
        {
            { "_anisotropicFiltering", new QualitySetting(() => QualitySettings.anisotropicFiltering, n => QualitySettings.anisotropicFiltering = (AnisotropicFiltering)n) },
            { "_antiAliasing", new QualitySetting(() => QualitySettings.antiAliasing, n => QualitySettings.antiAliasing = (int)n) },
            { "_pixelLightCount", new QualitySetting(() => QualitySettings.pixelLightCount, n => QualitySettings.pixelLightCount = (int)n) },
            { "_realtimeReflectionProbes", new QualitySetting(() => QualitySettings.realtimeReflectionProbes, n => QualitySettings.realtimeReflectionProbes = (bool)n) },
            { "_shadowCascades", new QualitySetting(() => QualitySettings.shadowCascades, n => QualitySettings.shadowCascades = (int)n) },
            { "_shadowDistance", new QualitySetting(() => QualitySettings.shadowDistance, n => QualitySettings.shadowDistance = (float)n) },
            { "_shadowResolution", new QualitySetting(() => QualitySettings.shadowResolution, n => QualitySettings.shadowResolution = (ShadowResolution)n) },
            { "_shadows", new QualitySetting(() => QualitySettings.shadows, n => QualitySettings.shadows = (ShadowQuality)n) },
            { "_softParticles", new QualitySetting(() => QualitySettings.softParticles, n => QualitySettings.softParticles = (bool)n) },
        };

        private readonly IReadonlyBeatmapData _beatmapData;

        [UsedImplicitly]
        private QualitySettingsManager(IReadonlyBeatmapData beatmapData)
        {
            _beatmapData = beatmapData;
        }

        public void Initialize()
        {
            _settings.Values.Do(n => n.Capture());

            if (_beatmapData is not CustomBeatmapData customBeatmapData)
            {
                return;
            }

            CustomData? settings = customBeatmapData.beatmapCustomData.Get<CustomData>("_qualitySettings");
            if (settings == null)
            {
                return;
            }

            foreach ((string? key, object? value) in settings)
            {
                if (key == null || value == null)
                {
                    continue;
                }

                if (!_settings.TryGetValue(key, out QualitySetting? setting))
                {
                    continue;
                }

                Log.Logger.Log($"Set [{key}] to [{value}].");
                setting.Set(value);
            }
        }

        public void Dispose()
        {
            _settings.Values.Do(n => n.Reset());
        }

        private class QualitySetting
        {
            private readonly Func<object> _get;
            private readonly Action<object> _set;

            private object _original;

            internal QualitySetting(Func<object> get, Action<object> set)
            {
                _get = get;
                _set = set;
                _original = _get();
            }

            public void Capture() => _original = _get();

            public void Reset() => _set(_original);

            internal void Set(object value) => _set(value);
        }
    }
}
