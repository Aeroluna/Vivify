namespace Vivify.PostProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Heck.Animation;
    using UnityEngine;

    internal sealed class CullingMaskController : TrackGameObjectController
    {
        internal CullingMaskController(IEnumerable<Track> tracks, bool whitelist)
            : base(tracks)
        {
            Whitelist = whitelist;
        }

        internal bool Whitelist { get; }

        internal HashSet<GameObject> GameObjects { get; } = new HashSet<GameObject>();

        protected override void OnGameObjectAdded(GameObject gameObject)
        {
            GameObjects.Add(gameObject);
        }

        protected override void OnGameObjectRemoved(GameObject gameObject)
        {
            GameObjects.Remove(gameObject);
        }
    }
}
