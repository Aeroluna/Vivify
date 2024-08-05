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
    private readonly IInstantiator _instantiator;
    private readonly SiraLog _log;

    [UsedImplicitly]
    private AddComponentsToCamera(SiraLog log, IInstantiator instantiator)
    {
        _log = log;
        _instantiator = instantiator;
    }

    [AffinityPostfix]
    [AffinityPatch(typeof(MainEffectController), nameof(MainEffectController.LazySetupImageEffectController))]
    private void AddComponents(MainEffectController __instance)
    {
        GameObject gameObject = __instance.gameObject;
        _log.Debug($"Created PostProcessingController for [{gameObject.name}]");
        SafeAddComponent<PostProcessingController>(gameObject);
        SafeAddComponent<CameraPropertyController>(gameObject);
    }

    private void SafeAddComponent<T>(GameObject gameObject)
        where T : Component
    {
        if (gameObject.GetComponent<T>() == null)
        {
            _instantiator.InstantiateComponent<T>(gameObject);
        }
    }
}
