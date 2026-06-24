using HarmonyLib;
using IPA.Loader;
using LeaderboardCore.Models;
using ScoreSaber.Features.Leaderboards.Domain;
using System.Collections.Generic;
using System.Reflection;
using HiveVersion = Hive.Versioning.Version;

namespace ScoreSaber.Features.Leaderboards.Adapters.LeaderboardCore {
    [HarmonyPatch]
    internal static class LegacyLeaderboardCoreDefaultLeaderboardPatch {
        private const string NavigationButtonsType = "LeaderboardCore.UI.ViewControllers.LeaderboardNavigationButtonsController";
        private const string LegacyScoreSaberLeaderboardCoreType = "LeaderboardCore.Models.ScoreSaberCustomLeaderboard";
        private static readonly HiveVersion MaxPatchedLeaderboardCoreVersion = new HiveVersion("1.7.0");

        private static bool Prepare() => ShouldPatchLegacyLeaderboardCore() && TargetShowDefaultLeaderboard() != null;

        private static MethodBase TargetMethod() => TargetShowDefaultLeaderboard();

        private static MethodBase TargetShowDefaultLeaderboard() {
            System.Type type = typeof(CustomLeaderboard).Assembly.GetType(NavigationButtonsType);
            return AccessTools.PropertyGetter(type, "ShowDefaultLeaderboard");
        }

        private static void Postfix(object __instance, ref bool __result) {
            if (!__result || !HasCustomLeaderboard(__instance) || !IsCustomLevel(__instance)) {
                return;
            }

            __result = false;
        }

        private static bool ShouldPatchLegacyLeaderboardCore() {
            PluginMetadata metadata = PluginManager.GetPluginFromId("LeaderboardCore");
            return metadata != null
                && metadata.HVersion.CompareTo(MaxPatchedLeaderboardCoreVersion) <= 0
                && typeof(CustomLeaderboard).Assembly.GetType(LegacyScoreSaberLeaderboardCoreType) != null;
        }

        private static bool HasCustomLeaderboard(object instance) {
            var leaderboards = Traverse.Create(instance).Field("orderedCustomLeaderboards").GetValue<List<CustomLeaderboard>>();
            return leaderboards != null && leaderboards.Count > 0;
        }

#if BEAT_SABER_1_29_0
        private static bool IsCustomLevel(object instance) {
            var selectedLevel = Traverse.Create(instance).Field("selectedLevel").GetValue<IPreviewBeatmapLevel>();
            return selectedLevel is CustomPreviewBeatmapLevel;
        }
#else
        private static bool IsCustomLevel(object instance) {
            var selectedLevelKey = Traverse.Create(instance).Field("selectedLevelKey").GetValue<BeatmapKey?>();
            return selectedLevelKey.HasValue && ScoreSaberBeatmapKey.IsCustomLevel(selectedLevelKey.Value);
        }
#endif
    }
}
