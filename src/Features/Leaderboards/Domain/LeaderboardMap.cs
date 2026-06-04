using ScoreSaber.Core.Api.Paging;
using ScoreSaber.Features.Replays;

namespace ScoreSaber.Features.Leaderboards.Domain {
    internal class LeaderboardMap {
        internal LeaderboardInfoMap LeaderboardInfo { get; set; }
        internal ScoreMap[] Scores { get; set; }
        internal LeaderboardScore PlayerScore { get; set; }

        internal LeaderboardMap(LeaderboardSnapshot leaderboard, BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, int maxMultipliedScore, ReplayStorageService replayStorageService) {
            LeaderboardInfo = new LeaderboardInfoMap(leaderboard.Leaderboard, beatmapLevel, beatmapKey);
            PlayerScore = leaderboard.PlayerScore;
            Scores = new ScoreMap[leaderboard.Scores.Items.Count];
            for (int i = 0; i < leaderboard.Scores.Items.Count; i++) {
                Scores[i] = new ScoreMap(leaderboard.Scores.Items[i], LeaderboardInfo, maxMultipliedScore, replayStorageService);
            }
        }
    }

    internal class LeaderboardSnapshot {
        internal LeaderboardDetails Leaderboard { get; set; } = new LeaderboardDetails();
        internal PagedResult<LeaderboardScore> Scores { get; set; } = new PagedResult<LeaderboardScore>();
        internal LeaderboardScore PlayerScore { get; set; }
    }

    internal class LeaderboardInfoMap {
        internal LeaderboardDetails Leaderboard { get; set; }
        internal BeatmapLevel BeatmapLevel { get; set; }
        internal BeatmapKey BeatmapKey { get; set; }
        internal string SongHash { get; set; }

        internal LeaderboardInfoMap(LeaderboardDetails leaderboard, BeatmapLevel beatmapLevel, BeatmapKey beatmapKey) {
            BeatmapLevel = beatmapLevel;
            BeatmapKey = beatmapKey;
            Leaderboard = leaderboard;
            SongHash = ScoreSaberBeatmapKey.GetSongHash(beatmapKey);
        }
    }
}
