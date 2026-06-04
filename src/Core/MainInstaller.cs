using ScoreSaber.Core.Api;
using ScoreSaber.Core.Presentation;
using ScoreSaber.Features.Leaderboards;
using ScoreSaber.Features.MainMenu;
using ScoreSaber.Features.Players;
using ScoreSaber.Features.Replays;
using ScoreSaber.Features.ScoreSubmission;
using ScoreSaber.Features.Multiplayer;
using Zenject;

namespace ScoreSaber.Core {
    internal class MainInstaller : Installer {

        public override void InstallBindings() {
            Container.BindInstance(new object()).WithId("ScoreSaberUIBindings").AsCached();
            Container.Bind<IScoreSaberApiClient>().To<ScoreSaberApiClient>().AsSingle();
            Container.Bind<RemoteImageService>().AsSingle();
            Container.Install<PlayersFeatureInstaller>();
            Container.Install<ReplayFeatureInstaller>();
            Container.Install<ScoreSubmissionFeatureInstaller>();
            Container.Install<LeaderboardFeatureInstaller>();
            Container.Install<MainMenuFeatureInstaller>();
            Container.BindInterfacesTo<MultiplayerSessionController>().AsSingle();
        }
    }
}
