using CustomJSONData;
using HarmonyLib;
using Heck;
using UnityEngine.SceneManagement;
using Vivify.Events;
using Vivify.PostProcessing;

namespace Vivify
{
    public static class VivifyController
    {
        internal const string CAPABILITY = "Vivify";
        internal const string HARMONYIDCORE = "com.aeroluna.BeatSaber.VivifyCore";
        internal const string HARMONYID = "com.aeroluna.BeatSaber.Vivify";

        internal const int CULLINGLAYER = 22;

        internal static readonly Harmony _harmonyInstanceCore = new(HARMONYIDCORE);
        internal static readonly Harmony _harmonyInstance = new(HARMONYID);

        internal static bool VivifyActive { get; private set; }

        public static void ToggleVivifyPatches(bool value)
        {
            if (value == VivifyActive)
            {
                return;
            }

            HeckPatchDataManager.TogglePatches(_harmonyInstance, value);

            VivifyActive = value;
            if (VivifyActive)
            {
                CustomEventCallbackController.didInitEvent += EventController.CustomEventCallbackInit;
            }
            else
            {
                CustomEventCallbackController.didInitEvent -= EventController.CustomEventCallbackInit;
            }
        }

        internal static void OnActiveSceneChanged(Scene current, Scene _)
        {
            if (current.name != "GameCore")
            {
                return;
            }

            PostProcessingController.ResetMaterial();
            AssetBundleController.ClearBundle();
            ToggleVivifyPatches(false);
        }
    }
}
