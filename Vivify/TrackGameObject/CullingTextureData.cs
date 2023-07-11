using System;
using System.Collections.Generic;
using System.Linq;
using Heck.Animation;
using UnityEngine;
using Vivify.Controllers;

namespace Vivify.TrackGameObject
{
    internal sealed class CullingTextureData : TrackGameObjectTracker
    {
        private readonly HashSet<RendererController> _maskRenderers = new();

        private bool _gameObjectsDirty;

        private GameObject[] _gameObjects = Array.Empty<GameObject>();

        internal CullingTextureData(IEnumerable<Track> tracks, bool whitelist, bool depthTexture)
            : base(tracks)
        {
            Whitelist = whitelist;
            DepthTexture = depthTexture;
            UpdateGameObjects();
        }

        internal bool Whitelist { get; }

        internal bool DepthTexture { get; }

        internal GameObject[] GameObjects
        {
            get
            {
                if (_gameObjectsDirty)
                {
                    _gameObjects = _maskRenderers.SelectMany(n => n.ChildRenderers).Select(n => n.gameObject).ToArray();
                }

                return _gameObjects;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (RendererController maskRenderer in _maskRenderers)
            {
                maskRenderer.OnDestroyed -= OnMaskRendererDestroyed;
                maskRenderer.OnTransformChanged -= UpdateGameObjects;
            }
        }

        protected override void OnGameObjectAdded(GameObject gameObject)
        {
            RendererController? maskRenderer = gameObject.GetComponent<RendererController>();
            maskRenderer ??= gameObject.AddComponent<RendererController>();

            _maskRenderers.Add(maskRenderer);
            maskRenderer.OnDestroyed += OnMaskRendererDestroyed;
            maskRenderer.OnTransformChanged += UpdateGameObjects;

            UpdateGameObjects();
        }

        protected override void OnGameObjectRemoved(GameObject gameObject)
        {
            RendererController rendererController = gameObject.GetComponent<RendererController>();
            if (rendererController != null)
            {
                OnMaskRendererDestroyed(rendererController);
            }
        }

        private void OnMaskRendererDestroyed(RendererController rendererController)
        {
            _maskRenderers.Remove(rendererController);
            rendererController.OnDestroyed -= OnMaskRendererDestroyed;
            rendererController.OnTransformChanged -= UpdateGameObjects;

            UpdateGameObjects();
        }

        private void UpdateGameObjects()
        {
            _gameObjectsDirty = true;
        }
    }
}
