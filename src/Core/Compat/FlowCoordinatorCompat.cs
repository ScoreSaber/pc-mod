using HMUI;
#if BEAT_SABER_1_29_0
using IPA.Utilities;
#endif

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
    }
}
