using System.IO;
using UnityEngine;

namespace Vivify.Managers
{
    internal static class InternalBundleManager
    {
        private const string PATH = "Vivify.vivifybundle";

        internal static Material DepthMaterial { get; private set; } = null!;

        internal static void LoadFromMemory()
        {
            byte[] bytes;

            using (Stream stream = typeof(AssetBundleManager).Assembly.GetManifestResourceStream(PATH)!)
            using (MemoryStream memoryStream = new())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }

            AssetBundle bundle = AssetBundle.LoadFromMemory(bytes, 1414251160);
            DepthMaterial = new Material(bundle.LoadAsset<Shader>("assets/depthblit.shader"));
        }
    }
}
