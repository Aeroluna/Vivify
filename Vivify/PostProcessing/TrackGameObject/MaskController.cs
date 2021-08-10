namespace Vivify.PostProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Heck.Animation;
    using UnityEngine;

    internal sealed class MaskController : TrackGameObjectController, IDisposable
    {
        internal MaskController(IEnumerable<Track> tracks)
            : base(tracks)
        {
        }

        internal HashSet<MaskRenderer> MaskRenderers { get; } = new HashSet<MaskRenderer>();

        public new void Dispose()
        {
            foreach (MaskRenderer maskRenderer in MaskRenderers)
            {
                maskRenderer.OnDestroyed -= OnMaskRendererDestroyed;
            }
        }

        internal void AddMaskRenderer(MaskRenderer maskRenderer)
        {
            MaskRenderers.Add(maskRenderer);
            maskRenderer.OnDestroyed += OnMaskRendererDestroyed;
        }

        protected override void OnGameObjectAdded(GameObject gameObject)
        {
            MaskRenderer maskRenderer = gameObject.GetComponent<MaskRenderer>();
            maskRenderer ??= gameObject.AddComponent<MaskRenderer>();
            AddMaskRenderer(maskRenderer);
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
