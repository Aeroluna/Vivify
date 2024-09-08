using System.IO;
using UnityEngine;

namespace Vivify.Managers;

internal static class DepthShaderManager
{
    private const string PATH = "Vivify.Resources.DepthBlit";

    internal static Material DepthArrayMaterial { get; private set; } = null!;

    internal static Material DepthMaterial { get; private set; } = null!;

    // TODO: use async so this doesnt block
    internal static void LoadFromMemory()
    {
        byte[] bytes;

        using (Stream stream = typeof(DepthShaderManager).Assembly.GetManifestResourceStream(PATH)!)
        using (MemoryStream memoryStream = new())
        {
            stream.CopyTo(memoryStream);
            bytes = memoryStream.ToArray();
        }

#if V1_29_1
        const uint crc = 1355036397;
#else
        const uint crc = 1746663828;
#endif
        AssetBundle bundle = AssetBundle.LoadFromMemory(bytes, crc);
        DepthMaterial = bundle.LoadAsset<Material>("assets/depthblit.mat");
        DepthArrayMaterial = bundle.LoadAsset<Material>("assets/depthblitarrayslice.mat");
        bundle.Unload(false);
    }
}
