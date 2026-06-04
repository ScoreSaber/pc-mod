using ScoreSaber.Features.Replays.Format;
using System.Collections.Generic;

namespace ScoreSaber.Features.Replays {
    internal class ReplayState {
        // State management
        internal BeatmapLevel CurrentBeatmapLevel;
        internal BeatmapKey CurrentBeatmapKey;
        internal GameplayModifiers CurrentModifiers;
        internal string CurrentPlayerName;

        // Legacy 
        internal bool IsLegacyReplay;
        internal bool IsPlaybackEnabled;
        internal List<Z.Keyframe> LoadedLegacyKeyframes;

        // New
        internal ReplayFile LoadedReplayFile;

        internal void Reset() {
            CurrentBeatmapLevel = null;
            CurrentModifiers = null;
            CurrentPlayerName = null;
            IsLegacyReplay = false;
            IsPlaybackEnabled = false;
            LoadedLegacyKeyframes = null;
            LoadedReplayFile = null;
        }

        internal void BeginReplay(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, GameplayModifiers modifiers, string playerName) {
            CurrentBeatmapLevel = beatmapLevel;
            CurrentBeatmapKey = beatmapKey;
            CurrentModifiers = modifiers;
            CurrentPlayerName = playerName;
        }

        internal void LoadLegacyReplay(List<Z.Keyframe> keyframes) {
            LoadedLegacyKeyframes = keyframes;
            LoadedReplayFile = null;
            IsPlaybackEnabled = true;
            IsLegacyReplay = true;
        }

        internal void LoadReplay(ReplayFile replay) {
            LoadedReplayFile = replay;
            LoadedLegacyKeyframes = null;
            IsLegacyReplay = false;
            IsPlaybackEnabled = true;
        }

        internal void EndPlayback() {
            IsPlaybackEnabled = false;
        }
    }
}
