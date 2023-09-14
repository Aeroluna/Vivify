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
        internal const string SOURCE = "source";
        internal const string DESTINATION = "destination";
        internal const string ASSET = "asset";
        internal const string PROPERTIES = "properties";
        internal const string WHITELIST = "whitelist";
        internal const string DEPTH_TEXTURE = "depthTexture";
        internal const string PREFAB_ID = "id";

        internal const string XRATIO = "xRatio";
        internal const string YRATIO = "yRatio";
        internal const string WIDTH = "width";
        internal const string HEIGHT = "height";
        internal const string FORMAT = "colorFormat";
        internal const string FILTER = "filterMode";

        internal const string CAMERA_DEPTH_TEXTURE_MODE = "depthTextureMode";

        internal const string NOTE_PREFAB = "note";

        internal const string APPLY_POST_PROCESSING = "Blit";
        internal const string ASSIGN_TRACK_PREFAB = "AssignTrackPrefab";
        internal const string DECLARE_CULLING_TEXTURE = "DeclareCullingTexture";
        internal const string DECLARE_TEXTURE = "DeclareRenderTexture";
        internal const string DESTROY_TEXTURE = "DestroyTexture";
        internal const string DESTROY_PREFAB = "DestroyPrefab";
        internal const string INSTANTIATE_PREFAB = "InstantiatePrefab";
        internal const string SET_MATERIAL_PROPERTY = "SetMaterialProperty";
        internal const string SET_ANIMATOR_PROPERTY = "SetAnimatorProperty";
        internal const string SET_RENDER_SETTING = "SetRenderSetting";
        internal const string SET_GLOBAL_PROPERTY = "SetGlobalProperty";
        internal const string SET_CAMERA_PROPERTY = "SetCameraProperty";

        internal const string CAMERA_TARGET = "_Main";

        internal const string ASSET_BUNDLE = "_assetBundle";
        internal const string BUNDLE = "bundle";

        internal const string CAPABILITY = "Vivify";
        internal const string ID = "Vivify";
        internal const string HARMONY_ID = "aeroluna.Vivify";

        internal const int CULLINGLAYER = 22;

        internal static HeckPatcher CorePatcher { get; } = new(HARMONY_ID + "Core");

        internal static HeckPatcher FeaturesPatcher { get; } = new(HARMONY_ID + "Features", PatchType.Features);

        internal static DataDeserializer Deserializer { get; } = DeserializerManager.Register<CustomDataManager>(ID);

        internal static Module FeaturesModule { get; } = ModuleManager.Register<ModuleCallbacks>(
            CAPABILITY,
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
        }
    }
}
