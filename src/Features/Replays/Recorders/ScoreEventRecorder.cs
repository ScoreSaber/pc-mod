using ScoreSaber.Features.Live.Replay;
using ScoreSaber.Features.Replays.Format;
using System;
using System.Collections.Generic;
using Zenject;

namespace ScoreSaber.Features.Replays.Recorders {
    internal class ScoreEventRecorder : TimeSynchronizer, IInitializable, IDisposable {
        private const int InitialScoreEventCapacity = 4096;
        private const int InitialComboEventCapacity = 4096;
        private const int InitialMultiplierEventCapacity = 32;

        private readonly ScoreController _scoreController;
        private readonly List<ScoreEvent> _scoreKeyframes;
        private readonly List<ComboEvent> _comboKeyframes;
        private readonly IComboController _comboController;
        private readonly List<MultiplierEvent> _multiplierKeyframes;
        private readonly LiveReplayStreamingService _liveReplayStreamingService;

        public ScoreEventRecorder(ScoreController scoreController, IComboController comboController, LiveReplayStreamingService liveReplayStreamingService) {

            _scoreController = scoreController;
            _comboController = comboController;
            _liveReplayStreamingService = liveReplayStreamingService;
            _scoreKeyframes = new List<ScoreEvent>(InitialScoreEventCapacity);
            _comboKeyframes = new List<ComboEvent>(InitialComboEventCapacity);
            _multiplierKeyframes = new List<MultiplierEvent>(InitialMultiplierEventCapacity);
        }

        public void Initialize() {

            _comboController.comboDidChangeEvent += ComboController_comboDidChangeEvent;
            _scoreController.scoreDidChangeEvent += ScoreController_scoreDidChangeEvent;
            _scoreController.multiplierDidChangeEvent += ScoreController_multiplierDidChangeEvent;
        }

        public void Dispose() {

            _comboController.comboDidChangeEvent -= ComboController_comboDidChangeEvent;
            _scoreController.scoreDidChangeEvent -= ScoreController_scoreDidChangeEvent;
            _scoreController.multiplierDidChangeEvent -= ScoreController_multiplierDidChangeEvent;
        }

        private void ScoreController_scoreDidChangeEvent(int rawScore, int score) {

            var scoreController = _scoreController;

            var scoreEvent = new ScoreEvent() {
                Score = rawScore,
                Time = audioTimeSyncController.songTime,
                ImmediateMaxPossibleScore = scoreController._immediateMaxPossibleMultipliedScore
            };
            _scoreKeyframes.Add(scoreEvent);
            _liveReplayStreamingService.RecordScore(scoreEvent);
        }

        private void ComboController_comboDidChangeEvent(int combo) {

            var comboEvent = new ComboEvent() { Combo = combo, Time = audioTimeSyncController.songTime };
            _comboKeyframes.Add(comboEvent);
            _liveReplayStreamingService.RecordCombo(comboEvent);
        }

        private void ScoreController_multiplierDidChangeEvent(int multiplier, float nextMultiplierProgress) {

            var multiplierEvent = new MultiplierEvent() {
                Multiplier = multiplier,
                NextMultiplierProgress = nextMultiplierProgress,
                Time = audioTimeSyncController.songTime
            };
            _multiplierKeyframes.Add(multiplierEvent);
            _liveReplayStreamingService.RecordMultiplier(multiplierEvent);
        }

        public List<ScoreEvent> ExportScoreKeyframes() {

            return _scoreKeyframes;
        }

        public List<ComboEvent> ExportComboKeyframes() {

            return _comboKeyframes;
        }

        public List<MultiplierEvent> ExportMultiplierKeyframes() {

            return _multiplierKeyframes;
        }

    }
}
