using ScoreSaber.Core.Api;
using ScoreSaber.Features.Players.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Players.Services {
    internal class PlayerProfileService {
        private readonly IScoreSaberApiClient _apiClient;

        public PlayerProfileService(IScoreSaberApiClient apiClient) {
            _apiClient = apiClient;
        }

        public async Task<PlayerProfile> GetPlayerInfo(string playerId, bool full) {
            PlayerProfile player = await _apiClient.GetPlayerProfile(playerId, full, null, CancellationToken.None);
            if (full) {
                player.GlobalHistory = await _apiClient.GetGlobalPlayerHistory(playerId, CancellationToken.None);
            }
            return player;
        }
    }
}
