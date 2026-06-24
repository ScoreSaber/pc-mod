using UnityEngine;

namespace ScoreSaber.Core.Compat {
    internal static class ColorSchemeCompat {
        internal static ColorScheme ForReplay(
            PlayerData playerData,
            Color? leftSaberColor,
            Color? rightSaberColor,
            Color? obstacleColor = null,
            Color? environmentColor0 = null,
            Color? environmentColor1 = null,
            Color? environmentColorW = null,
            Color? environmentColor0Boost = null,
            Color? environmentColor1Boost = null,
            Color? environmentColorWBoost = null,
            bool supportsEnvironmentColorBoost = false) {

            if (!leftSaberColor.HasValue || !rightSaberColor.HasValue) {
                return null;
            }

            ColorScheme baseScheme = playerData.colorSchemesSettings.GetOverrideColorScheme()
                ?? playerData.colorSchemesSettings.GetSelectedColorScheme();
            if (baseScheme == null) {
                return null;
            }

            bool hasLightColors = obstacleColor.HasValue
                && environmentColor0.HasValue
                && environmentColor1.HasValue
                && environmentColor0Boost.HasValue
                && environmentColor1Boost.HasValue;
            Color replayObstacleColor = hasLightColors ? obstacleColor.Value : baseScheme.obstaclesColor;
            Color replayEnvironmentColor0 = hasLightColors ? environmentColor0.Value : baseScheme.environmentColor0;
            Color replayEnvironmentColor1 = hasLightColors ? environmentColor1.Value : baseScheme.environmentColor1;
            Color replayEnvironmentColor0Boost = hasLightColors ? environmentColor0Boost.Value : baseScheme.environmentColor0Boost;
            Color replayEnvironmentColor1Boost = hasLightColors ? environmentColor1Boost.Value : baseScheme.environmentColor1Boost;
            bool replaySupportsEnvironmentColorBoost = hasLightColors ? supportsEnvironmentColorBoost : baseScheme.supportsEnvironmentColorBoost;
#if BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            Color replayEnvironmentColorW = hasLightColors && environmentColorW.HasValue ? environmentColorW.Value : baseScheme.environmentColorW;
            Color replayEnvironmentColorWBoost = hasLightColors && environmentColorWBoost.HasValue ? environmentColorWBoost.Value : baseScheme.environmentColorWBoost;
            return new ColorScheme(
                baseScheme,
                overrideNotes: true,
                leftSaberColor.Value,
                rightSaberColor.Value,
                overrideLights: hasLightColors,
                replayEnvironmentColor0,
                replayEnvironmentColor1,
                replayEnvironmentColorW,
                replaySupportsEnvironmentColorBoost,
                replayEnvironmentColor0Boost,
                replayEnvironmentColor1Boost,
                replayEnvironmentColorWBoost,
                replayObstacleColor);
#elif BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0
            Color replayEnvironmentColorW = hasLightColors && environmentColorW.HasValue ? environmentColorW.Value : baseScheme.environmentColorW;
            Color replayEnvironmentColorWBoost = hasLightColors && environmentColorWBoost.HasValue ? environmentColorWBoost.Value : baseScheme.environmentColorWBoost;
            return new ColorScheme(
                baseScheme,
                leftSaberColor.Value,
                rightSaberColor.Value,
                replayEnvironmentColor0,
                replayEnvironmentColor1,
                replayEnvironmentColorW,
                replaySupportsEnvironmentColorBoost,
                replayEnvironmentColor0Boost,
                replayEnvironmentColor1Boost,
                replayEnvironmentColorWBoost,
                replayObstacleColor);
#else
            return new ColorScheme(
                baseScheme,
                leftSaberColor.Value,
                rightSaberColor.Value,
                replayEnvironmentColor0,
                replayEnvironmentColor1,
                replaySupportsEnvironmentColorBoost,
                replayEnvironmentColor0Boost,
                replayEnvironmentColor1Boost,
                replayObstacleColor);
#endif
        }
    }
}
