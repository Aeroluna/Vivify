using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CustomJSONData.CustomBeatmap;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;
using static Vivify.VivifyController;
using Object = UnityEngine.Object;

namespace Vivify.Managers
{
    internal class AssetBundleManager : IDisposable
    {
        private readonly SiraLog _log;
        private readonly Dictionary<string, Object> _assets = new();
        private readonly AssetBundle? _mainBundle;

        [UsedImplicitly]
        private AssetBundleManager(SiraLog log, IDifficultyBeatmap difficultyBeatmap, Config config)
        {
            if (difficultyBeatmap is not CustomDifficultyBeatmap customDifficultyBeatmap)
            {
                throw new ArgumentException(
                    $"Was not correct type. Expected: {nameof(CustomDifficultyBeatmap)}, was: {difficultyBeatmap.GetType().Name}.",
                    nameof(difficultyBeatmap));
            }

            _log = log;

            string path = Path.Combine(((CustomBeatmapLevel)customDifficultyBeatmap.level).customLevelPath, BUNDLE);
            if (!File.Exists(path))
            {
                _log.Error($"[{BUNDLE}] not found");
                return;
            }

            if (Heck.HeckController.DebugMode)
            {
                _mainBundle = AssetBundle.LoadFromFile(path);
            }
            else
            {
                CustomData levelCustomData = ((CustomBeatmapSaveData)customDifficultyBeatmap.beatmapSaveData).levelCustomData;
                uint assetBundleChecksum = levelCustomData.GetRequired<uint>(ASSET_BUNDLE);
                _mainBundle = AssetBundle.LoadFromFile(path, assetBundleChecksum);
            }

            if (_mainBundle == null)
            {
                _log.Error($"Failed to load [{path}]");
                return;
            }

            string[] assetnames = _mainBundle.GetAllAssetNames();
            foreach (string name in assetnames)
            {
                Object asset = _mainBundle.LoadAsset(name);
                _assets.Add(name, asset);
            }

            /*
             In Version 1.31.0,
             If you have any of the DLC, AssetBundle.UnloadAllAssetBundles(true) is called in
             BeatmapLevelDataLoader.Dispose when switching scenes.
             AssetBundle used by the mod will be unloaded. */
            ////mainBundle.Unload(false);
        }

        public void Dispose()
        {
            if (_mainBundle != null)
            {
                _mainBundle.Unload(true);
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

                _log.Error($"Found {assetName}, but was null or not [{typeof(T).FullName}]");
            }
            else
            {
                _log.Error($"Could not find {typeof(T).FullName} [{assetName}]");
            }

            asset = default;
            return false;
        }
    }
}
