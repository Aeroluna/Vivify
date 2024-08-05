using System;
using HarmonyLib;
using UnityEngine;

namespace Vivify.Controllers;

[RequireComponent(typeof(Camera))]
internal class CameraPropertyController : MonoBehaviour
{
    private Camera _camera = null!;

#if LATEST
        private DepthTextureController? _depthTextureController;
#else
    private VisualEffectsController? _visualEffectsController;
#endif

    private static event Action<DepthTextureMode>? OnDepthTextureModeChanged;

    internal static DepthTextureMode DepthTextureMode
    {
        set => OnDepthTextureModeChanged?.Invoke(value);
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
#if LATEST
            _depthTextureController = GetComponent<DepthTextureController>();
#else
        _visualEffectsController = GetComponent<VisualEffectsController>();
#endif
        OnDepthTextureModeChanged += UpdateDepthTextureMode;
    }

    private void OnDestroy()
    {
        OnDepthTextureModeChanged -= UpdateDepthTextureMode;
        ResetThis();
    }

    private void UpdateDepthTextureMode(DepthTextureMode value)
    {
        _camera.depthTextureMode = value;
#if LATEST
            if (_depthTextureController != null)
            {
                _depthTextureController._cachedPreset = null;
            }
#endif
    }

    private void ResetThis()
    {
#if LATEST
            if (_depthTextureController != null)
            {
                _depthTextureController.Start();
            }
#else
        if (_visualEffectsController != null)
        {
            typeof(VisualEffectsController)
                .GetMethod("HandleDepthTextureEnabledDidChange", AccessTools.all)
                ?
                .Invoke(_visualEffectsController, []);
        }
#endif
    }
}
