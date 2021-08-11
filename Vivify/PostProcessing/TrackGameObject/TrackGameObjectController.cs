namespace Vivify.PostProcessing
{
    using System;
    using System.Collections.Generic;
    using Heck.Animation;
    using UnityEngine;

    internal abstract class TrackGameObjectController : IDisposable
    {
        private readonly IEnumerable<Track> _tracks;

        internal TrackGameObjectController(IEnumerable<Track> tracks)
        {
            _tracks = tracks;
            foreach (Track track in tracks)
            {
                foreach (GameObject gameObject in track.GameObjects)
                {
#pragma warning disable CA2214 // OnGameObjectAdded should not require initializition
                    OnGameObjectAdded(gameObject);
#pragma warning restore CA2214
                }

                track.OnGameObjectAdded += OnGameObjectAdded;
                track.OnGameObjectRemoved += OnGameObjectRemoved;
            }
        }

        public virtual void Dispose()
        {
            foreach (Track track in _tracks)
            {
                if (track != null)
                {
                    track.OnGameObjectAdded -= OnGameObjectAdded;
                    track.OnGameObjectRemoved -= OnGameObjectRemoved;
                }
            }
        }

        protected abstract void OnGameObjectAdded(GameObject gameObject);

        protected abstract void OnGameObjectRemoved(GameObject gameObject);
    }
}
