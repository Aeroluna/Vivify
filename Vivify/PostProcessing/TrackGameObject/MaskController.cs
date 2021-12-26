using System.Collections.Generic;
using Heck.Animation;
using UnityEngine;

namespace Vivify.PostProcessing.TrackGameObject
{
    internal sealed class MaskController : TrackGameObjectController
    {
        internal MaskController(IEnumerable<Track> tracks)
            : base(tracks)
        {
        }

        internal HashSet<MaskRenderer> MaskRenderers { get; } = new();

        public override void Dispose()
        {
            base.Dispose();

            foreach (MaskRenderer maskRenderer in MaskRenderers)
            {
                maskRenderer.OnDestroyed -= OnMaskRendererDestroyed;
            }
        }

        protected override void OnGameObjectAdded(GameObject gameObject)
        {
            MaskRenderer? maskRenderer = gameObject.GetComponent<MaskRenderer>();
            maskRenderer ??= gameObject.AddComponent<MaskRenderer>();

            MaskRenderers.Add(maskRenderer);
            maskRenderer.OnDestroyed += OnMaskRendererDestroyed;
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
            MaskRenderers.Remove(maskRenderer);
            maskRenderer.OnDestroyed -= OnMaskRendererDestroyed;
        }
    }
}
