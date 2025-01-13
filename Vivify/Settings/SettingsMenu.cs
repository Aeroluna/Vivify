using System;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using JetBrains.Annotations;
using Zenject;

namespace Vivify.Settings;

internal class SettingsMenu : IInitializable, IDisposable
{
    private readonly Config _config;
    private readonly BSMLSettings _bsmlSettings;

    [UsedImplicitly]
    [UIValue("ints")]
    private readonly List<object> _intChoices =
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20
    ];

    // i wish nico backported the bsml updates :(
    private SettingsMenu(
#if !V1_29_1
        BSMLSettings bsmlSettings,
#endif
        Config config)
    {
        _config = config;
#if !V1_29_1
        _bsmlSettings = bsmlSettings;
#else
        _bsmlSettings = BSMLSettings.instance;
#endif
    }

    [UsedImplicitly]
    [UIValue("max-camera2-cams")]
    public int MaxCamera2Cams
    {
        get => _config.MaxCamera2Cams;
        set => _config.MaxCamera2Cams = value;
    }

    public void Initialize()
    {
        _bsmlSettings.AddSettingsMenu("Vivify", "Vivify.Resources.Settings.bsml", this);
    }

    public void Dispose()
    {
        _bsmlSettings.RemoveSettingsMenu(this);
    }
}
