using Heck;
using UnityEngine.SceneManagement;
using Vivify.PostProcessing;

namespace Vivify
{
    internal enum PatchType
    {
        Features
    }

    public static class VivifyController
    {
        internal const string VALUE = "value";
        internal const string PASS = "pass";
        internal const string PRIORITY = "priority";
        internal const string TARGET = "target";
        internal const string ASSET = "asset";
        internal const string PROPERTIES = "properties";
        internal const string WHITELIST = "whitelist";
        internal const string DEPTH_TEXTURE = "depthTexture";
        internal const string PREFAB_ID = "id";

        internal const string XRATIO = "xRatio";
        internal const string YRATIO = "yRatio";
        internal const string WIDTH = "width";
        internal const string HEIGHT = "height";

        internal const string APPLY_POST_PROCESSING = "ApplyPostProcessing";
        internal const string DECLARE_CULLING_MASK = "DeclareCullingMask";
        internal const string DECLARE_TEXTURE = "DeclareRenderTexture";
        internal const string DESTROY_PREFAB = "DestroyPrefab";
        internal const string INSTANTIATE_PREFAB = "InstantiatePrefab";
        internal const string SET_MATERIAL_PROPERTY = "SetMaterialProperty";

        internal const string CAMERA_TARGET = "_Main";

        internal const string CAPABILITY = "Vivify";
        internal const string ID = "Vivify";
        internal const string HARMONY_ID = "aeroluna.Vivify";

        internal const int CULLINGLAYER = 22;

        internal static HeckPatcher CorePatcher { get; } = new(HARMONY_ID + "Core");

        internal static HeckPatcher FeaturesPatcher { get; } = new(HARMONY_ID + "Features", PatchType.Features);

        internal static DataDeserializer Deserializer { get; } = DeserializerManager.Register<CustomDataManager>(ID);

        internal static Module FeaturesModule { get; } = ModuleManager.Register<ModuleCallbacks>(
            "Vivify",
            2,
            RequirementType.Condition,
            null,
            new[] { "Heck" });

        internal static void OnActiveSceneChanged(Scene current, Scene _)
        {
            if (current.name != "GameCore")
            {
                return;
            }

            PostProcessingController.ResetMaterial();
            AssetBundleController.ClearBundle();
        }
    }
}
