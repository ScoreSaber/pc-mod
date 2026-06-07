using IPA.Utilities;
using ScoreSaber.Features.Replays.Format;
using System;
using System.Collections.Generic;

namespace ScoreSaber.Features.Replays.Playback {
    internal class MultiplierPlayer : TimeSynchronizer, IScroller {
        private ScoreController _scoreController;
        private readonly List<MultiplierEvent> _multiplierEvents;

        public MultiplierPlayer(ReplayFile file, ScoreController scoreController) {

            _scoreController = scoreController;
            _multiplierEvents = file.multiplierKeyframes;
        }

        public void TimeUpdate(float newTime) {

            if (_multiplierEvents.Count == 0) {
                return;
            }

            int nextIndex = ReplayTimeSearch.CountAtOrBefore(_multiplierEvents, newTime, multiplierEvent => multiplierEvent.Time);
            if (nextIndex == 0) {
                UpdateMultiplier(1, 0f);
                return;
            }

            var multiplierEvent = _multiplierEvents[nextIndex - 1];
            UpdateMultiplier(multiplierEvent.Multiplier, multiplierEvent.NextMultiplierProgress);
        }

        private void UpdateMultiplier(int multiplier, float progress) {

            var counter = _scoreController._scoreMultiplierCounter;
            counter._multiplier = multiplier;
            counter._multiplierIncreaseMaxProgress = multiplier * 2;
            counter._multiplierIncreaseProgress = (int)(progress * (multiplier * 2));
            FieldAccessor<ScoreController, Action<int, float>>.Get(_scoreController, "multiplierDidChangeEvent").Invoke(multiplier, progress);
        }
    }
}
