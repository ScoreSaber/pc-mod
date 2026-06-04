using ScoreSaber.Features.Replays.Format;
using ScoreSaber.Features.Replays.Recorders;
using System;
using Zenject;

namespace ScoreSaber.Features.Replays {
    internal class Recorder : IInitializable, IDisposable {
        private readonly string _id;
        private readonly PoseRecorder _poseRecorder;
        private readonly ReplayService _replayService;
        private readonly MetadataRecorder _metadataRecorder;
        private readonly NoteEventRecorder _noteEventRecorder;
        private readonly ScoreEventRecorder _scoreEventRecorder;
        private readonly HeightEventRecorder _heightEventRecorder;
        private readonly EnergyEventRecorder _energyEventRecorder;

        public Recorder(PoseRecorder poseRecorder, MetadataRecorder metadataRecorder, NoteEventRecorder noteEventRecorder, ScoreEventRecorder scoreEventRecorder, HeightEventRecorder heightEventRecorder, EnergyEventRecorder energyEventRecorder, ReplayService replayService) {

            _poseRecorder = poseRecorder;
            _replayService = replayService;
            _metadataRecorder = metadataRecorder;
            _noteEventRecorder = noteEventRecorder;
            _scoreEventRecorder = scoreEventRecorder;
            _heightEventRecorder = heightEventRecorder;
            _energyEventRecorder = energyEventRecorder;

            _id = Guid.NewGuid().ToString();
            Plugin.Log.Debug("Main replay recorder installed");
        }

        public void Initialize() {

            _replayService.NewPlayStarted(_id, this);
        }

        public void StopRecording() {
            _poseRecorder.StopRecording();
        }

        public ReplayFile Export() {

            return new ReplayFile() {
                metadata = _metadataRecorder.Export(),
                poseKeyframes = _poseRecorder.Export(),
                heightKeyframes = _heightEventRecorder.Export(),
                noteKeyframes = _noteEventRecorder.Export(),
                scoreKeyframes = _scoreEventRecorder.ExportScoreKeyframes(),
                comboKeyframes = _scoreEventRecorder.ExportComboKeyframes(),
                multiplierKeyframes = _scoreEventRecorder.ExportMultiplierKeyframes(),
                energyKeyframes = _energyEventRecorder.Export()
            };
        }

        public void Dispose() {
        }
    }
}
