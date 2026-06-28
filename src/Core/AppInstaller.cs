using ScoreSaber.Core.Presentation;
using ScoreSaber.Core.Timing;
using ScoreSaber.Features.Live.Compete.Services;
using ScoreSaber.Features.Live.Replay;
using ScoreSaber.Features.Replays;
using Zenject;

namespace ScoreSaber.Core {
    internal class AppInstaller : Installer {

        public override void InstallBindings() {
            Container.BindInstance(new ScoreSaberRuntimeInfo(Plugin.Instance.LibVersion, IPA.Utilities.UnityGame.GameVersion.SemverValue, IPA.Utilities.UnityGame.GameVersion.ToString())).AsSingle();
            Container.BindInstance(Plugin.SettingsService).AsSingle();
            Container.BindInstance(Plugin.Instance.HttpInstance).AsSingle();
            Container.BindInstance(Plugin.Instance.ReplayState).AsSingle();

            Container.Bind<ScoreSaberUIMaterials>().AsSingle();
            Container.BindInterfacesAndSelfTo<ScoreSaberClock>().AsSingle().NonLazy();
            Container.Bind<ReplayFileCodec>().AsSingle();
            Container.Bind<ReplayService>().AsSingle().NonLazy();
            Container.Bind<CompeteGameplayState>().AsSingle();
            Container.Bind<CompeteGameplayControl>().AsSingle();
            Container.BindInterfacesAndSelfTo<LiveReplayStreamingService>().AsSingle().NonLazy();
        }
    }
}
