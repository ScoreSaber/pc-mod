using ScoreSaber.Features.Replays.Format;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Replays.Recorders {
    internal class PauseEventRecorder : TimeSynchronizer, IInitializable, IDisposable {
        private const int InitialPauseEventCapacity = 4;

        private readonly IGamePause _gamePause;
        private readonly List<PauseEvent> _pauseEvents;
        private bool _paused;
        private float _pauseSongTime;
        private float _pauseRealtime;
        private long _pauseUnixStartTime;

        public PauseEventRecorder([InjectOptional] IGamePause gamePause) {

            _gamePause = gamePause;
            _pauseEvents = new List<PauseEvent>(InitialPauseEventCapacity);
        }

        public void Initialize() {

            if (_gamePause != null) {
                _gamePause.didPauseEvent += GamePauseDidPauseEvent;
                _gamePause.didResumeEvent += GamePauseDidResumeEvent;
            }
        }

        public void Dispose() {

            if (_gamePause != null) {
                _gamePause.didPauseEvent -= GamePauseDidPauseEvent;
                _gamePause.didResumeEvent -= GamePauseDidResumeEvent;
            }
        }

        public List<PauseEvent> Export() {

            FinishOpenPause();
            return _pauseEvents;
        }

        private void GamePauseDidPauseEvent() {

            if (_paused) {
                return;
            }

            _paused = true;
            _pauseSongTime = audioTimeSyncController.songTime;
            _pauseRealtime = Time.realtimeSinceStartup;
            _pauseUnixStartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private void GamePauseDidResumeEvent() => FinishOpenPause();

        private void FinishOpenPause() {

            if (!_paused) {
                return;
            }

            long duration = Math.Max(0L, (long)Math.Round((Time.realtimeSinceStartup - _pauseRealtime) * 1000f));
            long unixEndTime = Math.Max(_pauseUnixStartTime, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            _pauseEvents.Add(new PauseEvent {
                Time = _pauseSongTime,
                Duration = duration,
                UnixStartTime = _pauseUnixStartTime,
                UnixEndTime = unixEndTime
            });
            _paused = false;
        }
    }
}
