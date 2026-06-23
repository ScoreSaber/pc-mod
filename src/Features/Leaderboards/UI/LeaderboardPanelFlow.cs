using ScoreSaber.Core;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Live.Compete.Services;
using ScoreSaber.Features.Players.Domain;
using ScoreSaber.Features.Players.Services;
using ScoreSaber.Features.MainMenu;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Leaderboards.UI {
    internal class LeaderboardPanelFlow : IInitializable, ITickable, System.IDisposable {
        private const float LogoBlinkSeconds = 1f;

        private readonly PanelView _panelView;
        private readonly GameSessionService _gameSessionService;
        private readonly CompeteDirectoryService _competeDirectoryService;
        private readonly LocalPlayerPanelSession _localPlayerPanelSession;
        private readonly ScoreSaberMenuNavigator _menuNavigator;
        private readonly LeaderboardModalFlow _modalFlow;
        private readonly SettingsService _settings;

        private float _logoBlinkElapsed;
        private bool _logoHighlighted;
        private bool _hasActiveTournaments;
        private CancellationTokenSource _tournamentActionsCancellation;

        public LeaderboardPanelFlow(
            PanelView panelView,
            GameSessionService gameSessionService,
            CompeteDirectoryService competeDirectoryService,
            LocalPlayerPanelSession localPlayerPanelSession,
            ScoreSaberMenuNavigator menuNavigator,
            LeaderboardModalFlow modalFlow,
            SettingsService settings) {

            _panelView = panelView;
            _gameSessionService = gameSessionService;
            _competeDirectoryService = competeDirectoryService;
            _localPlayerPanelSession = localPlayerPanelSession;
            _menuNavigator = menuNavigator;
            _modalFlow = modalFlow;
            _settings = settings;
        }

        public void Initialize() {
            _panelView.Ready += PanelViewReady;
            _panelView.Disabled += _modalFlow.HideModals;
            _panelView.LogoSelected += PanelViewLogoSelected;
            _panelView.SettingsSelected += _menuNavigator.ShowSettings;
            _panelView.RankingSelected += PanelViewRankingSelected;
            _panelView.StatusSelected += _modalFlow.OpenCurrentLeaderboard;
            _panelView.CompeteSelected += _menuNavigator.ShowCompete;
            _gameSessionService.LoginStatusChanged += GameSessionServiceLoginStatusChanged;
            _localPlayerPanelSession.StateChanged += LocalPlayerPanelSessionStateChanged;
        }

        public void Tick() {
            _panelView.AdvanceSpecialBackground(Time.deltaTime);
            ApplyTournamentActionVisibility();
            TickLogoBlink(Time.deltaTime);
        }

        private void PanelViewReady() {
            ApplyLocalPlayerPanelState(_localPlayerPanelSession.CurrentState);
            RefreshTournamentActionVisibility();
            ApplyTournamentActionVisibility();
        }

        private void PanelViewLogoSelected() {
            if (!_settings.Current.hasClickedScoreSaberLogo) {
                _settings.Current.hasClickedScoreSaberLogo = true;
                _panelView.SetLogoColor(Color.white);
                _settings.Save();
            }

            _menuNavigator.ShowMain();
        }

        private void PanelViewRankingSelected() {
            if (_localPlayerPanelSession.CurrentState.HasPlayerProfile) {
                _modalFlow.ShowLocalPlayerProfile();
            }
        }

        private void LocalPlayerPanelSessionStateChanged(LocalPlayerPanelState state) {
            ApplyLocalPlayerPanelState(state);
        }

        private void GameSessionServiceLoginStatusChanged(GameSessionService.LoginStatus loginStatus, string status) {
            if (loginStatus == GameSessionService.LoginStatus.Success) {
                RefreshTournamentActionVisibility();
                return;
            }

            _hasActiveTournaments = false;
            CancelTournamentActionRefresh();
            ApplyTournamentActionVisibility();
        }

        private void ApplyLocalPlayerPanelState(LocalPlayerPanelState state) {
            if (!_panelView.IsReady) {
                return;
            }

            _panelView.SetGlobalLeaderboardRanking(state.GlobalRankingText);
            _panelView.Loaded(state.IsLoaded);
            _panelView.SetWilliumsMode(state.UsesWilliumsPanel);
            _panelView.SetDenyahMode(state.UsesDenyahPanel);

            if (!string.IsNullOrEmpty(state.PromptErrorText)) {
                _panelView.SetPromptError(state.PromptErrorText, false, state.PromptDismissTime);
            }
        }

        private void ApplyTournamentActionVisibility() {
            if (!_panelView.IsReady) {
                return;
            }

            _panelView.SetTournamentActionsVisible(HasAuthenticatedGameSession() && _hasActiveTournaments);
        }

        private void RefreshTournamentActionVisibility() {
            if (!HasAuthenticatedGameSession()) {
                _hasActiveTournaments = false;
                ApplyTournamentActionVisibility();
                return;
            }

            CancelTournamentActionRefresh();
            _tournamentActionsCancellation = new CancellationTokenSource();
            RefreshTournamentActionVisibility(_tournamentActionsCancellation).RunTask();
        }

        private async Task RefreshTournamentActionVisibility(CancellationTokenSource refreshSource) {
            CancellationToken cancellationToken = refreshSource.Token;
            bool hasActiveTournaments = false;
            bool completed = false;

            try {
                hasActiveTournaments = (await _competeDirectoryService.GetActiveTournaments(cancellationToken)).Count > 0;
                completed = true;
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to refresh live tournament availability: {ex.Message}");
                completed = true;
            } finally {
                if (_tournamentActionsCancellation == refreshSource) {
                    if (completed) {
                        _hasActiveTournaments = hasActiveTournaments;
                    }

                    _tournamentActionsCancellation = null;
                    ApplyTournamentActionVisibility();
                }

                refreshSource.Dispose();
            }
        }

        private bool HasAuthenticatedGameSession() {
            return _gameSessionService.HasAuthenticatedSession && _gameSessionService.Status == GameSessionService.LoginStatus.Success;
        }

        private void CancelTournamentActionRefresh() {
            if (_tournamentActionsCancellation == null) {
                return;
            }

            _tournamentActionsCancellation.Cancel();
            _tournamentActionsCancellation = null;
        }

        private void TickLogoBlink(float deltaTime) {
            if (!_panelView.IsReady || _settings.Current.hasClickedScoreSaberLogo) {
                return;
            }

            _logoBlinkElapsed += deltaTime;
            if (_logoBlinkElapsed < LogoBlinkSeconds) {
                return;
            }

            _logoBlinkElapsed = 0f;
            _logoHighlighted = !_logoHighlighted;
            _panelView.SetLogoColor(_logoHighlighted ? new Color(0.60f, 0.80f, 1f) : Color.white);
        }

        public void Dispose() {
            _panelView.Ready -= PanelViewReady;
            _panelView.Disabled -= _modalFlow.HideModals;
            _panelView.LogoSelected -= PanelViewLogoSelected;
            _panelView.SettingsSelected -= _menuNavigator.ShowSettings;
            _panelView.RankingSelected -= PanelViewRankingSelected;
            _panelView.StatusSelected -= _modalFlow.OpenCurrentLeaderboard;
            _panelView.CompeteSelected -= _menuNavigator.ShowCompete;
            _gameSessionService.LoginStatusChanged -= GameSessionServiceLoginStatusChanged;
            _localPlayerPanelSession.StateChanged -= LocalPlayerPanelSessionStateChanged;
            CancelTournamentActionRefresh();
        }
    }
}
