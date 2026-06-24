using ScoreSaber.Features.Replays;
using System;
using System.Linq;
using UnityEngine;

namespace ScoreSaber.Features.ScoreSubmission.Services {
    internal class ScoreSubmissionService {
        private readonly ReplayState _replayState;
        private bool _enabled = true;
        private Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults> _standardCallback;
        private Action<MultiplayerLevelScenesTransitionSetupDataSO, MultiplayerResultsData> _multiplayerCallback;

        public ScoreSubmissionService(ReplayState replayState) {
            _replayState = replayState;
            ScoreSubmissionRegistry.Use(this);
        }

        internal void RegisterCallbacks(
            Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults> standardCallback,
            Action<MultiplayerLevelScenesTransitionSetupDataSO, MultiplayerResultsData> multiplayerCallback) {

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

            var transition = Resources.FindObjectsOfTypeAll<StandardLevelScenesTransitionSetupDataSO>().FirstOrDefault();
            if (transition == null) {
                return;
            }

            transition.didFinishEvent -= _standardCallback;
            if (subscribed) {
                transition.didFinishEvent += _standardCallback;
            }
        }

        private void SetMultiplayerSubscribed(bool subscribed) {
            if (_multiplayerCallback == null) {
                return;
            }

            var transition = Resources.FindObjectsOfTypeAll<MultiplayerLevelScenesTransitionSetupDataSO>().FirstOrDefault();
            if (transition == null) {
                return;
            }

            transition.didFinishEvent -= _multiplayerCallback;
            if (subscribed) {
                transition.didFinishEvent += _multiplayerCallback;
            }
        }
    }
}
