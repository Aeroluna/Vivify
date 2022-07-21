using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace Vivify.Controllers
{
    [RequireComponent(typeof(Animator))]
    internal class AnimatorSyncController : MonoBehaviour
    {
        private AudioTimeSyncController _audioTimeSyncController = null!;
        private Animator _animator = null!;

        private float _songTime;

        [Inject]
        [UsedImplicitly]
        private void Construct(float startTime, AudioTimeSyncController audioTimeSyncController)
        {
            _songTime = startTime;
            _audioTimeSyncController = audioTimeSyncController;
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.updateMode = AnimatorUpdateMode.Normal;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            float songTime = _audioTimeSyncController.songTime;
            float deltaSongTime = songTime - _songTime;
            _songTime = songTime;

            if (deltaTime > 0 && deltaSongTime > 0)
            {
                _animator.speed = deltaSongTime / deltaTime;
            }
            else
            {
                _animator.speed = 0;
            }
        }
    }
}
