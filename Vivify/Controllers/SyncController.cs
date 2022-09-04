using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace Vivify.Controllers
{
    internal abstract class SyncController : MonoBehaviour
    {
        private AudioTimeSyncController _audioTimeSyncController = null!;
        private float _songTime;

        public abstract void Sync(float speed);

        [Inject]
        [UsedImplicitly]
        private void Construct(float startTime, AudioTimeSyncController audioTimeSyncController)
        {
            _songTime = startTime;
            _audioTimeSyncController = audioTimeSyncController;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            float songTime = _audioTimeSyncController.songTime;
            float deltaSongTime = songTime - _songTime;
            _songTime = songTime;

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
