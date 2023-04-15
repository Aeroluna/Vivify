using UnityEngine;
using UnityEngine.Rendering;
using Vivify.Controllers.TrackGameObject;
using Vivify.Managers;
using static Vivify.VivifyController;

namespace Vivify.PostProcessing
{
    // this is attached to the secondary camera because trying to set the targettexture of a camera disable stereo on a camera for some reason
    // https://forum.unity.com/threads/how-to-create-stereo-rendertextures-and-cameras.925175/#post-6968408
    // also for some reason trying to manually force a camera to render with Camera.Render() causes it to not write to the right eye
    // so we have a camera for each culling mask
    internal class CullingCameraController : MonoBehaviour
    {
        private static readonly int _arraySliceIndex = Shader.PropertyToID("_ArraySliceIndex");

        private Camera _camera = null!;
        private MainEffectRenderer _mainEffectRenderer = null!;

        private RenderTexture? _renderTexture;
        private RenderTexture? _renderTextureDepth;

        private string? _key;
        private CullingMaskController? _cullingMaskController;

        private int[]? _cachedLayers;

        private PostProcessingController _postProcessingController = null!;

        internal void Construct(PostProcessingController postProcessingController)
        {
            _postProcessingController = postProcessingController;
        }

        internal void Init(string key, CullingMaskController cullingMaskController)
        {
            _key = key;
            _cullingMaskController = cullingMaskController;
            _camera.CopyFrom(_postProcessingController.Camera);
            _camera.depthTextureMode = cullingMaskController.DepthTexture ? DepthTextureMode.Depth : DepthTextureMode.None;
            _camera.depth -= 1;

            // flip culling mask when whitelist mode enabled
            if (cullingMaskController.Whitelist)
            {
                _camera.cullingMask = 1 << CULLINGLAYER;
            }
        }

        private void OnPreCull()
        {
            if (_cullingMaskController == null)
            {
                return;
            }

            // Set renderers to culling layer
            GameObject[] gameObjects = _cullingMaskController.GameObjects;
            int length = gameObjects.Length;
            _cachedLayers = new int[length];
            for (int i = 0; i < length; i++)
            {
                GameObject renderedObject = gameObjects[i];
                _cachedLayers[i] = renderedObject.layer;
                renderedObject.layer = CULLINGLAYER;
            }
        }

        private void OnPostRender()
        {
            if (_cullingMaskController == null || _cachedLayers == null)
            {
                return;
            }

            // reset renderer layers
            GameObject[] gameObjects = _cullingMaskController.GameObjects;
            int length = gameObjects.Length;
            for (int i = 0; i < length; i++)
            {
                gameObjects[i].layer = _cachedLayers[i];
            }
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (_cullingMaskController == null)
            {
                return;
            }

            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(src.descriptor);
            }

            Shader.SetGlobalTexture(_key, _renderTexture);
            _mainEffectRenderer.Render(src, _renderTexture);

            if (!_cullingMaskController.DepthTexture)
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
                Material sliceMaterial = InternalBundleManager.DepthArrayMaterial;
                Log.Logger.Log(sliceMaterial.shader.name);
                sliceMaterial.SetFloat(_arraySliceIndex, 0);
                Graphics.Blit(null, _renderTextureDepth, sliceMaterial, -1, 0);
                sliceMaterial.SetFloat(_arraySliceIndex, 1);
                Graphics.Blit(null, _renderTextureDepth, sliceMaterial, -1, 1);
            }
            else
            {
                Graphics.Blit(null, _renderTextureDepth, InternalBundleManager.DepthMaterial);
            }
        }

        private void OnDisable()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
            }

            if (_renderTextureDepth != null)
            {
                _renderTextureDepth.Release();
            }
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _mainEffectRenderer = new MainEffectRenderer(gameObject.transform.parent.GetComponent<MainEffectController>());
        }
    }
}
