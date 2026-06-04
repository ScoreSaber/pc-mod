using ScoreSaber.Features.Leaderboards.Adapters.LeaderboardCore;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.Leaderboards.Services;
using ScoreSaber.Features.Leaderboards.UI;
using ScoreSaber.Features.Leaderboards.UI.Avatars;
using System;
using System.Threading;
using Zenject;

namespace ScoreSaber.Features.Leaderboards {
    internal class LeaderboardPresentationController : IInitializable, IDisposable {
        private readonly PanelView _panelView;
        private readonly LeaderboardScreenSession _leaderboardSession;
        private readonly ScoreSaberLeaderboardCoreViewController _leaderboardViewController;
        private readonly ScoreSaberLeaderboardOverlayController _overlayController;
        private readonly LeaderboardAvatarHost _avatarHost;

        private CancellationTokenSource _avatarCancellation;

        public LeaderboardPresentationController(
            PanelView panelView,
            LeaderboardScreenSession leaderboardSession,
            ScoreSaberLeaderboardCoreViewController leaderboardViewController,
            ScoreSaberLeaderboardOverlayController overlayController,
            LeaderboardAvatarHost avatarHost) {
            _panelView = panelView;
            _leaderboardSession = leaderboardSession;
            _leaderboardViewController = leaderboardViewController;
            _overlayController = overlayController;
            _avatarHost = avatarHost;
        }

        public void Initialize() => _leaderboardSession.StateChanged += LeaderboardStateChanged;

        private void LeaderboardStateChanged(LeaderboardScreenState state) {
            Plugin.Log.Debug($"Leaderboard UI state: {state.Status}, loaded={state.IsLoaded}, scores={state.Leaderboard?.Scores?.Length ?? 0}");
            _leaderboardViewController.SetRankColumnOffset(LeaderboardRankLayout.OffsetFor(state.Status == LeaderboardScreenStatus.Loaded ? state.Leaderboard : null));
            _leaderboardViewController.ApplyState(state);
            if (!_overlayController.IsParsed) {
                return;
            }

            switch (state.Status) {
                case LeaderboardScreenStatus.Loading:
                    ResetAvatarCancellation();
                    _avatarHost.ClearAvatars();
                    break;
                case LeaderboardScreenStatus.Loaded:
                    ShowLoadedLeaderboard(state);
                    break;
                default:
                    ShowLeaderboardError(state);
                    break;
            }
        }

        private void ShowLoadedLeaderboard(LeaderboardScreenState state) {
            _panelView.DismissPrompt();
            _panelView.SetRankedStatus(state.RankedStatus);
            if (state.Leaderboard == null) {
                return;
            }

            if (_avatarCancellation == null) {
                ResetAvatarCancellation();
            }
            _overlayController.ApplyAvatarLayout(state.Leaderboard);
            _avatarHost.LoadAvatars(state.Leaderboard, _avatarCancellation.Token);
        }

        private void ShowLeaderboardError(LeaderboardScreenState state) {
            _avatarHost.ClearAvatars();
            _panelView.DismissPrompt();
            _panelView.SetRankedStatus(state.Leaderboard != null ? state.RankedStatus : "Unavailable");
        }

        private void ResetAvatarCancellation() {
            _avatarCancellation?.Cancel();
            _avatarCancellation?.Dispose();
            _avatarCancellation = new CancellationTokenSource();
        }

        public void Dispose() {
            _avatarCancellation?.Cancel();
            _avatarCancellation?.Dispose();
            _leaderboardSession.StateChanged -= LeaderboardStateChanged;
        }
    }
}
