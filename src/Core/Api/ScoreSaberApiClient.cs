using Newtonsoft.Json;
using ScoreSaber.Core.Api.UploadTrust;
using ScoreSaber.Core.Api.Generated;
using ScoreSaber.Core.Api.Paging;
using ScoreSaber.Features.Players.Domain;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.ScoreSubmission.Domain;
using SiraUtil.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using GeneratedApiException = ScoreSaber.Core.Api.Generated.ApiException;

namespace ScoreSaber.Core.Api {

    internal class ScoreSaberApiClient : IScoreSaberApiClient {
        private readonly HttpClient _httpClient;
        private readonly Http _http;
        private readonly UploadTrustClient _uploadTrustClient;

        public ScoreSaberApiClient(IHttpService httpService, Http http, ScoreSaberRuntimeInfo runtimeInfo) {
            _httpClient = new HttpClient(new SiraUtilHttpMessageHandler(httpService));
            _http = http;
            _uploadTrustClient = new UploadTrustClient(runtimeInfo);
        }

        public async Task<GameAuthenticationResult> AuthenticateGame(GameAuthenticationRequest request, CancellationToken cancellationToken) {
            try {
                var client = CreateClient();
                var generatedRequest = new GameAuthenticateRequest {
                    At = request.AuthType,
                    PlayerId = request.PlayerId,
                    Nonce = request.Nonce,
                    Friends = request.FriendIds,
                    Name = request.PlayerName
                };
                _uploadTrustClient.ApplyAuthMetadata(generatedRequest);

                GameAuthenticateResponse response = await client.AuthenticateGameAsync(generatedRequest, cancellationToken);

                if (string.IsNullOrEmpty(response.SessionId) || string.IsNullOrEmpty(response.Key)) {
                    return GameAuthenticationResult.Failure(
                        "ScoreSaber authentication did not return a game session",
                        ScoreSaberApiError.FromMessage("Missing sessionId or key"));
                }

                var session = new GameSession {
                    PlayerId = request.PlayerId,
                    PlayerName = request.PlayerName,
                    SessionId = response.SessionId,
                    SessionKey = response.Key,
                    UploadTrust = _uploadTrustClient.CreateSession(response),
                    Player = new PlayerSummary {
                        Id = request.PlayerId,
                        Name = request.PlayerName
                    }
                };

                return GameAuthenticationResult.Success(session);
            } catch (GeneratedApiException ex) {
                return GameAuthenticationResult.Failure(ex.Message, MapError(ex));
            } catch (Exception ex) {
                return GameAuthenticationResult.Failure(ex.Message, ScoreSaberApiError.FromMessage(ex.Message));
            }
        }

        public async Task<ScoreUploadResult> UploadScore(GameSession session, string uploadData, string uploadVersionHash, byte[] replay, CancellationToken cancellationToken) {
            if (session == null || !session.IsAuthenticated) {
                return ErrorUploadResult("ScoreSaber is not authenticated", "Missing game session");
            }

            if (replay == null) {
                return ErrorUploadResult("Failed to serialize replay", "Replay payload was null");
            }

            Guid sessionId;
            if (!Guid.TryParse(session.SessionId, out sessionId)) {
                return ErrorUploadResult("ScoreSaber game session is invalid", "x-session-id was not a GUID");
            }

            if (session.UploadTrust == null || !session.UploadTrust.IsUploadProtocolV2) {
                return ErrorUploadResult("ScoreSaber upload trust is unavailable", "Current game session does not include v2 upload trust");
            }

            try {
                var form = new WWWForm();
                form.AddField("data", uploadData);
                form.AddBinaryData("zr", replay, "replay.dat", "application/octet-stream");

                Dictionary<string, string> headers = UploadTrustHeaderBuilder.BuildUploadHeaders(
                    sessionId.ToString(),
                    session.SessionKey,
                    session.PlayerId,
                    uploadVersionHash,
                    uploadData,
                    replay,
                    session.UploadTrust);

                Plugin.Log.Debug("ScoreSaber API POST /api/v2/game/upload using Unity multipart");
                string responseBody = await _http.PostAsync(
                    "/v2/game/upload",
                    form,
                    headers);

                GameUploadResponse response = JsonConvert.DeserializeObject<GameUploadResponse>(responseBody);
                bool success = response != null && response.Success;
                return new ScoreUploadResult {
                    Status = success ? ScoreUploadStatus.Success : ScoreUploadStatus.Error,
                    Success = success,
                    Message = success ? "Score uploaded!" : "Failed to upload score"
                };
            } catch (HttpErrorException ex) {
                string message = GetHttpErrorMessage(ex);
                return new ScoreUploadResult {
                    Status = ScoreUploadStatus.Error,
                    Success = false,
                    Message = message,
                    Error = new ScoreSaberApiError {
                        StatusCode = ex.statusCode,
                        NetworkError = ex.isNetworkError,
                        Message = message,
                        RawBody = ex.errorBody ?? string.Empty
                    }
                };
            } catch (Exception ex) {
                return new ScoreUploadResult {
                    Status = ScoreUploadStatus.Error,
                    Success = false,
                    Message = ex.Message,
                    Error = ScoreSaberApiError.FromMessage(ex.Message)
                };
            }
        }

        public async Task<LeaderboardSnapshot> GetLeaderboard(LeaderboardQuery query, GameSession session, CancellationToken cancellationToken) {
            Task<LeaderboardDetails> leaderboardTask = GetLeaderboardDetails(query, cancellationToken);
            Task<LeaderboardScoresSnapshot> scoresTask = GetLeaderboardScores(query, session, cancellationToken);

            LeaderboardScoresSnapshot scores;
            try {
                scores = await scoresTask;
            } catch (GeneratedApiException ex) when (IsNoPlayerScoreResponse(query, ex)) {
                scores = new LeaderboardScoresSnapshot();
            }

            return new LeaderboardSnapshot {
                Leaderboard = await leaderboardTask,
                Scores = scores.Scores,
                PlayerScore = scores.PlayerScore
            };
        }

        private static string GetHttpErrorMessage(HttpErrorException exception) {
            if (exception.scoreSaberError != null && !string.IsNullOrEmpty(exception.scoreSaberError.ErrorMessage)) {
                return exception.scoreSaberError.ErrorMessage;
            }

            if (exception.scoreSaberError != null && !string.IsNullOrEmpty(exception.scoreSaberError.Message)) {
                return exception.scoreSaberError.Message;
            }

            return "ScoreSaber upload failed";
        }

        private static bool IsNoPlayerScoreResponse(LeaderboardQuery query, GeneratedApiException exception) {
            if (query.Scope != LeaderboardQueryScope.AroundPlayer || exception.StatusCode != 404) {
                return false;
            }

            string errorText = $"{exception.Response} {exception.Message}";
            return errorText.IndexOf("hasn't set a score", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public async Task<LeaderboardDetails> GetLeaderboardDetails(LeaderboardQuery query, CancellationToken cancellationToken) {
            LeaderboardResponse response = await CreateClient().GetLeaderboardAsync(
                query.SongHash,
                query.GameMode,
                query.Difficulty,
                query.RealmId,
                cancellationToken);

            return GeneratedModelMapper.ToDomain(response);
        }

        public async Task<PagedResult<PlayerSummary>> GetPlayers(PlayerListQuery query, GameSession session, CancellationToken cancellationToken) {
            PlayerListResponse response = await CreateClient().GetPlayersAsync(
                query.Page,
                query.Limit,
                GetCountries(query),
                GetPlayerScope(query.Scope),
                null,
                query.RealmId,
                null,
                null,
                null,
                GetPlayerPivot(query.Scope),
                GetSessionId(session),
                GetSessionKey(session),
                cancellationToken);

            return new PagedResult<PlayerSummary> {
                Items = response.Data.Select(player => GeneratedModelMapper.ToDomain(player)).ToList(),
                Metadata = GeneratedModelMapper.ToDomain(response.Metadata)
            };
        }

        public async Task<PlayerProfile> GetPlayerProfile(string playerId, bool full, int? realmId, CancellationToken cancellationToken) {
            if (full) {
                PlayerProfileResponse response = await CreateClient().GetPlayerAsync(playerId, realmId, cancellationToken);
                return GeneratedModelMapper.ToDomain(response);
            }

            PlayerBasicProfileResponse basicResponse = await CreateClient().GetPlayerBasicAsync(playerId, realmId, cancellationToken);
            return GeneratedModelMapper.ToDomain(basicResponse);
        }

        public async Task<List<PlayerHistoryPoint>> GetGlobalPlayerHistory(string playerId, CancellationToken cancellationToken) {
            List<GlobalPlayerHistoryEntry> response = await CreateClient().GetGlobalPlayerHistoryAsync(playerId, cancellationToken);
            return response.Select(point => GeneratedModelMapper.ToDomain(point)).ToList();
        }

        public async Task<byte[]> DownloadReplay(int scoreId, CancellationToken cancellationToken) {
            using (FileResponse response = await CreateClient().DownloadReplayAsync(scoreId, cancellationToken)) {
                using (var memoryStream = new MemoryStream()) {
                    await response.Stream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        private async Task<LeaderboardScoresSnapshot> GetLeaderboardScores(LeaderboardQuery query, GameSession session, CancellationToken cancellationToken) {
            LeaderboardScoresResponse response = await CreateClient().GetLeaderboardScoresAsync(
                query.SongHash,
                query.GameMode,
                query.Difficulty,
                query.Page,
                query.Limit,
                GetLeaderboardPivot(query.Scope),
                GetLeaderboardScope(query),
                GetQueryFlag(query.HideNoArrows),
                null,
                null,
                null,
                query.RealmId,
                GetIncludePlayerScore(session),
                GetSessionId(session),
                GetSessionKey(session),
                cancellationToken);

            return new LeaderboardScoresSnapshot {
                Scores = new PagedResult<LeaderboardScore> {
                    Items = response.Data.Select(score => GeneratedModelMapper.ToDomain(score)).ToList(),
                    Metadata = GeneratedModelMapper.ToDomain(response.Metadata)
                },
                PlayerScore = GeneratedModelMapper.ToDomain(response.PlayerScore)
            };
        }

        private ScoreSaberApiGeneratedClient CreateClient() {
            return new ScoreSaberApiGeneratedClient(ScoreSaberUrls.WebsiteBaseUrl + "/", _httpClient);
        }

        private static ScoreUploadResult ErrorUploadResult(string message, string errorMessage) {
            return new ScoreUploadResult {
                Status = ScoreUploadStatus.Error,
                Success = false,
                Message = message,
                Error = ScoreSaberApiError.FromMessage(errorMessage)
            };
        }

        private static Pivot? GetLeaderboardPivot(LeaderboardQueryScope scope) {
            switch (scope) {
                case LeaderboardQueryScope.AroundPlayer:
                    return Pivot.Player;
                case LeaderboardQueryScope.Friends:
                    return Pivot.Friends;
                default:
                    return null;
            }
        }

        private static string GetLeaderboardScope(LeaderboardQuery query) {
            switch (query.Scope) {
                case LeaderboardQueryScope.Country:
                    return "country";
                case LeaderboardQueryScope.Region:
                    return "region";
                case LeaderboardQueryScope.Countries:
                    return query.Countries;
                default:
                    return null;
            }
        }

        private static Pivot2? GetPlayerPivot(PlayerQueryScope scope) {
            switch (scope) {
                case PlayerQueryScope.AroundPlayer:
                    return Pivot2.Player;
                case PlayerQueryScope.Friends:
                    return Pivot2.Friends;
                default:
                    return null;
            }
        }

        private static Scope? GetPlayerScope(PlayerQueryScope scope) {
            switch (scope) {
                case PlayerQueryScope.Country:
                    return Scope.Country;
                case PlayerQueryScope.Region:
                    return Scope.Region;
                default:
                    return null;
            }
        }

        private static string GetCountries(PlayerListQuery query) {
            return query.Scope == PlayerQueryScope.Countries ? query.Countries : null;
        }

        private static string GetSessionId(GameSession session) {
            return session != null && session.IsAuthenticated ? session.SessionId : null;
        }

        private static string GetSessionKey(GameSession session) {
            return session != null && session.IsAuthenticated ? session.SessionKey : null;
        }

        private static string GetIncludePlayerScore(GameSession session) {
            return GetQueryFlag(session != null && session.IsAuthenticated);
        }

        private static string GetQueryFlag(bool enabled) {
            return enabled ? "true" : null;
        }

        private static ScoreSaberApiError MapError(GeneratedApiException exception) {
            return new ScoreSaberApiError {
                StatusCode = exception.StatusCode,
                RawBody = exception.Response ?? string.Empty,
                Message = !string.IsNullOrEmpty(exception.Response) ? exception.Response : exception.Message
            };
        }

        private class LeaderboardScoresSnapshot {
            internal PagedResult<LeaderboardScore> Scores { get; set; } = new PagedResult<LeaderboardScore>();
            internal LeaderboardScore PlayerScore { get; set; }
        }
    }
}
