using ScoreSaber.Core;
using ScoreSaber.Core.Gameplay;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.Leaderboards.Services;
using ScoreSaber.Features.Live.Compete.Services;
using ScoreSaber.Features.Live.Replay;
using ScoreSaber.Features.Players.Services;
using ScoreSaber.Features.Replays;
using ScoreSaber.Features.ScoreSubmission.Domain;
using ScoreSaber.Features.ScoreSubmission.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.ScoreSubmission {
    internal interface IScoreSubmissionStatusSource {
        bool IsUploading { get; }
        event Action<ScoreSubmissionStatus> StatusChanged;
    }

    internal class ScoreSubmissionController : IInitializable, IDisposable, IScoreSubmissionStatusSource {
        public event Action<ScoreSubmissionStatus> StatusChanged;

        public bool IsUploading { get; private set; }

        private readonly ScoreSubmissionWorkflow _submissionWorkflow;
        private readonly ScoreSubmissionService _scoreSubmissionService;
        private readonly LeaderboardPlayerScoreCache _playerScoreCache;
        private readonly GameSessionService _gameSessionService;
        private readonly ReplayState _replayState;
        private readonly LiveReplayStreamingService _liveReplayStreamingService;
        private readonly CompeteGameplayState _competeGameplayState;
        private int _visibleUploadCount;

        public ScoreSubmissionController(ScoreSubmissionWorkflow submissionWorkflow, ScoreSubmissionService scoreSubmissionService, LeaderboardPlayerScoreCache playerScoreCache, GameSessionService gameSessionService, ReplayState replayState, LiveReplayStreamingService liveReplayStreamingService, CompeteGameplayState competeGameplayState) {
            _submissionWorkflow = submissionWorkflow;
            _scoreSubmissionService = scoreSubmissionService;
            _playerScoreCache = playerScoreCache;
            _gameSessionService = gameSessionService;
            _replayState = replayState;
            _liveReplayStreamingService = liveReplayStreamingService;
            _competeGameplayState = competeGameplayState;
            Plugin.Log.Debug("Upload service setup!");
        }

        public void Initialize() {
            _scoreSubmissionService.RegisterCallbacks(HandleStandardLevelFinished, HandleMultiplayerLevelFinished);
        }

        private void HandleStandardLevelFinished(StandardLevelScenesTransitionSetupData transition, LevelCompletionResults results) {
            HandleLevelFinished(new ScoreSubmissionRequest(transition.gameMode, transition.GetBeatmapLevel(), transition.GetBeatmapKey(), results, transition.practiceSettings != null, GetCurrentSongTime()));
        }

        private void HandleMultiplayerLevelFinished(MultiplayerLevelScenesTransitionSetupData transition, MultiplayerResultsData resultsData) {
            if (transition.GetBeatmapLevel() == null) {
                return;
            }

            MultiplayerLevelCompletionResults multiplayerResults = resultsData.localPlayerResultData.multiplayerLevelCompletionResults;
            if (multiplayerResults.levelCompletionResults == null) {
                return;
            }

            if (multiplayerResults.playerLevelEndReason == MultiplayerLevelCompletionResults.MultiplayerPlayerLevelEndReason.HostEndedLevel) {
                return;
            }

            HandleLevelFinished(new ScoreSubmissionRequest(transition.gameMode, transition.GetBeatmapLevel(), transition.GetBeatmapKey(), multiplayerResults.levelCompletionResults, false, GetCurrentSongTime()));
        }

        private void HandleLevelFinished(ScoreSubmissionRequest request) {
            try {
                ScoreSaberPlayOutcome? playOutcomeOverride = GetPlayOutcomeOverride(request);
                _liveReplayStreamingService.Complete(request.Results, request.PlayOutcomeTime, playOutcomeOverride);
                ScoreSubmissionDecision decision = Decide(request, playOutcomeOverride);
                Plugin.Log.Debug($"Score submission decision: {decision.Action} {decision.Reason}");

                switch (decision.Action) {
                    case ScoreSubmissionAction.Ignore:
                        _submissionWorkflow.DiscardReplay();
                        return;
                    case ScoreSubmissionAction.WriteReplayOnly:
                        WriteReplayOnly(request);
                        return;
                    case ScoreSubmissionAction.SubmitScore:
                        SubmitScore(request, decision.Visibility, playOutcomeOverride).RunTask();
                        return;
                }
            } catch (Exception ex) {
                Report(ScoreUploadStatus.Error, "Failed to upload score, error written to log.");
                Plugin.Log.Error($"Failed to upload score: {ex}");
            }
        }

        private async Task SubmitScore(ScoreSubmissionRequest request, ScoreSubmissionVisibility visibility, ScoreSaberPlayOutcome? playOutcomeOverride) {
            bool visibleUpload = visibility == ScoreSubmissionVisibility.Visible && ShouldShowUploadStatus(request);
            if (visibleUpload) {
                IsUploading = Interlocked.Increment(ref _visibleUploadCount) > 0;
            }

            try {
                ScoreUploadResult result = await _submissionWorkflow.SubmitScore(
                    request.BeatmapLevel,
                    request.BeatmapKey,
                    request.Results,
                    request.PlayOutcomeTime,
                    playOutcomeOverride,
                    visibleUpload,
                    visibleUpload ? Emit : (Action<ScoreSubmissionStatus>)null,
                    false,
                    false,
                    CancellationToken.None);

                if (result.Success) {
                    Plugin.Log.Info("Score uploaded!");
                }
                if (visibleUpload) {
                    Emit(ScoreSubmissionStatus.FromResult(result));
                }
                if (!result.Success) {
                    Plugin.Log.Error($"Failed to upload score: {result.Message}");
                }
            } catch (Exception ex) {
                if (visibleUpload) {
                    Report(ScoreUploadStatus.Error, "Failed to upload score, error written to log.");
                }
                Plugin.Log.Error($"Failed to upload score: {ex}");
            } finally {
                if (visibleUpload) {
                    int remainingVisibleUploads = Interlocked.Decrement(ref _visibleUploadCount);
                    if (remainingVisibleUploads <= 0) {
                        _visibleUploadCount = 0;
                        IsUploading = false;
                        Report(ScoreUploadStatus.Done, string.Empty);
                    }
                }
            }
        }

        private bool ShouldShowUploadStatus(ScoreSubmissionRequest request) {
            LeaderboardScore playerScore;
            if (!_playerScoreCache.TryGet(request.BeatmapKey, GetPlayerId(), out playerScore)) {
                Plugin.Log.Debug("No cached API player score for this leaderboard; showing score upload status");
                return true;
            }

            if (!playerScore.PersonalBest) {
                Plugin.Log.Debug("Cached API player score is not marked as a PB; showing score upload status");
                return true;
            }

            if (playerScore.PlayOutcome != ScoreSaberPlayOutcome.Clear) {
                Plugin.Log.Debug("Cached API player score is not a clear; showing score upload status");
                return true;
            }

            if (request.Results.multipliedScore > playerScore.ModifiedScore) {
                Plugin.Log.Debug($"Score beats cached API PB ({request.Results.multipliedScore} > {playerScore.ModifiedScore}); showing score upload status");
                return true;
            }

            Plugin.Log.Debug($"Score did not beat cached API PB ({request.Results.multipliedScore} <= {playerScore.ModifiedScore}); uploading silently");
            return false;
        }

        private string GetPlayerId() => _gameSessionService.GameSession != null ? _gameSessionService.GameSession.PlayerId : _gameSessionService.LocalPlayerInfo != null ? _gameSessionService.LocalPlayerInfo.playerId : string.Empty;

        private void WriteReplayOnly(ScoreSubmissionRequest request) => _submissionWorkflow.WriteReplayOnly(request.BeatmapLevel, request.BeatmapKey, request.Results, request.PlayOutcomeTime).RunTask();

        private void Report(ScoreUploadStatus status, string statusText) => Emit(ScoreSubmissionStatus.Progress(status, statusText));

        private void Emit(ScoreSubmissionStatus status) => StatusChanged?.Invoke(status);

        private ScoreSubmissionDecision Decide(ScoreSubmissionRequest request, ScoreSaberPlayOutcome? playOutcomeOverride) {
            if (_replayState.IsPlaybackEnabled) {
                return ScoreSubmissionDecision.Ignore("replay playback is active");
            }

            if (request.GameMode != "Solo" && request.GameMode != "Multiplayer") {
                return ScoreSubmissionDecision.Ignore("unsupported game mode");
            }

            if (!ScoreSaberBeatmapKey.IsSupported(request.BeatmapKey)) {
                return ScoreSubmissionDecision.Ignore("unsupported or WIP ScoreSaber level id");
            }

            if (request.Practicing || IsPracticeViewActive()) {
                return ScoreSubmissionDecision.WriteReplayOnly("practice run");
            }

            if (request.Results.multipliedScore <= 0) {
                return ScoreSubmissionDecision.Ignore("score is 0, server would reject it");
            }

            if (playOutcomeOverride.HasValue && playOutcomeOverride.Value == ScoreSaberPlayOutcome.Quit) {
                return ScoreSubmissionDecision.SubmitScore(ScoreSubmissionVisibility.Silent, "live map was stopped by host");
            }

            if (request.Results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed) {
                return ScoreSubmissionDecision.SubmitScore(ScoreSubmissionVisibility.Silent, "level failed");
            }

            if (request.Results.levelEndAction == LevelCompletionResults.LevelEndAction.Restart) {
                return ScoreSubmissionDecision.SubmitScore(ScoreSubmissionVisibility.Silent, "level was restarted");
            }

            if (request.Results.levelEndAction == LevelCompletionResults.LevelEndAction.Quit) {
                return ScoreSubmissionDecision.SubmitScore(ScoreSubmissionVisibility.Silent, "level was quit");
            }

            if (request.Results.levelEndStateType != LevelCompletionResults.LevelEndStateType.Cleared) {
                return ScoreSubmissionDecision.WriteReplayOnly($"level was not cleared ({request.Results.levelEndStateType}, {request.Results.levelEndAction})");
            }

            return ScoreSubmissionDecision.SubmitScore(ScoreSubmissionVisibility.Visible, "level cleared");
        }

        private static bool IsPracticeViewActive() => Resources.FindObjectsOfTypeAll<PracticeViewController>().FirstOrDefault()?.isInViewControllerHierarchy ?? false;

        private static float GetCurrentSongTime() => Resources.FindObjectsOfTypeAll<AudioTimeSyncController>().FirstOrDefault()?.songTime ?? 0f;

        private ScoreSaberPlayOutcome? GetPlayOutcomeOverride(ScoreSubmissionRequest request) {
            string songHash;
            if (!ScoreSaberBeatmapKey.TryGetSongHash(request.BeatmapKey, out songHash)) {
                songHash = string.Empty;
            }

            return _competeGameplayState.TryConsumeHostStop(songHash) ? ScoreSaberPlayOutcome.Quit : (ScoreSaberPlayOutcome?)null;
        }

        public void Dispose() {
            Plugin.Log.Info("Upload service succesfully deconstructed");
            _scoreSubmissionService.ClearCallbacks();
        }
    }

    internal class ScoreSubmissionRequest {
        internal ScoreSubmissionRequest(string gameMode, BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, LevelCompletionResults results, bool practicing, float playOutcomeTime) {
            GameMode = gameMode;
            BeatmapLevel = beatmapLevel;
            BeatmapKey = beatmapKey;
            Results = results;
            Practicing = practicing;
            PlayOutcomeTime = playOutcomeTime;
        }

        internal string GameMode { get; }
        internal BeatmapLevel BeatmapLevel { get; }
        internal BeatmapKey BeatmapKey { get; }
        internal LevelCompletionResults Results { get; }
        internal bool Practicing { get; }
        internal float PlayOutcomeTime { get; }
    }

    internal enum ScoreSubmissionAction {
        Ignore,
        WriteReplayOnly,
        SubmitScore
    }

    internal enum ScoreSubmissionVisibility {
        Visible,
        Silent
    }

    internal class ScoreSubmissionDecision {
        private ScoreSubmissionDecision(ScoreSubmissionAction action, string reason, ScoreSubmissionVisibility visibility) {
            Action = action;
            Reason = reason ?? string.Empty;
            Visibility = visibility;
        }

        internal ScoreSubmissionAction Action { get; }
        internal string Reason { get; }
        internal ScoreSubmissionVisibility Visibility { get; }

        internal static ScoreSubmissionDecision Ignore(string reason) => new ScoreSubmissionDecision(ScoreSubmissionAction.Ignore, reason, ScoreSubmissionVisibility.Silent);

        internal static ScoreSubmissionDecision WriteReplayOnly(string reason) => new ScoreSubmissionDecision(ScoreSubmissionAction.WriteReplayOnly, reason, ScoreSubmissionVisibility.Silent);

        internal static ScoreSubmissionDecision SubmitScore(ScoreSubmissionVisibility visibility, string reason) => new ScoreSubmissionDecision(ScoreSubmissionAction.SubmitScore, reason, visibility);
    }
}
