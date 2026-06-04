using ScoreSaber.Features.MainMenu.Settings.ViewControllers;
using ScoreSaber.Features.MainMenu.MainFlow;
using ScoreSaber.Features.MainMenu.MainFlow.FAQ;
using ScoreSaber.Features.MainMenu.MainFlow.GlobalLeaderboard;
using ScoreSaber.Features.MainMenu.MainFlow.Teams.UI;
using ScoreSaber.Features.MainMenu.Settings;
using Zenject;

namespace ScoreSaber.Features.MainMenu {
    internal class MainMenuFeatureInstaller : Installer {
        public override void InstallBindings() {
            Container.Bind<TeamViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<FAQViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<GlobalViewController>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<GlobalLeaderboardHost>().AsSingle();
            Container.Bind<ScoreSaberMenuNavigator>().AsSingle();
            Container.Bind<MainSettingsViewController>().FromNewComponentAsViewController().AsSingle();

            Container.BindInterfacesAndSelfTo<ScoreSaberFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
            Container.BindInterfacesAndSelfTo<ScoreSaberSettingsFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
        }
    }
}
