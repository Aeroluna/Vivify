using UnityEngine;

namespace Vivify.Controllers.Sync
{
    [RequireComponent(typeof(Animator))]
    internal class AnimatorSyncController : SyncController
    {
        private Animator _animator = null!;

        public override void Sync(float speed)
        {
            _animator.speed = speed;
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _animator.updateMode = AnimatorUpdateMode.Normal;
        }
    }
}
