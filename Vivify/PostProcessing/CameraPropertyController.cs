using System;
using HarmonyLib;
using UnityEngine;

namespace Vivify.PostProcessing
{
    [RequireComponent(typeof(Camera))]
    internal class CameraPropertyController : MonoBehaviour
    {
        private Camera _camera = null!;
        private VisualEffectsController? _visualEffectsController;

        private static event Action<DepthTextureMode>? OnDepthTextureModeChanged;

        private static event Action? OnReset;

        internal static DepthTextureMode DepthTextureMode
        {
            set => OnDepthTextureModeChanged?.Invoke(value);
        }

        internal static void ResetProperties()
        {
            OnReset?.Invoke();
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _visualEffectsController = GetComponent<VisualEffectsController>();
            OnDepthTextureModeChanged += UpdateDepthTextureMode;
            OnReset += ResetThis;
        }

        private void OnDestroy()
        {
            OnDepthTextureModeChanged -= UpdateDepthTextureMode;
            OnReset -= ResetThis;
        }

        private void UpdateDepthTextureMode(DepthTextureMode value)
        {
            _camera.depthTextureMode = value;
        }

        private void ResetThis()
        {
            if (_visualEffectsController != null)
            {
                typeof(VisualEffectsController).GetMethod("HandleDepthTextureEnabledDidChange", AccessTools.all)?
                    .Invoke(_visualEffectsController, Array.Empty<object>());
            }
        }
    }
}
