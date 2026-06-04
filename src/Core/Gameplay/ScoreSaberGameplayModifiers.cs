using System.Collections.Generic;

namespace ScoreSaber.Core.Gameplay {
    internal static class ScoreSaberGameplayModifiers {
        internal static GameplayModifiersMap FromCodes(string[] modifiers, bool isPositiveModifiersEnabled) {
            double totalMultiplier = 1;
            var energyType = GameplayModifiers.EnergyType.Bar;
            var obstacleType = GameplayModifiers.EnabledObstacleType.All;
            var songSpeed = GameplayModifiers.SongSpeed.Normal;
            bool NF = false;
            bool IF = false;
            bool NB = false;
            bool DA = false;
            bool GN = false;
            bool NA = false;
            bool PM = false;
            bool SC = false;
            bool SA = false;

            foreach (string modifier in modifiers) {
                switch (modifier) {
                    case "BE":
                        totalMultiplier += 0;
                        energyType = GameplayModifiers.EnergyType.Battery;
                        break;
                    case "NF":
                        totalMultiplier += -0.5;
                        NF = true;
                        break;
                    case "IF":
                        totalMultiplier += 0;
                        IF = true;
                        break;
                    case "NO":
                        totalMultiplier += -0.05;
                        obstacleType = GameplayModifiers.EnabledObstacleType.NoObstacles;
                        break;
                    case "NB":
                        totalMultiplier += -0.10;
                        NB = true;
                        break;
                    case "DA":
                        if (isPositiveModifiersEnabled) {
                            totalMultiplier += 0.02;
                        }
                        DA = true;
                        break;
                    case "GN":
                        if (isPositiveModifiersEnabled) {
                            totalMultiplier += 0.04;
                        }
                        GN = true;
                        break;
                    case "NA":
                        totalMultiplier += 0;
                        NA = true;
                        break;
                    case "SS":
                        totalMultiplier += -0.3;
                        songSpeed = GameplayModifiers.SongSpeed.Slower;
                        break;
                    case "FS":
                        if (isPositiveModifiersEnabled) {
                            totalMultiplier += 0.08;
                        }
                        songSpeed = GameplayModifiers.SongSpeed.Faster;
                        break;
                    case "SF":
                        songSpeed = GameplayModifiers.SongSpeed.SuperFast;
                        break;
                    case "PM":
                        PM = true;
                        break;
                    case "SC":
                        SC = true;
                        break;
                    case "SA":
                        SA = true;
                        break;
                }
            }

            var gameplayModifiers = new GameplayModifiers(energyType, NF, IF, false, obstacleType, NB, false, SA, DA, songSpeed, NA, GN, PM, false, SC);
            return new GameplayModifiersMap(gameplayModifiers) {
                TotalMultiplier = totalMultiplier
            };
        }

        internal static List<string> ToCodeList(LevelCompletionResults results) {
            return ToCodeList(results.gameplayModifiers, results.energy == 0);
        }

        internal static List<string> ToCodeList(GameplayModifiers gameplayModifiers, bool includeNoFail) {
            List<string> result = new List<string>();
            AddIf(result, gameplayModifiers.energyType == GameplayModifiers.EnergyType.Battery, "BE");
            AddIf(result, gameplayModifiers.noFailOn0Energy && includeNoFail, "NF");
            AddIf(result, gameplayModifiers.instaFail, "IF");
            AddIf(result, gameplayModifiers.failOnSaberClash, "SC");
            AddIf(result, gameplayModifiers.enabledObstacleType == GameplayModifiers.EnabledObstacleType.NoObstacles, "NO");
            AddIf(result, gameplayModifiers.noBombs, "NB");
            AddIf(result, gameplayModifiers.strictAngles, "SA");
            AddIf(result, gameplayModifiers.disappearingArrows, "DA");
            AddIf(result, gameplayModifiers.ghostNotes, "GN");
            AddIf(result, gameplayModifiers.songSpeed == GameplayModifiers.SongSpeed.Slower, "SS");
            AddIf(result, gameplayModifiers.songSpeed == GameplayModifiers.SongSpeed.Faster, "FS");
            AddIf(result, gameplayModifiers.songSpeed == GameplayModifiers.SongSpeed.SuperFast, "SF");
            AddIf(result, gameplayModifiers.smallCubes, "SC");
            AddIf(result, gameplayModifiers.proMode, "PM");
            AddIf(result, gameplayModifiers.noArrows, "NA");
            return result;
        }

        private static void AddIf(List<string> result, bool condition, string code) {
            if (condition) {
                result.Add(code);
            }
        }
    }

    internal class GameplayModifiersMap {
        internal GameplayModifiers GameplayModifiers { get; set; }
        internal double TotalMultiplier { get; set; } = 1;

        internal GameplayModifiersMap() { }

        internal GameplayModifiersMap(GameplayModifiers gameplayModifiers) {
            GameplayModifiers = gameplayModifiers;
        }
    }
}
