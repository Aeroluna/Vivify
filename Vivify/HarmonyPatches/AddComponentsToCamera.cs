using System.Collections.Generic;
using JetBrains.Annotations;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using UnityEngine;
using Vivify.Controllers;
using Vivify.PostProcessing;
using Zenject;

namespace Vivify.HarmonyPatches;

internal class AddComponentsToCamera : IAffinity
{
    private readonly SiraLog _log;
    private readonly DiContainer _container;

    private readonly List<Component> _injected = [];

    private readonly Dictionary<ImageEffectController, PostProcessingController> _postProcessingControllers = new();

    [UsedImplicitly]
    private AddComponentsToCamera(SiraLog log, DiContainer container)
    {
        _log = log;
        _container = container;
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(MainEffectController), nameof(MainEffectController.LazySetupImageEffectController))]
    private void AddComponents(MainEffectController __instance)
    {
        GameObject gameObject = __instance.gameObject;
#if !V1_29_1
        SafeAddComponent<MultipassKeywordController>(gameObject);
#endif
        SafeAddComponent<PostProcessingController>(gameObject);
        SafeAddComponent<CameraPropertyController>(gameObject);
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(MirrorRendererSO), nameof(MirrorRendererSO.CreateOrUpdateMirrorCamera))]
    private void AddMirrorComponents(MirrorRendererSO __instance)
    {
        GameObject gameObject = __instance._mirrorCamera.gameObject;
#if !V1_29_1
        SafeAddComponent<MultipassKeywordController>(gameObject);
#endif
        SafeAddComponent<CullingCameraController>(gameObject);
        SafeAddComponent<CameraPropertyController>(gameObject);
    }

    [AffinityPrefix]
    [AffinityPatch(typeof(ImageEffectController), nameof(ImageEffectController.OnRenderImage))]
    private bool StopRenderImage(ImageEffectController __instance, RenderTexture src, RenderTexture dest)
    {
        if (!_postProcessingControllers.TryGetValue(
                __instance,
                out PostProcessingController? postProcessingController))
        {
            postProcessingController = __instance.GetComponent<PostProcessingController>();
            if (postProcessingController != null)
            {
                _postProcessingControllers[__instance] = postProcessingController;
            }
            else
            {
                return true;
            }
        }

        if (!postProcessingController.enabled)
        {
            return true;
        }

        Graphics.Blit(src, dest);
        return false;
    }

    private void SafeAddComponent<T>(GameObject gameObject)
        where T : Component
    {
        string name = typeof(T).Name;
        T found = gameObject.GetComponent<T>();
        if (found != null)
        {
            if (_injected.Contains(found))
            {
                return;
            }

            // likely the component was duplicated without being injected by zenject
            _log.Debug($"Injected [{name}] for [{gameObject.name}]");
            _container.Inject(found);
            _injected.Add(found);
            return;
        }

        _log.Debug($"Created [{name}] for [{gameObject.name}]");
        _injected.Add(_container.InstantiateComponent<T>(gameObject));
    }
}
