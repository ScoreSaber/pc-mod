using ScoreSaber.Features.Replays.UI;
using Zenject;

namespace ScoreSaber.Features.Replays.Installers {
    internal class ImberInstaller : Installer {
        private readonly ReplayState _replayState;

        public ImberInstaller(ReplayState replayState) {
            _replayState = replayState;
        }

        public override void InstallBindings() {

            if (_replayState.IsPlaybackEnabled && !_replayState.IsLegacyReplay) {
                Container.BindInterfacesTo<ImberManager>().AsSingle();
                Container.BindInterfacesAndSelfTo<ImberScrubber>().AsSingle();
                Container.BindInterfacesAndSelfTo<ImberSpecsReporter>().AsSingle();
                Container.BindInterfacesAndSelfTo<ImberUIPositionController>().AsSingle();
                Container.Bind<MainImberPanelView>().FromNewComponentAsViewController().AsSingle();
                Container.Bind(typeof(ITickable), typeof(SpectateAreaController)).To<SpectateAreaController>().AsSingle();
            }
        }
    }
}
