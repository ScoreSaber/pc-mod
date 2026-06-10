namespace ScoreSaber.Core.Compat {
    // this ctor changed after 1.29 (arcsVisible -> arcVisibility, added headsetHapticIntensity)
    internal static class PlayerSettingsCompat {
        internal static PlayerSpecificSettings ForReplay(PlayerSpecificSettings localPlayerSettings, bool leftHanded, float playerHeight, bool automaticPlayerHeight) {
#if BEAT_SABER_1_29_0
            return new PlayerSpecificSettings(leftHanded, playerHeight, automaticPlayerHeight, localPlayerSettings.sfxVolume,
                localPlayerSettings.reduceDebris, localPlayerSettings.noTextsAndHuds, localPlayerSettings.noFailEffects,
                localPlayerSettings.advancedHud, localPlayerSettings.autoRestart, localPlayerSettings.saberTrailIntensity,
                localPlayerSettings.noteJumpDurationTypeSettings, localPlayerSettings.noteJumpFixedDuration,
                localPlayerSettings.noteJumpStartBeatOffset, localPlayerSettings.hideNoteSpawnEffect,
                localPlayerSettings.adaptiveSfx, localPlayerSettings.arcsHapticFeedback, localPlayerSettings.arcsVisible,
                localPlayerSettings.environmentEffectsFilterDefaultPreset,
                localPlayerSettings.environmentEffectsFilterExpertPlusPreset);
#else
            return new PlayerSpecificSettings(leftHanded, playerHeight, automaticPlayerHeight, localPlayerSettings.sfxVolume,
                localPlayerSettings.reduceDebris, localPlayerSettings.noTextsAndHuds, localPlayerSettings.noFailEffects,
                localPlayerSettings.advancedHud, localPlayerSettings.autoRestart, localPlayerSettings.saberTrailIntensity,
                localPlayerSettings.noteJumpDurationTypeSettings, localPlayerSettings.noteJumpFixedDuration,
                localPlayerSettings.noteJumpStartBeatOffset, localPlayerSettings.hideNoteSpawnEffect,
                localPlayerSettings.adaptiveSfx, localPlayerSettings.arcsHapticFeedback, localPlayerSettings.arcVisibility,
                localPlayerSettings.environmentEffectsFilterDefaultPreset,
                localPlayerSettings.environmentEffectsFilterExpertPlusPreset,
                localPlayerSettings.headsetHapticIntensity);
#endif
        }
    }
}
