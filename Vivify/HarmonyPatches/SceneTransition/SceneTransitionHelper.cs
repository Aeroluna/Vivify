using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomJSONData;
using CustomJSONData.CustomBeatmap;
using IPA.Logging;
using static Vivify.VivifyController;

namespace Vivify.HarmonyPatches.SceneTransition
{
    internal static class SceneTransitionHelper
    {
        internal static void Patch(IDifficultyBeatmap difficultyBeatmap, IPreviewBeatmapLevel previewBeatmapLevel)
        {
            if (previewBeatmapLevel is CustomPreviewBeatmapLevel customPreviewBeatmapLevel && difficultyBeatmap.beatmapData is CustomBeatmapData customBeatmapData)
            {
                IEnumerable<string>? requirements = customBeatmapData.beatmapCustomData.Get<List<object>>("_requirements")?.Cast<string>();
                bool assRequirement = requirements?.Contains(CAPABILITY) ?? false;

                if (assRequirement)
                {
                    string path = Path.Combine(customPreviewBeatmapLevel.customLevelPath, "bundle");

                    if (File.Exists(path))
                    {
                        ToggleVivifyPatches(AssetBundleController.SetNewBundle(path));
                        return;
                    }

                    Log.Logger.Log("bundle not found!", Logger.Level.Error);
                }
            }

            ToggleVivifyPatches(false);
        }
    }
}
