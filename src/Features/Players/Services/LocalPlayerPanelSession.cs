using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Players.Domain;
using ScoreSaber.Core.Presentation;
using ScoreSaber.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using Zenject;

namespace ScoreSaber.Features.Players.Services {
    internal class LocalPlayerPanelSession : IInitializable, IDisposable {
        private const int RefreshIntervalMilliseconds = 240000;

        internal event Action<LocalPlayerPanelState> StateChanged;

        private readonly GameSessionService _gameSessionService;
        private readonly PlayerProfileService _playerProfileService;
        private readonly SettingsService _settings;

        private LocalPlayerPanelData _currentPanelData;
        private CancellationTokenSource _refreshLoopCancellation;
        private CancellationTokenSource _refreshCancellation;

        internal LocalPlayerPanelState CurrentState { get; private set; } = LocalPlayerPanelState.Initial();

        public LocalPlayerPanelSession(GameSessionService gameSessionService, PlayerProfileService playerProfileService, SettingsService settings) {
            _gameSessionService = gameSessionService;
            _playerProfileService = playerProfileService;
            _settings = settings;
        }

        public void Initialize() {
            _gameSessionService.LoginStatusChanged += GameSessionServiceLoginStatusChanged;
            if (_gameSessionService.Status == GameSessionService.LoginStatus.Success) {
                StartRefreshLoop();
            }
        }

        internal Task Refresh() => Refresh(CancellationToken.None);

        internal void ApplyCurrentSettings() {
            if (_currentPanelData == null) {
                Publish(CurrentState);
                return;
            }

            PublishPlayer(_currentPanelData);
        }

        private void GameSessionServiceLoginStatusChanged(GameSessionService.LoginStatus loginStatus, string status) {
            if (loginStatus == GameSessionService.LoginStatus.Success) {
                StartRefreshLoop();
                return;
            }

            if (loginStatus == GameSessionService.LoginStatus.Error) {
                _refreshCancellation?.Cancel();
                Publish(LocalPlayerPanelState.Unavailable());
            }
        }

        private void StartRefreshLoop() {
            if (_refreshLoopCancellation != null) {
                return;
            }

            _refreshLoopCancellation = new CancellationTokenSource();
            RefreshLoop(_refreshLoopCancellation.Token).RunTask();
        }

        private async Task RefreshLoop(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                await Refresh(cancellationToken);
                await Task.Delay(RefreshIntervalMilliseconds, cancellationToken);
            }
        }

        private async Task Refresh(CancellationToken cancellationToken) {
            _refreshCancellation?.Cancel();
            _refreshCancellation?.Dispose();
            _refreshCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken refreshToken = _refreshCancellation.Token;

            try {
                Publish(LocalPlayerPanelState.Loading(CurrentState));
                LocalPlayerPanelData panelData = await GetLocalPlayerPanelData();
                if (refreshToken.IsCancellationRequested) {
                    return;
                }

                _currentPanelData = panelData;
                PublishPlayer(panelData);
            } catch (OperationCanceledException) {
            } catch (HttpErrorException ex) {
                Publish(FromHttpError(ex));
            }
        }

        private LocalPlayerPanelState FromHttpError(HttpErrorException ex) {
            if (!ex.isScoreSaberError) {
                Plugin.Log.Error("Failed to update local player ranking " + ex.ToString());
                return LocalPlayerPanelState.PromptError(CurrentState, "Failed to update local player ranking", 1.5f);
            }

            return ex.scoreSaberError.ErrorMessage == "Player not found"
                ? LocalPlayerPanelState.Message("Welcome to ScoreSaber! Set a score to create a profile")
                : LocalPlayerPanelState.Message($"Failed to load player ranking: {ex.scoreSaberError.ErrorMessage}");
        }

        private async Task<LocalPlayerPanelData> GetLocalPlayerPanelData() {
            await ScoreSaber.Core.TaskExtensions.WaitUntil(() => _gameSessionService.Status == GameSessionService.LoginStatus.Success);

            string playerId = _gameSessionService.LocalPlayerInfo.playerId;
            return new LocalPlayerPanelData {
                Player = await _playerProfileService.GetPlayerInfo(playerId, full: false),
                UsesWilliumsPanel = PlayerPresentation.UsesWilliumsPanel(playerId),
                UsesDenyahPanel = PlayerPresentation.UsesDenyahPanel(playerId)
            };
        }

        private string FormatRanking(LocalPlayerPanelData panelData) {
            if (!_settings.Current.showLocalPlayerRank) {
                return "<b><color=#FFDE1A>Global Ranking: </color></b>Hidden";
            }

            if (panelData.Player == null) {
                return CurrentState.GlobalRankingText;
            }

            return $"<b><color=#FFDE1A>Global Ranking: </color></b>#{string.Format("{0:n0}", panelData.Player.Stats.Rank)}<size=75%> (<color=#6772E5>{string.Format("{0:n0}", panelData.Player.Stats.TotalPP)}pp</color>)";
        }

        private void PublishPlayer(LocalPlayerPanelData panelData) {
            Publish(LocalPlayerPanelState.Player(panelData.Player, panelData.UsesWilliumsPanel, panelData.UsesDenyahPanel, FormatRanking(panelData)));
        }

        private void Publish(LocalPlayerPanelState state) {
            CurrentState = state;
            StateChanged?.Invoke(state);
        }

        public void Dispose() {
            _gameSessionService.LoginStatusChanged -= GameSessionServiceLoginStatusChanged;
            _refreshLoopCancellation?.Cancel();
            _refreshLoopCancellation?.Dispose();
            _refreshCancellation?.Cancel();
            _refreshCancellation?.Dispose();
        }

        private class LocalPlayerPanelData {
            internal PlayerProfile Player { get; set; }
            internal bool UsesWilliumsPanel { get; set; }
            internal bool UsesDenyahPanel { get; set; }
        }
    }
}
