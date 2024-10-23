using System;
using System.Collections.Generic;
using Heck.ReLoad;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using UnityEngine;
using Vivify.PostProcessing;
using Vivify.TrackGameObject;
using Zenject;

namespace Vivify.HarmonyPatches;

internal class PostProcessingEffectApplier : IAffinity, IDisposable
{
    private readonly Dictionary<MainEffectController, PostProcessingController> _postProcessingControllers = new();

    private readonly ReLoader? _reLoader;

    [UsedImplicitly]
    private PostProcessingEffectApplier(
        [InjectOptional] ReLoader? reLoader)
    {
        _reLoader = reLoader;
        if (reLoader != null)
        {
            reLoader.Rewinded += OnRewind;
        }
    }

    internal Dictionary<string, CullingTextureTracker> CullingTextureDatas { get; } = new();

    internal Dictionary<string, DeclareRenderTextureData> DeclaredTextureDatas { get; } = new();

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
        foreach (CullingTextureTracker cullingTextureTracker in CullingTextureDatas.Values)
        {
            cullingTextureTracker.Dispose();
        }

        CullingTextureDatas.Clear();
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
            postProcessingController.CullingTextureDatas = CullingTextureDatas;
            postProcessingController.DeclaredTextureDatas = DeclaredTextureDatas;
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
