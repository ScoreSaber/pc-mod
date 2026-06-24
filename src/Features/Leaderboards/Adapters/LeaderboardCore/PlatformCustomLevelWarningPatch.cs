using HarmonyLib;

namespace ScoreSaber.Features.Leaderboards.Adapters.LeaderboardCore {
    [HarmonyPatch(typeof(LoadingControl), nameof(LoadingControl.ShowText))]
    internal static class PlatformCustomLevelWarningPatch {
        private static bool Prefix(LoadingControl __instance, string text) => !ScoreSaberLeaderboardCoreViewController.ShouldSuppressPlatformCustomLevelWarning(__instance, text);
    }
}
