using System;

namespace ScoreSaber.Features.Leaderboards.Domain {
    internal static class ScoreSaberBeatmapKey {
        private const string CustomLevelPrefix = "custom_level_";

        internal static bool IsSupported(BeatmapKey beatmapKey) {
            string songHash;
            return TryGetSongHash(beatmapKey, out songHash);
        }

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

            songHash = levelId.Substring(CustomLevelPrefix.Length);
            return !string.IsNullOrEmpty(songHash);
        }
    }
}
