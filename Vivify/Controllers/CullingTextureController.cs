using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Vivify.Managers;
using Vivify.PostProcessing;
using Vivify.TrackGameObject;
using Zenject;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Vivify.Controllers;

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

    internal override int DefaultCullingMask => _postProcessingController.DefaultCullingMask;

    internal int Key { get; private set; }

    internal int DepthKey { get; private set; }

    internal Dictionary<Camera.MonoOrStereoscopicEye, RenderTexture> RenderTextures { get; } = new();

    internal Dictionary<Camera.MonoOrStereoscopicEye, RenderTexture> RenderTexturesDepth { get; } = new();

    internal void Init(string key, CullingTextureTracker cullingTextureTracker)
    {
        Key = Shader.PropertyToID(key);
        DepthKey = Shader.PropertyToID(key + "_Depth");
        CullingTextureData = cullingTextureTracker;
        Camera.CopyFrom(_postProcessingController.Camera); // TODO: skip this, lags too damn hard
        RefreshCamera();
    }

    protected override void OnPreCull()
    {
        base.OnPreCull();

        Camera camera = Camera;
        Camera other = _postProcessingController.Camera;
        if (!CamEquals(camera, other))
        {
            RefreshCamera();
        }

        Transform transform1 = transform;
        transform1.localPosition = Vector3.zero;
        transform1.localRotation = Quaternion.identity;
        camera.cullingMatrix = other.projectionMatrix * other.worldToCameraMatrix;
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
               Mathf.Approximately(lhs.fieldOfView, rhs.fieldOfView);
    }

    [UsedImplicitly]
    [Inject]
    private void Construct(PostProcessingController postProcessingController, DepthShaderManager depthShaderManager)
    {
        _postProcessingController = postProcessingController;
        _depthShaderManager = depthShaderManager;
    }

    private void RefreshCamera()
    {
        if (CullingTextureData == null)
        {
            return;
        }

        // copyfrom lags for some reason
        ////Camera.CopyFrom(_postProcessingController.Camera);
        Camera other = _postProcessingController.Camera;
        Camera.fieldOfView = other.fieldOfView;
        Camera.aspect = other.aspect;
        Camera.depthTextureMode = CullingTextureData.DepthTexture ? DepthTextureMode.Depth : DepthTextureMode.None;
        Camera.depth -= 1;
        RefreshCullingMask();
    }

    private void OnDisable()
    {
        RenderTextures.Values.Do(n => n.Release());
        RenderTextures.Clear();
        RenderTexturesDepth.Values.Do(n => n.Release());
        RenderTexturesDepth.Clear();
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (CullingTextureData == null)
        {
            return;
        }

        if (!RenderTextures.TryGetValue(Camera.stereoActiveEye, out RenderTexture colorTexture) ||
            !RTEquals(colorTexture, src))
        {
            colorTexture?.Release();
            colorTexture = new RenderTexture(src.descriptor);
            RenderTextures[Camera.stereoActiveEye] = colorTexture;
        }

        (_mainEffectRenderer ??=
            new MainEffectRenderer(gameObject.transform.parent.GetComponent<MainEffectController>())).Render(
            src,
            colorTexture);

        if (!CullingTextureData.DepthTexture)
        {
            return;
        }

        if (!RenderTexturesDepth.TryGetValue(Camera.stereoActiveEye, out RenderTexture depthTexture) ||
            !RTEquals(depthTexture, src))
        {
            depthTexture?.Release();
            RenderTextureDescriptor descriptor = src.descriptor;
            descriptor.graphicsFormat = GraphicsFormat.None;
            depthTexture = new RenderTexture(src.descriptor);
            RenderTexturesDepth[Camera.stereoActiveEye] = depthTexture;
        }

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
}
