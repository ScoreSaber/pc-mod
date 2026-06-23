using ScoreSaber.Features.Live.Replay;
using ScoreSaber.Features.Replays.Format;
using System;
using System.Collections.Generic;
using Zenject;

namespace ScoreSaber.Features.Replays.Recorders {
    internal class HeightEventRecorder : TimeSynchronizer, IInitializable, IDisposable {
        private const int InitialHeightEventCapacity = 8;

        private readonly PlayerHeightDetector _playerHeightDetector;
        private readonly LiveReplayStreamingService _liveReplayStreamingService;
        private readonly List<HeightEvent> _heightKeyframes;

        public HeightEventRecorder([InjectOptional] PlayerHeightDetector playerHeightDetector, LiveReplayStreamingService liveReplayStreamingService) {

            _playerHeightDetector = playerHeightDetector;
            _liveReplayStreamingService = liveReplayStreamingService;
            _heightKeyframes = new List<HeightEvent>(InitialHeightEventCapacity);
        }

        public void Initialize() {

            if (_playerHeightDetector != null) {
                _playerHeightDetector.playerHeightDidChangeEvent += PlayerHeightDetector_playerHeightDidChangeEvent;
            }
        }

        public void Dispose() {

            if (_playerHeightDetector != null) {
                _playerHeightDetector.playerHeightDidChangeEvent -= PlayerHeightDetector_playerHeightDidChangeEvent;
            }
        }

        private void PlayerHeightDetector_playerHeightDidChangeEvent(float newHeight) {

            var height = new HeightEvent() { Height = newHeight, Time = audioTimeSyncController.songTime };
            _heightKeyframes.Add(height);
            _liveReplayStreamingService.RecordHeight(height);
        }

        public List<HeightEvent> Export() {

            return _heightKeyframes;
        }

    }
}
