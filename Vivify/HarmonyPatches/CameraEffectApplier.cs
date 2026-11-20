using System;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using Heck.ReLoad;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using Vivify.PostProcessing;
using Zenject;

namespace Vivify.HarmonyPatches;

internal class CameraEffectApplier : IAffinity, IDisposable
{
    private readonly Dictionary<MainEffectController, PostProcessingController> _postProcessingControllers = new();

    private readonly ReLoader? _reLoader;
    private readonly int _prewarmCount;

    [UsedImplicitly]
    private CameraEffectApplier(
        IReadonlyBeatmapData beatmapData,
        [InjectOptional] ReLoader? reLoader)
    {
        _reLoader = reLoader;
        if (reLoader != null)
        {
            reLoader.Rewinded += OnRewind;
        }

        // TODO: correctly count concurrent created cameras
        _prewarmCount = ((CustomBeatmapData)beatmapData).customEventDatas.Any(
            n => n.eventType == VivifyController.DECLARE_CULLING_TEXTURE)
            ? 1
            : 0;

        foreach (PostProcessingOrder key in Enum.GetValues(typeof(PostProcessingOrder)))
        {
            if (!Effects.ContainsKey(key))
            {
                Effects[key] = [];
            }
        }
    }

    internal Dictionary<string, CreateCameraData> CameraDatas { get; } = new();

    internal Dictionary<string, CreateScreenTextureData> DeclaredTextureDatas { get; } = new();

    internal Dictionary<PostProcessingOrder, List<MaterialData>> Effects { get; } = new();

    public void Dispose()
    {
        Reset();

        if (_reLoader != null)
        {
            _reLoader.Rewinded -= OnRewind;
        }
    }

    private void OnRewind()
    {
        Reset();
    }

    private void Reset()
    {
        CameraDatas.Clear();
        DeclaredTextureDatas.Clear();

        foreach (List<MaterialData>? materials in Effects.Values)
        {
            materials.Clear();
        }
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(MainEffectController), nameof(MainEffectController.OnPreRender))]
    private void ApplyVivifyEffect(MainEffectController __instance)
    {
        if (_postProcessingControllers.TryGetValue(__instance, out PostProcessingController? postProcessingController))
        {
            return;
        }

        postProcessingController = __instance.GetComponent<PostProcessingController?>();
        if (postProcessingController == null)
        {
            return;
        }

        _postProcessingControllers[__instance] = postProcessingController;
        postProcessingController.CameraDatas = CameraDatas;
        postProcessingController.DeclaredTextureDatas = DeclaredTextureDatas;
        postProcessingController.Effects = Effects;
        postProcessingController.PrewarmCameras(_prewarmCount);
    }
}
