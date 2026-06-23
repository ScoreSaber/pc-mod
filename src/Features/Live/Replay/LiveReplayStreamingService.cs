using ScoreSaber.Core;
using ScoreSaber.Features.Live.Ludus.Services;
using ScoreSaber.Live.V1;
using System.Collections.Generic;
using Zenject;
using ReplayMetadataSource = ScoreSaber.Features.Replays.Format.Metadata;

namespace ScoreSaber.Features.Live.Replay {
    internal partial class LiveReplayStreamingService : ITickable {
        private const int MaxEventsPerChunk = 64;
        private const float MaxChunkAgeSeconds = 0.25f;
        private const uint RecommendedChunkSizeBytes = 64 * 1024;
        private const uint MaxChunkSizeBytes = 256 * 1024;

        private readonly ScoreSaberRuntimeInfo _runtimeInfo;

        private LudusSessionService _ludus;
        private ReplayMetadataSource _metadata;
        private StreamReplayEventBatch _pendingBatch;
        private ReplayEventCounts _counts;
        private string _streamId;
        private bool _recording;
        private bool _streaming;
        private bool _playerFollowRequested;
        private int _followViewerCount;
        private bool _playingPresenceSent;
        private bool _paused;
        private bool _pauseStatePublished;
        private bool _publishedPausedState;
        private float _lastPauseSongTime;
        private float _lastStreamSongTime;
        private ulong _nextSequence;
        private ulong _chunkCount;
        private int _pendingEventCount;
        private float _pendingBatchStartedAt;
        private uint _lastMaxScore;

        internal LiveReplayStreamingService(ScoreSaberRuntimeInfo runtimeInfo) {
            _runtimeInfo = runtimeInfo;
            ResetBatch();
            ResetCounts();
        }

        internal void AttachLudus(LudusSessionService ludus) {
            if (_ludus != null) {
                _ludus.PlayerFollowRequested -= PlayerFollowWasRequested;
                _ludus.ViewerListUpdated -= ViewerListWasUpdated;
            }

            _ludus = ludus;
            if (_ludus != null) {
                _ludus.PlayerFollowRequested += PlayerFollowWasRequested;
                _ludus.ViewerListUpdated += ViewerListWasUpdated;
            }
        }

        public void Tick() {
            _ludus?.Tick();
            if (!_recording) {
                return;
            }

            if (_streaming && (_ludus == null || !_ludus.IsConnectedToLudus)) {
                RestartStreamAfterConnectionLoss();
            }

            if (!_streaming) {
                TryStartStreaming();
                return;
            }

            FlushIfStale();
        }

        private void ResetStream() {
            _streaming = false;
            _playerFollowRequested = false;
            _followViewerCount = 0;
            _playingPresenceSent = false;
            _paused = false;
            _pauseStatePublished = false;
            _publishedPausedState = false;
            _lastPauseSongTime = 0f;
            _lastStreamSongTime = 0f;
            _streamId = string.Empty;
            _nextSequence = 1;
            _chunkCount = 0;
            _lastMaxScore = 0;
            ResetBatch();
            ResetCounts();
        }

        private void ViewerListWasUpdated(IReadOnlyList<LiveRoomViewerState> viewers) {
            _followViewerCount = viewers?.Count ?? 0;
        }

        private void ResetBatch() {
            _pendingBatch = new StreamReplayEventBatch();
            _pendingEventCount = 0;
            _pendingBatchStartedAt = 0f;
        }

        private void ResetCounts() {
            _counts = new ReplayEventCounts();
        }
    }
}
