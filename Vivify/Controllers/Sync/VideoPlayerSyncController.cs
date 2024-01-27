using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Video;
using Zenject;

namespace Vivify.Controllers.Sync
{
    [RequireComponent(typeof(VideoPlayer))]
    internal class VideoPlayerSyncController : MonoBehaviour
    {
        private VideoPlayer _videoPlayer = null!;
        private AudioTimeSyncController _audioTimeSyncController = null!;
        private float _startTime;

        [Inject]
        [UsedImplicitly]
        private void Construct(float startTime, AudioTimeSyncController audioTimeSyncController)
        {
            _startTime = startTime;
            _audioTimeSyncController = audioTimeSyncController;
            audioTimeSyncController.stateChangedEvent += OnStateChange;
        }

        private void OnStateChange()
        {
            switch (_audioTimeSyncController.state)
            {
                case AudioTimeSyncController.State.Playing:
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
        }

        private void OnDestroy()
        {
            if (_audioTimeSyncController != null)
            {
                _audioTimeSyncController.stateChangedEvent -= OnStateChange;
            }
        }

        private void Update()
        {
            _videoPlayer.playbackSpeed = _audioTimeSyncController.timeScale;
            _videoPlayer.time = _audioTimeSyncController.songTime - _startTime;
        }
    }
}
