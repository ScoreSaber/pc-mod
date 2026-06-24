using IPA.Utilities;
using ScoreSaber.Features.Replays.Format;
using System;
using System.Collections.Generic;
using Zenject;
using HeightEvent = ScoreSaber.Features.Replays.Format.HeightEvent;

namespace ScoreSaber.Features.Replays.Playback {
    internal class HeightPlayer : TimeSynchronizer, IInitializable, ITickable, IScroller {
        private int _nextIndex = 0;
        private readonly List<HeightEvent> _heightEvents;
        private readonly PlayerHeightDetector _playerHeightDetector;

        protected HeightPlayer(ReplayFile file, PlayerHeightDetector playerHeightDetector) {

            _playerHeightDetector = playerHeightDetector;
            _heightEvents = file.heightKeyframes;
        }

        public void Initialize() {

            _playerHeightDetector.OnDestroy();
        }

        public void Tick() {

            float? newHeight = null;
            while (_nextIndex < _heightEvents.Count && audioTimeSyncController.songTime >= _heightEvents[_nextIndex].Time) {
                newHeight = _heightEvents[_nextIndex].Height;
                _nextIndex++;
            }
            if (newHeight.HasValue) {
                FieldAccessor<PlayerHeightDetector, Action<float>>.Get(_playerHeightDetector, "playerHeightDidChangeEvent").Invoke(newHeight.Value);
            }
        }

        public void TimeUpdate(float newTime) {

            _nextIndex = ReplayTimeSearch.CountAtOrBefore(_heightEvents, newTime, heightEvent => heightEvent.Time);
            if (_nextIndex > 0) {
                FieldAccessor<PlayerHeightDetector, Action<float>>.Get(_playerHeightDetector, "playerHeightDidChangeEvent").Invoke(_heightEvents[_nextIndex - 1].Height);
            }
        }
    }
}
