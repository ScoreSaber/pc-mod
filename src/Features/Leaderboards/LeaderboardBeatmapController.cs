using LeaderboardCore.Interfaces;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.Leaderboards.Services;
using ScoreSaber.Features.Leaderboards.UI;
using ScoreSaber.Features.Leaderboards.UI.Avatars;
using ScoreSaber.Features.Players.Services;

namespace ScoreSaber.Features.Leaderboards {
    internal class LeaderboardBeatmapController : INotifyLeaderboardSet {
        private readonly ScoreSaberLeaderboardOverlayController _overlayController;
        private readonly LeaderboardAvatarHost _avatarHost;
        private readonly GameSessionService _gameSessionService;
        private readonly LeaderboardScreenSession _leaderboardSession;

        public LeaderboardBeatmapController(
            ScoreSaberLeaderboardOverlayController overlayController,
            LeaderboardAvatarHost avatarHost,
            GameSessionService gameSessionService,
            LeaderboardScreenSession leaderboardSession) {
            _overlayController = overlayController;
            _avatarHost = avatarHost;
            _gameSessionService = gameSessionService;
            _leaderboardSession = leaderboardSession;
        }

        public void OnLeaderboardSet(BeatmapKey beatmapKey) {
            if (!ScoreSaberBeatmapKey.IsCustomLevel(beatmapKey)) {
                _leaderboardSession.ClearBeatmap();
                return;
            }

            bool parsed = _overlayController.IsParsed;
            _overlayController.EnsureParsed();
            if (!parsed) {
                _avatarHost.ClearAvatars();
            }

            if (!ScoreSaberBeatmapKey.IsWip(beatmapKey)) {
                _gameSessionService.EnsureAuthenticated();
            }
            _leaderboardSession.SetBeatmap(beatmapKey);
        }
    }
}
