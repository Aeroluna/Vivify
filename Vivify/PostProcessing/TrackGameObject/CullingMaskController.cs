namespace Vivify.PostProcessing
{
    using System.Collections.Generic;
    using System.Linq;
    using Heck.Animation;
    using UnityEngine;

    internal sealed class CullingMaskController : TrackGameObjectController
    {
        private readonly HashSet<MaskRenderer> _maskRenderers = new HashSet<MaskRenderer>();

        internal CullingMaskController(IEnumerable<Track> tracks, bool whitelist)
            : base(tracks)
        {
            Whitelist = whitelist;
            UpdateGameObjects();
        }

        internal bool Whitelist { get; }

        internal GameObject[] GameObjects { get; private set; } = System.Array.Empty<GameObject>();

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
