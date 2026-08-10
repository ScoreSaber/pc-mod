using ScoreSaber.Features.Replays.HarmonyPatches;
using ScoreSaber.Features.Replays.Legacy;
using ScoreSaber.Features.Replays.Legacy.UI;
using ScoreSaber.Features.Replays.Playback;
using ScoreSaber.Features.Replays.UI;
using ScoreSaber.Patches;
using SiraUtil.Affinity;
using Zenject;

namespace ScoreSaber.Features.Replays.Installers {

    internal class PlaybackInstaller : Installer {
        private readonly GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;
        private readonly ReplayState _replayState;

        public PlaybackInstaller(GameplayCoreSceneSetupData gameplayCoreSceneSetupData, ReplayState replayState) {

            _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
            _replayState = replayState;
        }

        public override void InstallBindings() {

            if (_replayState.IsPlaybackEnabled) {
                Container.Bind<RoomSettings>().AsSingle();
                Container.BindInstance(new object()).WithId("ScoreSaberReplay").AsCached();
                if (!_replayState.IsLegacyReplay) {
                    Container.BindInstance(_replayState.LoadedReplayFile).AsSingle();
                    Container.BindInterfacesAndSelfTo<PosePlayer>().AsSingle();
                    Container.BindInterfacesTo<NotePlayer>().AsSingle();
                    Container.BindInterfacesTo<EnergyPlayer>().AsSingle(); // needs to be injected before the ScorePlayer to make the TimeUpdate methods run in the correct order
                    Container.BindInterfacesTo<ScorePlayer>().AsSingle();
                    Container.BindInterfacesTo<ComboPlayer>().AsSingle();
                    Container.BindInterfacesTo<MultiplierPlayer>().AsSingle();
                    Container.BindInterfacesTo<ReplayMovementDataEventHandler>().AsSingle();
                    if (_gameplayCoreSceneSetupData.playerSpecificSettings.automaticPlayerHeight)
                        Container.BindInterfacesTo<HeightPlayer>().AsSingle();
                    Container.BindInterfacesAndSelfTo<ReplayTimeSyncController>().AsSingle();
                    Container.Bind<NonVRReplayUI>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
                    Container.Bind<IAffinity>().To<CancelScoreControllerBufferFinisher>().AsSingle();
                    Container.Bind<IAffinity>().To<CancelSaberCuttingPatch>().AsSingle();
                } else {
                    Container.Bind<IAffinity>().To<CancelScoreControllerBufferFinisher>().AsSingle();
                    Container.BindInstance(_replayState.LoadedLegacyKeyframes).AsSingle();
                    Container.BindInterfacesAndSelfTo<LegacyReplayPlayer>().AsSingle();
                    Container.BindInterfacesTo<LegacyReplayPatches>().AsSingle();
                }
                Container.Bind<GameReplayUI>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            }
        }
    }
}
