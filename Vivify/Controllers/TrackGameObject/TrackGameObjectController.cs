using System;
using System.Collections.Generic;
using System.Linq;
using Heck.Animation;
using UnityEngine;

namespace Vivify.PostProcessing.TrackGameObject
{
    internal abstract class TrackGameObjectController : IDisposable
    {
        private readonly IEnumerable<Track> _tracks;

        internal TrackGameObjectController(IEnumerable<Track> tracks)
        {
            _tracks = tracks.ToList();
            foreach (Track track in _tracks)
            {
                foreach (GameObject gameObject in track.GameObjects)
                {
                    // ReSharper disable once VirtualMemberCallInConstructor
                    OnGameObjectAdded(gameObject);
                }

                track.GameObjectAdded += OnGameObjectAdded;
                track.GameObjectRemoved += OnGameObjectRemoved;
            }
        }

        public virtual void Dispose()
        {
            foreach (Track track in _tracks)
            {
                if (track == null)
                {
                    continue;
                }

                track.GameObjectAdded -= OnGameObjectAdded;
                track.GameObjectRemoved -= OnGameObjectRemoved;
            }
        }

        protected abstract void OnGameObjectAdded(GameObject gameObject);

        protected abstract void OnGameObjectRemoved(GameObject gameObject);
    }
}
