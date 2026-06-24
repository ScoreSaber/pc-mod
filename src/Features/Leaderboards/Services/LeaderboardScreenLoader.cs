using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.Players.Services;
using System;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeneratedApiException = ScoreSaber.Core.Api.Generated.ApiException;

namespace ScoreSaber.Features.Leaderboards.Services {
    internal class LeaderboardScreenLoader {
        private readonly BeatmapLevelsModel _beatmapLevelsModel;
        private readonly LeaderboardQueryService _leaderboardQueryService;
        private readonly MaxScoreCache _maxScoreCache;
        private readonly GameSessionService _gameSessionService;
        private readonly SettingsService _settings;

        public LeaderboardScreenLoader(
            BeatmapLevelsModel beatmapLevelsModel,
            LeaderboardQueryService leaderboardQueryService,
            MaxScoreCache maxScoreCache,
            GameSessionService gameSessionService,
            SettingsService settings) {
            _beatmapLevelsModel = beatmapLevelsModel;
            _leaderboardQueryService = leaderboardQueryService;
            _maxScoreCache = maxScoreCache;
            _gameSessionService = gameSessionService;
            _settings = settings;
        }

        internal async Task<LeaderboardScreenState> Load(BeatmapKey beatmapKey, LeaderboardScreenScope scope, int page, CancellationToken cancellationToken) {
            if (ScoreSaberBeatmapKey.IsWip(beatmapKey)) {
                return LeaderboardScreenState.Failed(LeaderboardScreenStatus.Error, "ScoreSaber doesn't support WIP levels", false, null, string.Empty, false, page);
            }

            if (!ScoreSaberBeatmapKey.IsSupported(beatmapKey)) {
                return LeaderboardScreenState.Failed(LeaderboardScreenStatus.Error, string.Empty, false, null, string.Empty, false, page);
            }

            BeatmapLevel beatmapLevel = _beatmapLevelsModel.GetBeatmapLevel(beatmapKey.levelId);
            if (beatmapLevel == null) {
                return LeaderboardScreenState.Failed(LeaderboardScreenStatus.Error, "Failed to load beatmap", false, null, string.Empty, false, page);
            }

            if (!_gameSessionService.HasAuthenticatedSession) {
                _gameSessionService.EnsureAuthenticated();
                if (_gameSessionService.Status != GameSessionService.LoginStatus.Error) {
                    return LeaderboardScreenState.Loading(page);
                }

                return LeaderboardScreenState.Failed(LeaderboardScreenStatus.Error, "Authentication failed. Restart Beat Saber and try again.", false, null, string.Empty, false, page);
            }

            bool filterAroundCountry = ShouldFilterAroundCountry(scope);
            int maxScore = await _maxScoreCache.GetMaxScore(beatmapLevel, beatmapKey);
            LeaderboardMap leaderboard;
            try {
                leaderboard = await _leaderboardQueryService.GetLeaderboardData(maxScore, beatmapLevel, beatmapKey, scope, page, filterAroundCountry, cancellationToken);
            } catch (GeneratedApiException ex) when (IsLeaderboardNotFoundResponse(ex)) {
                return LeaderboardScreenState.Failed(
                    LeaderboardScreenStatus.NoLeaderboard,
                    "Play this level to create a ScoreSaber leaderboard",
                    true,
                    null,
                    "Unranked",
                    false,
                    page);
            } catch (GeneratedApiException ex) when (IsNoPlayerScoreResponse(ex)) {
                return LeaderboardScreenState.Failed(LeaderboardScreenStatus.NoPlayerScore, GetApiMessage(ex), true, null, string.Empty, false, page);
            } catch (GeneratedApiException ex) {
                return LeaderboardScreenState.Failed(LeaderboardScreenStatus.Error, GetApiMessage(ex), true, null, string.Empty, false, page);
            }

            return CreateLoadedState(leaderboard, scope, filterAroundCountry, page);
        }

        private LeaderboardScreenState CreateLoadedState(LeaderboardMap leaderboard, LeaderboardScreenScope scope, bool filterAroundCountry, int page) {
            int playerScoreIndex = GetPlayerScoreIndex(leaderboard);
            bool canPage = CanPageScope(scope, filterAroundCountry);
            string rankedStatus = GetRankedStatus(leaderboard.LeaderboardInfo.Leaderboard);
            if (scope == LeaderboardScreenScope.AroundPlayer && playerScoreIndex == -1 && !filterAroundCountry) {
                return LeaderboardScreenState.Failed(LeaderboardScreenStatus.NoPlayerScore, "You haven't set a score on this leaderboard", true, leaderboard, rankedStatus, canPage, page);
            }

            if (leaderboard.Scores.Length == 0) {
                string emptyText = page > 1 ? "No scores on this page" : "No scores on this leaderboard, be the first!";
                return LeaderboardScreenState.Failed(LeaderboardScreenStatus.Empty, emptyText, true, leaderboard, rankedStatus, canPage, page);
            }

            return LeaderboardScreenState.Loaded(leaderboard, playerScoreIndex, rankedStatus, canPage, page);
        }

        private bool ShouldFilterAroundCountry(LeaderboardScreenScope scope) => scope == LeaderboardScreenScope.Country && _settings.Current.enableCountryLeaderboards;

        private static bool CanPageScope(LeaderboardScreenScope scope, bool filterAroundCountry) => scope != LeaderboardScreenScope.AroundPlayer || filterAroundCountry;

        private static bool IsNoPlayerScoreResponse(GeneratedApiException ex) => ex.StatusCode == 404 && GetApiMessage(ex).IndexOf("hasn't set a score", StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool IsLeaderboardNotFoundResponse(GeneratedApiException ex) =>
            ex.StatusCode == 404 && GetApiMessage(ex).IndexOf("Leaderboard not found", StringComparison.OrdinalIgnoreCase) >= 0;

        private static string GetApiMessage(GeneratedApiException ex) {
            if (!string.IsNullOrEmpty(ex.Response)) {
                try {
                    JObject body = JObject.Parse(ex.Response);
                    string message = body.Value<string>("message") ?? body.Value<string>("errorMessage") ?? body.Value<string>("error");
                    if (!string.IsNullOrEmpty(message)) {
                        return message;
                    }
                } catch (Exception) {
                }
            }

            return string.IsNullOrEmpty(ex.Message) ? "Failed to load leaderboard" : ex.Message;
        }

        private int GetPlayerScoreIndex(LeaderboardMap leaderboard) => Array.FindIndex(leaderboard.Scores, score => score.Score.Player.Id == _gameSessionService.LocalPlayerInfo.playerId);

        private static string GetRankedStatus(LeaderboardDetails leaderboardInfo) => leaderboardInfo.Status switch {
            LeaderboardStatus.Ranked => leaderboardInfo.PositiveModifiers ? "Ranked (DA = +0.02, GN +0.04)" : "Ranked (modifiers disabled)",
            LeaderboardStatus.Qualified => "Qualified",
            LeaderboardStatus.Loved => "Loved",
            _ => "Unranked"
        };
    }
}
