namespace ScoreSaber.Features.Leaderboards {
    internal partial class LeaderboardBeatmapController {
        public void OnLeaderboardSet(IDifficultyBeatmap difficultyBeatmap) => OnLeaderboardSet(new BeatmapKey(difficultyBeatmap));
    }
}
