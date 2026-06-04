using ScoreSaber.Core.Api;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Players.Services;
using ScoreSaber.Features.Replays;
using ScoreSaber.Features.Leaderboards.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Leaderboards.Services {
    internal class LeaderboardQueryService {

        private readonly IScoreSaberApiClient _apiClient;
        private readonly GameSessionService _gameSessionService;
        private readonly ReplayStorageService _replayStorageService;
        private readonly LeaderboardPlayerScoreCache _playerScoreCache;
        private readonly SettingsService _settings;

        public LeaderboardQueryService(IScoreSaberApiClient apiClient, GameSessionService gameSessionService, ReplayStorageService replayStorageService, LeaderboardPlayerScoreCache playerScoreCache, SettingsService settings) {
            _apiClient = apiClient;
            _gameSessionService = gameSessionService;
            _replayStorageService = replayStorageService;
            _playerScoreCache = playerScoreCache;
            _settings = settings;
            Plugin.Log.Debug("LeaderboardQueryService Setup");
        }

        public async Task<LeaderboardMap> GetLeaderboardData(int maxMultipliedScore, BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, LeaderboardScreenScope scope, int page, bool filterAroundCountry, CancellationToken cancellationToken) {

            LeaderboardQuery query = GetLeaderboardQuery(beatmapKey, scope, page, filterAroundCountry);
            LeaderboardSnapshot snapshot = await _apiClient.GetLeaderboard(query, _gameSessionService.GameSession, cancellationToken);
            _playerScoreCache.Remember(query, GetPlayerId(), snapshot.PlayerScore);

            Plugin.Log.Debug($"Current leaderboard set to: {beatmapKey.levelId}:{beatmapLevel.songName}");
            return new LeaderboardMap(snapshot, beatmapLevel, beatmapKey, maxMultipliedScore, _replayStorageService);
        }

        private LeaderboardQuery GetLeaderboardQuery(BeatmapKey beatmapKey, LeaderboardScreenScope scope, int page, bool filterAroundCountry) {
            var query = new LeaderboardQuery {
                SongHash = ScoreSaberBeatmapKey.GetSongHash(beatmapKey),
                GameMode = $"Solo{beatmapKey.beatmapCharacteristic.serializedName}",
                Difficulty = BeatmapDifficultyMethods.DefaultRating(beatmapKey.difficulty),
                Page = page,
                Limit = 10,
                Scope = filterAroundCountry ? GetLocationScope() : QueryScopeFor(scope),
                HideNoArrows = _settings.Current.hideNAScoresFromLeaderboard
            };

            return query;
        }

        private LeaderboardQueryScope QueryScopeFor(LeaderboardScreenScope scope) => scope switch {
            LeaderboardScreenScope.AroundPlayer => LeaderboardQueryScope.AroundPlayer,
            LeaderboardScreenScope.Friends => LeaderboardQueryScope.Friends,
            LeaderboardScreenScope.Country => GetLocationScope(),
            _ => LeaderboardQueryScope.Global
        };

        private LeaderboardQueryScope GetLocationScope() {
            switch (_settings.Current.locationFilterMode.ToLower()) {
                case "region":
                    return LeaderboardQueryScope.Region;
                case "country":
                    return LeaderboardQueryScope.Country;
                default:
                    Plugin.Log.Error("Invalid location filter mode, falling back to country");
                    return LeaderboardQueryScope.Country;
            }
        }

        private string GetPlayerId() => _gameSessionService.GameSession != null ? _gameSessionService.GameSession.PlayerId : _gameSessionService.LocalPlayerInfo != null ? _gameSessionService.LocalPlayerInfo.playerId : string.Empty;
    }
}
