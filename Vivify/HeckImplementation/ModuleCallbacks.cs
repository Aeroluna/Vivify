using System.IO;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using Heck;
using IPA.Logging;
using static Vivify.VivifyController;

namespace Vivify
{
    internal class ModuleCallbacks
    {
        [ModuleCondition]
        private static bool Condition(
            Capabilities capabilities,
            IDifficultyBeatmap difficultyBeatmap,
            IPreviewBeatmapLevel previewBeatmapLevel)
        {
            if (!capabilities.Requirements.Contains(CAPABILITY)
                || previewBeatmapLevel is not CustomPreviewBeatmapLevel customPreviewBeatmapLevel
                || difficultyBeatmap is not CustomDifficultyBeatmap { beatmapSaveData: CustomBeatmapSaveData { version2_6_0AndEarlier: false } })
            {
                return false;
            }

            string path = Path.Combine(customPreviewBeatmapLevel.customLevelPath, "bundle");

            if (File.Exists(path))
            {
                return AssetBundleController.SetNewBundle(path);
            }

            Log.Logger.Log("bundle not found!", Logger.Level.Error);
            return false;
        }

        [ModuleCallback]
        private static void Toggle(bool value)
        {
            FeaturesPatcher.Enabled = value;
            Deserializer.Enabled = value;
        }
    }
}
