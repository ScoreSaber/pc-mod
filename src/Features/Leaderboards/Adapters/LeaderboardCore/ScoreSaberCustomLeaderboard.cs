using HMUI;
using LeaderboardCore.Managers;
using LeaderboardCore.Models;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.Leaderboards.UI;
using System;
using Zenject;

namespace ScoreSaber.Features.Leaderboards.Adapters.LeaderboardCore {

    internal class ScoreSaberCustomLeaderboard : CustomLeaderboard, IInitializable, IDisposable {
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

        public override bool ShowForLevel(BeatmapKey? beatmapKey) => beatmapKey.HasValue && ScoreSaberBeatmapKey.IsSupported(beatmapKey.Value);

        public void Initialize() => _customLeaderboardManager.Register(this);

        public void Dispose() => _customLeaderboardManager.Unregister(this);
    }
}
