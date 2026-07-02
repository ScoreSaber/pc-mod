using ScoreSaber.Features.Replays.Format;
using System;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Replays {

    internal class ReplayService {

        public event Action<byte[]> ReplaySerialized;

        private readonly ReplayFileCodec _replayFileCodec;
        private string _currentPlayId;
        private Recorder _replayRecorder;

        public ReplayService(ReplayFileCodec replayFileCodec) {
            _replayFileCodec = replayFileCodec;
        }

        public void NewPlayStarted(string playId, Recorder replayRecorder) {
            _currentPlayId = playId;
            _replayRecorder = replayRecorder;
            Plugin.Log.Debug($"New play started with id: {playId}");
        }

        public void DiscardReplay() {
            if (_replayRecorder == null) {
                return;
            }

            Plugin.Log.Debug($"Discarding replay with id: {_currentPlayId}");
            _replayRecorder.StopRecording();
            ClearRecorder(_currentPlayId, _replayRecorder);
        }

        public async Task<ReplaySerializationResult> WriteReplay() {
            Recorder recorder = _replayRecorder;
            string playId = _currentPlayId;
            if (recorder == null) {
                Plugin.Log.Debug("Skipping replay write because no recorder is active");
                return null;
            }

            recorder.StopRecording();

            Plugin.Log.Debug($"Writing replay with id: {playId}");
            var replayFile = recorder.Export();
            float failTime = replayFile.metadata.FailTime;
            try {
                byte[] serializedReplay = await _replayFileCodec.Write(replayFile);
                if (serializedReplay == null) {
                    Plugin.Log.Warn($"Replay serialization failed: {playId}");
                    return null;
                }
                Plugin.Log.Debug($"Replay written: {playId}");
                ReplaySerialized?.Invoke(serializedReplay);
                return new ReplaySerializationResult(serializedReplay, failTime);
            } finally {
                ClearRecorder(playId, recorder);
            }
        }

        private void ClearRecorder(string playId, Recorder recorder) {
            if (_currentPlayId != playId || !ReferenceEquals(_replayRecorder, recorder)) {
                return;
            }

            _currentPlayId = null;
            _replayRecorder = null;
        }
    }

    internal class ReplaySerializationResult {
        internal ReplaySerializationResult(byte[] replay, float failTime) {
            Replay = replay;
            FailTime = failTime;
        }

        internal byte[] Replay { get; }
        internal float FailTime { get; }
    }
}
