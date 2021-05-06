namespace Vivify
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using HarmonyLib;
    using UnityEngine.SceneManagement;
    using static Vivify.Plugin;

    public static class VivifyController
    {
        internal static bool VivifyActive { get; private set; } = false;

        public static void ToggleVivifyPatches(bool value)
        {
            if (value != VivifyActive)
            {
                Heck.HeckData.TogglePatches(_harmonyInstance, value);

                VivifyActive = value;
                if (VivifyActive)
                {
                    CustomJSONData.CustomEventCallbackController.customEventCallbackControllerInit += Events.EventController.CustomEventCallbackInit;
                }
                else
                {
                    CustomJSONData.CustomEventCallbackController.customEventCallbackControllerInit -= Events.EventController.CustomEventCallbackInit;
                }
            }
        }

        internal static void OnActiveSceneChanged(Scene current, Scene _)
        {
            if (current.name == "GameCore")
            {
                Events.AssetBundleController.ClearBundle();
                ToggleVivifyPatches(false);
            }
        }
    }
}
