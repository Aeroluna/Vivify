using System.Collections.Generic;
using UnityEngine;
using Logger = IPA.Logging.Logger;

namespace Vivify
{
    internal static class AssetBundleController
    {
        private static AssetBundle? _mainBundle;

        internal static Dictionary<string, Object> Assets { get; private set; } = new();

        internal static Dictionary<string, GameObject> InstantiatedPrefabs { get; private set; } = new();

        internal static T? TryGetAsset<T>(string assetName)
        {
            if (Assets.TryGetValue(assetName, out Object gameObject))
            {
                if (gameObject is T t)
                {
                    return t;
                }

                Log.Logger.Log($"Found {assetName}, but was null or not {typeof(T).FullName}!", Logger.Level.Error);
            }
            else
            {
                Log.Logger.Log($"Could not find {typeof(T).FullName} {assetName}", Logger.Level.Error);
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
                Log.Logger.Log($"Failed to load [{path}]", Logger.Level.Error);
                return false;
            }

            Assets = new Dictionary<string, Object>();
            InstantiatedPrefabs = new Dictionary<string, GameObject>();

            string[] assetnames = _mainBundle.GetAllAssetNames();
            foreach (string name in assetnames)
            {
                Log.Logger.Log($"Loaded [{name}]");
                Object asset = _mainBundle.LoadAsset(name);
                Assets.Add(name, asset);
            }

            return true;
        }
    }
}
