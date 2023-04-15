using System.IO;
using UnityEngine;

namespace Vivify.Managers
{
    internal static class InternalBundleManager
    {
        private const string PATH = "Vivify.Managers.DepthBlitShaders";

        internal static Material DepthMaterial { get; private set; } = null!;

        internal static Material DepthArrayMaterial { get; private set; } = null!;

        internal static void LoadFromMemory()
        {
            byte[] bytes;

            using (Stream stream = typeof(InternalBundleManager).Assembly.GetManifestResourceStream(PATH)!)
            using (MemoryStream memoryStream = new())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }

            AssetBundle bundle = AssetBundle.LoadFromMemory(bytes, 1737660153);
            DepthMaterial = bundle.LoadAsset<Material>("assets/depthblit.mat");
            DepthArrayMaterial = bundle.LoadAsset<Material>("assets/depthblitarrayslice.mat");
        }
    }
}
