using ScoreSaber.Features.Leaderboards.Adapters.LeaderboardCore;
using ScoreSaber.Features.Leaderboards.Services;
using ScoreSaber.Features.Leaderboards.UI;
using ScoreSaber.Features.Leaderboards.UI.Avatars;
using Zenject;

namespace ScoreSaber.Features.Leaderboards {
    internal class LeaderboardFeatureInstaller : Installer {
        public override void InstallBindings() {
            Container.Bind<MaxScoreCache>().AsSingle();
            Container.Bind<LeaderboardPlayerScoreCache>().AsSingle();
            Container.Bind<LeaderboardScreenLoader>().AsSingle();
            Container.BindInterfacesAndSelfTo<LeaderboardScreenSession>().AsSingle();
            Container.Bind<LeaderboardTweeningService>().AsSingle();
            Container.Bind<LeaderboardQueryService>().AsSingle();
            Container.Bind<LeaderboardAvatarHost>().AsSingle();
            Container.BindInterfacesTo<LeaderboardPanelFlow>().AsSingle().NonLazy();
            Container.BindInterfacesTo<LeaderboardBeatmapController>().AsSingle().NonLazy();
            Container.BindInterfacesTo<LeaderboardInteractionController>().AsSingle().NonLazy();
            Container.BindInterfacesTo<LeaderboardPresentationController>().AsSingle().NonLazy();
            Container.BindInterfacesTo<LeaderboardStatusController>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LeaderboardModalFlow>().AsSingle();

            Container.Bind<PanelView>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<ScoreSaberLeaderboardCoreViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<ScoreSaberLeaderboardOverlayController>().AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<ScoreSaberCustomLeaderboard>().AsSingle();
        }
    }
}
