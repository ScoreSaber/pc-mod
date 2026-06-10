using HMUI;
using ScoreSaber.Core.Compat;
using ScoreSaber.Features.MainMenu.MainFlow;
using ScoreSaber.Features.MainMenu.Settings;

namespace ScoreSaber.Features.MainMenu {
    internal class ScoreSaberMenuNavigator {
        private readonly MainFlowCoordinator _mainFlowCoordinator;
        private readonly ScoreSaberFlowCoordinator _scoreSaberFlowCoordinator;
        private readonly ScoreSaberSettingsFlowCoordinator _settingsFlowCoordinator;

        public ScoreSaberMenuNavigator(MainFlowCoordinator mainFlowCoordinator, ScoreSaberFlowCoordinator scoreSaberFlowCoordinator, ScoreSaberSettingsFlowCoordinator settingsFlowCoordinator) {
            _mainFlowCoordinator = mainFlowCoordinator;
            _scoreSaberFlowCoordinator = scoreSaberFlowCoordinator;
            _settingsFlowCoordinator = settingsFlowCoordinator;
        }

        internal void ShowMain() {
            Present(_scoreSaberFlowCoordinator);
        }

        internal void ShowSettings() {
            Present(_settingsFlowCoordinator);
        }

        private void Present(IScoreSaberFlowCoordinator flowCoordinator) {
            FlowCoordinator activeFlow = DeepestChildFlowCoordinator(_mainFlowCoordinator);
            activeFlow.Present(flowCoordinator.FlowCoordinator);
            flowCoordinator.SetPresentingFlowCoordinator(activeFlow);
        }

        private FlowCoordinator DeepestChildFlowCoordinator(FlowCoordinator root) {
            FlowCoordinator flow = root.childFlowCoordinator;
            if (flow == null) {
                return root;
            }

            if (flow.childFlowCoordinator == null || flow.childFlowCoordinator == flow) {
                return flow;
            }

            return DeepestChildFlowCoordinator(flow);
        }
    }

    internal interface IScoreSaberFlowCoordinator {
        FlowCoordinator FlowCoordinator { get; }
        void SetPresentingFlowCoordinator(FlowCoordinator flowCoordinator);
    }
}
