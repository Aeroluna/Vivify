using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using Logger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

namespace Vivify.Managers
{
    internal class AssetBundleManager : IDisposable
    {
        private const string BUNDLE = "bundle";

        private readonly Dictionary<string, Object> _assets = new();

        private readonly AssetBundle _mainBundle;

        [UsedImplicitly]
        private AssetBundleManager(IDifficultyBeatmap difficultyBeatmap)
        {
            if (difficultyBeatmap is not CustomDifficultyBeatmap { level: CustomBeatmapLevel customBeatmapLevel })
            {
                throw new ArgumentException(
                    $"Was not correct type. Expected: {nameof(CustomDifficultyBeatmap)}, was: {difficultyBeatmap.GetType().Name}.",
                    nameof(difficultyBeatmap));
            }

            string path = Path.Combine(customBeatmapLevel.customLevelPath, BUNDLE);

            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"[{BUNDLE}] not found!");
            }

            _mainBundle = AssetBundle.LoadFromFile(path);
            if (_mainBundle == null)
            {
                throw new InvalidOperationException($"Failed to load [{path}]");
            }

            string[] assetnames = _mainBundle.GetAllAssetNames();
            foreach (string name in assetnames)
            {
                Log.Logger.Log($"Loaded [{name}].");
                Object asset = _mainBundle.LoadAsset(name);
                _assets.Add(name, asset);
            }
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
