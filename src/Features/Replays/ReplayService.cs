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

        public async Task<ReplaySerializationResult> WriteReplay() {
            if (_replayRecorder == null) {
                Plugin.Log.Debug("Skipping replay write because no recorder is active");
                return null;
            }

            _replayRecorder.StopRecording();

            Plugin.Log.Debug($"Writing replay with id: {_currentPlayId}");
            var replayFile = _replayRecorder.Export();
            byte[] serializedReplay = await _replayFileCodec.Write(replayFile);
            Plugin.Log.Debug($"Replay written: {_currentPlayId}");
            ReplaySerialized?.Invoke(serializedReplay);
            return new ReplaySerializationResult(serializedReplay, replayFile.metadata.FailTime);
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
