using Legato.XR.Headset;
using Legato.XR.InputFocus;
using System;
using Zenject;

namespace ScoreSaber.Features.Replays {
    internal sealed class ReplayXrEventHandler : IInitializable, IDisposable {
        private readonly ReplayState _replayState;

        public ReplayXrEventHandler(ReplayState replayState) {
            _replayState = replayState;
        }

        public void Initialize() {
            HeadsetEvents.HeadsetUnmounted += HandleHeadsetUnmounted;
            InputFocusEvents.InputFocusEvaluated += HandleInputFocusEvaluated;
        }

        public void Dispose() {
            HeadsetEvents.HeadsetUnmounted -= HandleHeadsetUnmounted;
            InputFocusEvents.InputFocusEvaluated -= HandleInputFocusEvaluated;
        }

        private void HandleHeadsetUnmounted(object sender, HeadsetUnmountedEventArgs eventArgs) {
            if (_replayState.IsPlaybackEnabled) {
                eventArgs.SuppressGameHandling = true;
            }
        }

        private void HandleInputFocusEvaluated(ref bool hasInputFocus) {
            if (_replayState.IsPlaybackEnabled) {
                hasInputFocus = true;
            }
        }
    }
}
