using ScoreSaber.Core;
using ScoreSaber.Features.Replays.Format;
using ScoreSaber.Live.V1;
using System.Collections.Generic;
using ReplayComboEventSource = ScoreSaber.Features.Replays.Format.ComboEvent;
using ReplayEnergyEventSource = ScoreSaber.Features.Replays.Format.EnergyEvent;
using ReplayHeightEventSource = ScoreSaber.Features.Replays.Format.HeightEvent;
using ReplayMetadataSource = ScoreSaber.Features.Replays.Format.Metadata;
using ReplayMultiplierEventSource = ScoreSaber.Features.Replays.Format.MultiplierEvent;
using ReplayNoteEventSource = ScoreSaber.Features.Replays.Format.NoteEvent;
using ReplayPoseGroupSource = ScoreSaber.Features.Replays.Format.VRPoseGroup;
using ReplayScoreEventSource = ScoreSaber.Features.Replays.Format.ScoreEvent;

namespace ScoreSaber.Features.Live.Replay {
    internal partial class LiveReplayStreamingService {
        internal void Begin(ReplayMetadataSource metadata, byte[] hsvConfig) {
            _metadata = metadata;
            _hsvConfig = hsvConfig;
            _recording = true;
            _streaming = false;
            _streamId = string.Empty;
            _nextSequence = 1;
            _chunkCount = 0;
            _lastMaxScore = 0;
            _playingPresenceSent = false;
            _paused = false;
            _pauseStatePublished = false;
            _publishedPausedState = false;
            _lastPauseSongTime = 0f;
            _lastStreamSongTime = 0f;
            ResetBatch();
            ResetCounts();
            Plugin.Log.Info("Live replay: Recording started.");
            TrySendPlayingPresence();
            TryStartStreaming();
        }

        internal void RecordPose(ReplayPoseGroupSource frame) {
            if (_paused) {
                TryStartStreaming();
                return;
            }

            if (!CanRecordEvent()) {
                return;
            }

            _pendingBatch.PoseFrames.Add(ToReplayPoseFrame(frame));
            _counts.PoseFrames++;
            MarkEventTime(frame.Time);
            FlushIfFull();
        }

        internal void SetPaused(bool paused, float songTime) {
            if (!_recording || _paused == paused) {
                return;
            }

            _paused = paused;
            _pauseStatePublished = false;
            _lastPauseSongTime = songTime;
            TrySendPlayingPresence();
            TryStartStreaming();
            PublishPauseState(songTime);
        }

        internal void RecordHeight(ReplayHeightEventSource height) {
            if (!CanRecordEvent()) {
                return;
            }

            _pendingBatch.HeightEvents.Add(new ReplayHeightEvent {
                Height = height.Height,
                TimeSeconds = height.Time
            });
            _counts.HeightEvents++;
            MarkEventTime(height.Time);
            FlushIfFull();
        }

        internal void RecordNote(ReplayNoteEventSource note) {
            if (!CanRecordEvent()) {
                return;
            }

            _pendingBatch.NoteEvents.Add(ToReplayNoteEvent(note));
            _counts.NoteEvents++;
            MarkEventTime(note.Time);
            FlushIfFull();
        }

        internal void RecordScore(ReplayScoreEventSource score) {
            if (!CanRecordEvent()) {
                return;
            }

            if (score.ImmediateMaxPossibleScore is int maxScore && maxScore > 0) {
                _lastMaxScore = (uint)maxScore;
            }

            var replayScore = new ReplayScoreEvent {
                Score = score.Score,
                TimeSeconds = score.Time
            };
            if (score.ImmediateMaxPossibleScore.HasValue) {
                replayScore.ImmediateMaxPossibleScore = score.ImmediateMaxPossibleScore.Value;
            }

            _pendingBatch.ScoreEvents.Add(replayScore);
            _counts.ScoreEvents++;
            MarkEventTime(score.Time);
            FlushIfFull();
        }

        internal void RecordCombo(ReplayComboEventSource combo) {
            if (!CanRecordEvent()) {
                return;
            }

            _pendingBatch.ComboEvents.Add(new ReplayComboEvent {
                Combo = combo.Combo,
                TimeSeconds = combo.Time
            });
            _counts.ComboEvents++;
            MarkEventTime(combo.Time);
            FlushIfFull();
        }

        internal void RecordMultiplier(ReplayMultiplierEventSource multiplier) {
            if (!CanRecordEvent()) {
                return;
            }

            _pendingBatch.MultiplierEvents.Add(new ReplayMultiplierEvent {
                Multiplier = multiplier.Multiplier,
                NextMultiplierProgress = multiplier.NextMultiplierProgress,
                TimeSeconds = multiplier.Time
            });
            _counts.MultiplierEvents++;
            MarkEventTime(multiplier.Time);
            FlushIfFull();
        }

        internal void RecordEnergy(ReplayEnergyEventSource energy) {
            if (!CanRecordEvent()) {
                return;
            }

            _pendingBatch.EnergyEvents.Add(new ReplayEnergyEvent {
                Energy = energy.Energy,
                TimeSeconds = energy.Time
            });
            _counts.EnergyEvents++;
            MarkEventTime(energy.Time);
            FlushIfFull();
        }

        internal void RecordWall(WallEvent wall) {
            if (!CanRecordEvent()) {
                return;
            }

            ReplayExtensionEntry entry = ReplayExtensionPayloads.CreateWallEvents(new List<WallEvent> { wall });
            _pendingExtensions.Add(ToReplayExtension(entry));
            MarkEventTime(wall.ExitTime);
            FlushIfFull();
        }

        private bool CanRecordEvent() {
            if (!_recording) {
                return false;
            }

            if (_streaming && (_ludus == null || !_ludus.IsConnectedToLudus)) {
                RestartStreamAfterConnectionLoss();
                return false;
            }

            TrySendPlayingPresence();
            TryStartStreaming();
            return _streaming;
        }
    }
}
