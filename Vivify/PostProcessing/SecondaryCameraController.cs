using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using Vivify.Controllers;
using Vivify.Managers;
using Zenject;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
#if !V1_29_1
using UnityEngine.XR.OpenXR;
#endif

namespace Vivify.PostProcessing;

// this is attached to the secondary camera because trying to set the targettexture of a camera disable stereo on a camera for some reason
// https://forum.unity.com/threads/how-to-create-stereo-rendertextures-and-cameras.925175/#post-6968408
// also for some reason trying to manually force a camera to render with Camera.Render() causes it to not write to the right eye
// so we have a camera for each culling mask
internal class CullingTextureController : CullingCameraController
{
    private static readonly int _arraySliceIndex = Shader.PropertyToID("_ArraySliceIndex");

    private MainEffectRenderer? _mainEffectRenderer;

    private PostProcessingController _postProcessingController = null!;
    private DepthShaderManager _depthShaderManager = null!;
    private CameraPropertyController _cameraPropertyController = null!;

    private bool _ready;

    internal int? Key { get; private set; }

    internal int? DepthKey { get; private set; }

    internal Dictionary<Camera.MonoOrStereoscopicEye, RenderTexture> RenderTextures { get; } = new();

    internal Dictionary<Camera.MonoOrStereoscopicEye, RenderTexture> RenderTexturesDepth { get; } = new();

    internal void Init(CreateCameraData cameraData)
    {
        _cameraPropertyController.Id = cameraData.Name;
        if (cameraData.Texture != null)
        {
            Key = Shader.PropertyToID(cameraData.Texture);
        }

        if (cameraData.DepthTexture != null)
        {
            DepthKey = Shader.PropertyToID(cameraData.DepthTexture);
        }

        RefreshCamera();
    }

    protected override void OnPreCull()
    {
        Camera camera = Camera;
        Camera other = _postProcessingController.Camera;
        if (!CamEquals(camera, other))
        {
            RefreshCamera();
        }

        Transform transform1 = transform;
        transform1.localPosition = Vector3.zero;
        transform1.localRotation = Quaternion.identity;

        base.OnPreCull();

#if !V1_29_1
        if (OpenXRSettings.Instance.renderMode == OpenXRSettings.RenderMode.MultiPass)
        {
            return;
        }
#endif

        camera.cullingMatrix = other.projectionMatrix * other.worldToCameraMatrix;
        camera.projectionMatrix = other.projectionMatrix;
        camera.nonJitteredProjectionMatrix = other.nonJitteredProjectionMatrix;
        camera.worldToCameraMatrix = other.worldToCameraMatrix;
    }

    // very simple comparison
    private static bool RTEquals(RenderTexture lhs, RenderTexture rhs)
    {
        return lhs.vrUsage == rhs.vrUsage &&
               lhs.width == rhs.width &&
               lhs.height == rhs.height;
    }

    private static bool CamEquals(Camera lhs, Camera rhs)
    {
        return lhs.stereoEnabled == rhs.stereoEnabled &&
               lhs.cullingMask == rhs.cullingMask &&
               Mathf.Approximately(lhs.fieldOfView, rhs.fieldOfView) &&
               Mathf.Approximately(lhs.nearClipPlane, rhs.nearClipPlane) &&
               Mathf.Approximately(lhs.farClipPlane, rhs.farClipPlane);
    }

    [UsedImplicitly]
    [Inject]
    private void Construct(
        IInstantiator instantiator,
        PostProcessingController postProcessingController,
        DepthShaderManager depthShaderManager)
    {
        _postProcessingController = postProcessingController;
        _depthShaderManager = depthShaderManager;
        _cameraPropertyController = instantiator.InstantiateComponent<CameraPropertyController>(gameObject);
        _cameraPropertyController.enabled = false;
        Camera.CopyFrom(_postProcessingController.Camera);
    }

    private void RefreshCamera()
    {
        // copyfrom lags for some reason
        ////Camera.CopyFrom(_postProcessingController.Camera);
        Camera other = _postProcessingController.Camera;
        Camera.stereoTargetEye = other.stereoTargetEye;
        Camera.fieldOfView = other.fieldOfView;
        Camera.aspect = other.aspect;
        Camera.depth = other.depth - 1;
        Camera.nearClipPlane = other.nearClipPlane;
        Camera.farClipPlane = other.farClipPlane;
        Camera.layerCullDistances = other.layerCullDistances;
        Camera.targetTexture = null;
        Camera.cullingMask = other.cullingMask;
    }

    private void OnDestroy()
    {
        RenderTextures.Values.Do(n => n.Release());
        RenderTextures.Clear();
        RenderTexturesDepth.Values.Do(n => n.Release());
        RenderTexturesDepth.Clear();
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        Camera.MonoOrStereoscopicEye stereoActiveEye = Camera.stereoActiveEye;
        RenderTextureDescriptor descriptor = src.descriptor;
        descriptor.msaaSamples = 1;

        if (!_ready)
        {
            GetRenderTexture(RenderTextures, descriptor, false, true);
            GetRenderTexture(RenderTexturesDepth, descriptor, true, true);

            _cameraPropertyController.enabled = true;
            _ready = true;
        }

        if (Key != null)
        {
            RenderTexture colorTexture = GetRenderTexture(RenderTextures, descriptor, false, false);

            if (MainEffect)
            {
                (_mainEffectRenderer ??=
                    new MainEffectRenderer(gameObject.transform.parent.GetComponent<MainEffectController>())).Render(
                    src,
                    colorTexture);
            }
            else
            {
                Graphics.Blit(src, colorTexture);
            }
        }

        // ReSharper disable once InvertIf
        if (DepthKey != null)
        {
            RenderTexture depthTexture = GetRenderTexture(RenderTexturesDepth, descriptor, true, false);

            if (depthTexture.dimension == TextureDimension.Tex2DArray)
            {
                Material? sliceMaterial = _depthShaderManager.DepthArrayMaterial;
                if (sliceMaterial == null)
                {
                    return;
                }

                sliceMaterial.SetFloat(_arraySliceIndex, 0);
                Graphics.Blit(null, depthTexture, sliceMaterial, -1, 0);
                sliceMaterial.SetFloat(_arraySliceIndex, 1);
                Graphics.Blit(null, depthTexture, sliceMaterial, -1, 1);
            }
            else
            {
                Material? depthMaterial = _depthShaderManager.DepthMaterial;
                if (depthMaterial == null)
                {
                    return;
                }

                Graphics.Blit(null, depthTexture, depthMaterial);
            }
        }

        if (Key == null && DepthKey == null)
        {
            gameObject.SetActive(false);
        }

        return;

        RenderTexture GetRenderTexture(
            Dictionary<Camera.MonoOrStereoscopicEye, RenderTexture> dictionary,
            RenderTextureDescriptor renderTextureDescriptor,
            bool depth,
            bool create)
        {
            // ReSharper disable once InvertIf
            if (!dictionary.TryGetValue(stereoActiveEye, out RenderTexture renderTexture) ||
                !RTEquals(renderTexture, src))
            {
                renderTexture?.Release();

                if (depth)
                {
                    renderTextureDescriptor.colorFormat = RenderTextureFormat.RFloat;
                }

                renderTexture = new RenderTexture(renderTextureDescriptor);

                dictionary[stereoActiveEye] = renderTexture;

                if (create)
                {
                    renderTexture.Create();
                }
            }

            return renderTexture;
        }
    }
}
