using ScoreSaber.Features.Players.Services;
using Zenject;

namespace ScoreSaber.Features.Players {
    internal class PlayersFeatureInstaller : Installer {
        public override void InstallBindings() {
            Container.Bind<GameSessionService>().AsSingle();
            Container.Bind<PlayerProfileService>().AsSingle();
            Container.BindInterfacesAndSelfTo<LocalPlayerPanelSession>().AsSingle().NonLazy();
            Container.Bind<GlobalPlayerQueryService>().AsSingle();
            Container.Bind<GlobalPlayerSession>().AsSingle();
        }
    }
}
