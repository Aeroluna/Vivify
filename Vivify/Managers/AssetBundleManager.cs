using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CustomJSONData.CustomBeatmap;
using Heck;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;
using static Vivify.VivifyController;
using Object = UnityEngine.Object;

namespace Vivify.Managers;

internal class AssetBundleManager : IDisposable
{
    private readonly Dictionary<string, Object> _assets = new();
    private readonly SiraLog _log;
    private readonly AssetBundle? _mainBundle;

    [UsedImplicitly]
    private AssetBundleManager(
        SiraLog log,
        IReadonlyBeatmapData beatmapData,
#if !PRE_V1_37_1
        BeatmapLevel beatmapLevel,
#else
        IDifficultyBeatmap difficultyBeatmap,
#endif
        Config config)
    {
#if !PRE_V1_37_1
        if (beatmapLevel.previewMediaData is not FileSystemPreviewMediaData fileSystemPreviewMediaData)
        {
            throw new ArgumentException(
                $"Was not correct type. Expected: {nameof(FileSystemPreviewMediaData)}, was: {beatmapLevel.previewMediaData.GetType().Name}.",
                nameof(beatmapLevel));
        }
#else
        if (difficultyBeatmap is not CustomDifficultyBeatmap customDifficultyBeatmap)
        {
            throw new ArgumentException(
                $"Was not correct type. Expected: {nameof(CustomDifficultyBeatmap)}, was: {difficultyBeatmap.GetType().Name}.",
                nameof(difficultyBeatmap));
        }
#endif

        if (beatmapData is not CustomBeatmapData customBeatmapData)
        {
            throw new ArgumentException(
                $"Was not correct type. Expected: {nameof(CustomBeatmapData)}, was: {beatmapData.GetType().Name}.",
                nameof(beatmapData));
        }

        _log = log;

#if !PRE_V1_37_1
        string path = Path.Combine(
            Path.GetDirectoryName(fileSystemPreviewMediaData._previewAudioClipPath)!,
            BUNDLE_FILE);
#else
        string path = Path.Combine(
            ((CustomBeatmapLevel)customDifficultyBeatmap.level).customLevelPath,
            BUNDLE_FILE);
#endif
        if (!File.Exists(path))
        {
            _log.Error($"[{BUNDLE_FILE}] not found");
            return;
        }

        if (HeckController.DebugMode)
        {
            _mainBundle = AssetBundle.LoadFromFile(path);
        }
        else
        {
            CustomData levelCustomData = customBeatmapData.levelCustomData;
            uint? assetBundleChecksum = levelCustomData.Get<CustomData>(ASSET_BUNDLE)?.Get<uint>(BUNDLE_CHECKSUM);
            if (assetBundleChecksum != null)
            {
                _mainBundle = AssetBundle.LoadFromFile(path, assetBundleChecksum.Value);
            }
            else
            {
                _log.Error("Checksum not defined");
            }
        }

        if (_mainBundle == null)
        {
            _log.Error($"Failed to load [{path}]");
            return;
        }

        string[] assetnames = _mainBundle.GetAllAssetNames();
        TMPro.TMP_FontAsset fallbackFont = null;

        foreach (string name in assetnames)
        {
            Object asset = _mainBundle.LoadAsset(name);

            // Sanitize the raw prefab asset and inject a fallback font if broken
            if (asset is GameObject gameObject)
            {
                TMPro.TMP_Text[] textComponents = gameObject.GetComponentsInChildren<TMPro.TMP_Text>(true);
                foreach (TMPro.TMP_Text textComponent in textComponents)
                {
                    if (textComponent.font == null || textComponent.fontSharedMaterial == null || textComponent.font.material == null)
                    {
                        if (fallbackFont == null)
                        {
                            TMPro.TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>();
                            foreach (TMPro.TMP_FontAsset f in fonts)
                            {
                                if (f.name == "Teko-Medium SDF")
                                {
                                    fallbackFont = f;
                                    break;
                                }
                            }
                            if (fallbackFont == null && fonts.Length > 0)
                            {
                                fallbackFont = fonts[0];
                            }
                        }

                        if (fallbackFont != null)
                        {
                            _log.Warn($"[Vivify] Repairing broken TMPro component on raw prefab: {name} with fallback font.");
                            textComponent.font = fallbackFont;
                            textComponent.fontSharedMaterial = fallbackFont.material;

                            textComponent.textWrappingMode = TMPro.TextWrappingModes.Normal;
                            textComponent.overflowMode = TMPro.TextOverflowModes.Overflow;

                            textComponent.SetAllDirty();
                            textComponent.ForceMeshUpdate(true);
                        }
                        else
                        {
                            _log.Warn($"[Vivify] Stripping broken TMPro component from raw asset bundle prefab: {name} (No fallback available)");
                            Object.DestroyImmediate(textComponent, true);
                        }
                    }
                }
            }

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
