using ScoreSaber.Features.Live.Replay;
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
        private readonly LiveReplayStreamingService _liveReplayStreamingService;
        private readonly IGamePause _gamePause;
        private readonly AudioTimeSyncController _audioTimeSyncController;

        public Recorder(PoseRecorder poseRecorder, MetadataRecorder metadataRecorder, NoteEventRecorder noteEventRecorder, ScoreEventRecorder scoreEventRecorder, HeightEventRecorder heightEventRecorder, EnergyEventRecorder energyEventRecorder, ReplayService replayService, LiveReplayStreamingService liveReplayStreamingService, [InjectOptional] IGamePause gamePause, [InjectOptional] AudioTimeSyncController audioTimeSyncController) {

            _poseRecorder = poseRecorder;
            _replayService = replayService;
            _metadataRecorder = metadataRecorder;
            _noteEventRecorder = noteEventRecorder;
            _scoreEventRecorder = scoreEventRecorder;
            _heightEventRecorder = heightEventRecorder;
            _energyEventRecorder = energyEventRecorder;
            _liveReplayStreamingService = liveReplayStreamingService;
            _gamePause = gamePause;
            _audioTimeSyncController = audioTimeSyncController;

            _id = Guid.NewGuid().ToString();
            Plugin.Log.Debug("Main replay recorder installed");
        }

        public void Initialize() {

            _replayService.NewPlayStarted(_id, this);
            _liveReplayStreamingService.Begin(_metadataRecorder.Export());
            if (_gamePause != null) {
                _gamePause.didPauseEvent += GamePauseDidPauseEvent;
                _gamePause.didResumeEvent += GamePauseDidResumeEvent;
            }
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
            if (_gamePause != null) {
                _gamePause.didPauseEvent -= GamePauseDidPauseEvent;
                _gamePause.didResumeEvent -= GamePauseDidResumeEvent;
            }
        }

        private void GamePauseDidPauseEvent() => _liveReplayStreamingService.SetPaused(true, CurrentSongTime());

        private void GamePauseDidResumeEvent() => _liveReplayStreamingService.SetPaused(false, CurrentSongTime());

        private float CurrentSongTime() => _audioTimeSyncController != null ? _audioTimeSyncController.songTime : 0f;
    }
}
