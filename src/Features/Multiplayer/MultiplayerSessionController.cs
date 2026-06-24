using ScoreSaber.Features.Players.Services;
using ScoreSaber.Features.Leaderboards.UI;
using System;
using Zenject;

namespace ScoreSaber.Features.Multiplayer {
    internal class MultiplayerSessionController : IInitializable, IDisposable {

        private readonly GameSessionService _gameSessionService;
        private readonly GameServerLobbyFlowCoordinator _gameServerLobbyFlowCoordinator;
        private readonly LeaderboardModalFlow _modalFlow;

        public MultiplayerSessionController(GameSessionService gameSessionService, GameServerLobbyFlowCoordinator gameServerLobbyFlowCoordinator, LeaderboardModalFlow modalFlow) {
            _gameSessionService = gameSessionService;
            _gameServerLobbyFlowCoordinator = gameServerLobbyFlowCoordinator;
            _modalFlow = modalFlow;
        }

        public void Initialize() {

            _gameServerLobbyFlowCoordinator.didSetupEvent += GameServerLobbyFlowCoordinator_didSetupEvent;
            _gameServerLobbyFlowCoordinator.didFinishEvent += GameServerLobbyFlowCoordinator_didFinishEvent;
        }

        private void GameServerLobbyFlowCoordinator_didSetupEvent() {

            _gameSessionService.EnsureAuthenticated();
            _modalFlow.AllowReplayWatching(false);
        }

        private void GameServerLobbyFlowCoordinator_didFinishEvent() => _modalFlow.AllowReplayWatching(true);

        public void Dispose() {

            _gameServerLobbyFlowCoordinator.didSetupEvent -= GameServerLobbyFlowCoordinator_didSetupEvent;
            _gameServerLobbyFlowCoordinator.didFinishEvent -= GameServerLobbyFlowCoordinator_didFinishEvent;
        }
    }
}
