using ScoreSaber.Core.Api.Paging;
using ScoreSaber.Features.Players.Domain;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.ScoreSubmission.Domain;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Core.Api {

    internal interface IScoreSaberApiClient {
        Task<GameAuthenticationResult> AuthenticateGame(GameAuthenticationRequest request, CancellationToken cancellationToken);
        Task<ScoreUploadResult> UploadScore(GameSession session, string uploadData, string uploadVersionHash, byte[] replay, CancellationToken cancellationToken);
        Task<LeaderboardSnapshot> GetLeaderboard(LeaderboardQuery query, GameSession session, CancellationToken cancellationToken);
        Task<LeaderboardDetails> GetLeaderboardDetails(LeaderboardQuery query, CancellationToken cancellationToken);
        Task<PagedResult<PlayerSummary>> GetPlayers(PlayerListQuery query, GameSession session, CancellationToken cancellationToken);
        Task<PlayerProfile> GetPlayerProfile(string playerId, bool full, int? realmId, CancellationToken cancellationToken);
        Task<List<PlayerHistoryPoint>> GetGlobalPlayerHistory(string playerId, CancellationToken cancellationToken);
        Task<byte[]> DownloadReplay(int scoreId, CancellationToken cancellationToken);
    }
}
