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

        public ScoreEventRecorder(ScoreController scoreController, IComboController comboController) {

            _scoreController = scoreController;
            _comboController = comboController;
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

            _scoreKeyframes.Add(new ScoreEvent() {
                Score = rawScore,
                Time = audioTimeSyncController.songTime,
                ImmediateMaxPossibleScore = Accessors.ImmediateMultipliedPossible(ref scoreController)
            });
        }

        private void ComboController_comboDidChangeEvent(int combo) {

            _comboKeyframes.Add(new ComboEvent() { Combo = combo, Time = audioTimeSyncController.songTime });
        }

        private void ScoreController_multiplierDidChangeEvent(int multiplier, float nextMultiplierProgress) {

            _multiplierKeyframes.Add(new MultiplierEvent() {
                Multiplier = multiplier,
                NextMultiplierProgress = nextMultiplierProgress,
                Time = audioTimeSyncController.songTime
            });
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
