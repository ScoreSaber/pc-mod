using ScoreSaber.Core.Api;
using ScoreSaber.Core.Gameplay;
using ScoreSaber.Features.Players.Services;
using ScoreSaber.Features.Replays;
using ScoreSaber.Features.ScoreSubmission.Domain;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.ScoreSubmission.Services {

    internal class ScoreSubmissionWorkflow {
        private const int MaxUploadAttempts = 3;

        private readonly GameSessionService _gameSessionService;
        private readonly ReplayService _replayService;
        private readonly ReplayStorageService _replayStorageService;
        private readonly ScoreUploadPayloadBuilder _payloadBuilder;
        private readonly IScoreSaberApiClient _apiClient;

        public ScoreSubmissionWorkflow(
            GameSessionService gameSessionService,
            ReplayService replayService,
            ReplayStorageService replayStorageService,
            ScoreUploadPayloadBuilder payloadBuilder,
            IScoreSaberApiClient apiClient) {
            _gameSessionService = gameSessionService;
            _replayService = replayService;
            _replayStorageService = replayStorageService;
            _payloadBuilder = payloadBuilder;
            _apiClient = apiClient;
        }

        internal async Task<ScoreUploadResult> SubmitScore(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, LevelCompletionResults results, float playOutcomeTime, ScoreSaberPlayOutcome? playOutcomeOverride, bool saveLocalReplay, Action<ScoreSubmissionStatus> statusChanged, bool forceAuthenticationRefresh, bool notifyAuthenticationStatus, CancellationToken cancellationToken) {
            Report(statusChanged, ScoreUploadStatus.Packaging, "Packaging score...");
            ReplaySerializationResult replay = await WriteSerializedReplay(statusChanged);
            if (replay == null || replay.Replay == null) {
                return Error("Failed to upload (failed to serialize replay)");
            }

            if (!await EnsureScoreUploadSession(forceAuthenticationRefresh, notifyAuthenticationStatus, cancellationToken)) {
                return Error("ScoreSaber is not authenticated");
            }

            float outcomeTime = GetPlayOutcomeTime(results, playOutcomeTime, replay.FailTime);
            ScoreUploadPayload payload = _payloadBuilder.Build(beatmapLevel, beatmapKey, results, _gameSessionService.LocalPlayerInfo, outcomeTime, playOutcomeOverride);

            Plugin.Log.Debug($"Upload payload size: data={payload.EncryptedScoreData.Length} chars, replay={replay.Replay.Length} bytes");
            ScoreUploadResult result = await UploadWithRetries(payload.EncryptedScoreData, payload.ScoreData.InfoHash, replay.Replay, statusChanged, notifyAuthenticationStatus, cancellationToken);
            if (result.Success && saveLocalReplay) {
                _replayStorageService.SaveLocalReplay(payload.ScoreData, beatmapKey, replay.Replay);
            }

            return result;
        }

        internal void DiscardReplay() => _replayService.DiscardReplay();

        internal Task WriteReplayOnly(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, LevelCompletionResults results, float playOutcomeTime) {
            _replayService.DiscardReplay();
            return Task.CompletedTask;
        }

        private async Task<ReplaySerializationResult> WriteSerializedReplay(Action<ScoreSubmissionStatus> statusChanged) {
            Report(statusChanged, ScoreUploadStatus.Packaging, "Packaging replay...");
            ReplaySerializationResult replay = await _replayService.WriteReplay();
            if (replay == null || replay.Replay == null) {
                return null;
            }

            Plugin.Log.Debug($"Replay size: {replay.Replay.Length}");
            return replay;
        }

        private static float GetPlayOutcomeTime(LevelCompletionResults results, float playOutcomeTime, float recordedFailTime) => results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed && recordedFailTime > 0f ? recordedFailTime : playOutcomeTime;

        private async Task<ScoreUploadResult> UploadWithRetries(string uploadData, string uploadVersionHash, byte[] replay, Action<ScoreSubmissionStatus> statusChanged, bool notifyAuthenticationStatus, CancellationToken cancellationToken) {
            for (int attempt = 1; attempt <= MaxUploadAttempts; attempt++) {
                Report(statusChanged, ScoreUploadStatus.Uploading, "Uploading score...");
                Plugin.Log.Info("Attempting score upload...");

                ScoreUploadResult result = await _apiClient.UploadScore(_gameSessionService.GameSession, uploadData, uploadVersionHash, replay, cancellationToken);
                if (result.Success) {
                    return result;
                }

                Plugin.Log.Error($"Failed to upload score: {result.Message}");
                if (attempt < MaxUploadAttempts && IsRetryable(result)) {
                    if (RequiresAuthenticationRefresh(result) && !_gameSessionService.CanUseUploadProtocolV2) {
                        return result;
                    }

                    Report(statusChanged, ScoreUploadStatus.Retrying, $"Failed, attempting again ({attempt} of {MaxUploadAttempts} tries...)");
                    await Task.Delay(1000, cancellationToken);
                    if (RequiresAuthenticationRefresh(result) && !await RefreshUploadTrust(cancellationToken)) {
                        return result;
                    }
                    continue;
                }

                return result;
            }

            return Error("Failed to upload score");
        }

        private Task<bool> EnsureScoreUploadSession(bool forceAuthenticationRefresh, bool notifyAuthenticationStatus, CancellationToken cancellationToken) {
            if (!_gameSessionService.CanUseUploadProtocolV2) {
                return _gameSessionService.EnsureAuthenticated(forceAuthenticationRefresh, cancellationToken, notifyAuthenticationStatus);
            }

            if (!_gameSessionService.HasAuthenticatedSession ||
                forceAuthenticationRefresh ||
                !_gameSessionService.GameSession.UsesUploadProtocolV2) {
                return _gameSessionService.EnsureAuthenticated(forceAuthenticationRefresh || _gameSessionService.HasAuthenticatedSession, cancellationToken, notifyAuthenticationStatus);
            }

            return Task.FromResult(true);
        }

        private Task<bool> RefreshUploadTrust(CancellationToken cancellationToken) => _gameSessionService.CanUseUploadProtocolV2 ? _gameSessionService.RefreshUploadTrust(cancellationToken) : Task.FromResult(false);

        private static bool IsRetryable(ScoreUploadResult result) {
            int statusCode = result?.Error?.StatusCode ?? 0;
            return statusCode == 0 || statusCode == 401 || statusCode >= 500 || IsUploadTrustError(result) || IsNonceError(result);
        }

        private static bool RequiresAuthenticationRefresh(ScoreUploadResult result) => (result?.Error?.StatusCode ?? 0) == 401 || IsUploadTrustError(result);

        private static bool IsUploadTrustError(ScoreUploadResult result) =>
            ContainsMessage(result, "upload protocol") ||
            ContainsMessage(result, "upload trust") ||
            ContainsMessage(result, "trusted for uploads") ||
            ContainsMessage(result, "upload version") ||
            ContainsMessage(result, "upload timestamp") ||
            ContainsMessage(result, "upload signature") ||
            ContainsMessage(result, "upload build") ||
            ContainsMessage(result, "official build");

        private static bool IsNonceError(ScoreUploadResult result) => ContainsMessage(result, "nonce");

        private static bool ContainsMessage(ScoreUploadResult result, string value) => (result?.Error?.Message ?? result?.Message ?? string.Empty).IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

        private static void Report(Action<ScoreSubmissionStatus> statusChanged, ScoreUploadStatus status, string message) {
            statusChanged?.Invoke(ScoreSubmissionStatus.Progress(status, message));
        }

        private static ScoreUploadResult Error(string message) {
            return new ScoreUploadResult {
                Status = ScoreUploadStatus.Error,
                Success = false,
                Message = message,
                Error = ScoreSaberApiError.FromMessage(message)
            };
        }
    }
}
