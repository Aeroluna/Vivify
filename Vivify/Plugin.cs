using System.Reflection;
using Heck;
using IPA;
using IPA.Logging;
using JetBrains.Annotations;
using SongCore;
using UnityEngine.SceneManagement;
using static Vivify.VivifyController;

namespace Vivify
{
    [Plugin(RuntimeOptions.DynamicInit)]
    internal class Plugin
    {
#pragma warning disable CA1822
        [UsedImplicitly]
        [Init]
        public void Init(Logger pluginLogger)
        {
            Log.Logger = new HeckLogger(pluginLogger);
            HeckPatchDataManager.InitPatches(_harmonyInstance, Assembly.GetExecutingAssembly());
        }

        [UsedImplicitly]
        [OnEnable]
        public void OnEnable()
        {
            Collections.RegisterCapability(CAPABILITY);
            _harmonyInstanceCore.PatchAll(Assembly.GetExecutingAssembly());

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        [UsedImplicitly]
        [OnDisable]
        public void OnDisable()
        {
            Collections.DeregisterizeCapability(CAPABILITY);
            _harmonyInstanceCore.UnpatchAll(HARMONYIDCORE);
            _harmonyInstanceCore.UnpatchAll(HARMONYID);

            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }
#pragma warning restore CA1822
    }
}
