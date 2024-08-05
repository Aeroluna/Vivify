using System;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using HarmonyLib;
using IPA.Utilities;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

namespace Vivify.Managers;

internal class QualitySettingsManager : IInitializable, IDisposable
{
    private readonly IReadonlyBeatmapData _beatmapData;

    private readonly SiraLog _log;

    private readonly Dictionary<string, ICapturedSetting> _settings = new()
    {
        {
            "_anisotropicFiltering",
            new QualityEnumCapturedSetting<AnisotropicFiltering>(nameof(QualitySettings.anisotropicFiltering))
        },
        { "_antiAliasing", new QualityIntCapturedSetting(nameof(QualitySettings.antiAliasing)) },
        { "_pixelLightCount", new QualityIntCapturedSetting(nameof(QualitySettings.pixelLightCount)) },
        {
            "_realtimeReflectionProbes",
            new QualityBoolCapturedSetting(nameof(QualitySettings.realtimeReflectionProbes))
        },
        { "_shadowCascades", new QualityIntCapturedSetting(nameof(QualitySettings.shadowCascades)) },
        { "_shadowDistance", new QualityFloatCapturedSetting(nameof(QualitySettings.shadowDistance)) },
        { "_shadowmaskMode", new QualityEnumCapturedSetting<ShadowmaskMode>(nameof(QualitySettings.shadowmaskMode)) },
        { "_shadowNearPlaneOffset", new QualityFloatCapturedSetting(nameof(QualitySettings.shadowNearPlaneOffset)) },
        {
            "_shadowProjection",
            new QualityEnumCapturedSetting<ShadowProjection>(nameof(QualitySettings.shadowProjection))
        },
        {
            "_shadowResolution",
            new QualityEnumCapturedSetting<ShadowResolution>(nameof(QualitySettings.shadowResolution))
        },
        { "_shadows", new QualityEnumCapturedSetting<ShadowQuality>(nameof(QualitySettings.shadows)) },
        { "_softParticles", new QualityBoolCapturedSetting(nameof(QualitySettings.softParticles)) }
    };

    [UsedImplicitly]
    private QualitySettingsManager(SiraLog log, IReadonlyBeatmapData beatmapData)
    {
        _log = log;
        _beatmapData = beatmapData;
    }

    public void Dispose()
    {
        _settings.Values.Do(n => n.Reset());
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

            if (!_settings.TryGetValue(key, out ICapturedSetting? setting))
            {
                continue;
            }

            _log.Debug($"Set [{key}] to [{value}]");
            setting.Set(value);
        }
    }

    private class QualityEnumCapturedSetting<TEnum> : EnumCapturedSetting<QualitySettings, TEnum>
        where TEnum : struct, Enum
    {
        internal QualityEnumCapturedSetting(string property)
            : base(property)
        {
        }
    }

    private class QualityIntCapturedSetting : IntCapturedSetting<QualitySettings>
    {
        internal QualityIntCapturedSetting(string property)
            : base(property)
        {
        }
    }

    private class QualityFloatCapturedSetting : FloatCapturedSetting<QualitySettings>
    {
        internal QualityFloatCapturedSetting(string property)
            : base(property)
        {
        }
    }

    private class QualityBoolCapturedSetting : BoolCapturedSetting<QualitySettings>
    {
        internal QualityBoolCapturedSetting(string property)
            : base(property)
        {
        }
    }
}
