using Heck;
using IPA;
using IPA.Config.Stores;
using IPA.Logging;
using JetBrains.Annotations;
using SiraUtil.Zenject;
using SongCore;
using UnityEngine.SceneManagement;
using Vivify.Installers;
using static Vivify.VivifyController;

namespace Vivify
{
    [Plugin(RuntimeOptions.DynamicInit)]
    internal class Plugin
    {
        [UsedImplicitly]
        [Init]
        public Plugin(Logger pluginLogger, IPA.Config.Config conf, Zenjector zenjector)
        {
            Log.Logger = new HeckLogger(pluginLogger);

            zenjector.Install<VivifyAppInstaller>(Location.App, conf.Generated<Config>());
            zenjector.Install<VivifyPlayerInstaller>(Location.Player);
            zenjector.Install<VivifyMenuInstaller>(Location.Menu);
        }

#pragma warning disable CA1822
        [UsedImplicitly]
        [OnEnable]
        public void OnEnable()
        {
            Collections.RegisterCapability(CAPABILITY);
            CorePatcher.Enabled = true;
            FeaturesModule.Enabled = true;

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        [UsedImplicitly]
        [OnDisable]
        public void OnDisable()
        {
            Collections.DeregisterizeCapability(CAPABILITY);
            CorePatcher.Enabled = false;
            FeaturesPatcher.Enabled = false;
            FeaturesModule.Enabled = false;

            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }
#pragma warning restore CA1822
    }
}
