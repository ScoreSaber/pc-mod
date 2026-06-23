using System;
using Zenject;

namespace ScoreSaber.Features.Live.Compete.Services {
    internal class CompetePauseGuard : IInitializable, IDisposable {
        private readonly PauseController _pauseController;
        private readonly CompeteGameplayState _gameplayState;

        internal CompetePauseGuard(PauseController pauseController, CompeteGameplayState gameplayState) {
            _pauseController = pauseController;
            _gameplayState = gameplayState;
        }

        public void Initialize() {
            _pauseController.canPauseEvent += CanPause;
        }

        public void Dispose() {
            _pauseController.canPauseEvent -= CanPause;
        }

        private void CanPause(Action<bool> canPause) {
            if (_gameplayState.IsLiveGameplayActive) {
                canPause?.Invoke(false);
            }
        }
    }
}
