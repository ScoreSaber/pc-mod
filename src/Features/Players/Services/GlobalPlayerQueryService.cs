using ScoreSaber.Core.Api;
using ScoreSaber.Core.Api.Paging;
using ScoreSaber.Features.Players.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Players.Services {

    internal class GlobalPlayerQueryService {

        private readonly IScoreSaberApiClient _apiClient;
        private readonly GameSessionService _gameSessionService;

        public GlobalPlayerQueryService(IScoreSaberApiClient apiClient, GameSessionService gameSessionService) {
            _apiClient = apiClient;
            _gameSessionService = gameSessionService;
            Plugin.Log.Debug("GlobalPlayerQueryService Setup");
        }

        public async Task<GlobalPlayerPage> GetPlayerPage(GlobalPlayerScope scope, int page, CancellationToken cancellationToken) {

            PlayerListQuery query = BuildQuery(scope, page);
            PagedResult<PlayerSummary> players = await _apiClient.GetPlayers(query, _gameSessionService.GameSession, cancellationToken);
            return new GlobalPlayerPage {
                Scope = scope,
                Page = page,
                Players = players.Items.ToArray()
            };
        }

        private PlayerListQuery BuildQuery(GlobalPlayerScope scope, int page) {
            return new PlayerListQuery {
                Page = page,
                Limit = scope == GlobalPlayerScope.AroundPlayer ? 6 : 5,
                Scope = QueryScopeFor(scope)
            };
        }

        private static PlayerQueryScope QueryScopeFor(GlobalPlayerScope scope) => scope switch {
            GlobalPlayerScope.AroundPlayer => PlayerQueryScope.AroundPlayer,
            GlobalPlayerScope.Friends => PlayerQueryScope.Friends,
            GlobalPlayerScope.Country => PlayerQueryScope.Country,
            GlobalPlayerScope.Region => PlayerQueryScope.Region,
            _ => PlayerQueryScope.Global
        };

    }
}
