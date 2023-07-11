using BepInEx;
using BepInEx.Logging;
using SiraUtil.Zenject;
using SongCore;
using UnityEngine.SceneManagement;
using Vivify.Installers;
using Vivify.Managers;
using static Vivify.VivifyController;

namespace Vivify
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("BeatSaberMarkupLanguage")]
    [BepInDependency("BSIPA_Utilities")]
    [BepInDependency("CustomJSONData")]
    [BepInDependency("SiraUtil")]
    [BepInDependency("Heck")]
    [BepInProcess("Beat Saber.exe")]
    internal class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; } = null!;

        private void Awake()
        {
            Log = Logger;

            DepthShaderManager.LoadFromMemory();
            Zenjector zenjector = Zenjector.ConstructZenjector(Info);
            zenjector.Install<VivifyAppInstaller>(Location.App, new Config(Config));
            zenjector.Install<VivifyPlayerInstaller>(Location.Player);
            zenjector.Install<VivifyMenuInstaller>(Location.Menu);
        }

#pragma warning disable CA1822
        private void OnEnable()
        {
            Collections.RegisterCapability(CAPABILITY);
            CorePatcher.Enabled = true;
            FeaturesModule.Enabled = true;

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDisable()
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
