using UnityEngine;
using UnityEngine.Rendering;
using Vivify.Managers;
using Vivify.PostProcessing;
using Vivify.TrackGameObject;

namespace Vivify.Controllers
{
    // this is attached to the secondary camera because trying to set the targettexture of a camera disable stereo on a camera for some reason
    // https://forum.unity.com/threads/how-to-create-stereo-rendertextures-and-cameras.925175/#post-6968408
    // also for some reason trying to manually force a camera to render with Camera.Render() causes it to not write to the right eye
    // so we have a camera for each culling mask
    internal class CullingTextureController : CullingCameraController
    {
        private static readonly int _arraySliceIndex = Shader.PropertyToID("_ArraySliceIndex");

        private MainEffectRenderer _mainEffectRenderer = null!;

        private RenderTexture? _renderTextureDepth;

        private string? _key;

        private PostProcessingController _postProcessingController = null!;

        internal override int DefaultCullingMask => _postProcessingController.DefaultCullingMask;

        internal RenderTexture? RenderTexture { get; private set; }

        internal void Construct(PostProcessingController postProcessingController)
        {
            _postProcessingController = postProcessingController;
        }

        internal void Init(string key, CullingTextureData cullingTextureData)
        {
            _key = key;
            Camera.CopyFrom(_postProcessingController.Camera);
            Camera.depthTextureMode = cullingTextureData.DepthTexture ? DepthTextureMode.Depth : DepthTextureMode.None;
            Camera.depth -= 1;
            CullingTextureData = cullingTextureData;
        }

        protected override void Awake()
        {
            base.Awake();
            _mainEffectRenderer = new MainEffectRenderer(gameObject.transform.parent.GetComponent<MainEffectController>());
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (CullingTextureData == null)
            {
                return;
            }

            if (RenderTexture == null)
            {
                RenderTexture = new RenderTexture(src.descriptor);
            }

            Shader.SetGlobalTexture(_key, RenderTexture);
            _mainEffectRenderer.Render(src, RenderTexture);

            if (!CullingTextureData.DepthTexture)
            {
                return;
            }

            if (_renderTextureDepth == null)
            {
                _renderTextureDepth = new RenderTexture(src.descriptor);
            }

            Shader.SetGlobalTexture(_key + "_Depth", _renderTextureDepth);
            if (_renderTextureDepth.dimension == TextureDimension.Tex2DArray)
            {
                Material sliceMaterial = DepthShaderManager.DepthArrayMaterial;
                Plugin.Log.LogDebug(sliceMaterial.shader.name);
                sliceMaterial.SetFloat(_arraySliceIndex, 0);
                Graphics.Blit(null, _renderTextureDepth, sliceMaterial, -1, 0);
                sliceMaterial.SetFloat(_arraySliceIndex, 1);
                Graphics.Blit(null, _renderTextureDepth, sliceMaterial, -1, 1);
            }
            else
            {
                Graphics.Blit(null, _renderTextureDepth, DepthShaderManager.DepthMaterial);
            }
        }

        private void OnDisable()
        {
            if (RenderTexture != null)
            {
                RenderTexture.Release();
            }

            if (_renderTextureDepth != null)
            {
                _renderTextureDepth.Release();
            }
        }
    }
}
