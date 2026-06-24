using Zenject;
using ScoreSaber.Core.Compat;
using ScoreSaber.Features.Live.Compete.Services;
using ScoreSaber.Features.Replays.Recorders;

namespace ScoreSaber.Features.Replays.Installers {
    internal class RecordInstaller : Installer {
        private readonly ReplayState _replayState;

        public RecordInstaller(ReplayState replayState) {
            _replayState = replayState;
        }

        public override void InstallBindings() {

            if (!_replayState.IsPlaybackEnabled) {
                Plugin.Log.Debug("Installing replay recorders");
                Container.Bind<RoomSettings>().AsSingle();
                Container.BindInterfacesAndSelfTo<Recorder>().AsSingle();
                Container.BindInterfacesAndSelfTo<MetadataRecorder>().AsSingle();
                Container.BindInterfacesAndSelfTo<HeightEventRecorder>().AsSingle();
                Container.BindInterfacesAndSelfTo<NoteEventRecorder>().AsSingle();
                Container.BindInterfacesAndSelfTo<PoseRecorder>().AsSingle();
                Container.BindInterfacesAndSelfTo<ScoreEventRecorder>().AsSingle();
                Container.BindInterfacesAndSelfTo<EnergyEventRecorder>().AsSingle();
                Container.BindInterfacesAndSelfTo<PauseEventRecorder>().AsSingle();
                Container.BindInterfacesAndSelfTo<WallEventRecorder>().AsSingle();
                Container.Bind<HsvConfigRecorder>().AsSingle();
                Container.BindInterfacesTo<CompetePauseGuard>().AsSingle();
                Plugin.Log.Debug("Replay recorders installed");
            }
        }
    }
}
