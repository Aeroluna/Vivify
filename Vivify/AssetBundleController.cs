namespace Vivify
{
    using System.Collections.Generic;
    using UnityEngine;

    internal static class AssetBundleController
    {
        private static AssetBundle _mainBundle;

        internal static Dictionary<string, Object> Assets { get; private set; }

        internal static Dictionary<Material, MaterialData> MaterialData { get; private set; }

        internal static T TryGetAsset<T>(string assetName)
        {
            if (Assets.TryGetValue(assetName, out Object gameObject))
            {
                if (gameObject is T t)
                {
                    return t;
                }
                else
                {
                    Plugin.Logger.Log($"Found {assetName}, but was null or not {typeof(T).FullName}!", IPA.Logging.Logger.Level.Error);
                }
            }
            else
            {
                Plugin.Logger.Log($"Could not find {typeof(T).FullName} {assetName}", IPA.Logging.Logger.Level.Error);
            }

            return default;
        }

        internal static void ClearBundle()
        {
            if (_mainBundle != null)
            {
                _mainBundle.Unload(true);
            }
        }

        internal static bool SetNewBundle(string path)
        {
            ClearBundle();

            _mainBundle = AssetBundle.LoadFromFile(path);
            if (_mainBundle == null)
            {
                Plugin.Logger.Log($"Failed to load [{path}]", IPA.Logging.Logger.Level.Error);
                return false;
            }

            Assets = new Dictionary<string, Object>();
            MaterialData = new Dictionary<Material, MaterialData>();

            string[] assetnames = _mainBundle.GetAllAssetNames();
            foreach (string name in assetnames)
            {
                Plugin.Logger.Log($"Loaded [{name}]");
                Object asset = _mainBundle.LoadAsset(name);
                Assets.Add(name, asset);

                if (asset is Material mat)
                {
                    MaterialData.Add(mat, new MaterialData(mat));
                }
            }

            return true;
        }
    }

    internal class MaterialData
    {
        internal MaterialData(Material material)
        {
            Material = material;
        }

        internal Material Material { get; }

        internal Dictionary<string, TextureRequest> TextureRequests { get; } = new Dictionary<string, TextureRequest>();
    }
}
