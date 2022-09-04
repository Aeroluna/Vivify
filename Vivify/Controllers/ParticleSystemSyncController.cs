using UnityEngine;

namespace Vivify.Controllers
{
    [RequireComponent(typeof(ParticleSystem))]
    internal class ParticleSystemSyncController : SyncController
    {
        private ParticleSystem _particleSystem = null!;

        public override void Sync(float speed)
        {
            ParticleSystem.MainModule particleSystemMain = _particleSystem.main;
            particleSystemMain.simulationSpeed = speed;
        }

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }
    }
}
