using HMUI;
using LeaderboardCore.Managers;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.Leaderboards.UI;
using System;
using Zenject;

namespace ScoreSaber.Features.Leaderboards.Adapters.LeaderboardCore {

    internal class ScoreSaberCustomLeaderboard : CustomLeaderboardAdapter, IInitializable, IDisposable {
        private readonly CustomLeaderboardManager _customLeaderboardManager;
        private readonly ScoreSaberLeaderboardCoreViewController _leaderboardViewController;
        private readonly PanelView _panelView;

        public ScoreSaberCustomLeaderboard(CustomLeaderboardManager customLeaderboardManager, ScoreSaberLeaderboardCoreViewController leaderboardViewController, PanelView panelView) {
            _customLeaderboardManager = customLeaderboardManager;
            _leaderboardViewController = leaderboardViewController;
            _panelView = panelView;
        }

        protected override string leaderboardId => "ScoreSaber";

        protected override ViewController leaderboardViewController => _leaderboardViewController;

        protected override ViewController panelViewController => _panelView;

        protected override bool ShowForLevelId(string levelId) => ScoreSaberBeatmapKey.IsSupportedLevelId(levelId);

        public void Initialize() => _customLeaderboardManager.Register(this);

        public void Dispose() => _customLeaderboardManager.Unregister(this);
    }
}
