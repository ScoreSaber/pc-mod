using ScoreSaber.Features.Live.Compete.Services;
using Zenject;

namespace ScoreSaber.Features.Live {
    internal class LiveGameplayInstaller : Installer {
        public override void InstallBindings() {
            Container.BindInterfacesTo<CompeteGameplayControlBinder>().AsSingle();
        }
    }
}
