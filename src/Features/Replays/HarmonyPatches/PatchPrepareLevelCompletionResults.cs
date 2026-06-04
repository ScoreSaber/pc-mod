using HarmonyLib;

namespace ScoreSaber.Features.Replays.HarmonyPatches {
    [HarmonyPatch(typeof(PrepareLevelCompletionResults), nameof(PrepareLevelCompletionResults.FillLevelCompletionResults))]
    internal class PatchPrepareLevelCompletionResults {
        internal static void Prefix(ref LevelCompletionResults.LevelEndStateType levelEndStateType) {
            if (ReplayStateRegistry.IsPlaybackEnabled) {
                levelEndStateType = LevelCompletionResults.LevelEndStateType.Incomplete;
            }
        }
    }
}
