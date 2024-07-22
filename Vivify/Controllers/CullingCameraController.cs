using System.Collections.Generic;
using UnityEngine;
using Vivify.TrackGameObject;
using static Vivify.VivifyController;

namespace Vivify.Controllers
{
    internal abstract class CullingCameraController : MonoBehaviour
    {
        private readonly List<(GameObject, int)> _cachedLayers = new();

        private CullingTextureTracker? _cullingTextureData;

        internal abstract int DefaultCullingMask { get; }

        internal Camera Camera { get; private set; } = null!;

        internal CullingTextureTracker? CullingTextureData
        {
            get => _cullingTextureData;

            set
            {
                _cullingTextureData = value;

                // flip culling mask when whitelist mode enabled
                Camera.cullingMask = value?.Whitelist ?? false ? 1 << CULLING_LAYER : DefaultCullingMask;
            }
        }

        protected virtual void Awake()
        {
            Camera = GetComponent<Camera>();
        }

        protected virtual void OnPreCull()
        {
            if (_cullingTextureData == null)
            {
                return;
            }

            // Set renderers to culling layer
            GameObject[] gameObjects = _cullingTextureData.GameObjects;
            int length = gameObjects.Length;
            for (int i = 0; i < length; i++)
            {
                GameObject renderedObject = gameObjects[i];
                _cachedLayers.Add((renderedObject, renderedObject.layer));
                renderedObject.layer = CULLING_LAYER;
            }
        }

        private void OnPostRender()
        {
            if (_cachedLayers.Count == 0)
            {
                return;
            }

            // reset renderer layers
            _cachedLayers.ForEach(n => n.Item1.layer = n.Item2);
            _cachedLayers.Clear();
        }
    }
}
