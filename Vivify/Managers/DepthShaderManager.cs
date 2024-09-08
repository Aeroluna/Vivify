using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Vivify.Managers;

internal class DepthShaderManager : IInitializable
{
    private const string PATH = "Vivify.Resources.DepthBlit";

    internal Material? DepthArrayMaterial { get; private set; }

    internal Material? DepthMaterial { get; private set; }

    public void Initialize()
    {
        _ = Load();
    }

    // shamelessly stolen from AssetBundleLoadingTools
    private static async Task<AssetBundle?> LoadFromMemoryAsync(byte[] binary, uint crc)
    {
        TaskCompletionSource<AssetBundle> taskCompletionSource = new();
        AssetBundleCreateRequest? bundleRequest = AssetBundle.LoadFromMemoryAsync(binary, crc);
        bundleRequest.completed += _ =>
        {
            taskCompletionSource.SetResult(bundleRequest.assetBundle);
        };

        return await taskCompletionSource.Task;
    }

    private static async Task<T?> LoadAssetAsync<T>(AssetBundle assetBundle, string path)
        where T : Object
    {
        TaskCompletionSource<T> taskCompletionSource = new();
        AssetBundleRequest? assetRequest = assetBundle.LoadAssetAsync<T>(path);
        assetRequest.completed += _ =>
        {
            taskCompletionSource.SetResult((T)assetRequest.asset);
        };

        return await taskCompletionSource.Task;
    }

    private async Task Load()
    {
        byte[] bytes;

        using (Stream stream = typeof(DepthShaderManager).Assembly.GetManifestResourceStream(PATH)!)
        using (MemoryStream memoryStream = new())
        {
            await stream.CopyToAsync(memoryStream);
            bytes = memoryStream.ToArray();
        }

#if V1_29_1
        const uint crc = 1355036397;
#else
        const uint crc = 1746663828;
#endif
        AssetBundle? bundle = await LoadFromMemoryAsync(bytes, crc);
        if (bundle == null)
        {
            return;
        }

        Task getDepthBlit = LoadAssetAsync<Material>(bundle, "assets/depthblit.mat")
            .ContinueWith(n => DepthMaterial = n.Result);
        Task getDepthBlitArraySlice = LoadAssetAsync<Material>(bundle, "assets/depthblitarrayslice.mat")
            .ContinueWith(n => DepthArrayMaterial = n.Result);
        await Task.WhenAll(getDepthBlit, getDepthBlitArraySlice);
#if V1_29_1
        bundle.Unload(false);
#else
        bundle.UnloadAsync(false);
#endif
    }
}
