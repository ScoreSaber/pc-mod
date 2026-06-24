using ScoreSaber.Features.Leaderboards.Domain;
using System.Collections.Generic;

namespace ScoreSaber.Features.Leaderboards.Services {
    internal class LeaderboardPlayerScoreCache {
        private readonly Dictionary<string, LeaderboardScore> _scores = new Dictionary<string, LeaderboardScore>();

        internal void Remember(LeaderboardQuery query, string playerId, LeaderboardScore score) {
            if (string.IsNullOrEmpty(playerId)) {
                return;
            }

            string key = CreateKey(query.SongHash, query.GameMode, query.Difficulty, query.RealmId, playerId);
            if (score == null) {
                _scores.Remove(key);
                return;
            }

            _scores[key] = score;
        }

        internal bool TryGet(BeatmapKey beatmapKey, string playerId, out LeaderboardScore score) {
            string songHash;
            if (string.IsNullOrEmpty(playerId) || !ScoreSaberBeatmapKey.TryGetSongHash(beatmapKey, out songHash)) {
                score = null;
                return false;
            }

            return _scores.TryGetValue(CreateKey(
                songHash,
                $"Solo{beatmapKey.beatmapCharacteristic.serializedName}",
                BeatmapDifficultyMethods.DefaultRating(beatmapKey.difficulty),
                null,
                playerId), out score);
        }

        private static string CreateKey(string songHash, string gameMode, int difficulty, int? realmId, string playerId) {
            string realm = realmId.HasValue ? realmId.Value.ToString() : string.Empty;
            return $"{playerId}|{realm}|{songHash}|{gameMode}|{difficulty}";
        }
    }
}
