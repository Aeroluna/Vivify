namespace Vivify.HarmonyPatches
{
    using System;
    using HarmonyLib;

    [HarmonyPatch(
        typeof(StandardLevelScenesTransitionSetupDataSO),
        new Type[] { typeof(string), typeof(IDifficultyBeatmap), typeof(IPreviewBeatmapLevel), typeof(OverrideEnvironmentSettings), typeof(ColorScheme), typeof(GameplayModifiers), typeof(PlayerSpecificSettings), typeof(PracticeSettings), typeof(string), typeof(bool) })]
    [HarmonyPatch("Init")]
    internal static class StandardLevelScenesTransitionSetupDataSOInit
    {
        private static void Postfix(IDifficultyBeatmap difficultyBeatmap, IPreviewBeatmapLevel previewBeatmapLevel)
        {
            SceneTransitionHelper.Patch(difficultyBeatmap, previewBeatmapLevel);
        }
    }
}
