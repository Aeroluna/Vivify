using System;
using System.Collections.Generic;
using System.Linq;
using Heck.Animation;
using JetBrains.Annotations;
using UnityEngine;

namespace Vivify.PostProcessing.TrackGameObject
{
    internal sealed class CullingMaskController : TrackGameObjectController
    {
        private readonly HashSet<MaskRenderer> _maskRenderers = new();

        internal CullingMaskController(IEnumerable<Track> tracks, bool whitelist, bool depthTexture)
            : base(tracks)
        {
            Whitelist = whitelist;
            DepthTexture = depthTexture;
            UpdateGameObjects();
        }

        internal bool Whitelist { get; }

        // TODO: implement depthtexture
        [PublicAPI]
        internal bool DepthTexture { get; }

        internal GameObject[] GameObjects { get; private set; } = Array.Empty<GameObject>();

        public override void Dispose()
        {
            base.Dispose();

            foreach (MaskRenderer maskRenderer in _maskRenderers)
            {
                maskRenderer.OnDestroyed -= OnMaskRendererDestroyed;
                maskRenderer.OnTransformChanged -= UpdateGameObjects;
            }
        }

        protected override void OnGameObjectAdded(GameObject gameObject)
        {
            MaskRenderer? maskRenderer = gameObject.GetComponent<MaskRenderer>();
            maskRenderer ??= gameObject.AddComponent<MaskRenderer>();

            _maskRenderers.Add(maskRenderer);
            maskRenderer.OnDestroyed += OnMaskRendererDestroyed;
            maskRenderer.OnTransformChanged += UpdateGameObjects;

            UpdateGameObjects();
        }

        protected override void OnGameObjectRemoved(GameObject gameObject)
        {
            MaskRenderer maskRenderer = gameObject.GetComponent<MaskRenderer>();
            if (maskRenderer != null)
            {
                OnMaskRendererDestroyed(maskRenderer);
            }
        }

        private void OnMaskRendererDestroyed(MaskRenderer maskRenderer)
        {
            _maskRenderers.Remove(maskRenderer);
            maskRenderer.OnDestroyed -= OnMaskRendererDestroyed;
            maskRenderer.OnTransformChanged -= UpdateGameObjects;

            UpdateGameObjects();
        }

        private void UpdateGameObjects()
        {
            GameObjects = _maskRenderers.SelectMany(n => n.ChildRenderers).Select(n => n.gameObject).ToArray();
        }
    }
}
