namespace ScoreSaber.Core.Compat {
    internal static class LeaderboardViewCompat {
        internal static BeatmapKey GetBeatmapKey(this LevelSelectionNavigationController controller) {
#if BEAT_SABER_1_29_0
            return new BeatmapKey(controller.selectedDifficultyBeatmap);
#else
            return controller.beatmapKey;
#endif
        }

        internal static void SetDataCompat(this PlatformLeaderboardViewController controller, in BeatmapKey beatmapKey) {
#if BEAT_SABER_1_29_0
            controller.SetData(beatmapKey.difficultyBeatmap);
#else
            controller.SetData(beatmapKey);
#endif
        }

    }
}
