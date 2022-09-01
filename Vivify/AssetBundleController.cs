using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using Logger = IPA.Logging.Logger;
using Object = UnityEngine.Object;

namespace Vivify
{
    internal class AssetBundleController : IDisposable
    {
        private const string BUNDLE = "bundle";

        private readonly AssetBundle _mainBundle;

        [UsedImplicitly]
        private AssetBundleController(IDifficultyBeatmap difficultyBeatmap)
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
                Assets.Add(name, asset);
            }
        }

        internal Dictionary<string, Object> Assets { get; } = new();

        internal Dictionary<string, GameObject> InstantiatedPrefabs { get; } = new();

        public void Dispose()
        {
            if (_mainBundle != null)
            {
                _mainBundle.Unload(true);
            }
        }

        internal T? TryGetAsset<T>(string assetName)
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

        internal void DestroyAllPrefabs()
        {
            foreach (KeyValuePair<string, GameObject> keyValuePair in InstantiatedPrefabs)
            {
                Object.Destroy(keyValuePair.Value);
            }

            InstantiatedPrefabs.Clear();
        }
    }
}
