using ScoreSaber.Core.Platform;
using ScoreSaber.Core.Presentation;
using ScoreSaber.Features.Leaderboards.Domain;
using System;

namespace ScoreSaber.Features.Leaderboards.UI.ScoreDetails {
    internal class ScoreDetailData {
        internal ScoreMap Score { get; set; }
        internal string PlayerId { get; set; }
        internal string PlayerNameText { get; set; }
        internal string DeviceHMDText { get; set; }
        internal string DeviceControllerLeftText { get; set; }
        internal string DeviceControllerRightText { get; set; }
        internal string ScoreText { get; set; }
        internal string PPText { get; set; }
        internal string MaxComboText { get; set; }
        internal string FullComboText { get; set; }
        internal string BadCutsText { get; set; }
        internal string MissedNotesText { get; set; }
        internal string ModifiersText { get; set; }
        internal string TimeSetText { get; set; }
        internal string CrownImage { get; set; }
        internal string CrownDescription { get; set; }
        internal bool HasCrown => !string.IsNullOrEmpty(CrownImage);
        internal bool HasReplay { get; set; }

        internal static ScoreDetailData Create(ScoreMap scoreMap) {
            LeaderboardScore score = scoreMap.Score;
            Tuple<string, string> crownDetails = PlayerPresentation.GetCrownDetails(score.Player.Id);
            bool givesPP = scoreMap.Parent.Leaderboard.Status == LeaderboardStatus.Ranked;
            return new ScoreDetailData {
                Score = scoreMap,
                PlayerId = score.Player.Id,
                PlayerNameText = $"{score.Player.Name}'s score",
                DeviceHMDText = score.Device.HMD ?? VRDevices.GetLegacyHMDFriendlyName(score.LegacyHMDId),
                DeviceControllerLeftText = string.IsNullOrEmpty(score.Device.ControllerLeft) ? "N/A" : score.Device.ControllerLeft,
                DeviceControllerRightText = string.IsNullOrEmpty(score.Device.ControllerRight) ? "N/A" : score.Device.ControllerRight,
                ScoreText = $"{string.Format("{0:n0}", score.ModifiedScore)} (<color=#FFD42A>{scoreMap.Accuracy}%</color>)",
                PPText = givesPP ? $"<color=#6772E5>{score.PP}pp</color>" : "N/A",
                MaxComboText = score.MaxCombo != 0 ? score.MaxCombo.ToString() : "N/A",
                FullComboText = score.MaxCombo != 0 ? score.FullCombo ? "<color=#9EDBB1>Yes</color>" : "<color=#FF0000>No</color>" : "N/A",
                BadCutsText = score.MaxCombo != 0 ? score.BadCuts > 0 ? $"<color=#FF0000>{score.BadCuts}</color>" : score.BadCuts.ToString() : "N/A",
                MissedNotesText = score.MaxCombo != 0 ? score.MissedNotes > 0 ? $"<color=#FF0000>{score.MissedNotes}</color>" : score.MissedNotes.ToString() : "N/A",
                ModifiersText = scoreMap.ModifierText,
                TimeSetText = ScoreAgeFormatter.FormatAgo(score.CreatedAt),
                CrownImage = crownDetails.Item1,
                CrownDescription = crownDetails.Item2,
                HasReplay = score.HasReplay
            };
        }
    }
}
