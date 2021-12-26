using Heck;
using JetBrains.Annotations;

namespace Vivify.HarmonyPatches
{
    [HeckPatch(typeof(BeatmapObjectSpawnController))]
    [HeckPatch("Start")]
    internal static class BeatmapObjectSpawnControllerStart
    {
        internal static BeatmapObjectSpawnController BeatmapObjectSpawnController { get; private set; } = null!;

        [UsedImplicitly]
        private static void Postfix(BeatmapObjectSpawnController __instance)
        {
            BeatmapObjectSpawnController = __instance;
        }
    }
}
