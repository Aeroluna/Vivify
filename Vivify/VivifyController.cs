namespace Vivify
{
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
                    CustomJSONData.CustomEventCallbackController.didInitEvent += Events.EventController.CustomEventCallbackInit;
                }
                else
                {
                    CustomJSONData.CustomEventCallbackController.didInitEvent -= Events.EventController.CustomEventCallbackInit;
                }
            }
        }

        internal static void OnActiveSceneChanged(Scene current, Scene _)
        {
            if (current.name == "GameCore")
            {
                PostProcessingController.ResetMaterial();
                AssetBundleController.ClearBundle();
                ToggleVivifyPatches(false);
            }
        }
    }
}
