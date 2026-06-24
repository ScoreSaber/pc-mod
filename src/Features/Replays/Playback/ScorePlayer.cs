using IPA.Utilities;
using ScoreSaber.Core.Gameplay;
using ScoreSaber.Features.Replays.Format;
using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace ScoreSaber.Features.Replays.Playback {
    internal class ScorePlayer : TimeSynchronizer, ITickable, IScroller {
        private int _nextIndex;
        private ScoreController _scoreController;
        private readonly List<ScoreEvent> _scoreEvents;
        private readonly float[] _scoringNoteEventTimes;
        private readonly IGameEnergyCounter _gameEnergyCounter;

        public ScorePlayer(ReplayFile file, ScoreController scoreController, IGameEnergyCounter gameEnergyCounter) {

            _scoreController = scoreController;
            _gameEnergyCounter = gameEnergyCounter;
            _scoreEvents = file.scoreKeyframes;
            _scoringNoteEventTimes = file.noteKeyframes
                .Where(ReplayTimeSearch.IsScoringNoteEvent)
                .Select(nk => nk.Time)
                .OrderBy(time => time)
                .ToArray();
        }

        public void Tick() {

            int? recentMultipliedScore = null;
            int? recentImmediateMaxPossibleScore = null;
            while (_nextIndex < _scoreEvents.Count && audioTimeSyncController.songTime >= _scoreEvents[_nextIndex].Time) {
                ScoreEvent activeEvent = _scoreEvents[_nextIndex++];
                recentMultipliedScore = activeEvent.Score;
                recentImmediateMaxPossibleScore = activeEvent.ImmediateMaxPossibleScore;
            }

            if (recentMultipliedScore is int score) {
                UpdateScore(score, recentImmediateMaxPossibleScore, audioTimeSyncController.songTime);
            }
        }

        public void TimeUpdate(float newTime) {

            UpdateMultiplier();

            _nextIndex = ReplayTimeSearch.CountAtOrBefore(_scoreEvents, newTime, scoreEvent => scoreEvent.Time);

            if (_nextIndex > 0) {
                var scoreEvent = _scoreEvents[_nextIndex - 1];
                UpdateScore(scoreEvent.Score, scoreEvent.ImmediateMaxPossibleScore, newTime);
            } else {
                UpdateScore(0, 0, newTime);
            }
        }

        private void UpdateMultiplier() {

            var totalMultiplier = _scoreController._gameplayModifiersModel.GetTotalMultiplier(_scoreController._gameplayModifierParams, _gameEnergyCounter.energy);
            _scoreController._prevMultiplierFromModifiers = totalMultiplier;
        }

        private void UpdateScore(int newScore, int? immediateMaxPossibleScore, float time) {

            var immediate = immediateMaxPossibleScore ?? ScoreSaberScoreModel.OldMaxRawScoreForNumberOfNotes(CalculatePostNoteCountForTime(time));
            var multiplier = _scoreController._prevMultiplierFromModifiers;

            var newModifiedScore = ScoreModel.GetModifiedScoreForGameplayModifiersScoreMultiplier(newScore, multiplier);

            _scoreController._multipliedScore = newScore;
            _scoreController._immediateMaxPossibleMultipliedScore = immediate;
            _scoreController._modifiedScore = newModifiedScore;
            _scoreController._immediateMaxPossibleModifiedScore = ScoreModel.GetModifiedScoreForGameplayModifiersScoreMultiplier(immediate, multiplier);

            FieldAccessor<ScoreController, Action<int, int>>.Get(_scoreController, "scoreDidChangeEvent").Invoke(newScore, newModifiedScore);
        }

        private int CalculatePostNoteCountForTime(float time) {
            return ReplayTimeSearch.CountAtOrBefore(_scoringNoteEventTimes, time);
        }
    }
}
