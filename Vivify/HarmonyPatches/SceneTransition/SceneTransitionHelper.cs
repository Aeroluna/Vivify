namespace Vivify.HarmonyPatches
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;
    using UnityEngine;
    using Vivify.Events;
    using static Vivify.Plugin;

    internal static class SceneTransitionHelper
    {
        internal static void Patch(IDifficultyBeatmap difficultyBeatmap, IPreviewBeatmapLevel previewBeatmapLevel)
        {
            if (previewBeatmapLevel is CustomPreviewBeatmapLevel customPreviewBeatmapLevel && difficultyBeatmap.beatmapData is CustomBeatmapData customBeatmapData)
            {
                IEnumerable<string> requirements = ((List<object>)Trees.at(customBeatmapData.beatmapCustomData, "_requirements"))?.Cast<string>();
                bool assRequirement = requirements?.Contains(CAPABILITY) ?? false;

                if (assRequirement)
                {
                    string path = Path.Combine(customPreviewBeatmapLevel.customLevelPath, "bundle");

                    if (File.Exists(path))
                    {
                        VivifyController.ToggleVivifyPatches(AssetBundleController.SetNewBundle(path));
                        return;
                    }
                    else
                    {
                        Plugin.Logger.Log("bundle not found!", IPA.Logging.Logger.Level.Error);
                    }
                }
            }

            VivifyController.ToggleVivifyPatches(false);
        }
    }
}
