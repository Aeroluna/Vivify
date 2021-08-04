namespace Vivify.HarmonyPatches
{
    using Heck;

    [HeckPatch(typeof(GameNoteController))]
    [HeckPatch("Init")]
    internal static class GameNoteControllerInit
    {
        private static void Postfix(GameNoteController __instance)
        {
            if (__instance.gameObject.GetComponent<MaskRenderer>() == null && PostProcessingController.Masks.TryGetValue("_Notes", out PostProcessingController.MaskController controller))
            {
                controller.AddMaskRenderer(__instance.gameObject.AddComponent<MaskRenderer>());
            }
        }
    }
}
