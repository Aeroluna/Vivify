using System;
using System.Collections.Generic;
using System.Linq;
using CustomJSONData.CustomBeatmap;
using Heck.ReLoad;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using UnityEngine;
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
    }

    internal Dictionary<string, CreateCameraData> CameraDatas { get; } = new();

    internal Dictionary<string, CreateScreenTextureData> DeclaredTextureDatas { get; } = new();

    internal List<MaterialData> PreEffects { get; } = [];

    internal List<MaterialData> PostEffects { get; } = [];

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

        PreEffects.Clear();
        PostEffects.Clear();
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(MainEffectController), nameof(MainEffectController.ImageEffectControllerCallback))]
    private bool ApplyVivifyEffect(MainEffectController __instance, RenderTexture src, RenderTexture dest)
    {
        if (!_postProcessingControllers.TryGetValue(__instance, out PostProcessingController? postProcessingController))
        {
            postProcessingController = __instance.GetComponent<PostProcessingController?>();
            if (postProcessingController == null)
            {
                return true;
            }

            _postProcessingControllers[__instance] = postProcessingController;
            postProcessingController.CameraDatas = CameraDatas;
            postProcessingController.DeclaredTextureDatas = DeclaredTextureDatas;
            postProcessingController.PrewarmCameras(_prewarmCount);
        }

        RenderTextureDescriptor descriptor = src.descriptor;
        postProcessingController.CreateDeclaredTextures(descriptor);
        RenderTexture main = src;
        if (PreEffects.Count > 0)
        {
            main = postProcessingController.RenderImage(main, PreEffects);
        }

        MainEffectSO mainEffect = __instance._mainEffectContainer.mainEffect;

        if (PostEffects.Count > 0)
        {
            RenderTexture temp = RenderTexture.GetTemporary(descriptor);
            mainEffect.Render(main, temp, __instance._fadeValue);
            if (main != src)
            {
                RenderTexture.ReleaseTemporary(main);
            }

            main = postProcessingController.RenderImage(temp, PostEffects);
            if (temp != main)
            {
                RenderTexture.ReleaseTemporary(temp);
            }

            Graphics.Blit(main, dest);
            RenderTexture.ReleaseTemporary(main);
        }
        else
        {
            mainEffect.Render(main, dest, __instance._fadeValue);
            if (main != src)
            {
                RenderTexture.ReleaseTemporary(main);
            }
        }

        return false;
    }
}
