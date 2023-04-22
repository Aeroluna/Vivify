using UnityEngine;
using Vivify.TrackGameObject;
using static Vivify.VivifyController;

namespace Vivify.Controllers
{
    internal abstract class CullingCameraController : MonoBehaviour
    {
        private CullingTextureData? _cullingTextureData;

        private int[]? _cachedLayers;

        internal abstract int DefaultCullingMask { get; }

        internal Camera Camera { get; private set; } = null!;

        protected CullingTextureData? CullingTextureData
        {
            get => _cullingTextureData;

            set
            {
                _cullingTextureData = value;

                // flip culling mask when whitelist mode enabled
                Camera.cullingMask = value?.Whitelist ?? false ? 1 << CULLINGLAYER : DefaultCullingMask;
            }
        }

        protected virtual void Awake()
        {
            Camera = GetComponent<Camera>();
        }

        private void OnPreCull()
        {
            if (_cullingTextureData == null)
            {
                return;
            }

            // Set renderers to culling layer
            GameObject[] gameObjects = _cullingTextureData.GameObjects;
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
            if (_cullingTextureData == null || _cachedLayers == null)
            {
                return;
            }

            // reset renderer layers
            GameObject[] gameObjects = _cullingTextureData.GameObjects;
            int length = gameObjects.Length;
            for (int i = 0; i < length; i++)
            {
                gameObjects[i].layer = _cachedLayers[i];
            }
        }
    }
}
