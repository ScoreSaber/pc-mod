using HarmonyLib;
using System;

namespace ScoreSaber.Features.Replays.HarmonyPatches {
    [HarmonyPatch(typeof(RelativeScoreAndImmediateRankCounter), nameof(RelativeScoreAndImmediateRankCounter.UpdateRelativeScoreAndImmediateRank))]
    internal class ImmediateRankReinitializer {
        internal static bool Prefix(RelativeScoreAndImmediateRankCounter __instance, int score, int maxPossibleScore, ref Action ___relativeScoreOrImmediateRankDidChangeEvent) {
            if (!ReplayStateRegistry.IsModernPlaybackEnabled || score != 0 || maxPossibleScore != 0) {
                return true;
            }

            Accessors.RelativeScore(ref __instance, 1f);
            Accessors.ImmediateRank(ref __instance, RankModel.Rank.SS);
            ___relativeScoreOrImmediateRankDidChangeEvent?.Invoke();
            return false;
        }
    }
}
