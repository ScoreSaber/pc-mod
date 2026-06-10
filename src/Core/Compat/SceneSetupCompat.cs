namespace ScoreSaber.Core.Compat {
    // 1.29 uses old beatmap types, so shared code goes through these
    internal static class SceneSetupCompat {
#if BEAT_SABER_1_29_0
        internal static BeatmapLevel GetBeatmapLevel(this StandardLevelScenesTransitionSetupDataSO transition) => LevelFor(transition.difficultyBeatmap);

        internal static BeatmapKey GetBeatmapKey(this StandardLevelScenesTransitionSetupDataSO transition) => new BeatmapKey(transition.difficultyBeatmap);

        internal static BeatmapLevel GetBeatmapLevel(this MultiplayerLevelScenesTransitionSetupDataSO transition) => transition.previewBeatmapLevel == null ? null : new BeatmapLevel(transition.previewBeatmapLevel);

        internal static BeatmapKey GetBeatmapKey(this MultiplayerLevelScenesTransitionSetupDataSO transition) => new BeatmapKey(transition.difficultyBeatmap);

        internal static BeatmapLevel GetBeatmapLevel(this ResultsViewController resultsViewController) => LevelFor(resultsViewController._difficultyBeatmap);

        internal static BeatmapKey GetBeatmapKey(this ResultsViewController resultsViewController) => new BeatmapKey(resultsViewController._difficultyBeatmap);

        internal static BeatmapLevel GetBeatmapLevel(this GameplayCoreSceneSetupData sceneSetupData) => sceneSetupData.previewBeatmapLevel == null ? null : new BeatmapLevel(sceneSetupData.previewBeatmapLevel);

        internal static BeatmapKey GetBeatmapKey(this GameplayCoreSceneSetupData sceneSetupData) => new BeatmapKey(sceneSetupData.difficultyBeatmap);

        internal static string GetEnvironmentSerializedName(this GameplayCoreSceneSetupData sceneSetupData) => sceneSetupData.environmentInfo.serializedName;

        internal static EnvironmentInfoSO GetTargetEnvironmentInfo(this GameplayCoreSceneSetupData sceneSetupData) => sceneSetupData.environmentInfo;

        private static BeatmapLevel LevelFor(IDifficultyBeatmap difficultyBeatmap) => difficultyBeatmap?.level == null ? null : new BeatmapLevel(difficultyBeatmap.level);
#else
        internal static BeatmapLevel GetBeatmapLevel(this StandardLevelScenesTransitionSetupDataSO transition) => transition.beatmapLevel;

        internal static BeatmapKey GetBeatmapKey(this StandardLevelScenesTransitionSetupDataSO transition) => transition.beatmapKey;

        internal static BeatmapLevel GetBeatmapLevel(this MultiplayerLevelScenesTransitionSetupDataSO transition) => transition.beatmapLevel;

        internal static BeatmapKey GetBeatmapKey(this MultiplayerLevelScenesTransitionSetupDataSO transition) => transition.beatmapKey;

        internal static BeatmapLevel GetBeatmapLevel(this ResultsViewController resultsViewController) => resultsViewController._beatmapLevel;

        internal static BeatmapKey GetBeatmapKey(this ResultsViewController resultsViewController) => resultsViewController._beatmapKey;

        internal static BeatmapLevel GetBeatmapLevel(this GameplayCoreSceneSetupData sceneSetupData) => sceneSetupData.beatmapLevel;

        internal static BeatmapKey GetBeatmapKey(this GameplayCoreSceneSetupData sceneSetupData) => sceneSetupData.beatmapKey;

#if BEAT_SABER_1_37_1
        // 1.37 has modern beatmaps but still uses the old environment field
        internal static string GetEnvironmentSerializedName(this GameplayCoreSceneSetupData sceneSetupData) => sceneSetupData.environmentInfo.serializedName;

        internal static EnvironmentInfoSO GetTargetEnvironmentInfo(this GameplayCoreSceneSetupData sceneSetupData) => sceneSetupData.environmentInfo;
#else
        internal static string GetEnvironmentSerializedName(this GameplayCoreSceneSetupData sceneSetupData) => sceneSetupData.targetEnvironmentInfo.serializedName;

        internal static EnvironmentInfoSO GetTargetEnvironmentInfo(this GameplayCoreSceneSetupData sceneSetupData) => sceneSetupData.targetEnvironmentInfo;
#endif
#endif
    }
}
