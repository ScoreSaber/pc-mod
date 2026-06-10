using System;

namespace ScoreSaber.Features.Leaderboards.Domain {
    internal static class ScoreSaberBeatmapKey {
        private const string CustomLevelPrefix = "custom_level_";
        private const string WipLevelSuffix = " WIP";
        private const string WipLevelSegment = "_WIP";

        internal static bool IsSupported(BeatmapKey beatmapKey) => TryGetSongHash(beatmapKey, out _);

        internal static bool IsCustomLevel(BeatmapKey beatmapKey) => IsCustomLevelId(beatmapKey.levelId);

        internal static bool IsCustomLevelId(string levelId) => !string.IsNullOrEmpty(levelId) && levelId.StartsWith(CustomLevelPrefix, StringComparison.Ordinal);

        internal static bool IsWip(BeatmapKey beatmapKey) => IsCustomLevel(beatmapKey) && IsWipLevelId(beatmapKey.levelId);

        internal static bool TryGetSongHash(BeatmapKey beatmapKey, out string songHash) => TryGetSongHash(beatmapKey.levelId, out songHash);

        internal static string GetSongHash(BeatmapKey beatmapKey) {
            string songHash;
            if (!TryGetSongHash(beatmapKey, out songHash)) {
                throw new InvalidOperationException($"Unsupported ScoreSaber level id: {beatmapKey.levelId}");
            }

            return songHash;
        }

        private static bool TryGetSongHash(string levelId, out string songHash) {
            songHash = string.Empty;
            if (string.IsNullOrEmpty(levelId) || !levelId.StartsWith(CustomLevelPrefix, StringComparison.Ordinal)) {
                return false;
            }

            if (IsWipLevelId(levelId)) {
                return false;
            }

            songHash = GetLevelInfo(levelId)[0];
            return !string.IsNullOrEmpty(songHash);
        }

        private static bool IsWipLevelId(string levelId) => levelId.IndexOf(WipLevelSuffix, StringComparison.OrdinalIgnoreCase) >= 0 || levelId.IndexOf(WipLevelSegment, StringComparison.OrdinalIgnoreCase) >= 0;

        private static string[] GetLevelInfo(string levelId) => levelId.Substring(CustomLevelPrefix.Length).Split('_');
    }
}
