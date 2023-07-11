namespace Vivify.PostProcessing
{
    // Cannot copy BloomPrePass because it initializes on Awake()
    internal class LateBloomPrePass : BloomPrePass
    {
#pragma warning disable CA1822
        private new void Awake()
#pragma warning restore CA1822
        {
        }

        private void Start()
        {
            base.Awake();
        }
    }
}
