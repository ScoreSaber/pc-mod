using ScoreSaber.Features.Replays.UI;
using SiraUtil.Tools.FPFC;
using Zenject;

namespace ScoreSaber.Features.Replays.Installers {
    internal class ImberInstaller : Installer {
        private readonly ReplayState _replayState;
        private readonly IFPFCSettings _fpfcSettings;

        public ImberInstaller(ReplayState replayState, IFPFCSettings fpfcSettings) {
            _replayState = replayState;
            _fpfcSettings = fpfcSettings;
        }

        public override void InstallBindings() {

            if (_replayState.IsPlaybackEnabled && !_replayState.IsLegacyReplay && !_fpfcSettings.Enabled) {
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
