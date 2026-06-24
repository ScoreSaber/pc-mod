using Zenject;
#if BEAT_SABER_1_29_0
using System.Collections;
using UnityEngine;
#endif

namespace ScoreSaber.Core.Compat {
    internal class CompatInstaller : Installer {
        public override void InstallBindings() {
#if BEAT_SABER_1_29_0
            Container.Bind<EnvironmentsListModel>().AsSingle();
            Container.Bind<ICoroutineStarter>().To<CompatCoroutineStarter>().AsSingle();
#endif
        }
    }

#if BEAT_SABER_1_29_0
    // unity 2019 refuses to `AddComponent` mod `MonoBehaviours` here (coUld nOt be insTanTiAted) so lean on the games runner
    internal class CompatCoroutineStarter : ICoroutineStarter {
        public Coroutine StartCoroutine(IEnumerator routine) => SharedCoroutineStarter.instance.StartCoroutine(routine);
    }
#endif
}
