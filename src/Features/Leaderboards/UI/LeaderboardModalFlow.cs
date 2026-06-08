using BeatSaberMarkupLanguage.Parser;
using HMUI;
using ScoreSaber.Features.Replays.Services;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.Leaderboards.Services;
using ScoreSaber.Features.Players.Services;
using ScoreSaber.Features.Replays;
using ScoreSaber.Features.Replays.Format;
using ScoreSaber.Core.Api;
using ScoreSaber.Core;
using ScoreSaber.Features.Leaderboards.UI.ScoreDetails;
using ScoreSaber.Features.Players.Profile;
using System;
using System.Threading.Tasks;
using IPA.Utilities;
using UnityEngine;

namespace ScoreSaber.Features.Leaderboards.UI {
    internal class LeaderboardModalFlow : IDisposable {
        private static readonly FieldAccessor<ModalView, bool>.Accessor AnimateParentCanvas =
            FieldAccessor<ModalView, bool>.GetAccessor("_animateParentCanvas");

        private readonly PanelView _panelView;
        private readonly ReplayLoader _replayLoader;
        private readonly ReplayQueryService _replayQueryService;
        private readonly LeaderboardScreenSession _leaderboardSession;
        private readonly GameSessionService _gameSessionService;

        private BSMLParserParams _parserParams;
        private ProfileDetailView _profileDetailView;
        private bool _replayDownloading;

        internal ScoreDetailView ScoreDetailView { get; }

        public LeaderboardModalFlow(
            PanelView panelView,
            ReplayLoader replayLoader,
            ReplayQueryService replayQueryService,
            LeaderboardScreenSession leaderboardSession,
            GameSessionService gameSessionService) {

            _panelView = panelView;
            _replayLoader = replayLoader;
            _replayQueryService = replayQueryService;
            _leaderboardSession = leaderboardSession;
            _gameSessionService = gameSessionService;
            ScoreDetailView = new ScoreDetailView();
            ScoreDetailView.showProfile += ShowProfile;
            ScoreDetailView.startReplay += StartReplay;
        }

        internal void Bind(BSMLParserParams parserParams, ProfileDetailView profileDetailView) {
            _parserParams = parserParams;
            _profileDetailView = profileDetailView;
        }

        internal void ShowScore(ScoreMap score) {
            if (score == null || _parserParams == null) {
                return;
            }

            _parserParams.EmitEvent("present-score-info");
            ScoreDetailView.SetScoreInfo(score, _replayDownloading);
        }

        internal void AllowReplayWatching(bool value) => ScoreDetailView.AllowReplayWatching(value);

        internal void HideModals() {
            if (ScoreDetailView.detailModalRoot != null && _profileDetailView?.profileModalRoot != null) {
                ScoreDetailView.detailModalRoot.gameObject.SetActive(false);
                _profileDetailView.profileModalRoot.gameObject.SetActive(false);
                AnimateParentCanvas(ref ScoreDetailView.detailModalRoot) = true;
                AnimateParentCanvas(ref _profileDetailView.profileModalRoot) = true;
            }
        }

        internal void OpenCurrentLeaderboard() {
            LeaderboardMap leaderboard = _leaderboardSession.CurrentState?.Leaderboard;
            if (leaderboard == null) {
                return;
            }

            CloseModals();
            Application.OpenURL(ScoreSaberUrls.Leaderboard(leaderboard.LeaderboardInfo.Leaderboard.Id));
        }

        internal void ShowLocalPlayerProfile() {
            if (_gameSessionService.LocalPlayerInfo == null) {
                return;
            }

            ShowProfile(_gameSessionService.LocalPlayerInfo.playerId);
        }

        private void ShowProfile(string playerId) {
            if (_profileDetailView == null || _parserParams == null) {
                return;
            }

            CloseModals();
            _parserParams.EmitEvent("show-profile");
            _profileDetailView.ShowProfile(playerId).RunTask();
        }

        private void StartReplay(ScoreMap score) => StartReplayAsync(score).RunTask();

        private async Task StartReplayAsync(ScoreMap score) {
            CloseModals();
            _replayDownloading = true;

            try {
                _panelView.SetPromptInfo("Downloading Replay...", true);
                byte[] replay = await _replayQueryService.GetReplayData(score);
                _panelView.SetPromptInfo("Replay downloaded! Unpacking...", true);
                await _replayLoader.Load(replay, score.Parent.BeatmapLevel, score.Parent.BeatmapKey, score.GameplayModifiers, score.Score.Player.Name);
                _panelView.SetPromptSuccess("Replay Started!", false, 1f);
            } catch (ReplayVersionException ex) {
                _panelView.SetPromptError("Unsupported replay version", false);
                Plugin.Log.Error($"Failed to start replay (unsupported version): {ex}");
            } catch (Exception ex) {
                _panelView.SetPromptError("Failed to start replay! Error written to log.", false);
                Plugin.Log.Error($"Failed to start replay: {ex}");
            }

            _replayDownloading = false;
        }

        private void CloseModals() => _parserParams?.EmitEvent("close-modals");

        public void Dispose() {
            ScoreDetailView.startReplay -= StartReplay;
            ScoreDetailView.showProfile -= ShowProfile;
        }
    }
}
