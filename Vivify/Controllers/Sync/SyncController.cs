using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace Vivify.Controllers.Sync
{
    internal abstract class SyncController : MonoBehaviour, ISync
    {
        private AudioTimeSyncController _audioTimeSyncController = null!;

        protected float SongTime { get; private set; }

        public abstract void Sync(float speed);

        public void SetStartTime(float time)
        {
            SongTime = time;
        }

        [Inject]
        [UsedImplicitly]
        private void Construct(float startTime, AudioTimeSyncController audioTimeSyncController)
        {
            SongTime = startTime;
            _audioTimeSyncController = audioTimeSyncController;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            float songTime = _audioTimeSyncController.songTime;
            float deltaSongTime = songTime - SongTime;
            SongTime = songTime;

            if (deltaTime > 0 && deltaSongTime > 0)
            {
                Sync(deltaSongTime / deltaTime);
            }
            else
            {
                Sync(0);
            }
        }
    }
}
