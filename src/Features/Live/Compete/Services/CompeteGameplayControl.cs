using SiraUtil.Tools.SongControl;
using System;
using Zenject;

namespace ScoreSaber.Features.Live.Compete.Services {
    internal class CompeteGameplayControl {
        private readonly CompeteGameplayState _gameplayState;
        private ISongControl _songControl;

        internal CompeteGameplayControl(CompeteGameplayState gameplayState) {
            _gameplayState = gameplayState;
        }

        internal void Register(ISongControl songControl) {
            _songControl = songControl;
        }

        internal void Unregister(ISongControl songControl) {
            if (ReferenceEquals(_songControl, songControl)) {
                _songControl = null;
            }
        }

        internal bool TryStopMap(string matchId) {
            if (!_gameplayState.IsLiveGameplayActive || _songControl == null) {
                return false;
            }

            if (!string.IsNullOrEmpty(matchId) && !string.Equals(matchId, _gameplayState.MatchId, StringComparison.Ordinal)) {
                return false;
            }

            _gameplayState.MarkHostStopRequested();
            _songControl.Quit();
            return true;
        }
    }

    internal class CompeteGameplayControlBinder : IInitializable, IDisposable {
        private readonly CompeteGameplayControl _gameplayControl;
        private readonly ISongControl _songControl;

        internal CompeteGameplayControlBinder(CompeteGameplayControl gameplayControl, ISongControl songControl) {
            _gameplayControl = gameplayControl;
            _songControl = songControl;
        }

        public void Initialize() {
            _gameplayControl.Register(_songControl);
        }

        public void Dispose() {
            _gameplayControl.Unregister(_songControl);
        }
    }
}
