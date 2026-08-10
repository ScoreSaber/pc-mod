namespace ScoreSaber.Core {
    internal partial class MainInstaller {
        partial void InstallGameBindings() {
            Container.Bind<EnvironmentsListModel>().AsSingle();
            Container.Bind<ICoroutineStarter>().To<SharedCoroutineStarterAdapter>().AsSingle();
        }
    }
}
