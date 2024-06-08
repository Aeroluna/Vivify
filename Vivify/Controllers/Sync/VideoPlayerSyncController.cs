using System;
using System.Collections;
using JetBrains.Annotations;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.Video;
using Zenject;

namespace Vivify.Controllers.Sync
{
    [RequireComponent(typeof(VideoPlayer))]
    internal class VideoPlayerSyncController : MonoBehaviour, ISync
    {
        private SiraLog _log = null!;
        private VideoPlayer _videoPlayer = null!;
        private AudioTimeSyncController _audioTimeSyncController = null!;
        private float _startTime;

        private bool _seeking;

        private float SongTime => _audioTimeSyncController.songTime - _startTime;

        public void SetStartTime(float time)
        {
            _startTime = time;
        }

        [Inject]
        [UsedImplicitly]
        private void Construct(
            SiraLog log,
            float startTime,
            AudioTimeSyncController audioTimeSyncController)
        {
            _log = log;
            _startTime = startTime;
            _audioTimeSyncController = audioTimeSyncController;
            audioTimeSyncController.stateChangedEvent += OnStateChange;
        }

        private void OnStateChange()
        {
            if (!_videoPlayer.isPrepared)
            {
                return;
            }

            switch (_audioTimeSyncController.state)
            {
                case AudioTimeSyncController.State.Playing:
                    ResyncTime();
                    _videoPlayer.Play();
                    break;

                case AudioTimeSyncController.State.Paused:
                    _videoPlayer.Pause();
                    break;

                case AudioTimeSyncController.State.Stopped:
                    _videoPlayer.Stop();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Awake()
        {
            _videoPlayer = GetComponent<VideoPlayer>();
            _videoPlayer.errorReceived += OnErrorRecieved;
            _videoPlayer.prepareCompleted += OnPrepareCompleted;
            _videoPlayer.skipOnDrop = false;
        }

        private void OnEnable()
        {
            StartCoroutine(Prepare());
        }

        private void OnDestroy()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.errorReceived -= OnErrorRecieved;
                _videoPlayer.prepareCompleted -= OnPrepareCompleted;
            }

            if (_audioTimeSyncController != null)
            {
                _audioTimeSyncController.stateChangedEvent -= OnStateChange;
            }
        }

        private void Update()
        {
            if (!_videoPlayer.isPrepared)
            {
                return;
            }

            if (Math.Abs(_videoPlayer.time - SongTime) > 0.2)
            {
                ResyncTime();
            }
        }

        private void ResyncTime()
        {
            if (_seeking)
            {
                return;
            }

            _seeking = true;
            _videoPlayer.playbackSpeed = _audioTimeSyncController.timeScale;
            _videoPlayer.seekCompleted += OnSeekCompleted;
            _videoPlayer.time = SongTime;
        }

        private void OnSeekCompleted(VideoPlayer _)
        {
            _videoPlayer.seekCompleted -= OnSeekCompleted;
            StartCoroutine(SeekCompleteDelay());
        }

        private void OnErrorRecieved(VideoPlayer _, string error)
        {
            _log.Error(error);
        }

        private void OnPrepareCompleted(VideoPlayer _)
        {
            OnStateChange();
        }

        private IEnumerator Prepare()
        {
            yield return new WaitUntil(() => _videoPlayer != null && _videoPlayer.isActiveAndEnabled);
            _videoPlayer.Prepare();
        }

        private IEnumerator SeekCompleteDelay()
        {
            yield return new WaitForEndOfFrame();
            _seeking = false;
        }
    }
}
