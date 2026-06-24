using ScoreSaber.Core.Gameplay;
using ScoreSaber.Features.Replays;
using System;
using System.Linq;

namespace ScoreSaber.Features.Leaderboards.Domain {
    internal class ScoreMap {

        internal LeaderboardScore Score { get; private set; }
        internal LeaderboardInfoMap Parent { get; set; }
        internal bool HasLocalReplay { get; set; }
        internal double Accuracy { get; set; }
        internal GameplayModifiers GameplayModifiers { get; set; }
        internal string ModifierText { get; private set; }

        internal ScoreMap(LeaderboardScore score, LeaderboardInfoMap leaderboardInfo, int maxMultipliedScore, ReplayStorageService replayStorageService) {
            Score = score;
            ModifierText = string.Join(",", score.Mods);

            GameplayModifiersMap replayMods = new GameplayModifiersMap();
            if (score.Mods.Count > 0) {
                replayMods = ScoreSaberGameplayModifiers.FromCodes(score.Mods.ToArray(), false);
            }

            double maxScore = maxMultipliedScore * replayMods.TotalMultiplier;

            Parent = leaderboardInfo;
            HasLocalReplay = replayStorageService.LocalReplayExists(leaderboardInfo.BeatmapLevel, leaderboardInfo.BeatmapKey, this);
            Score.Weight = Math.Round(score.Weight * 100, 2);
            Score.PP = Math.Round(score.PP, 2);
            Accuracy = Math.Round((score.ModifiedScore / maxScore) * 100, 2);
            GameplayModifiers = replayMods.GameplayModifiers;
            if (HasLocalReplay) {
                Score.HasReplay = true;
            }
        }

    }
}
