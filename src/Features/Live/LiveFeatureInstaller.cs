using ScoreSaber.Features.Live.Ludus.Services;
using ScoreSaber.Features.Live.Compete.Services;
using ScoreSaber.Features.Live.Compete.UI.FlowCoordinators;
using ScoreSaber.Features.Live.Compete.UI.ViewControllers.CodeEntry;
using ScoreSaber.Features.Live.Compete.UI.ViewControllers.Entry;
using ScoreSaber.Features.Live.Compete.UI.ViewControllers.Room.Center;
using ScoreSaber.Features.Live.Compete.UI.ViewControllers.Room.Left;
using ScoreSaber.Features.Live.Compete.UI.ViewControllers.Rooms;
using ScoreSaber.Features.Live.Compete.UI.ViewControllers.Shared;
using ScoreSaber.Features.Live.Ludus.UI;
using ScoreSaber.Features.Live.UI.ViewControllers;
using Zenject;

namespace ScoreSaber.Features.Live {
    internal class LiveFeatureInstaller : Installer {
        public override void InstallBindings() {
            Container.Bind<CompeteModeSelectionViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<TournamentBrowserViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<CompeteRoomListViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<CompeteRoomViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<CompetePlayerListViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<LiveChatFloatingViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<CompeteCodeEntryViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<CompeteLoadingViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<CompeteDirectoryService>().AsSingle();
            Container.Bind<CompeteSongService>().AsSingle();
            Container.Bind<LiveChatSongNavigator>().AsSingle();
            Container.Bind<LiveChatLinkService>().AsSingle();
            Container.Bind<CompeteGameplayLauncher>().AsSingle();
            Container.BindInterfacesAndSelfTo<LudusSessionService>().AsSingle().NonLazy();
            Container.BindInterfacesTo<LiveChatOverlayController>().AsSingle().NonLazy();
            Container.Bind<CompeteFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
        }
    }
}
