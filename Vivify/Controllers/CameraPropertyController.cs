using UnityEngine;
using Vivify.Managers;
#if !LATEST
using HarmonyLib;
#endif

namespace Vivify.Controllers;

[RequireComponent(typeof(Camera))]
internal class CameraPropertyController : MonoBehaviour
{
    private Camera _camera = null!;
    private DepthTextureMode _cachedDepthTextureMode;
    private CameraClearFlags _cachedClearFlags;
    private Color _cachedBackgroundColor;

#if LATEST
    private DepthTextureController? _depthTextureController;
#else
    private VisualEffectsController? _visualEffectsController;
#endif

    internal DepthTextureMode? DepthTextureMode
    {
        set
        {
            if (value == null)
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
                        .GetMethod("HandleDepthTextureEnabledDidChange", AccessTools.all)?
                        .Invoke(_visualEffectsController, []);
                }
#endif
            }
            else
            {
                _camera.depthTextureMode = value.Value | _cachedDepthTextureMode;
            }
#if LATEST
            if (_depthTextureController != null)
            {
                _depthTextureController._cachedPreset = null;
            }
#endif
        }
    }

    internal CameraClearFlags? ClearFlags
    {
        set => _camera.clearFlags = value ?? _cachedClearFlags;
    }

    internal Color? BackgroundColor
    {
        set => _camera.backgroundColor = value ?? _cachedBackgroundColor;
    }

    internal void Reset()
    {
        DepthTextureMode = null;
        ClearFlags = null;
        BackgroundColor = null;
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
#if LATEST
        _depthTextureController = GetComponent<DepthTextureController>();
#else
        _visualEffectsController = GetComponent<VisualEffectsController>();
#endif
    }

    private void OnEnable()
    {
        _cachedDepthTextureMode = _camera.depthTextureMode;
        _cachedClearFlags = _camera.clearFlags;
        _cachedBackgroundColor = _camera.backgroundColor;
        CameraPropertyManager.AddController(this);
    }

    private void OnDisable()
    {
        CameraPropertyManager.RemoveController(this);
    }
}
