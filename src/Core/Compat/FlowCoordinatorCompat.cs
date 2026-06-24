using HMUI;
using IPA.Utilities;
using System;
using System.Threading.Tasks;

namespace ScoreSaber.Core.Compat {
    internal static class FlowCoordinatorCompat {
        internal static void Present(this FlowCoordinator activeFlow, FlowCoordinator flowCoordinator) {
#if BEAT_SABER_1_29_0
            activeFlow.InvokeMethod<object, FlowCoordinator>("PresentFlowCoordinator",
                flowCoordinator, null, ViewController.AnimationDirection.Horizontal, false, false);
#else
            activeFlow.PresentFlowCoordinator(flowCoordinator);
#endif
        }

        internal static void Dismiss(this FlowCoordinator activeFlow, FlowCoordinator flowCoordinator) {
#if BEAT_SABER_1_29_0
            activeFlow.InvokeMethod<object, FlowCoordinator>("DismissFlowCoordinator",
                flowCoordinator, ViewController.AnimationDirection.Horizontal, null, false);
#else
            activeFlow.DismissFlowCoordinator(flowCoordinator);
#endif
        }

        internal static Task DismissView(this FlowCoordinator flowCoordinator, ViewController viewController, Action finishedCallback = null) {
#if BEAT_SABER_1_29_0
            var completion = new TaskCompletionSource<object>();
            Action callback = () => {
                finishedCallback?.Invoke();
                completion.TrySetResult(null);
            };
            flowCoordinator.InvokeMethod<object, FlowCoordinator>("DismissViewController",
                viewController, ViewController.AnimationDirection.Horizontal, callback, false);
            return completion.Task;
#else
            return flowCoordinator.InvokeMethod<Task, FlowCoordinator>("DismissViewController",
                viewController, ViewController.AnimationDirection.Horizontal, finishedCallback, false);
#endif
        }
    }
}
