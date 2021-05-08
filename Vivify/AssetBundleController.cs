namespace Vivify
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CustomJSONData;
    using CustomJSONData.CustomBeatmap;
    using UnityEngine;

    internal static class AssetBundleController
    {
        private static AssetBundle _mainBundle;

        internal static Dictionary<string, UnityEngine.Object> Assets { get; private set; }

        internal static Dictionary<Material, MaterialData> MaterialData { get; private set; }

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

            Assets = new Dictionary<string, UnityEngine.Object>();
            MaterialData = new Dictionary<Material, MaterialData>();

            string[] assetnames = _mainBundle.GetAllAssetNames();
            foreach (string name in assetnames)
            {
                Plugin.Logger.Log($"Loaded [{name}]");
                UnityEngine.Object asset = _mainBundle.LoadAsset(name);
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
        internal Material Material { get; }

        internal Dictionary<string, string> TextureRequests { get; } = new Dictionary<string, string>();

        internal MaterialData(Material material)
        {
            Material = material;
        }
    }
}
