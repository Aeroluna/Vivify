using System.IO;
using UnityEngine;

namespace Vivify.Managers
{
    internal static class DepthShaderManager
    {
        private const string PATH = "Vivify.Resources.DepthBlitShaders";

        internal static Material DepthMaterial { get; private set; } = null!;

        internal static Material DepthArrayMaterial { get; private set; } = null!;

        internal static void LoadFromMemory()
        {
            byte[] bytes;

            using (Stream stream = typeof(DepthShaderManager).Assembly.GetManifestResourceStream(PATH)!)
            using (MemoryStream memoryStream = new())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }

            AssetBundle bundle = AssetBundle.LoadFromMemory(bytes, 511639300);
            DepthMaterial = bundle.LoadAsset<Material>("assets/depthblit.mat");
            DepthArrayMaterial = bundle.LoadAsset<Material>("assets/depthblitarrayslice.mat");
            bundle.Unload(false);
        }
    }
}
