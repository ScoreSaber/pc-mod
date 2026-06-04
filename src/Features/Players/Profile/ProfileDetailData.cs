using ScoreSaber.Core.Presentation;
using ScoreSaber.Features.Players.Domain;
using System;
using System.Collections.Generic;

namespace ScoreSaber.Features.Players.Profile {
    internal class ProfileDetailData {
        internal PlayerProfile Player { get; set; }
        internal string DisplayName { get; set; }
        internal string Avatar { get; set; }
        internal string RankText { get; set; }
        internal string PPText { get; set; }
        internal string RankedAccuracyText { get; set; }
        internal string TotalScoreText { get; set; }
        internal bool UsesFurryFont { get; set; }
        internal ProfileCrownData Crown { get; set; }
        internal List<ProfileBadgeData> Badges { get; set; } = new List<ProfileBadgeData>();

        internal static ProfileDetailData Create(PlayerProfile player) {
            Tuple<string, string> crownDetails = PlayerPresentation.GetCrownDetails(player.Id);
            var data = new ProfileDetailData {
                Player = player,
                DisplayName = player.Name,
                Avatar = player.Avatar,
                RankText = $"#{string.Format("{0:n0}", player.Stats.Rank)}",
                PPText = $"<color=#6772E5>{string.Format("{0:n0}", player.Stats.TotalPP)}pp</color>",
                RankedAccuracyText = $"{Math.Round(player.Stats.WeightedAverageAccuracy, 2)}%",
                TotalScoreText = string.Format("{0:n0}", player.Stats.TotalScore),
                UsesFurryFont = PlayerPresentation.UsesFurryFont(player.Id),
                Crown = new ProfileCrownData {
                    Image = crownDetails.Item1,
                    Description = crownDetails.Item2
                }
            };

            foreach (PlayerBadge badge in player.Badges) {
                data.Badges.Add(new ProfileBadgeData {
                    Image = badge.Image,
                    Description = badge.Description
                });
            }

            return data;
        }
    }

    internal class ProfileCrownData {
        internal string Image { get; set; }
        internal string Description { get; set; }
        internal bool HasCrown => !string.IsNullOrEmpty(Image);
    }

    internal class ProfileBadgeData {
        internal string Image { get; set; }
        internal string Description { get; set; }
    }
}
