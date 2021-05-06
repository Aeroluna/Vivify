namespace Vivify
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using HarmonyLib;
    using IPA;
    using IPA.Config;
    using IPA.Config.Stores;
    using Heck;
    using UnityEngine.SceneManagement;
    using UnityEngine;
    using IPALogger = IPA.Logging.Logger;

    [Plugin(RuntimeOptions.DynamicInit)]
    internal class Plugin
    {
        internal const string CAPABILITY = "Vivify";
        internal const string HARMONYIDCORE = "com.aeroluna.BeatSaber.VivifyCore";
        internal const string HARMONYID = "com.aeroluna.BeatSaber.Vivify";

        internal static readonly Harmony _harmonyInstanceCore = new Harmony(HARMONYIDCORE);
        internal static readonly Harmony _harmonyInstance = new Harmony(HARMONYID);

        internal static HeckLogger Logger { get; private set; }

        [Init]
        public void Init(IPALogger pluginLogger)
        {
            Logger = new HeckLogger(pluginLogger);
            HeckData.InitPatches(_harmonyInstance, Assembly.GetExecutingAssembly());
        }

        [OnEnable]
        public void OnEnable()
        {
            SongCore.Collections.RegisterCapability(CAPABILITY);
            _harmonyInstanceCore.PatchAll(Assembly.GetExecutingAssembly());

            SceneManager.activeSceneChanged += VivifyController.OnActiveSceneChanged;
        }

        [OnDisable]
        public void OnDisable()
        {
            SongCore.Collections.DeregisterizeCapability(CAPABILITY);
            _harmonyInstanceCore.UnpatchAll(HARMONYIDCORE);
            _harmonyInstanceCore.UnpatchAll(HARMONYID);

            SceneManager.activeSceneChanged -= VivifyController.OnActiveSceneChanged;
        }
    }
}
