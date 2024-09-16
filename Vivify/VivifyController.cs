using Heck;
using UnityEngine.SceneManagement;
using Vivify.PostProcessing;

namespace Vivify;

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
    internal const string ID_FIELD = "id";

    internal const string X_RATIO = "xRatio";
    internal const string Y_RATIO = "yRatio";
    internal const string WIDTH = "width";
    internal const string HEIGHT = "height";
    internal const string FORMAT = "colorFormat";
    internal const string FILTER = "filterMode";

    internal const string CAMERA_DEPTH_TEXTURE_MODE = "depthTextureMode";
    internal const string CAMERA_CLEAR_FLAGS = "clearFlags";
    internal const string CAMERA_BACKGROUND_COLOR = "backgroundColor";

    internal const string ASSIGN_PREFAB_LOAD_MODE = "loadMode";
    internal const string NOTE_PREFAB = "colorNotes";
    internal const string BOMB_PREFAB = "bombNotes";
    internal const string CHAIN_PREFAB = "burstSliders";
    internal const string CHAIN_ELEMENT_PREFAB = "burstSliderElements";
    internal const string DEBRIS_ASSET = "debrisAsset";
    internal const string ANY_ASSET = "anyDirectionAsset";
    internal const string SABER_PREFAB = "saber";
    internal const string SABER_TYPE = "type";
    internal const string SABER_TRAIL_ASSET = "trailAsset";
    internal const string SABER_TRAIL_TOP_POS = "trailTopPos";
    internal const string SABER_TRAIL_BOTTOM_POS = "trailBottomPos";
    internal const string SABER_TRAIL_DURATION = "trailDuration";
    internal const string SABER_TRAIL_SAMPLE_FREQ = "trailSamplingFrequency";
    internal const string SABER_TRAIL_GRANULARITY = "trailGranularity";

    internal const string RENDER_SETTINGS = "renderSettings";
    internal const string QUALITY_SETTINGS = "qualitySettings";
    internal const string XR_SETTINGS = "xrSettings";

    internal const string APPLY_POST_PROCESSING = "Blit";
    internal const string ASSIGN_OBJECT_PREFAB = "AssignObjectPrefab";
    internal const string DECLARE_CULLING_TEXTURE = "DeclareCullingTexture";
    internal const string DECLARE_TEXTURE = "DeclareRenderTexture";
    internal const string DESTROY_TEXTURE = "DestroyTexture";
    internal const string DESTROY_PREFAB = "DestroyPrefab";
    internal const string INSTANTIATE_PREFAB = "InstantiatePrefab";
    internal const string SET_MATERIAL_PROPERTY = "SetMaterialProperty";
    internal const string SET_ANIMATOR_PROPERTY = "SetAnimatorProperty";
    internal const string SET_RENDERING_SETTINGS = "SetRenderingSettings";
    internal const string SET_GLOBAL_PROPERTY = "SetGlobalProperty";
    internal const string SET_CAMERA_PROPERTY = "SetCameraProperty";

    internal const string CAMERA_TARGET = "_Main";

    internal const string ASSET_BUNDLE = "_assetBundle";
    internal const string BUNDLE = "bundle";

#if !V1_29_1
    internal const string BUNDLE_SUFFIX = "_windows2021";
#else
    internal const string BUNDLE_SUFFIX = "_windows2019";
#endif

    internal const string CAPABILITY = "Vivify";
    internal const string ID = "Vivify";
    internal const string HARMONY_ID = "aeroluna.Vivify";

    internal const int CULLING_LAYER = 22;

    internal static Capability Capability { get; } = new(CAPABILITY);

    internal static void OnActiveSceneChanged(Scene current, Scene _)
    {
        if (current.name != "GameCore")
        {
            return;
        }

        PostProcessingController.ResetMaterial();
    }
}
