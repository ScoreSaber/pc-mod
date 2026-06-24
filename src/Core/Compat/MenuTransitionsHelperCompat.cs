using System;

namespace ScoreSaber.Core.Compat {
    // StartStandardLevel changes shape between versions, so replay launch goes through here
    internal static class MenuTransitionsHelperCompat {
        internal static void StartReplayLevel(
            this MenuTransitionsHelper menuTransitionsHelper,
            in BeatmapKey beatmapKey,
            BeatmapLevel beatmapLevel,
            PlayerData playerData,
            GameplayModifiers gameplayModifiers,
            PlayerSpecificSettings playerSpecificSettings,
            EnvironmentsListModel environmentsListModel,
            OverrideEnvironmentSettings overrideEnvironmentSettings,
            ColorScheme replayColorScheme,
            Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults> levelFinishedCallback) {
            StartStandardLevelCompat(
                menuTransitionsHelper,
                beatmapKey,
                beatmapLevel,
                playerData,
                gameplayModifiers,
                playerSpecificSettings,
                environmentsListModel,
                overrideEnvironmentSettings,
                replayColorScheme,
                "Replay",
                "Exit Replay",
                levelFinishedCallback);
        }

        internal static void StartLiveLevel(
            this MenuTransitionsHelper menuTransitionsHelper,
            in BeatmapKey beatmapKey,
            BeatmapLevel beatmapLevel,
            PlayerData playerData,
            GameplayModifiers gameplayModifiers,
            PlayerSpecificSettings playerSpecificSettings,
            EnvironmentsListModel environmentsListModel,
            Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults> levelFinishedCallback) {
            StartStandardLevelCompat(
                menuTransitionsHelper,
                beatmapKey,
                beatmapLevel,
                playerData,
                gameplayModifiers,
                playerSpecificSettings,
                environmentsListModel,
                null,
                null,
                "Solo",
                "Menu",
                levelFinishedCallback);
        }

        private static void StartStandardLevelCompat(
            MenuTransitionsHelper menuTransitionsHelper,
            in BeatmapKey beatmapKey,
            BeatmapLevel beatmapLevel,
            PlayerData playerData,
            GameplayModifiers gameplayModifiers,
            PlayerSpecificSettings playerSpecificSettings,
            EnvironmentsListModel environmentsListModel,
            OverrideEnvironmentSettings overrideEnvironmentSettings,
            ColorScheme replayColorScheme,
            string gameMode,
            string backButtonText,
            Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults> levelFinishedCallback) {
#if BEAT_SABER_1_29_0
            menuTransitionsHelper.StartStandardLevel(
                gameMode: gameMode,
                difficultyBeatmap: beatmapKey.difficultyBeatmap,
                previewBeatmapLevel: beatmapKey.difficultyBeatmap.level,
                overrideEnvironmentSettings: overrideEnvironmentSettings ?? playerData.overrideEnvironmentSettings,
                overrideColorScheme: replayColorScheme ?? playerData.colorSchemesSettings.GetOverrideColorScheme(),
                gameplayModifiers: gameplayModifiers,
                playerSpecificSettings: playerSpecificSettings,
                practiceSettings: null,
                backButtonText: backButtonText,
                useTestNoteCutSoundEffects: false,
                startPaused: false,
                beforeSceneSwitchCallback: null,
                afterSceneSwitchCallback: null,
                levelFinishedCallback: levelFinishedCallback,
                levelRestartedCallback: null
            );
#elif BEAT_SABER_1_37_1
            // same shape as 1.38, but the scene switch callbacks still use the older names
            menuTransitionsHelper.StartStandardLevel(
                gameMode: gameMode,
                beatmapKey: beatmapKey,
                beatmapLevel: beatmapLevel,
                overrideEnvironmentSettings: overrideEnvironmentSettings ?? playerData.overrideEnvironmentSettings,
                overrideColorScheme: replayColorScheme ?? playerData.colorSchemesSettings.GetOverrideColorScheme(),
                beatmapOverrideColorScheme: beatmapLevel.GetColorScheme(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty),
                gameplayModifiers: gameplayModifiers,
                playerSpecificSettings: playerSpecificSettings,
                practiceSettings: null,
                environmentsListModel: environmentsListModel,
                backButtonText: backButtonText,
                useTestNoteCutSoundEffects: false,
                startPaused: false,
                beforeSceneSwitchCallback: null,
                afterSceneSwitchCallback: null,
                levelFinishedCallback: levelFinishedCallback,
                levelRestartedCallback: null
            );
#elif BEAT_SABER_1_38_0
            menuTransitionsHelper.StartStandardLevel(
                gameMode: gameMode,
                beatmapKey: beatmapKey,
                beatmapLevel: beatmapLevel,
                overrideEnvironmentSettings: overrideEnvironmentSettings ?? playerData.overrideEnvironmentSettings,
                overrideColorScheme: replayColorScheme ?? playerData.colorSchemesSettings.GetOverrideColorScheme(),
                beatmapOverrideColorScheme: beatmapLevel.GetColorScheme(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty),
                gameplayModifiers: gameplayModifiers,
                playerSpecificSettings: playerSpecificSettings,
                practiceSettings: null,
                environmentsListModel: environmentsListModel,
                backButtonText: backButtonText,
                useTestNoteCutSoundEffects: false,
                startPaused: false,
                beforeSceneSwitchToGameplayCallback: null,
                afterSceneSwitchToGameplayCallback: null,
                levelFinishedCallback: levelFinishedCallback,
                levelRestartedCallback: null
            );
#elif BEAT_SABER_1_40_0
            menuTransitionsHelper.StartStandardLevel(
                gameMode: gameMode,
                beatmapKey: beatmapKey,
                beatmapLevel: beatmapLevel,
                overrideEnvironmentSettings: overrideEnvironmentSettings ?? playerData.overrideEnvironmentSettings,
                playerOverrideColorScheme: replayColorScheme ?? playerData.colorSchemesSettings.GetOverrideColorScheme(),
                playerOverrideLightshowColors: replayColorScheme?.overrideLights ?? playerData.colorSchemesSettings.ShouldOverrideLightshowColors(),
                beatmapOverrideColorScheme: beatmapLevel.GetColorScheme(beatmapKey.beatmapCharacteristic, beatmapKey.difficulty),
                gameplayModifiers: gameplayModifiers,
                playerSpecificSettings: playerSpecificSettings,
                practiceSettings: null,
                environmentsListModel: environmentsListModel,
                backButtonText: backButtonText,
                useTestNoteCutSoundEffects: false,
                startPaused: false,
                beforeSceneSwitchToGameplayCallback: null,
                afterSceneSwitchToGameplayCallback: null,
                levelFinishedCallback: levelFinishedCallback,
                levelRestartedCallback: null
            );
#else
            menuTransitionsHelper.StartStandardLevel(
                gameMode: gameMode,
                beatmapKey: beatmapKey,
                beatmapLevel: beatmapLevel,
                overrideEnvironmentSettings: overrideEnvironmentSettings ?? playerData.overrideEnvironmentSettings,
                playerOverrideColorScheme: replayColorScheme ?? playerData.colorSchemesSettings.GetOverrideColorScheme(),
                playerOverrideLightshowColors: replayColorScheme?.overrideLights ?? playerData.colorSchemesSettings.ShouldOverrideLightshowColors(),
                gameplayModifiers: gameplayModifiers,
                playerSpecificSettings: playerSpecificSettings,
                practiceSettings: null,
                environmentsListModel: environmentsListModel,
                gameplayAdditionalInformation: new GameplayAdditionalInformation(backButtonText: backButtonText),
                beforeSceneSwitchToGameplayCallback: null,
                afterSceneSwitchToGameplayCallback: null,
                levelFinishedCallback: levelFinishedCallback,
                levelRestartedCallback: null
            );
#endif
        }
    }
}
