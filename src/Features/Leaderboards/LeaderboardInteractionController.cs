using ScoreSaber.Features.Leaderboards.Adapters.LeaderboardCore;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.Leaderboards.Services;
using ScoreSaber.Features.Leaderboards.UI;
using System;
using Zenject;

namespace ScoreSaber.Features.Leaderboards {
    internal class LeaderboardInteractionController : IInitializable, IDisposable {
        private readonly ScoreSaberLeaderboardCoreViewController _leaderboardViewController;
        private readonly LeaderboardScreenSession _leaderboardSession;
        private readonly LeaderboardModalFlow _modalFlow;

        public LeaderboardInteractionController(
            ScoreSaberLeaderboardCoreViewController leaderboardViewController,
            LeaderboardScreenSession leaderboardSession,
            LeaderboardModalFlow modalFlow) {
            _leaderboardViewController = leaderboardViewController;
            _leaderboardSession = leaderboardSession;
            _modalFlow = modalFlow;
        }

        public void Initialize() {
            _leaderboardViewController.ScoreSelected += LeaderboardViewControllerScoreSelected;
            _leaderboardViewController.ScopeSelected += LeaderboardViewControllerScopeSelected;
            _leaderboardViewController.PageUpRequested += LeaderboardViewControllerPageUpRequested;
            _leaderboardViewController.PageDownRequested += LeaderboardViewControllerPageDownRequested;
        }

        private void LeaderboardViewControllerScoreSelected(int index) {
            ScoreMap score = _leaderboardSession.GetScore(index);
            if (score == null) {
                return;
            }

            _modalFlow.ShowScore(score);
        }

        private void LeaderboardViewControllerScopeSelected(LeaderboardScreenScope scope) => _leaderboardSession.SelectScope(scope);

        private void LeaderboardViewControllerPageUpRequested() => _leaderboardSession.PageUp();

        private void LeaderboardViewControllerPageDownRequested() => _leaderboardSession.PageDown();

        public void Dispose() {
            _leaderboardViewController.ScoreSelected -= LeaderboardViewControllerScoreSelected;
            _leaderboardViewController.ScopeSelected -= LeaderboardViewControllerScopeSelected;
            _leaderboardViewController.PageUpRequested -= LeaderboardViewControllerPageUpRequested;
            _leaderboardViewController.PageDownRequested -= LeaderboardViewControllerPageDownRequested;
        }
    }
}
