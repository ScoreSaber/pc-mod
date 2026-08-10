using HMUI;
using IPA.Utilities.Async;
using ScoreSaber.Core;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.Leaderboards.Services;
using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Compete.Services;
using ScoreSaber.Features.Live.Ludus.Services;
using ScoreSaber.Features.Live.Compete.UI.ViewControllers.CodeEntry;
using ScoreSaber.Features.Live.Compete.UI.ViewControllers.Entry;
using ScoreSaber.Features.Live.Compete.UI.ViewControllers.Room.Center;
using ScoreSaber.Features.Live.Compete.UI.ViewControllers.Room.Left;
using ScoreSaber.Features.Live.Compete.UI.ViewControllers.Rooms;
using ScoreSaber.Features.Live.Compete.UI.ViewControllers.Shared;
using ScoreSaber.Features.Live.UI.ViewControllers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zenject;

namespace ScoreSaber.Features.Live.Compete.UI.FlowCoordinators {
    internal class CompeteFlowCoordinator : FlowCoordinator {
        private const int LoadingTransitionDelayMs = 450;

        internal event Action DidFinishEvent;

        private readonly object _promptLock = new object();
        private readonly Queue<CompeteOrganizerPrompt> _pendingPrompts = new Queue<CompeteOrganizerPrompt>();

        private CompeteDirectoryService _directoryService;
        private LudusSessionService _ludusSession;
        private CompeteGameplayState _competeGameplayState;
        private CompeteModeSelectionViewController _modeSelectionViewController;
        private TournamentBrowserViewController _tournamentBrowserViewController;
        private CompeteRoomListViewController _roomListViewController;
        private CompeteRoomViewController _roomViewController;
        private CompetePlayerListViewController _playerListViewController;
        private GameplaySetupViewController _gameplaySetupViewController;
        private LeaderboardScreenSession _leaderboardSession;
        private PlatformLeaderboardViewController _platformLeaderboardViewController;
        private LevelSelectionNavigationController _levelSelectionNavigationController;
        private CompeteCodeEntryViewController _codeEntryViewController;
        private CompeteLoadingViewController _loadingViewController;
        private CompeteTournament _selectedTournament;
        private CompeteRoom _selectedRoom;
        private bool _rightPanelShowingLeaderboard;
        private bool _promptShowing;
        private bool _roomTransitioning;
        private bool _loadingTransitioning;
        private bool _tournamentBrowserEventsSubscribed;
        private bool _roomUiPending;
        private bool _roomUiPendingSongChanged;
        private bool _roomClosedDuringGameplay;
        private string _roomCloseStatus;
        private string _gameplayRoomCloseStatus;
        private CancellationTokenSource _loadingCancellation;

        [Inject]
        internal void Construct(
            CompeteDirectoryService directoryService,
            LudusSessionService ludusSession,
            CompeteGameplayState competeGameplayState,
            CompeteModeSelectionViewController modeSelectionViewController,
            TournamentBrowserViewController tournamentBrowserViewController,
            CompeteRoomListViewController roomListViewController,
            CompeteRoomViewController roomViewController,
            CompetePlayerListViewController playerListViewController,
            GameplaySetupViewController gameplaySetupViewController,
            LeaderboardScreenSession leaderboardSession,
            PlatformLeaderboardViewController platformLeaderboardViewController,
            LevelSelectionNavigationController levelSelectionNavigationController,
            CompeteCodeEntryViewController codeEntryViewController,
            CompeteLoadingViewController loadingViewController) {

            _directoryService = directoryService;
            _ludusSession = ludusSession;
            _competeGameplayState = competeGameplayState;
            _modeSelectionViewController = modeSelectionViewController;
            _tournamentBrowserViewController = tournamentBrowserViewController;
            _roomListViewController = roomListViewController;
            _roomViewController = roomViewController;
            _playerListViewController = playerListViewController;
            _gameplaySetupViewController = gameplaySetupViewController;
            _leaderboardSession = leaderboardSession;
            _platformLeaderboardViewController = platformLeaderboardViewController;
            _levelSelectionNavigationController = levelSelectionNavigationController;
            _codeEntryViewController = codeEntryViewController;
            _loadingViewController = loadingViewController;

            _modeSelectionViewController.BrowserSelected += SelectBrowser;
            _modeSelectionViewController.JoinViaCodeSelected += SelectJoinViaCode;
            _roomListViewController.RefreshRequested += RefreshRooms;
            _roomListViewController.RoomSelected += SelectRoom;
            _roomViewController.ReadyToggled += ToggleReady;
            _roomViewController.PlayersPanelSelected += ShowPlayersPanel;
            _roomViewController.LeaderboardPanelSelected += ShowLeaderboardPanel;
            _roomViewController.PromptAnswered += PromptWasAnswered;
            _codeEntryViewController.JoinRequested += JoinViaCode;
            _ludusSession.RoomUpdated += RoomWasUpdated;
            _ludusSession.RoomClosed += RoomWasClosed;
            _ludusSession.PromptReceived += ReceivePrompt;
            _ludusSession.MapStartCountdownChanged += MapStartCountdownWasChanged;
            _ludusSession.StatusChanged += LudusStatusChanged;
            _competeGameplayState.LiveGameplayActiveChanged += LiveGameplayActiveChanged;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) {
            SubscribeTournamentBrowserEvents();

            if (firstActivation) {
                SetTitle("ScoreSaber Compete", ViewController.AnimationType.None);
                showBackButton = true;
                ProvideInitialViewControllers(_modeSelectionViewController);
            }
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling) {
            UnsubscribeTournamentBrowserEvents();

            if (removedFromHierarchy) {
                lock (_promptLock) {
                    _pendingPrompts.Clear();
                    _promptShowing = false;
                }

                _roomTransitioning = false;
                _loadingTransitioning = false;
                _selectedRoom = null;
                _rightPanelShowingLeaderboard = false;
                _roomViewController.HideMapStartCountdown();
                _loadingCancellation?.Cancel();
                RestoreMenuLeaderboard();
                _ludusSession.ReturnToPublicPresence();
                SetLeftScreenViewController(null, ViewController.AnimationType.None);
                SetRightScreenViewController(null, ViewController.AnimationType.None);
            }
        }

        protected override void BackButtonWasPressed(ViewController topViewController) {
            if (_loadingTransitioning) {
                return;
            }

            if (topViewController == _roomViewController) {
                BackToRooms();
                return;
            }

            if (topViewController == _roomListViewController) {
                BackToTournaments();
                return;
            }

            if (topViewController == _tournamentBrowserViewController || topViewController == _codeEntryViewController) {
                BackToModeSelection();
                return;
            }

            DidFinishEvent?.Invoke();
        }

        private void SelectBrowser() {
            LoadTournaments(true).RunTask();
        }

        private void SelectJoinViaCode() {
            _codeEntryViewController.Reset();
            PresentViewController(_codeEntryViewController);
        }

        private void RefreshTournaments() {
            LoadTournaments(false).RunTask();
        }

        private void SelectTournament(CompeteTournament tournament) {
            _selectedTournament = tournament;
            _roomListViewController.SetTournament(tournament);
            LoadRooms(true).RunTask();
        }

        private void RefreshRooms() {
            if (_selectedTournament == null) {
                return;
            }

            LoadRooms(false).RunTask();
        }

        private void SelectRoom(CompeteRoom room) {
            if (_roomTransitioning || _loadingTransitioning) {
                return;
            }

            EnterRoom(room, false, RoomJoinFailed).RunTask();
        }

        private void BackToModeSelection() {
            if (topViewController == _tournamentBrowserViewController || topViewController == _codeEntryViewController) {
                _selectedTournament = null;
                this.DismissView(topViewController).RunTask();
            }
        }

        private void BackToTournaments() {
            if (topViewController == _roomListViewController) {
                _selectedTournament = null;
                this.DismissView(_roomListViewController).RunTask();
            }
        }

        private void BackToRooms() {
            if (topViewController == _roomViewController) {
                LeaveRoomView(true);
            }
        }

        private void ToggleReady() {
            if (_selectedRoom == null) {
                return;
            }

            _ludusSession.SetReady(!_selectedRoom.LocalPlayerReady);
        }

        private void JoinViaCode(string code) {
            JoinViaCodeAsync(code).RunTask();
        }

        private async Task LoadTournaments(bool present) {
            if (present) {
                await PresentWithLoading(
                    _tournamentBrowserViewController,
                    "Loading tournaments...",
                    async token => {
                        IReadOnlyList<CompeteTournament> tournaments = await _directoryService.GetActiveTournaments(token);
                        await OnMainThread(() => _tournamentBrowserViewController.SetTournaments(tournaments));
                    });
                return;
            }

            try {
                IReadOnlyList<CompeteTournament> tournaments = await _directoryService.GetActiveTournaments(CancellationToken.None);
                await OnMainThread(() => _tournamentBrowserViewController.SetTournaments(tournaments));
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to refresh live tournaments: {ex.Message}");
            }
        }

        private async Task LoadRooms(bool present, string refreshingStatus = null, string finishedStatus = null) {
            if (_selectedTournament == null) {
                return;
            }

            if (present) {
                await PresentWithLoading(
                    _roomListViewController,
                    "Loading rooms...",
                    async token => {
                        IReadOnlyList<CompeteRoom> rooms = await _directoryService.GetJoinableRooms(_selectedTournament.Id, token);
                        await OnMainThread(() => _roomListViewController.SetRooms(rooms));
                    });
                return;
            }

            try {
                await OnMainThread(() => {
                    _roomListViewController.SetStatus(refreshingStatus);
                    _roomListViewController.SetRefreshing(true);
                });
                IReadOnlyList<CompeteRoom> rooms = await _directoryService.GetJoinableRooms(_selectedTournament.Id, CancellationToken.None);
                await OnMainThread(() => {
                    _roomListViewController.SetRooms(rooms);
                    _roomListViewController.SetStatus(finishedStatus ?? string.Empty);
                });
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to refresh live rooms: {ex.Message}");
                await OnMainThread(() => _roomListViewController.SetStatus("Couldn't refresh rooms."));
            } finally {
                await OnMainThread(() => _roomListViewController.SetRefreshing(false));
            }
        }

        private async Task EnterRoom(CompeteRoom room, bool roomAlreadyLoaded, Action<Exception> failed = null) {
            _roomTransitioning = true;
            await PresentWithLoading(
                _roomViewController,
                "Joining room...",
                async token => {
                    _selectedRoom = roomAlreadyLoaded
                        ? room
                        : await _directoryService.GetRoom(room.TournamentId, room.Id, token);
                    await _ludusSession.ConnectAndJoin(_selectedRoom, token);
                },
                () => {
                    _roomViewController.SetRoom(_selectedRoom);
                    _playerListViewController.SetRoom(_selectedRoom);
                    _gameplaySetupViewController.Setup(
                        showModifiers: false,
                        showEnvironmentOverrideSettings: true,
                        showColorSchemesSettings: true,
                        showMultiplayer: false,
                        PlayerSettingsPanelController.PlayerSettingsPanelLayout.Singleplayer);
                    SetLeftScreenViewController(_gameplaySetupViewController, ViewController.AnimationType.In);
                    ShowPlayersPanel(ViewController.AnimationType.In);
                },
                RoomTransitionFinished,
                failed);
        }

        private async Task JoinViaCodeAsync(string code) {
            try {
                _codeEntryViewController.SetStatus("Looking up room...");
                CompeteRoom room = await _directoryService.GetRoomByInviteCode(code, CancellationToken.None);
                _selectedTournament = null;
                _codeEntryViewController.SetStatus(string.Empty);
                await EnterRoom(room, true, _ => _codeEntryViewController.SetStatus("That room could not be joined."));
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to join live room by code: {ex.Message}");
                await OnMainThread(() => _codeEntryViewController.SetStatus("No room was found for that code."));
            }
        }

        private async Task PresentWithLoading(
            ViewController viewController,
            string loadingMessage,
            Func<CancellationToken, Task> load,
            Action beforeShowTarget = null,
            Action finishedCallback = null,
            Action<Exception> failedCallback = null) {

            if (_loadingTransitioning) {
                return;
            }

            _loadingCancellation?.Cancel();
            _loadingCancellation = new CancellationTokenSource();
            CancellationToken token = _loadingCancellation.Token;
            _loadingTransitioning = true;
            _loadingViewController.SetMessage(loadingMessage);
            PresentViewController(_loadingViewController);

            try {
                await Task.Delay(LoadingTransitionDelayMs, token);
                await load(token);
                await OnMainThread(() => {
                    if (token.IsCancellationRequested) {
                        _loadingTransitioning = false;
                        _roomTransitioning = false;
                        return;
                    }

                    beforeShowTarget?.Invoke();
                    ReplaceTopViewController(viewController, () => {
                        _loadingTransitioning = false;
                        finishedCallback?.Invoke();
                    });
                });
            } catch (OperationCanceledException) {
                _loadingTransitioning = false;
                _roomTransitioning = false;
            } catch (Exception ex) {
                Plugin.Log.Warn($"Live compete load failed: {ex.Message}");
                bool showJoinError = IsTournamentJoinBlockedStatus(ex.Message);
                if (showJoinError) {
                    await OnMainThread(() => _loadingViewController.SetMessage(ex.Message, false));
                    await Task.Delay(3000);
                }

                await OnMainThread(() => {
                    _loadingTransitioning = false;
                    _roomTransitioning = false;
                    failedCallback?.Invoke(ex);
                    if (topViewController == _loadingViewController) {
                        this.DismissView(_loadingViewController).RunTask();
                    }
                });
            }
        }

        private static Task OnMainThread(Action action) {
            return UnityMainThreadTaskScheduler.Factory.StartNew(action);
        }

        private void ReceivePrompt(CompeteOrganizerPrompt prompt) {
            lock (_promptLock) {
                _pendingPrompts.Enqueue(prompt);
            }

            UnityMainThreadTaskScheduler.Factory.StartNew(DrainPromptQueue).RunTask();
        }

        private void DrainPromptQueue() {
            if (_competeGameplayState.IsLiveGameplayActive || topViewController != _roomViewController || !_roomViewController.ReadyForPrompt || _roomTransitioning || _promptShowing) {
                return;
            }

            CompeteOrganizerPrompt prompt;
            lock (_promptLock) {
                if (_pendingPrompts.Count == 0) {
                    return;
                }

                prompt = _pendingPrompts.Dequeue();
                _promptShowing = true;
            }

            _roomViewController.ShowPrompt(prompt);
        }

        private void RoomTransitionFinished() {
            _roomTransitioning = false;
            DrainPromptQueue();
        }

        private void PromptWasAnswered(CompeteOrganizerPrompt prompt, bool accepted) {
            _ludusSession.SendPromptResponse(prompt, accepted);
            Plugin.Log.Debug($"Live tournament prompt answered: {accepted}");

            lock (_promptLock) {
                _promptShowing = false;
            }

            DrainPromptQueue();
        }

        private void RoomWasUpdated(CompeteRoom room) {
            if (_selectedRoom == null || room == null || room.Id != _selectedRoom.Id) {
                return;
            }

            bool songChanged = SongChanged(_selectedRoom.Song, room.Song);
            _selectedRoom = room;
            if (!CanRenderRoomUi()) {
                _roomUiPending = true;
                _roomUiPendingSongChanged |= songChanged;
                return;
            }

            RenderRoomUi(room, songChanged);
        }

        private bool CanRenderRoomUi() {
            return !_competeGameplayState.IsLiveGameplayActive && topViewController == _roomViewController;
        }

        private void RenderRoomUi(CompeteRoom room, bool songChanged) {
            _roomViewController.SetRoom(room);
            _playerListViewController.SetRoom(room);
            if (_rightPanelShowingLeaderboard && songChanged && !RefreshRoomLeaderboard()) {
                ShowPlayersPanel(ViewController.AnimationType.In);
            }
            _roomUiPending = false;
            _roomUiPendingSongChanged = false;
        }

        private void ApplyPendingRoomUi() {
            if (!_roomUiPending || _selectedRoom == null || !CanRenderRoomUi()) {
                return;
            }

            RenderRoomUi(_selectedRoom, _roomUiPendingSongChanged);
        }

        private void LiveGameplayActiveChanged(bool active) {
            if (active) {
                return;
            }

            if (_roomClosedDuringGameplay) {
                _roomClosedDuringGameplay = false;
                string status = _gameplayRoomCloseStatus;
                _gameplayRoomCloseStatus = null;
                HandleRoomWasClosed(status);
                return;
            }

            ApplyPendingRoomUi();
            DrainPromptQueue();
        }

        private void RoomWasClosed() {
            string roomCloseStatus = _roomCloseStatus;
            _roomCloseStatus = null;
            if (_competeGameplayState.IsLiveGameplayActive) {
                _roomClosedDuringGameplay = true;
                _gameplayRoomCloseStatus = roomCloseStatus;
                return;
            }

            HandleRoomWasClosed(roomCloseStatus);
        }

        private void HandleRoomWasClosed(string roomCloseStatus) {
            if (_roomTransitioning && topViewController == _loadingViewController && IsTournamentJoinBlockedStatus(roomCloseStatus)) {
                return;
            }

            if (topViewController == _roomViewController) {
                LeaveRoomView(false);
                RefreshRoomsAfterClose(roomCloseStatus);
                return;
            }

            if (_roomTransitioning && topViewController == _loadingViewController) {
                _loadingCancellation?.Cancel();
                _selectedRoom = null;
                _loadingTransitioning = false;
                _roomTransitioning = false;
                this.DismissView(_loadingViewController).RunTask();
                if (_selectedTournament == null) {
                    _codeEntryViewController.SetStatus(roomCloseStatus ?? "That room could not be joined.");
                } else {
                    RefreshRoomsAfterClose(roomCloseStatus);
                }
            }
        }

        private void RoomJoinFailed(Exception ex) {
            Plugin.Log.Warn($"Failed to join live room: {ex.Message}");
            string status = IsTournamentJoinBlockedStatus(ex.Message) ? ex.Message : "That room could not be joined.";
            LoadRooms(false, $"{status} Refreshing rooms...", status).RunTask();
        }

        private void RefreshRoomsAfterClose(string status = null) {
            if (_selectedTournament == null) {
                _codeEntryViewController.SetStatus(status ?? "Room closed.");
                return;
            }

            LoadRooms(false, status ?? "Room closed. Refreshing rooms...", status).RunTask();
        }

        private void LeaveRoomView(bool returnToPublicPresence) {
            ClearPrompts();
            _selectedRoom = null;
            _roomUiPending = false;
            _roomUiPendingSongChanged = false;
            _rightPanelShowingLeaderboard = false;
            _roomViewController.HideMapStartCountdown();
            RestoreMenuLeaderboard();
            if (returnToPublicPresence) {
                _ludusSession.ReturnToPublicPresence();
            }
            SetLeftScreenViewController(null, ViewController.AnimationType.Out);
            SetRightScreenViewController(null, ViewController.AnimationType.Out);
            _roomTransitioning = true;
            this.DismissView(_roomViewController, RoomTransitionFinished).RunTask();
        }

        private void ShowPlayersPanel() => ShowPlayersPanel(ViewController.AnimationType.In);

        private void ShowLeaderboardPanel() => ShowLeaderboardPanel(ViewController.AnimationType.In);

        private void ShowPlayersPanel(ViewController.AnimationType animationType) {
            _rightPanelShowingLeaderboard = false;
            SetRightScreenViewController(_playerListViewController, animationType);
        }

        private void ShowLeaderboardPanel(ViewController.AnimationType animationType) {
            _rightPanelShowingLeaderboard = true;
            if (!RefreshRoomLeaderboard()) {
                ShowPlayersPanel(animationType);
                return;
            }

            SetRightScreenViewController(_platformLeaderboardViewController, animationType);
        }

        private bool RefreshRoomLeaderboard() {
            CompeteSongSelection song = _selectedRoom?.Song;
            if (song == null || !ScoreSaberBeatmapKey.IsSupported(song.BeatmapKey)) {
                return false;
            }

            _platformLeaderboardViewController.SetData(song.BeatmapKey);
            return true;
        }

        private void RestoreMenuLeaderboard() {
            BeatmapKey beatmapKey = _levelSelectionNavigationController.GetBeatmapKey();
            if (ScoreSaberBeatmapKey.IsSupported(beatmapKey)) {
                _platformLeaderboardViewController.SetData(beatmapKey);
                return;
            }

            _leaderboardSession.ClearBeatmap();
        }

        private static bool SongChanged(CompeteSongSelection previous, CompeteSongSelection next) {
            if (ReferenceEquals(previous, next)) {
                return false;
            }

            if (previous == null || next == null) {
                return previous != next;
            }

            if (!string.Equals(previous.MapHash, next.MapHash, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            return !string.Equals(previous.Difficulty, next.Difficulty, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(previous.Characteristic, next.Characteristic, StringComparison.OrdinalIgnoreCase);
        }

        private void MapStartCountdownWasChanged(CompeteMapStartCountdown countdown) {
            if (_competeGameplayState.IsLiveGameplayActive) {
                return;
            }

            if (countdown == null) {
                _roomViewController.HideMapStartCountdown();
                return;
            }

            if (_selectedRoom == null || countdown.MatchId != _selectedRoom.Id || topViewController != _roomViewController) {
                return;
            }

            _roomViewController.ShowMapStartCountdown(countdown);
        }

        private void LudusStatusChanged(string status) {
            if (IsTournamentJoinBlockedStatus(status)) {
                _roomCloseStatus = status;
            }

            Plugin.Log.Debug($"Ludus: {status}");
        }

        private static bool IsTournamentJoinBlockedStatus(string status) {
            if (string.IsNullOrEmpty(status)) {
                return false;
            }

            return status.IndexOf("denied mods", StringComparison.OrdinalIgnoreCase) >= 0 ||
                status.IndexOf("requires ScoreSaber to report installed mods", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ClearPrompts() {
            _roomViewController.ClearPrompt();

            lock (_promptLock) {
                _pendingPrompts.Clear();
                _promptShowing = false;
            }
        }

        private void SubscribeTournamentBrowserEvents() {
            if (_tournamentBrowserEventsSubscribed) {
                return;
            }

            _tournamentBrowserViewController.RefreshRequested += RefreshTournaments;
            _tournamentBrowserViewController.TournamentSelected += SelectTournament;
            _tournamentBrowserEventsSubscribed = true;
        }

        private void UnsubscribeTournamentBrowserEvents() {
            if (!_tournamentBrowserEventsSubscribed) {
                return;
            }

            _tournamentBrowserViewController.RefreshRequested -= RefreshTournaments;
            _tournamentBrowserViewController.TournamentSelected -= SelectTournament;
            _tournamentBrowserEventsSubscribed = false;
        }
    }
}
