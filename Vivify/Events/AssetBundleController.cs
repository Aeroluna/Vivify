namespace Vivify.Events
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

            string[] assetnames = _mainBundle.GetAllAssetNames();
            foreach (string name in assetnames)
            {
                Plugin.Logger.Log($"Loaded [{name}]");
                Assets.Add(name, _mainBundle.LoadAsset(name));
            }

            return true;
        }
    }
}
