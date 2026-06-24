using System;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Live.Compete.Services {
    internal class CompeteFpsStabilityStartGate : IInitializable, ITickable, IDisposable {
        private const float StabilityDurationSeconds = 0.3f;
        private const float MaxWaitSeconds = 5f;
        private const float FallbackRefreshRate = 80f;

        private readonly CompeteGameplayState _gameplayState;
        private readonly AudioTimeSyncController _audioTimeSyncController;
        private readonly SongController _songController;
        private readonly ScoreController _scoreController;

        private bool _waitingForInitialStart = true;
        private bool _initialPausePending;
        private bool _waitingForStableFps;
        private bool _scoreControllerWasEnabled;
        private bool _scoreControllerStateCaptured;
        private int _initialPauseFrame;
        private float _stableSeconds;
        private float _waitSeconds;
        private float _fpsThreshold;

        internal CompeteFpsStabilityStartGate(
            CompeteGameplayState gameplayState,
            AudioTimeSyncController audioTimeSyncController,
            SongController songController,
            [InjectOptional] ScoreController scoreController) {

            _gameplayState = gameplayState;
            _audioTimeSyncController = audioTimeSyncController;
            _songController = songController;
            _scoreController = scoreController;
        }

        public void Initialize() {
            _fpsThreshold = RecommendedFpsThreshold();
            _audioTimeSyncController.stateChangedEvent += AudioTimeSyncControllerStateChangedEvent;
        }

        public void Dispose() {
            _audioTimeSyncController.stateChangedEvent -= AudioTimeSyncControllerStateChangedEvent;
            RestoreScoreController();
        }

        public void Tick() {
            if (_initialPausePending) {
                if (Time.frameCount <= _initialPauseFrame) {
                    return;
                }

                StartStableFpsWait();
                return;
            }

            if (!_waitingForStableFps) {
                return;
            }

            if (!_gameplayState.IsLiveGameplayActive) {
                CancelStableFpsWait("live gameplay ended before FPS stabilized.");
                return;
            }

            if (!IsAudioPaused()) {
                CancelStableFpsWait("playback state changed before FPS stabilized; leaving song alone.");
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f) {
                return;
            }

            _waitSeconds += deltaTime;
            float fps = 1f / deltaTime;
            if (fps >= _fpsThreshold) {
                _stableSeconds += deltaTime;
            } else {
                _stableSeconds = 0f;
            }

            if (_stableSeconds >= StabilityDurationSeconds) {
                ResumeSong($"FPS stabilized at {fps:0}.");
            } else if (_waitSeconds >= MaxWaitSeconds) {
                ResumeSong($"FPS did not stabilize within {MaxWaitSeconds:0.#}s; starting anyway.");
            }
        }

        private void AudioTimeSyncControllerStateChangedEvent() {
            if (!_waitingForInitialStart || !_gameplayState.IsLiveGameplayActive || !IsAudioPlaying()) {
                return;
            }

            _waitingForInitialStart = false;
            _initialPauseFrame = Time.frameCount;
            _initialPausePending = true;
        }

        private void StartStableFpsWait() {
            _initialPausePending = false;
            if (!_gameplayState.IsLiveGameplayActive || !IsAudioPlaying()) {
                Plugin.Log.Info("Ludus: Skipping FPS start gate because playback is no longer starting.");
                return;
            }

            _stableSeconds = 0f;
            _waitSeconds = 0f;
            _scoreControllerWasEnabled = _scoreController?.enabled ?? false;
            _scoreControllerStateCaptured = _scoreController != null;

            if (_scoreController != null) {
                _scoreController.enabled = false;
            }

            _songController.PauseSong();
            if (!IsAudioPaused()) {
                RestoreScoreController();
                Plugin.Log.Warn("Ludus: Skipping FPS start gate because the start pause did not take.");
                return;
            }

            _waitingForStableFps = true;
            Plugin.Log.Info($"Ludus: Waiting for stable FPS before live map start (target {_fpsThreshold:0}+ FPS).");
        }

        private void ResumeSong(string reason) {
            if (!_waitingForStableFps) {
                return;
            }

            _waitingForStableFps = false;
            RestoreScoreController();
            if (IsAudioPaused()) {
                _songController.ResumeSong();
            } else {
                Plugin.Log.Warn("Ludus: FPS start gate ended while playback was not paused; leaving song alone.");
            }

            Plugin.Log.Info($"Ludus: {reason}");
        }

        private void CancelStableFpsWait(string reason) {
            if (!_waitingForStableFps) {
                return;
            }

            _waitingForStableFps = false;
            RestoreScoreController();
            Plugin.Log.Warn($"Ludus: Canceling FPS start gate: {reason}");
        }

        private void RestoreScoreController() {
            if (_scoreController != null && _scoreControllerStateCaptured) {
                _scoreController.enabled = _scoreControllerWasEnabled;
                _scoreControllerStateCaptured = false;
            }
        }

        private static float RecommendedFpsThreshold() {
            float refreshRate = UnityEngine.XR.XRDevice.refreshRate;
            if (refreshRate <= 0f) {
                refreshRate = FallbackRefreshRate;
            }

            return Mathf.Max(1f, Mathf.Round(Mathf.Round(refreshRate) / 5f) * 5f - 5f);
        }

        private bool IsAudioPlaying() {
#if BEAT_SABER_1_42_0
            return _audioTimeSyncController.state == IAudioTimeSource.State.Playing;
#else
            return _audioTimeSyncController.state == AudioTimeSyncController.State.Playing;
#endif
        }

        private bool IsAudioPaused() {
#if BEAT_SABER_1_42_0
            return _audioTimeSyncController.state == IAudioTimeSource.State.Paused;
#else
            return _audioTimeSyncController.state == AudioTimeSyncController.State.Paused;
#endif
        }
    }
}
