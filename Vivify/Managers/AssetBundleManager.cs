using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CustomJSONData.CustomBeatmap;
using JetBrains.Annotations;
using UnityEngine;
using static Vivify.VivifyController;
using Logger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

namespace Vivify.Managers
{
    internal class AssetBundleManager : IDisposable
    {
        private readonly Dictionary<string, Object> _assets = new();

        [UsedImplicitly]
        private AssetBundleManager(IDifficultyBeatmap difficultyBeatmap, Config config)
        {
            AssetBundle mainBundle;
            if (difficultyBeatmap is not CustomDifficultyBeatmap customDifficultyBeatmap)
            {
                throw new ArgumentException(
                    $"Was not correct type. Expected: {nameof(CustomDifficultyBeatmap)}, was: {difficultyBeatmap.GetType().Name}.",
                    nameof(difficultyBeatmap));
            }

            string path = Path.Combine(((CustomBeatmapLevel)customDifficultyBeatmap.level).customLevelPath, BUNDLE);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"[{BUNDLE}] not found!"); // TODO: Figure out a way to not just obliterate everything
            }

            if (Heck.HeckController.DebugMode)
            {
                mainBundle = AssetBundle.LoadFromFile(path);
            }
            else
            {
                CustomData levelCustomData = ((CustomBeatmapSaveData)customDifficultyBeatmap.beatmapSaveData).levelCustomData;
                uint assetBundleChecksum = levelCustomData.GetRequired<uint>(ASSET_BUNDLE);
                mainBundle = AssetBundle.LoadFromFile(path, assetBundleChecksum);
            }

            if (mainBundle == null)
            {
                throw new InvalidOperationException($"Failed to load [{path}]");
            }

            string[] assetnames = mainBundle.GetAllAssetNames();
            foreach (string name in assetnames)
            {
                Log.Logger.Log($"Loaded [{name}].");
                Object asset = mainBundle.LoadAsset(name);
                _assets.Add(name, asset);
            }

            mainBundle.Unload(false);
        }

        public void Dispose()
        {
            foreach (Object asset in _assets.Values)
            {
                Object.Destroy(asset);
            }
        }

        internal bool TryGetAsset<T>(string assetName, [NotNullWhen(true)] out T? asset)
        {
            if (_assets.TryGetValue(assetName, out Object gameObject))
            {
                if (gameObject is T t)
                {
                    asset = t;
                    return true;
                }

                Log.Logger.Log($"Found {assetName}, but was null or not [{typeof(T).FullName}]!", Logger.Level.Error);
            }
            else
            {
                Log.Logger.Log($"Could not find {typeof(T).FullName} [{assetName}].", Logger.Level.Error);
            }

            asset = default;
            return false;
        }
    }
}
