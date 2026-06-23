using ScoreSaber.Core;
using ScoreSaber.Live.V1;
using System;

namespace ScoreSaber.Features.Live.Replay {
    internal partial class LiveReplayStreamingService {
        internal void Complete(LevelCompletionResults results, float playOutcomeTime) {
            if (!_recording) {
                return;
            }

            _recording = false;
            if (!_streaming || _ludus == null) {
                TrySendIdlePresence();
                ResetStream();
                return;
            }

            try {
                Flush();
                _ludus.SendReplayPacket(new ReplayStreamPacket {
                    StreamId = _streamId,
                    PlayerId = _ludus.LocalPlayerId,
                    MatchId = _ludus.CurrentLudusMatchId,
                    End = new ReplayStreamEnd {
                        Cursor = Cursor(_nextSequence++, playOutcomeTime),
                        Completion = CompletionFromResults(results),
                        Score = ScoreSummary(results),
                        ChunkCount = _chunkCount,
                        CumulativeEventCounts = CloneCounts(_counts)
                    }
                });
                TrySendIdlePresence();
                Plugin.Log.Info($"Live replay: Stream finished ({_chunkCount} chunks).");
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to finish live replay stream: {ex.Message}");
            } finally {
                ResetStream();
            }
        }

        internal void StopPublicPresenceStream() {
            _playerFollowRequested = false;
            _followViewerCount = 0;
            if (_ludus == null || _ludus.IsInTournamentRoom) {
                return;
            }

            if (!_streaming) {
                _playingPresenceSent = false;
                return;
            }

            try {
                Flush();
                ulong lastCommittedSequence = _nextSequence > 1 ? _nextSequence - 1 : 0;
                _ludus.SendReplayPacket(new ReplayStreamPacket {
                    StreamId = _streamId,
                    PlayerId = _ludus.LocalPlayerId,
                    MatchId = _ludus.CurrentLudusMatchId,
                    Failure = new ReplayStreamFailure {
                        LastCommittedCursor = Cursor(lastCommittedSequence, _lastStreamSongTime),
                        Reason = ReplayFailureReason.ReplayFailureReasonClientDisconnected,
                        State = ReplayStreamState.ReplayStreamStateFailed,
                        Message = "Public live presence disabled.",
                        CanResume = false
                    }
                });
                Plugin.Log.Info("Live replay: Public presence stream stopped.");
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to stop public presence stream: {ex.Message}");
            }

            ResetPublicStreamState();
        }

        private void PlayerFollowWasRequested(int viewerCount) {
            viewerCount = Math.Max(0, viewerCount);
            if (_playerFollowRequested && _followViewerCount == viewerCount) {
                TryStartStreaming();
                return;
            }

            _playerFollowRequested = true;
            _followViewerCount = viewerCount;
            Plugin.Log.Info($"Live replay: Follow requested{FormatViewerSource(viewerCount)}.");
            TryStartStreaming();
        }

        private void TryStartStreaming() {
            if (_streaming || !_recording || _ludus == null || !_ludus.IsConnectedToLudus) {
                return;
            }

            TrySendPlayingPresence();
            bool shouldStream = _ludus.IsInTournamentRoom || (_playerFollowRequested && _ludus.IsInPublicPresence);
            if (!shouldStream) {
                return;
            }

            _streamId = $"pc-{Guid.NewGuid():N}";
            _streaming = true;
            Plugin.Log.Info($"Live replay: Streaming started{FormatViewerSuffix(_followViewerCount)}.");
            _ludus.SendReplayPacket(new ReplayStreamPacket {
                StreamId = _streamId,
                PlayerId = _ludus.LocalPlayerId,
                MatchId = _ludus.CurrentLudusMatchId,
                Start = new ReplayStreamStart {
                    ProtocolVersion = 1,
                    Player = PlayerIdentity(),
                    Beatmap = BeatmapIdentity(),
                    PayloadFormat = ReplayPayloadFormat.ReplayPayloadFormatScoresaberStreamV1,
                    PayloadFormatVersion = 1,
                    PayloadCompression = ReplayCompression.ReplayCompressionNone,
                    RecommendedChunkSizeBytes = RecommendedChunkSizeBytes,
                    MaxChunkSizeBytes = MaxChunkSizeBytes,
                    ClientStartTimeUnixMs = UnixNowMs(),
                    GameSessionId = _ludus.GameSessionId,
                    Features = { ReplayFeature.ReplayFeatureSpectatorCatchup },
                    ReplayMetadata = StreamMetadata()
                }
            });
            if (_paused) {
                PublishPauseState(_lastPauseSongTime);
            }
        }

        private void TrySendPlayingPresence() {
            if (_playingPresenceSent || !_recording || _ludus == null || !_ludus.IsConnectedToLudus) {
                return;
            }

            if (!_ludus.IsInTournamentRoom && !_ludus.IsInPublicPresence) {
                return;
            }

            _playingPresenceSent = true;
            _ludus.SendPresence(LudusPlayState.LudusPlayStateInGame, LudusDownloadState.LudusDownloadStateNone, ExtractMapHash(_metadata.LevelID));
            Plugin.Log.Info("Live replay: Playing presence sent.");
        }

        private void TrySendIdlePresence() {
            if (!_playingPresenceSent || _ludus == null) {
                return;
            }

            _ludus.SendPresence(LudusPlayState.LudusPlayStateInMenus, LudusDownloadState.LudusDownloadStateNone, string.Empty);
            Plugin.Log.Info("Live replay: Idle presence sent.");
        }

        private void FlushIfFull() {
            if (_pendingEventCount >= MaxEventsPerChunk) {
                Flush();
            }
        }

        private void FlushIfStale() {
            if (_pendingEventCount == 0 || _pendingBatchStartedAt <= 0f) {
                return;
            }

            if (UnityEngine.Time.realtimeSinceStartup - _pendingBatchStartedAt >= MaxChunkAgeSeconds) {
                Flush();
            }
        }

        private void PublishPauseState(float songTime) {
            if (!_recording || !_streaming || _ludus == null) {
                return;
            }

            if (_pauseStatePublished && _publishedPausedState == _paused) {
                return;
            }

            Flush();
            _pendingBatch.PauseEvents.Add(new ReplayPauseEvent {
                Paused = _paused,
                TimeSeconds = songTime,
                ClientTimeUnixMs = UnixNowMs()
            });
            _counts.PauseEvents++;
            MarkEventTime(songTime);
            Flush();
            _pauseStatePublished = true;
            _publishedPausedState = _paused;
            Plugin.Log.Info(_paused ? "Live replay: Pause event sent." : "Live replay: Resume event sent.");
        }

        private void Flush() {
            if (_pendingEventCount == 0 || _ludus == null || !_ludus.IsConnectedToLudus || string.IsNullOrEmpty(_streamId)) {
                return;
            }

            _chunkCount++;
            _ludus.SendReplayPacket(new ReplayStreamPacket {
                StreamId = _streamId,
                PlayerId = _ludus.LocalPlayerId,
                MatchId = _ludus.CurrentLudusMatchId,
                Chunk = new ReplayChunk {
                    Cursor = Cursor(_nextSequence++, _pendingBatch.MaxTimeSeconds),
                    Events = _pendingBatch,
                    CumulativeEventCounts = CloneCounts(_counts)
                }
            });
            ResetBatch();
        }

        private void RestartStreamAfterConnectionLoss() {
            _streaming = false;
            _playingPresenceSent = false;
            _pauseStatePublished = false;
            _publishedPausedState = false;
            _streamId = string.Empty;
            _nextSequence = 1;
            _chunkCount = 0;
            ResetBatch();
            ResetCounts();
            Plugin.Log.Info("Live replay: Stream interrupted; waiting for Ludus reconnect.");
        }

        private void ResetPublicStreamState() {
            _streaming = false;
            _followViewerCount = 0;
            _playingPresenceSent = false;
            _pauseStatePublished = false;
            _publishedPausedState = false;
            _lastStreamSongTime = 0f;
            _streamId = string.Empty;
            _nextSequence = 1;
            _chunkCount = 0;
            _lastMaxScore = 0;
            ResetBatch();
            ResetCounts();
        }

        private static string FormatViewerSuffix(int viewerCount) {
            if (viewerCount <= 0) {
                return string.Empty;
            }

            return viewerCount == 1 ? " for 1 viewer" : $" for {viewerCount} viewers";
        }

        private static string FormatViewerSource(int viewerCount) {
            if (viewerCount <= 0) {
                return string.Empty;
            }

            return viewerCount == 1 ? " by 1 viewer" : $" by {viewerCount} viewers";
        }

        private void MarkEventTime(float time) {
            if (_pendingEventCount == 0) {
                _pendingBatch.MinTimeSeconds = time;
                _pendingBatch.MaxTimeSeconds = time;
                _pendingBatchStartedAt = UnityEngine.Time.realtimeSinceStartup;
            } else {
                _pendingBatch.MinTimeSeconds = Math.Min(_pendingBatch.MinTimeSeconds, time);
                _pendingBatch.MaxTimeSeconds = Math.Max(_pendingBatch.MaxTimeSeconds, time);
            }

            _pendingEventCount++;
            _lastStreamSongTime = Math.Max(_lastStreamSongTime, time);
        }
    }
}
