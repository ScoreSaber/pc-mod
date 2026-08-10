using ScoreSaber.Features.Replays.Format;
using ScoreSaber.Features.Replays.Playback;
using System;
using Zenject;

namespace ScoreSaber.Features.Replays.UI {
    internal class ImberSpecsReporter : IInitializable, IDisposable {
        private readonly PosePlayer _posePlayer;
        private readonly SaberManager _saberManager;
        public event Action<int, float, float> DidReport;

        public ImberSpecsReporter(PosePlayer posePlayer, SaberManager saberManager) {

            _posePlayer = posePlayer;
            _saberManager = saberManager;
        }

        public void Initialize() {

            _posePlayer.DidUpdatePose += PosePlayer_DidUpdatePose;
        }

        private void PosePlayer_DidUpdatePose(VRPoseGroup pose) {

            DidReport?.Invoke(pose.FPS, _saberManager.leftSaber.GetMovementDataForLogic().bladeSpeed, _saberManager.rightSaber.GetMovementDataForLogic().bladeSpeed);
        }

        public void Dispose() {

            _posePlayer.DidUpdatePose -= PosePlayer_DidUpdatePose;
        }
    }
}
