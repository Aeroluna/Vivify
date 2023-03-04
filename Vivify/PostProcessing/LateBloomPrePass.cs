namespace Vivify.PostProcessing
{
    // Cannot copy BloomPrePass because it initializes on Awake()
    internal class LateBloomPrePass : BloomPrePass
    {
        public override void Awake()
        {
        }

        private void Start()
        {
            base.Awake();
        }
    }
}
