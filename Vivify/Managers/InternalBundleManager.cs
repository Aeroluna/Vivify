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

            using (Stream stream = typeof(AssetBundleManager).Assembly.GetManifestResourceStream(PATH)!)
            using (MemoryStream memoryStream = new())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }

            AssetBundle bundle = AssetBundle.LoadFromMemory(bytes, 699710125);
            DepthMaterial = new Material(bundle.LoadAsset<Shader>("assets/depthblit.shader"));
            DepthArrayMaterial = new Material(bundle.LoadAsset<Shader>("assets/depthblitarrayslice.shader"));
        }
    }
}
