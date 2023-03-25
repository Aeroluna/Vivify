using System.Collections.Generic;
using System.Linq;
using IPA.Utilities;
using UnityEngine;
using Vivify.Controllers.TrackGameObject;
using Vivify.Managers;
using static Vivify.VivifyController;

namespace Vivify.PostProcessing
{
    // this is attached to the secondary camera because trying to set the targettexture of a camera disable stereo on a camera for some reason
    // https://forum.unity.com/threads/how-to-create-stereo-rendertextures-and-cameras.925175/#post-6968408
    internal class CameraCullingMaskController : MonoBehaviour
    {
        private Camera _camera = null!;
        private MainEffectRenderer _mainEffectRenderer = null!;

        private RenderTexture? _renderTexture;
        private RenderTexture? _renderTextureDepth;

        internal RenderTextureDescriptor? Descriptor { get; set; }

        internal IEnumerable<RenderTexture> RenderCullingMasks(Dictionary<string, CullingMaskController> CullingMasks)
        {
            if (Descriptor == null || CullingMasks.Count == 0)
            {
                return Enumerable.Empty<RenderTexture>();
            }

            RenderTextureDescriptor descriptor = Descriptor.Value;
            descriptor.depthBufferBits = 16;
            RenderTextureDescriptor descriptorDepth = descriptor;
            ////descriptorDepth.colorFormat = RenderTextureFormat.Depth;
            HashSet<RenderTexture> returnedTextures = new(CullingMasks.Count);
            int cachedCullingMask = _camera.cullingMask;
            foreach ((string key, CullingMaskController controller) in CullingMasks)
            {
                // Set renderers to culling layer
                GameObject[] gameObjects = controller.GameObjects;
                int length = gameObjects.Length;
                int[] cachedLayers = new int[length];
                for (int i = 0; i < length; i++)
                {
                    GameObject renderer = gameObjects[i];
                    cachedLayers[i] = renderer.layer;
                    renderer.layer = CULLINGLAYER;
                }

                // setup color render texture
                _renderTexture = RenderTexture.GetTemporary(descriptor);
                Shader.SetGlobalTexture(key, _renderTexture);
                returnedTextures.Add(_renderTexture);

                // flip culling mask when whitelist mode enabled
                if (controller.Whitelist)
                {
                    _camera.cullingMask = 1 << CULLINGLAYER;
                }

                // Setup targets
                if (controller.DepthTexture)
                {
                    _renderTextureDepth = RenderTexture.GetTemporary(descriptorDepth);
                    Shader.SetGlobalTexture(key + "_Depth", _renderTextureDepth);
                    returnedTextures.Add(_renderTextureDepth);
                }

                // render
                _camera.Render();

                // reset culling mask
                _camera.cullingMask = cachedCullingMask;

                // reset renderer layers
                for (int i = 0; i < length; i++)
                {
                    gameObjects[i].layer = cachedLayers[i];
                }
            }

            return returnedTextures;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (_renderTexture != null)
            {
                _mainEffectRenderer.Render(src, _renderTexture);
            }

            if (_renderTextureDepth != null)
            {
                Graphics.Blit(src, _renderTextureDepth, InternalBundleManager.DepthMaterial);
            }

            _renderTexture = null;
            _renderTextureDepth = null;
            ////Graphics.Blit(src, dst);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _mainEffectRenderer = new MainEffectRenderer(gameObject.transform.parent.GetComponent<MainEffectController>());
        }
    }
}
