using ScoreSaber.Core;
using ScoreSaber.Features.Leaderboards.Services;
using ScoreSaber.Features.Leaderboards.UI;
using ScoreSaber.Features.Leaderboards.UI.Avatars;
using ScoreSaber.Features.Players.Services;
using ScoreSaber.Features.ScoreSubmission;
using ScoreSaber.Features.ScoreSubmission.Domain;
using System;
using Zenject;

namespace ScoreSaber.Features.Leaderboards {
    internal class LeaderboardStatusController : IInitializable, IDisposable {
        private readonly PanelView _panelView;
        private readonly IScoreSubmissionStatusSource _submissionStatusSource;
        private readonly GameSessionService _gameSessionService;
        private readonly LocalPlayerPanelSession _localPlayerPanelSession;
        private readonly LeaderboardScreenSession _leaderboardSession;
        private readonly LeaderboardAvatarHost _avatarHost;

        public LeaderboardStatusController(
            PanelView panelView,
            IScoreSubmissionStatusSource submissionStatusSource,
            GameSessionService gameSessionService,
            LocalPlayerPanelSession localPlayerPanelSession,
            LeaderboardScreenSession leaderboardSession,
            LeaderboardAvatarHost avatarHost) {
            _panelView = panelView;
            _submissionStatusSource = submissionStatusSource;
            _gameSessionService = gameSessionService;
            _localPlayerPanelSession = localPlayerPanelSession;
            _leaderboardSession = leaderboardSession;
            _avatarHost = avatarHost;
        }

        public void Initialize() {
            _panelView.Ready += PanelViewReady;
            _gameSessionService.LoginStatusChanged += GameSessionServiceLoginStatusChanged;
            _submissionStatusSource.StatusChanged += SubmissionStatusSourceStatusChanged;
            if (_gameSessionService.Status != GameSessionService.LoginStatus.None) {
                ApplyLoginStatus(_gameSessionService.Status, _gameSessionService.StatusText, true);
            }
        }

        private void PanelViewReady() => ApplyLoginStatus(_gameSessionService.Status, _gameSessionService.StatusText, false);

        private void GameSessionServiceLoginStatusChanged(GameSessionService.LoginStatus loginStatus, string status) => ApplyLoginStatus(loginStatus, status, true);

        private void ApplyLoginStatus(GameSessionService.LoginStatus loginStatus, string status, bool refreshLeaderboard) {
            switch (loginStatus) {
                case GameSessionService.LoginStatus.InProgress:
                    _panelView.SetPromptInfo(status, true);
                    break;
                case GameSessionService.LoginStatus.Error:
                    _panelView.SetPromptError(status, false);
                    break;
                case GameSessionService.LoginStatus.Success:
                    _panelView.SetPromptSuccess(status, false, 2f);
                    if (refreshLeaderboard) {
                        _leaderboardSession.RefreshFromFirstPage();
                    }
                    break;
            }
            if (!string.IsNullOrEmpty(status)) {
                Plugin.Log.Debug(status);
            }
        }

        private void SubmissionStatusSourceStatusChanged(ScoreSubmissionStatus status) {
            if (status.Message != string.Empty) {
                Plugin.Log.Debug(status.Message);
            }

            switch (status.Status) {
                case ScoreUploadStatus.Packaging:
                    _panelView.SetPromptInfo(status.Message, true);
                    _avatarHost.ClearAvatars();
                    break;
                case ScoreUploadStatus.Uploading:
                    _panelView.SetPromptInfo(status.Message, true);
                    break;
                case ScoreUploadStatus.Success:
                    _panelView.SetPromptSuccess(status.Message, false, 2f);
                    break;
                case ScoreUploadStatus.Retrying:
                    _panelView.SetPromptError(status.Message, true);
                    break;
                case ScoreUploadStatus.Error:
                    _panelView.SetPromptError(status.Message, false, 3f);
                    break;
                case ScoreUploadStatus.Done:
                    _leaderboardSession.RefreshFromFirstPage();
                    _localPlayerPanelSession.Refresh().RunTask();
                    break;
            }
        }

        public void Dispose() {
            _panelView.Ready -= PanelViewReady;
            _gameSessionService.LoginStatusChanged -= GameSessionServiceLoginStatusChanged;
            _submissionStatusSource.StatusChanged -= SubmissionStatusSourceStatusChanged;
        }
    }
}
