using ScoreSaber.Core.Api;

namespace ScoreSaber.Features.Players.Domain {
    internal enum GameSessionStatus {
        None,
        InProgress,
        Success,
        Error
    }

    internal class GameAuthenticationResult {
        internal GameSessionStatus Status { get; set; }
        internal GameSession Session { get; set; }
        internal string Message { get; set; } = string.Empty;
        internal ScoreSaberApiError Error { get; set; }

        internal static GameAuthenticationResult Success(GameSession session) {
            return new GameAuthenticationResult {
                Status = GameSessionStatus.Success,
                Session = session,
                Message = "Authenticated"
            };
        }

        internal static GameAuthenticationResult Failure(string message, ScoreSaberApiError error) {
            return new GameAuthenticationResult {
                Status = GameSessionStatus.Error,
                Message = message,
                Error = error
            };
        }
    }
}
