using System;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace Vivify.Controllers.Sync;

[RequireComponent(typeof(AudioSource))]
internal class AudioSourceSyncController : MonoBehaviour, ISync
{
    private AudioTimeSyncController _audioTimeSyncController = null!;

    private float _startTime;
    private AudioSource _audioSource = null!;

    private float SongTime => _audioTimeSyncController.songTime + _audioTimeSyncController._audioLatency - _startTime;

    public void SetStartTime(float time)
    {
        _startTime = time;
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    [Inject]
    [UsedImplicitly]
    private void Construct(
        float startTime,
        AudioTimeSyncController audioTimeSyncController)
    {
        _startTime = startTime;
        _audioTimeSyncController = audioTimeSyncController;
        audioTimeSyncController.stateChangedEvent += OnStateChange;
        _audioSource.outputAudioMixerGroup = audioTimeSyncController._audioSource.outputAudioMixerGroup;
    }

    private void OnDestroy()
    {
        if (_audioTimeSyncController != null)
        {
            _audioTimeSyncController.stateChangedEvent -= OnStateChange;
        }
    }

    private void OnStateChange()
    {
        switch (_audioTimeSyncController.state)
        {
            case AudioTimeSyncController.State.Playing:
                ResyncTime();
                _audioSource.Play();
                break;

            case AudioTimeSyncController.State.Paused:
                _audioSource.Pause();
                break;

            case AudioTimeSyncController.State.Stopped:
                _audioSource.Stop();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ResyncTime()
    {
        _audioSource.time = SongTime;
    }

    private void Update()
    {
        if (SongTime >= _audioSource.clip.length)
        {
            return;
        }

        if (Math.Abs(_audioSource.time - SongTime) > 0.2)
        {
            ResyncTime();
        }
    }
}
