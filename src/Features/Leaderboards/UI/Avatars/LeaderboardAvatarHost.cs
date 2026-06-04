using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Core.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ScoreSaber.Features.Leaderboards.UI.Avatars {
    internal class LeaderboardAvatarHost {
        private const int MaximumAvatars = 10;

        internal List<LeaderboardAvatarView> Avatars { get; }

        public LeaderboardAvatarHost(RemoteImageService remoteImageService, ScoreSaberUIMaterials materials) {
            Avatars = Enumerable.Range(0, MaximumAvatars).Select(_ => new LeaderboardAvatarView(remoteImageService, materials)).ToList();
        }

        internal void LoadAvatars(LeaderboardMap leaderboard, CancellationToken cancellationToken) {
            ClearAvatars();
            int count = Math.Min(leaderboard.Scores.Length, Avatars.Count);
            for (int i = 0; i < count; i++) {
                Avatars[i].Load(leaderboard.Scores[i].Score.Player.Avatar, cancellationToken);
            }
        }

        internal void ClearAvatars() {
            Avatars.ForEach(avatar => avatar.Clear());
        }
    }
}
