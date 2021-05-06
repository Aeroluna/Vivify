namespace Vivify.HarmonyPatches
{
    using Heck;

    [HeckPatch(typeof(BeatmapObjectSpawnController))]
    [HeckPatch("Start")]
    internal static class BeatmapObjectSpawnControllerStart
    {
        internal static BeatmapObjectSpawnController BeatmapObjectSpawnController { get; private set; }

        private static void Postfix(BeatmapObjectSpawnController __instance)
        {
            BeatmapObjectSpawnController = __instance;
        }
    }
}
