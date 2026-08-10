using ScoreSaber.Features.Replays;
using System;

namespace ScoreSaber.Features.ScoreSubmission.Services {
    internal class ScoreSubmissionService {
        private readonly ReplayState _replayState;
        private readonly StandardLevelScenesTransitionSetupData _standardTransition;
        private readonly MultiplayerLevelScenesTransitionSetupData _multiplayerTransition;
        private bool _enabled = true;
        private Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults> _standardCallback;
        private Action<MultiplayerLevelScenesTransitionSetupData, MultiplayerResultsData> _multiplayerCallback;

        public ScoreSubmissionService(ReplayState replayState, StandardLevelScenesTransitionSetupData standardTransition, MultiplayerLevelScenesTransitionSetupData multiplayerTransition) {
            _replayState = replayState;
            _standardTransition = standardTransition;
            _multiplayerTransition = multiplayerTransition;
            ScoreSubmissionRegistry.Use(this);
        }

        internal void RegisterCallbacks(
            Action<StandardLevelScenesTransitionSetupData, LevelCompletionResults> standardCallback,
            Action<MultiplayerLevelScenesTransitionSetupData, MultiplayerResultsData> multiplayerCallback) {

            ClearCallbacks();
            _standardCallback = standardCallback;
            _multiplayerCallback = multiplayerCallback;
            ApplyState();
        }

        internal void ClearCallbacks() {
            SetStandardSubscribed(false);
            SetMultiplayerSubscribed(false);
            _standardCallback = null;
            _multiplayerCallback = null;
        }

        internal void SetEnabled(bool enabled) {
            _enabled = enabled;
            ApplyState();
        }

        internal void SuspendForReplay() {
            SetStandardSubscribed(false);
            SetMultiplayerSubscribed(false);
        }

        internal void ResumeAfterReplay() {
            ApplyState();
        }

        private void ApplyState() {
            bool shouldSubscribe = _enabled && !_replayState.IsPlaybackEnabled;
            SetStandardSubscribed(shouldSubscribe);
            SetMultiplayerSubscribed(shouldSubscribe);
        }

        private void SetStandardSubscribed(bool subscribed) {
            if (_standardCallback == null) {
                return;
            }

            _standardTransition.didFinishEvent -= _standardCallback;
            if (subscribed) {
                _standardTransition.didFinishEvent += _standardCallback;
            }
        }

        private void SetMultiplayerSubscribed(bool subscribed) {
            if (_multiplayerCallback == null) {
                return;
            }

            _multiplayerTransition.didFinishEvent -= _multiplayerCallback;
            if (subscribed) {
                _multiplayerTransition.didFinishEvent += _multiplayerCallback;
            }
        }
    }
}
