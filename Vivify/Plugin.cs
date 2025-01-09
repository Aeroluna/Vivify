using Heck.Patcher;
using IPA;
using IPA.Config.Stores;
using IPA.Logging;
using JetBrains.Annotations;
using SiraUtil.Zenject;
using Vivify.Installers;
using static Vivify.VivifyController;

namespace Vivify;

[Plugin(RuntimeOptions.DynamicInit)]
internal class Plugin
{
    [UsedImplicitly]
    [Init]
    public Plugin(Logger pluginLogger, IPA.Config.Config conf, Zenjector zenjector)
    {
        Log = pluginLogger;

        Config config = conf.Generated<Config>();
        Config = config;
        zenjector.Install<VivifyAppInstaller>(Location.App, config);
        zenjector.Install<VivifyPlayerInstaller>(Location.Player);
        zenjector.Install<VivifyMenuInstaller>(Location.Menu);
        zenjector.UseLogger(pluginLogger);

        HeckPatchManager.Register(HARMONY_ID);
    }

    internal static Config Config { get; private set; } = null!;

    internal static Logger Log { get; private set; } = null!;

#pragma warning disable CA1822
    [UsedImplicitly]
    [OnEnable]
    public void OnEnable()
    {
        Capability.Register();
    }

    [UsedImplicitly]
    [OnDisable]
    public void OnDisable()
    {
        Capability.Deregister();
    }
#pragma warning restore CA1822
}
