namespace ScoreSaber.Core.Compat {
    // this ctor changed after 1.29 (arcsVisible -> arcVisibility, added headsetHapticIntensity)
    internal static class PlayerSettingsCompat {
        internal static PlayerSpecificSettings ForReplay(
            PlayerSpecificSettings localPlayerSettings,
            bool leftHanded,
            float playerHeight,
            bool automaticPlayerHeight,
            bool useRecordedPlayerSettings,
            bool noTextsAndHuds,
            float saberTrailIntensity,
            bool hideNoteSpawnEffect,
            bool arcsHapticFeedback,
            int arcVisibility,
            int environmentEffectsFilterDefaultPreset,
            int environmentEffectsFilterExpertPlusPreset) {

            bool replayNoTextsAndHuds = useRecordedPlayerSettings ? noTextsAndHuds : localPlayerSettings.noTextsAndHuds;
            float replaySaberTrailIntensity = useRecordedPlayerSettings ? saberTrailIntensity : localPlayerSettings.saberTrailIntensity;
            bool replayHideNoteSpawnEffect = useRecordedPlayerSettings ? hideNoteSpawnEffect : localPlayerSettings.hideNoteSpawnEffect;
            bool replayArcsHapticFeedback = useRecordedPlayerSettings ? arcsHapticFeedback : localPlayerSettings.arcsHapticFeedback;
            EnvironmentEffectsFilterPreset replayEnvironmentEffectsFilterDefaultPreset = useRecordedPlayerSettings
                ? EnvironmentEffectsFilterPresetOrDefault(environmentEffectsFilterDefaultPreset, localPlayerSettings.environmentEffectsFilterDefaultPreset)
                : localPlayerSettings.environmentEffectsFilterDefaultPreset;
            EnvironmentEffectsFilterPreset replayEnvironmentEffectsFilterExpertPlusPreset = useRecordedPlayerSettings
                ? EnvironmentEffectsFilterPresetOrDefault(environmentEffectsFilterExpertPlusPreset, localPlayerSettings.environmentEffectsFilterExpertPlusPreset)
                : localPlayerSettings.environmentEffectsFilterExpertPlusPreset;
#if BEAT_SABER_1_29_0
            ArcVisibilityType replayArcVisibility = useRecordedPlayerSettings
                ? ArcVisibilityOrDefault(arcVisibility, localPlayerSettings.arcsVisible)
                : localPlayerSettings.arcsVisible;
            return new PlayerSpecificSettings(leftHanded, playerHeight, automaticPlayerHeight, localPlayerSettings.sfxVolume,
                localPlayerSettings.reduceDebris, replayNoTextsAndHuds, localPlayerSettings.noFailEffects,
                localPlayerSettings.advancedHud, localPlayerSettings.autoRestart, replaySaberTrailIntensity,
                localPlayerSettings.noteJumpDurationTypeSettings, localPlayerSettings.noteJumpFixedDuration,
                localPlayerSettings.noteJumpStartBeatOffset, replayHideNoteSpawnEffect,
                localPlayerSettings.adaptiveSfx, replayArcsHapticFeedback, replayArcVisibility,
                replayEnvironmentEffectsFilterDefaultPreset,
                replayEnvironmentEffectsFilterExpertPlusPreset);
#else
            ArcVisibilityType replayArcVisibility = useRecordedPlayerSettings
                ? ArcVisibilityOrDefault(arcVisibility, localPlayerSettings.arcVisibility)
                : localPlayerSettings.arcVisibility;
            return new PlayerSpecificSettings(leftHanded, playerHeight, automaticPlayerHeight, localPlayerSettings.sfxVolume,
                localPlayerSettings.reduceDebris, replayNoTextsAndHuds, localPlayerSettings.noFailEffects,
                localPlayerSettings.advancedHud, localPlayerSettings.autoRestart, replaySaberTrailIntensity,
                localPlayerSettings.noteJumpDurationTypeSettings, localPlayerSettings.noteJumpFixedDuration,
                localPlayerSettings.noteJumpStartBeatOffset, replayHideNoteSpawnEffect,
                localPlayerSettings.adaptiveSfx, replayArcsHapticFeedback, replayArcVisibility,
                replayEnvironmentEffectsFilterDefaultPreset,
                replayEnvironmentEffectsFilterExpertPlusPreset,
                localPlayerSettings.headsetHapticIntensity);
#endif
        }

        private static ArcVisibilityType ArcVisibilityOrDefault(int value, ArcVisibilityType fallback) {

            return value >= (int)ArcVisibilityType.None && value <= (int)ArcVisibilityType.High
                ? (ArcVisibilityType)value
                : fallback;
        }

        private static EnvironmentEffectsFilterPreset EnvironmentEffectsFilterPresetOrDefault(int value, EnvironmentEffectsFilterPreset fallback) {

            switch (value) {
                case (int)EnvironmentEffectsFilterPreset.AllEffects:
                case (int)EnvironmentEffectsFilterPreset.StrobeFilter:
                case (int)EnvironmentEffectsFilterPreset.NoEffects:
                    return (EnvironmentEffectsFilterPreset)value;
                default:
                    return fallback;
            }
        }
    }
}
