using ScoreSaber.Features.ScoreSubmission.Services;
using Zenject;

namespace ScoreSaber.Features.ScoreSubmission {
    internal class ScoreSubmissionFeatureInstaller : Installer {
        public override void InstallBindings() {
            Container.Bind<ScoreUploadPayloadBuilder>().AsSingle();
            Container.Bind<ScoreSubmissionWorkflow>().AsSingle();
            Container.Bind<ScoreSubmissionService>().AsSingle();
            Container.BindInterfacesAndSelfTo<ScoreSubmissionController>().AsSingle().NonLazy();
        }
    }
}
