using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.PlayView;
using JetBrains.Annotations;
using SiraUtil.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Zenject;
using static Vivify.VivifyController;
#if !PRE_V1_37_1
using CustomJSONData;
#endif

// ReSharper disable FieldCanBeMadeReadOnly.Local
namespace Vivify.Controllers;

[PlayViewControllerSettings(100, "vivify")]
internal class AssetBundleDownloadViewController : BSMLResourceViewController, IPlayViewController
{
    [UIComponent("loadingbar")]
    private VerticalLayoutGroup _barGroup = null!;

    private Config _config = null!;
    private AssetDownloader _assetDownloader = null!;
    private View _currentView = View.None;

    private bool _doAbort;
    private uint _downloadChecksum;
    private bool _downloadFinished;

    [UIComponent("downloading")]
    private VerticalLayoutGroup _downloadingGroup = null!;

    private string? _downloadPath;
    private float _downloadProgress;
    private Coroutine? _downloadWaiter;

    [UIComponent("error")]
    private VerticalLayoutGroup _error = null!;

    [UIComponent("errortext")]
    private TMP_Text _errorText = null!;

    private string _lastError = string.Empty;

    private Image _loadingBar = null!;
    private SiraLog _log = null!;
    private View _newView = View.None;

    [UIComponent("percentage")]
    private TMP_Text _percentageText = null!;

    [UIComponent("tos")]
    private VerticalLayoutGroup _tosGroup = null!;

    public event Action? Finished;

    private enum View
    {
        None,
        Tos,
        Downloading,
        Error
    }

    public override string ResourceName => "Vivify.Resources.AssetBundleDownloading.bsml";

    public bool Init(StartStandardLevelParameters standardLevelParameters)
    {
#if !PRE_V1_37_1
        if (standardLevelParameters.BeatmapLevel.previewMediaData is not FileSystemPreviewMediaData fileSystemPreviewMediaData)
        {
            return false;
        }

        CustomData beatmapCustomData =
            standardLevelParameters.BeatmapLevel.GetBeatmapCustomData(standardLevelParameters.BeatmapKey);
        CustomData levelCustomData = standardLevelParameters.BeatmapLevel.GetLevelCustomData();
#else
        if (standardLevelParameters.DifficultyBeatmap is not CustomDifficultyBeatmap customDifficultyBeatmap)
        {
            return false;
        }

        Version3CustomBeatmapSaveData saveData = (Version3CustomBeatmapSaveData)customDifficultyBeatmap.beatmapSaveData;
        CustomData beatmapCustomData = saveData.beatmapCustomData;
        CustomData levelCustomData = saveData.levelCustomData;
#endif

        // check is vivify map
        string[] requirements = beatmapCustomData.Get<List<object>>("_requirements")?.Cgit sast<string>().ToArray() ?? [];
        if (!requirements.Contains(CAPABILITY))
        {
            return false;
        }

        // check if bundle already downloaded
#if !PRE_V1_37_1
        string path =
            Path.Combine(
                Path.GetDirectoryName(fileSystemPreviewMediaData._previewAudioClipPath)!,
                BUNDLE_FILE);
#else
        string path = Path.Combine(
            ((CustomBeatmapLevel)customDifficultyBeatmap.level).customLevelPath,
            BUNDLE_FILE);
#endif
        if (File.Exists(path))
        {
            return false;
        }

        _log.Error($"[{path}] not found, attempting to download remotely");
        uint assetBundleChecksum =
            levelCustomData.GetRequired<CustomData>(ASSET_BUNDLE).GetRequired<uint>(BUNDLE_CHECKSUM);
        _doAbort = false;
        _downloadFinished = false;
        if (_config.AllowDownload)
        {
            _assetDownloader.StartCoroutine(
                DownloadAndSave(
                    path,
                    assetBundleChecksum));
        }
        else
        {
            _downloadPath = path;
            _downloadChecksum = assetBundleChecksum;
        }

        return true;
    }

    [UsedImplicitly]
    [Inject]
    private void Construct(SiraLog log, Config config, AssetDownloader assetDownloader)
    {
        _log = log;
        _config = config;
        _assetDownloader = assetDownloader;
        _newView = config.AllowDownload ? View.Downloading : View.Tos;
    }

    // TODO: figure out a way to resolve the fact that multiplayer does NOT have enough time to download bundles
    private IEnumerator DownloadAndSave(
        string savePath,
        uint checksum)
    {
        _newView = View.Downloading;
        string url = _config.BundleRepository + checksum;
        _log.Debug($"Fetching asset bundle info from [{url}]");
        using UnityWebRequest apiRequest = UnityWebRequest.Get(url);
        apiRequest.SendWebRequest();

        while (!apiRequest.isDone)
        {
            if (!_doAbort)
            {
                yield return null;
                continue;
            }

            apiRequest.Abort();
            _log.Debug("Fetch cancelled");
            yield break;
        }

#pragma warning disable CS0618
        if (apiRequest.isNetworkError || apiRequest.isHttpError)
        {
            if (apiRequest.isNetworkError)
            {
                _lastError = $"Network error while fetching bundle.\n{apiRequest.error}";
            }
            else if (apiRequest.isHttpError)
            {
                _lastError = $"Server sent error response code while fetching bundle.\n({apiRequest.responseCode})";
            }

            _log.Error(_lastError);
            _newView = View.Error;
            yield break;
        }
#pragma warning restore CS0618

        RepoJson repoJson = JsonUtility.FromJson<RepoJson>(apiRequest.downloadHandler.text);
        string downloadUrl = repoJson.downloadUrl;
        _log.Debug($"Attempting to download asset bundle from [{downloadUrl}]");
        using UnityWebRequest www = UnityWebRequest.Get(downloadUrl);
        www.SendWebRequest();
        while (!www.isDone)
        {
            if (!_doAbort)
            {
                _downloadProgress = www.downloadProgress;
                yield return null;
                continue;
            }

            www.Abort();
            _log.Debug("Download cancelled");
            yield break;
        }

#pragma warning disable CS0618
        if (www.isNetworkError || www.isHttpError)
        {
            if (www.isNetworkError)
            {
                _lastError = $"Network error while downloading bundle.\n{www.error}";
            }
            else if (www.isHttpError)
            {
                _lastError = $"Server sent error response code while downloading bundle.\n({www.responseCode})";
            }

            _log.Error(_lastError);
            _newView = View.Error;
            yield break;
        }
#pragma warning restore CS0618

        File.WriteAllBytes(savePath, www.downloadHandler.data);
        _log.Debug($"Successfully downloaded bundle to [{savePath}]");
        _downloadFinished = true;
    }

    [UsedImplicitly]
    [UIAction("accept-click")]
    private void OnAcceptClick()
    {
        _config.AllowDownload = true;
        if (_downloadPath != null)
        {
            _assetDownloader.StartCoroutine(
                DownloadAndSave(
                    _downloadPath,
                    _downloadChecksum));
        }
    }

    [UsedImplicitly]
    private void OnEarlyDismiss()
    {
        _doAbort = true;
        if (_downloadWaiter != null)
        {
            _assetDownloader.StopCoroutine(_downloadWaiter);
        }
    }

    [UsedImplicitly]
    private void OnShow()
    {
        if (!_downloadFinished)
        {
            _assetDownloader.StartCoroutine(WaitForDownload());
        }
        else
        {
            Finished?.Invoke();
            _newView = View.None;
        }
    }

    private void Start()
    {
        Vector2 loadingBarSize = new(0, 8);

        // shamelessly stolen from songcore
        _loadingBar = new GameObject("Loading Bar").AddComponent<Image>();
        RectTransform barTransform = (RectTransform)_loadingBar.transform;
        barTransform.SetParent(_barGroup.transform, false);
        barTransform.sizeDelta = loadingBarSize;
        Texture2D? tex = Texture2D.whiteTexture;
        Sprite? sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);
        _loadingBar.sprite = sprite;
        _loadingBar.type = Image.Type.Filled;
        _loadingBar.fillMethod = Image.FillMethod.Horizontal;
        _loadingBar.color = new Color(1, 1, 1, 0.5f);

        Image loadingBackg = new GameObject("Background").AddComponent<Image>();
        RectTransform loadingBackTransform = (RectTransform)loadingBackg.transform;
        loadingBackTransform.sizeDelta = loadingBarSize;
        loadingBackTransform.SetParent(_barGroup.transform, false);
        loadingBackg.color = new Color(0, 0, 0, 0.2f);
    }

    private void Update()
    {
        if (_currentView != _newView)
        {
            _currentView = _newView;
            switch (_currentView)
            {
                case View.Tos:
                    _tosGroup.gameObject.SetActive(true);
                    _downloadingGroup.gameObject.SetActive(false);
                    _error.gameObject.SetActive(false);
                    break;

                case View.Downloading:
                    _tosGroup.gameObject.SetActive(false);
                    _downloadingGroup.gameObject.SetActive(true);
                    _error.gameObject.SetActive(false);

                    _loadingBar.fillAmount = _downloadProgress;
                    float percentage = _downloadProgress * 100;
                    _percentageText.text = $"{percentage:0.0}%";
                    break;

                case View.Error:
                    _tosGroup.gameObject.SetActive(false);
                    _downloadingGroup.gameObject.SetActive(false);
                    _error.gameObject.SetActive(true);
                    _errorText.text = _lastError;
                    break;

                case View.None:
                    _tosGroup.gameObject.SetActive(false);
                    _downloadingGroup.gameObject.SetActive(false);
                    _error.gameObject.SetActive(false);
                    break;
            }
        }
    }

    private IEnumerator WaitForDownload()
    {
        while (!_downloadFinished)
        {
            yield return null;
        }

        Finished?.Invoke();
        _newView = View.None;
    }

    internal class AssetDownloader : MonoBehaviour;

    [Serializable]
    private class RepoJson
    {
        // might be ugly, but damn is it fast
#pragma warning disable SA1401
#pragma warning disable SA1307
#pragma warning disable CS8618
        public string downloadUrl;
#pragma warning restore CS8618
#pragma warning restore SA1307
#pragma warning restore SA1401
    }
}
