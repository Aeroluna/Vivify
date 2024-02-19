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

// ReSharper disable FieldCanBeMadeReadOnly.Local
namespace Vivify.Controllers
{
    [PlayViewControllerSettings(100, "vivify")]
    internal class AssetBundleDownloadViewController : BSMLResourceViewController, IPlayViewController
    {
        private SiraLog _log = null!;
        private Config _config = null!;
        private CoroutineBastard _coroutineBastard = null!;

        private Image _loadingBar = null!;

        private bool _doAbort;
        private bool _downloadFinished;
        private Coroutine? _downloadWaiter;
        private float _downloadProgress;
        private string _lastError = string.Empty;
        private View _currentView = View.None;
        private View _newView = View.None;

        private string? _downloadPath;
        private uint _downloadChecksum;

        [UIComponent("percentage")]
        private TMP_Text _percentageText = null!;

        [UIComponent("errortext")]
        private TMP_Text _errorText = null!;

        [UIComponent("loadingbar")]
        private VerticalLayoutGroup _barGroup = null!;

        [UIComponent("tos")]
        private VerticalLayoutGroup _tosGroup = null!;

        [UIComponent("downloading")]
        private VerticalLayoutGroup _downloadingGroup = null!;

        [UIComponent("error")]
        private VerticalLayoutGroup _error = null!;

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
            if (standardLevelParameters.DifficultyBeatmap is not CustomDifficultyBeatmap customDifficultyBeatmap)
            {
                return false;
            }

            CustomBeatmapSaveData saveData = (CustomBeatmapSaveData)customDifficultyBeatmap.beatmapSaveData;
            CustomData beatmapCustomData = saveData.beatmapCustomData;

            // check is vivify map
            string[] requirements = beatmapCustomData.Get<List<object>>("_requirements")?.Cast<string>().ToArray() ?? Array.Empty<string>();
            if (!requirements.Contains(CAPABILITY))
            {
                return false;
            }

            // check if bundle already downloaded
            string path = Path.Combine(((CustomBeatmapLevel)customDifficultyBeatmap.level).customLevelPath, BUNDLE);
            if (File.Exists(path))
            {
                return false;
            }

            CustomData levelCustomData = saveData.levelCustomData;
            uint assetBundleChecksum = levelCustomData.GetRequired<uint>(ASSET_BUNDLE);
            _doAbort = false;
            _downloadFinished = false;
            if (_config.AllowDownload)
            {
                _coroutineBastard.StartCoroutine(DownloadAndSave(
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
                        break;

                    case View.Error:
                        _tosGroup.gameObject.SetActive(false);
                        _downloadingGroup.gameObject.SetActive(false);
                        _error.gameObject.SetActive(true);
                        _errorText.text = _lastError;
                        break;
                }
            }

            if (_currentView != View.Downloading)
            {
                return;
            }

            _loadingBar.fillAmount = _downloadProgress;
            float percentage = _downloadProgress * 100;
            _percentageText.text = $"{percentage:0.0}%";
        }

        [UsedImplicitly]
        [Inject]
        private void Construct(SiraLog log, Config config, CoroutineBastard coroutineBastard)
        {
            _log = log;
            _config = config;
            _coroutineBastard = coroutineBastard;
            _newView = config.AllowDownload ? View.Downloading : View.Tos;
        }

        // TODO: figure out a way to resolve the fact that multiplayer does NOT have enough time to download bundles
        private IEnumerator DownloadAndSave(
            string savePath,
            uint checksum)
        {
            _newView = View.Downloading;
            string url = _config.BundleRepository + checksum;
            _log.Debug($"Attempting to download asset bundle from [{url}].");
            using UnityWebRequest www = UnityWebRequest.Get(url);
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
                _log.Debug("Download cancelled.");
                yield break;
            }

#pragma warning disable CS0618
            if (www.isHttpError)
            {
                _lastError = $"The server returned an error response.\n({www.responseCode})";
            }

            if (www.isNetworkError)
            {
                _lastError = $"Network error.\n{www.error}";
            }

            if (www.isNetworkError || www.isHttpError)
            {
                if (www.isNetworkError)
                {
                    _lastError = $"Network error while downloading bundle.\n{www.error}";
                    _log.Error(_lastError);
                }
                else if (www.isHttpError)
                {
                    _lastError = $"Server sent error response code while downloading bundle.\n({www.responseCode})";
                    _log.Error(_lastError);
                }

                _newView = View.Error;
                yield break;
            }
#pragma warning restore CS0618

            File.WriteAllBytes(savePath, www.downloadHandler.data);
            _log.Debug($"Successfully downloaded bundle to [{savePath}].");
            _downloadFinished = true;
        }

        [UsedImplicitly]
        [UIAction("accept-click")]
        private void OnAcceptClick()
        {
            _config.AllowDownload = true;
            if (_downloadPath != null)
            {
                _coroutineBastard.StartCoroutine(DownloadAndSave(
                    _downloadPath,
                    _downloadChecksum));
            }
        }

        [UsedImplicitly]
        private void OnShow()
        {
            if (!_downloadFinished)
            {
                _coroutineBastard.StartCoroutine(WaitForDownload());
            }
            else
            {
                Finished?.Invoke();
            }
        }

        private IEnumerator WaitForDownload()
        {
            while (!_downloadFinished)
            {
                yield return null;
            }

            Finished?.Invoke();
        }

        [UsedImplicitly]
        private void OnEarlyDismiss()
        {
            _doAbort = true;
            if (_downloadWaiter != null)
            {
                _coroutineBastard.StopCoroutine(_downloadWaiter);
            }
        }

        internal class CoroutineBastard : MonoBehaviour
        {
        }
    }
}
