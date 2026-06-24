using ScoreSaber.Core;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Compete.Packets;
using ScoreSaber.Features.Live.Compete.Services;
using ScoreSaber.Features.Live.Ludus.Domain;
using ScoreSaber.Features.Live.Ludus.Packets;
using ScoreSaber.Features.Live.Protocol;
using ScoreSaber.Features.Live.Replay;
using ScoreSaber.Features.Players.Domain;
using ScoreSaber.Features.Players.Services;
using ScoreSaber.Live.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Live.Ludus.Services {
    internal class LudusSessionService : IInitializable, ITickable, IDisposable {
        private const float ReconnectMinDelaySeconds = 0.5f;
        private const float ReconnectMaxDelaySeconds = 10f;
        private static readonly TimeSpan GameSessionReconnectRefreshInterval = TimeSpan.FromHours(3);
        private static readonly TimeSpan GameSessionRefreshRetryDelay = TimeSpan.FromMinutes(10);
        private static readonly Regex DisplayMarkupTagPattern = new Regex(@"<[^>\r\n]{1,128}>", RegexOptions.Compiled);

        internal event Action<int> PlayerFollowRequested;
        internal event Action<IReadOnlyList<LiveRoomViewerState>> ViewerListUpdated;
        internal event Action<CompeteRoom> RoomUpdated;
        internal event Action<CompeteOrganizerPrompt> PromptReceived;
        internal event Action<CompeteMapStartCountdown> MapStartCountdownChanged;
        internal event Action<IReadOnlyList<LiveChatEntry>> ChatMessagesChanged;
        internal event Action<string> StatusChanged;

        private readonly SettingsService _settings;
        private readonly GameSessionService _gameSessionService;
        private readonly ScoreSaberRuntimeInfo _runtimeInfo;
        private readonly LiveReplayStreamingService _replayStreamingService;
        private readonly LudusMainThreadQueue _mainThread;
        private readonly LudusSessionTransport _transport;
        private readonly LudusPacketSender _outgoing;
        private readonly LudusMapStartCountdown _mapStartCountdown;
        private readonly LudusChatMessageBuffer _chatMessages;
        private readonly ILudusSessionPacketContext _packetContext;
        private readonly LudusPacketDispatcher<ILudusSessionPacketContext> _packetDispatcher;

        private CompeteRoom _tournamentRoom;
        private CompeteRoom _pendingTournamentRoom;
        private IReadOnlyList<LiveRoomViewerState> _currentViewers = Array.Empty<LiveRoomViewerState>();
        private string _connectionId;
        private string _currentMatchId;
        private string _currentTournamentId;
        private Task _connectTask;
        private float _heartbeatIntervalSeconds = 5f;
        private float _nextHeartbeatAt;
        private string _nextLudusUrl;
        private float _nextReconnectAt;
        private int _reconnectAttempt;
        private bool _reconnectScheduled;
        private bool _active;
        private bool _forceAuthenticationRefreshOnNextConnect;
        private DateTime _lastConnectionAuthRefreshAtUtc = DateTime.MinValue;
        private DateTime _nextConnectionAuthRefreshAttemptAtUtc = DateTime.MinValue;
        private LudusRoomContextType _roomContext = LudusRoomContextType.LudusRoomContextTypeUnspecified;
        private LudusClientType _clientType = LudusClientType.LudusClientTypePlayer;

        internal LudusSessionService(
            SettingsService settings,
            GameSessionService gameSessionService,
            ScoreSaberRuntimeInfo runtimeInfo,
            CompeteSongService songService,
            CompeteDirectoryService directoryService,
            CompeteGameplayLauncher gameplayLauncher,
            CompeteGameplayControl gameplayControl,
            LiveReplayStreamingService replayStreamingService) {

            _settings = settings;
            _gameSessionService = gameSessionService;
            _runtimeInfo = runtimeInfo;
            _replayStreamingService = replayStreamingService;
            _mainThread = new LudusMainThreadQueue();
            _transport = new LudusSessionTransport(_mainThread);
            _outgoing = new LudusPacketSender(_transport.Send);
            _mapStartCountdown = new LudusMapStartCountdown(_mainThread, () => _tournamentRoom?.Id ?? string.Empty);
            ILudusServerCommandSession commandSession = new CompeteLudusCommandSession(
                () => LocalPlayerId,
                () => _tournamentRoom,
                room => _tournamentRoom = room,
                () => _transport.Token,
                songService,
                directoryService,
                gameplayLauncher,
                gameplayControl,
                viewerCount => PlayerFollowRequested?.Invoke(viewerCount),
                UpdateViewerList,
                room => RoomUpdated?.Invoke(room),
                prompt => PromptReceived?.Invoke(prompt),
                status => StatusChanged?.Invoke(status),
                SendDownloadState,
                SendPresence,
                _mapStartCountdown.Begin,
                _mapStartCountdown.TryCancel,
                _mapStartCountdown.Complete);
            _chatMessages = new LudusChatMessageBuffer();
            _packetContext = new LudusSessionPacketContext(
                () => _outgoing.LastReceivedSequence,
                sequence => _outgoing.LastReceivedSequence = sequence,
                () => _connectionId,
                connectionId => _connectionId = connectionId,
                () => _heartbeatIntervalSeconds,
                heartbeatIntervalSeconds => _heartbeatIntervalSeconds = heartbeatIntervalSeconds,
                () => _clientType,
                () => _roomContext,
                () => CurrentLudusMatchId,
                () => _pendingTournamentRoom,
                ApplyClientContext,
                EnterTournamentRoom,
                RequestAuthenticationRefresh,
                () => _nextHeartbeatAt = Time.realtimeSinceStartup + _heartbeatIntervalSeconds,
                (reason, delayOverrideSeconds) => ScheduleReconnect(reason, delayOverrideSeconds),
                SendPresence,
                url => _nextLudusUrl = url,
                messages => ChatMessagesChanged?.Invoke(messages),
                status => StatusChanged?.Invoke(status));
            _packetDispatcher = CompeteLudusPacketDispatcher.CreateDefault(commandSession, _chatMessages);

            _transport.MessageReceived += bytes => _packetDispatcher.Handle(_packetContext, bytes);
            _transport.ReceiveFailed += ReceiveFailed;
            _transport.SendFailed += SendFailed;
            _transport.ReconnectRequested += reason => ScheduleReconnect(reason, null);
            _transport.Disconnected += TransportDisconnected;
            _mapStartCountdown.Changed += countdown => MapStartCountdownChanged?.Invoke(countdown);
        }

        internal bool IsConnectedToLudus => _transport.IsOpen && !string.IsNullOrEmpty(_connectionId);
        internal bool IsInTournamentRoom => IsConnectedToLudus && _roomContext == LudusRoomContextType.LudusRoomContextTypeTournament && !string.IsNullOrEmpty(_currentMatchId);
        internal bool IsInPublicPresence => IsConnectedToLudus && _roomContext == LudusRoomContextType.LudusRoomContextTypePublicPresence;
        internal string CurrentLudusMatchId => _currentMatchId ?? string.Empty;
        internal string LocalPlayerId => GetLocalPlayerId();
        internal string ScoreSaberPlayerId => _gameSessionService.GameSession?.PlayerId ?? string.Empty;
        internal string GameSessionId => _gameSessionService.GameSession?.SessionId ?? string.Empty;
        internal string LocalAuthType => _gameSessionService.LocalPlayerInfo?.authType ?? string.Empty;
        internal IReadOnlyList<LiveRoomViewerState> CurrentViewers => _currentViewers;
        internal int CurrentViewerCount => _currentViewers.Count;
        internal IReadOnlyList<LiveChatEntry> CurrentChatMessages => _chatMessages.CurrentMessages;

        public void Initialize() {
            Plugin.Log.Info("Ludus session initialized.");
            _gameSessionService.LoginStatusChanged += GameSessionStatusChanged;
            _replayStreamingService.AttachLudus(this);
            if (_gameSessionService.HasAuthenticatedSession || _gameSessionService.Status == GameSessionService.LoginStatus.Success) {
                EnsureSessionConnection(CancellationToken.None).RunTask();
            }
        }

        public void Tick() {
            _mainThread.Drain();
            ReconnectIfDue();
            SendHeartbeatIfDue();
        }

        public void Dispose() {
            _gameSessionService.LoginStatusChanged -= GameSessionStatusChanged;
            _replayStreamingService.AttachLudus(null);
            Disconnect();
        }

        internal async Task ConnectAndJoin(CompeteRoom room, CancellationToken cancellationToken) {
            if (room == null) {
                throw new ArgumentNullException(nameof(room));
            }

            _tournamentRoom = room;
            _pendingTournamentRoom = room;

            await EnsureSessionConnection(cancellationToken);
            if (IsConnectedToLudus) {
                EnterTournamentRoom(room);
            }
        }

        internal void Disconnect() {
            _active = false;
            _roomContext = LudusRoomContextType.LudusRoomContextTypeUnspecified;
            _reconnectScheduled = false;
            _nextReconnectAt = 0f;
            _reconnectAttempt = 0;
            _pendingTournamentRoom = null;
            _tournamentRoom = null;
            UpdateViewerList(null);
            ClearChatMessages();
            _nextLudusUrl = null;
            _mapStartCountdown.Cancel();

            _transport.DisposeSocket();
            _connectionId = null;
            _clientType = LudusClientType.LudusClientTypePlayer;
            _currentMatchId = string.Empty;
            _currentTournamentId = string.Empty;
            _connectTask = null;
        }

        internal void ReturnToPublicPresence() {
            _mapStartCountdown.Cancel();
            _pendingTournamentRoom = null;
            _tournamentRoom = null;
            UpdateViewerList(null);
            ClearChatMessages();
            _nextLudusUrl = null;

            ApplyDefaultSessionRoomContext();
        }

        internal void ApplyPublicLivePresencePreference() {
            if (_roomContext == LudusRoomContextType.LudusRoomContextTypeTournament || _tournamentRoom != null || _pendingTournamentRoom != null) {
                Plugin.Log.Info("Ludus: Public live presence preference updated; keeping current tournament room.");
                return;
            }

            if (_settings.Current.publicLivePresenceOptOut) {
                _replayStreamingService.StopPublicPresenceStream();
            }

            ApplyDefaultSessionRoomContext();
        }

        private void EnterTournamentRoom(CompeteRoom room) {
            if (room == null || !IsConnectedToLudus) {
                return;
            }

            _pendingTournamentRoom = null;
            _tournamentRoom = room;
            RequestClientType(LudusClientType.LudusClientTypePlayer);
            ApplyRoomContext(LudusRoomContextType.LudusRoomContextTypeTournament, room.TournamentId, room.Id);
            _outgoing.SetRoomContext(LudusRoomContextType.LudusRoomContextTypeTournament, room.TournamentId, _connectionId);
            _outgoing.JoinRoom(room.Id, _connectionId);
        }

        private void ApplyRoomContext(LudusRoomContextType roomContext, string tournamentId, string currentMatchId) {
            _roomContext = roomContext;
            _currentTournamentId = tournamentId ?? string.Empty;
            _currentMatchId = ResolveCurrentMatchId(roomContext, currentMatchId);
        }

        private void ApplyClientContext(DecodedLudusEnvelope envelope) {
            _clientType = NormalizeClientType(envelope.ClientType);
            ApplyRoomContext(envelope.RoomContext, envelope.TournamentId, envelope.CurrentMatchId);
        }

        private void RequestAuthenticationRefresh() {
            _forceAuthenticationRefreshOnNextConnect = true;
            _nextConnectionAuthRefreshAttemptAtUtc = DateTime.MinValue;
        }

        private void UpdateViewerList(IReadOnlyList<LiveRoomViewerState> viewers) {
            _currentViewers = viewers == null ? Array.Empty<LiveRoomViewerState>() : viewers.ToArray();
            ViewerListUpdated?.Invoke(_currentViewers);
        }

        private void RequestClientType(LudusClientType clientType) {
            if (!IsConnectedToLudus || _clientType == clientType) {
                return;
            }

            _outgoing.SetClientType(clientType, _connectionId);
        }

        internal void SetReady(bool ready) {
            if (_tournamentRoom == null || string.IsNullOrEmpty(_connectionId)) {
                return;
            }

            _outgoing.ReadyState(_tournamentRoom.Id, ready, _connectionId);
            _tournamentRoom = _tournamentRoom.WithPlayers(_tournamentRoom.Players.Select(player =>
                player.IsLocalPlayer
                    ? new CompetePlayer(player.Name, ready ? "Ready" : "Waiting", player.TeamId, player.Rank, true, player.PlayerId, player.IsBot, player.AvatarUrl)
                    : player), ready);
            RoomUpdated?.Invoke(_tournamentRoom);
        }

        internal void SendPromptResponse(CompeteOrganizerPrompt prompt, bool accepted) {
            if (prompt == null || string.IsNullOrEmpty(_connectionId)) {
                return;
            }

            _outgoing.PromptResponse(prompt, string.IsNullOrEmpty(prompt.MatchId) ? _tournamentRoom?.Id : prompt.MatchId, GetLocalPlayerId(), accepted, _connectionId);
        }

        internal bool SendChatMessage(string text) {
            if (!IsConnectedToLudus || string.IsNullOrEmpty(_currentMatchId) || string.IsNullOrWhiteSpace(text)) {
                return false;
            }

            _outgoing.ChatMessage(_currentMatchId, text.Trim(), LocalPlayerDisplayName(), _connectionId);
            return true;
        }

        internal bool TrySetLinkedSong(CompeteSongSelection song) {
            if (song == null || _tournamentRoom == null) {
                return false;
            }

            _tournamentRoom = _tournamentRoom.WithSong(song);
            RoomUpdated?.Invoke(_tournamentRoom);
            return true;
        }

        internal void SendPresence(LudusPlayState playState, LudusDownloadState downloadState, string currentMapHash) {
            if (!IsConnectedToLudus || string.IsNullOrEmpty(_currentMatchId)) {
                return;
            }

            _outgoing.Presence(playState, downloadState, _currentMatchId, currentMapHash, _connectionId);
        }

        private void SendDownloadState(LudusDownloadState state, string errorMessage) {
            if (_tournamentRoom != null) {
                _outgoing.DownloadState(_tournamentRoom.Id, state, errorMessage, _connectionId);
            }
        }

        internal void SendReplayPacket(ReplayStreamPacket packet) {
            if (packet == null || !IsConnectedToLudus) {
                return;
            }

            if (string.IsNullOrEmpty(packet.PlayerId)) {
                packet.PlayerId = GetLocalPlayerId();
            }
            if (string.IsNullOrEmpty(packet.MatchId)) {
                packet.MatchId = _currentMatchId;
            }

            _outgoing.ReplayPacket(packet, _connectionId);
        }

        private string GetLocalPlayerId() => _gameSessionService.LocalPlayerInfo?.playerId ?? _gameSessionService.GameSession?.PlayerId ?? string.Empty;

        private string PublicPresenceMatchId() {
            string playerId = GetLocalPlayerId();
            return string.IsNullOrEmpty(playerId) ? string.Empty : $"player:{playerId}";
        }

        private Task EnsureSessionConnection(CancellationToken cancellationToken) {
            if (IsConnectedToLudus) {
                return Task.CompletedTask;
            }

            if (_connectTask != null && !_connectTask.IsCompleted) {
                return _connectTask;
            }

            _connectTask = OpenSessionConnection(cancellationToken);
            return _connectTask;
        }

        private async Task OpenSessionConnection(CancellationToken cancellationToken) {
            if (IsConnectedToLudus) {
                return;
            }

            bool hadCachedSession = _gameSessionService.HasAuthenticatedSession;
            bool forceAuthenticationRefresh = ShouldRefreshAuthenticationForConnection();
            bool authenticated = await _gameSessionService.EnsureAuthenticated(forceAuthenticationRefresh, cancellationToken);
            bool usedCachedSessionAfterRefreshFailure = false;
            if (!authenticated && hadCachedSession && _gameSessionService.HasAuthenticatedSession && !AuthenticationRefreshIsRequired()) {
                _nextConnectionAuthRefreshAttemptAtUtc = DateTime.UtcNow + GameSessionRefreshRetryDelay;
                authenticated = true;
                usedCachedSessionAfterRefreshFailure = true;
                Plugin.Log.Warn("Ludus: Game session refresh failed; retrying cached session.");
            }
            if (!authenticated || !_gameSessionService.HasAuthenticatedSession) {
                throw new InvalidOperationException("ScoreSaber game session is not available");
            }
            MarkAuthenticationAvailable(forceAuthenticationRefresh && !usedCachedSessionAfterRefreshFailure);

            PrepareConnectionAttempt(cancellationToken);

            try {
                string url = NormalizeLudusUrl(_nextLudusUrl ?? ScoreSaberEndpoints.LudusUrl);
                Plugin.Log.Info($"Ludus: Connecting to {url}");
                await _transport.ConnectAsync(new Uri(url));
                _reconnectAttempt = 0;
                SendConnect();
                _transport.StartReceiveLoop();
            } catch (OperationCanceledException) {
                _transport.DisposeSocket();
                throw;
            } catch (Exception ex) {
                _transport.DisposeSocket();
                ScheduleReconnect(ex.Message, null);
                Plugin.Log.Warn($"Ludus connection failed: {ex.Message}");
                throw;
            }
        }

        private bool ShouldRefreshAuthenticationForConnection() {
            if (!_gameSessionService.HasAuthenticatedSession) {
                _forceAuthenticationRefreshOnNextConnect = false;
                return false;
            }

            DateTime now = DateTime.UtcNow;
            if (_forceAuthenticationRefreshOnNextConnect) {
                return true;
            }
            if (_lastConnectionAuthRefreshAtUtc == DateTime.MinValue) {
                return false;
            }
            if (now < _nextConnectionAuthRefreshAttemptAtUtc) {
                return false;
            }
            return now - _lastConnectionAuthRefreshAtUtc >= GameSessionReconnectRefreshInterval;
        }

        private bool AuthenticationRefreshIsRequired() {
            return _forceAuthenticationRefreshOnNextConnect;
        }

        private void MarkAuthenticationAvailable(bool refreshAttempted) {
            if (refreshAttempted || _lastConnectionAuthRefreshAtUtc == DateTime.MinValue) {
                _lastConnectionAuthRefreshAtUtc = DateTime.UtcNow;
                _nextConnectionAuthRefreshAttemptAtUtc = DateTime.MinValue;
            }
            if (refreshAttempted) {
                _forceAuthenticationRefreshOnNextConnect = false;
            }
        }

        private void GameSessionStatusChanged(GameSessionService.LoginStatus status, string _) {
            if (status != GameSessionService.LoginStatus.Success) {
                return;
            }

            Plugin.Log.Info("Ludus: authenticated session available, connecting session.");
            EnsureSessionConnection(CancellationToken.None).RunTask();
        }

        private void ReceiveFailed(string message) {
            StatusChanged?.Invoke($"Ludus receive failed: {message}");
            Plugin.Log.Warn($"Ludus receive failed: {message}");
        }

        private void SendFailed(string message) {
            StatusChanged?.Invoke($"Ludus send failed: {message}");
            Plugin.Log.Warn($"Ludus send failed: {message}");
        }

        private void TransportDisconnected() {
            if (!_active) {
                return;
            }

            StatusChanged?.Invoke("Ludus disconnected");
            Plugin.Log.Warn("Ludus disconnected.");
            ScheduleReconnect("disconnected", null);
        }

        private void ReconnectIfDue() {
            if (!_active || !_reconnectScheduled || Time.realtimeSinceStartup < _nextReconnectAt) {
                return;
            }

            _reconnectScheduled = false;
            _nextReconnectAt = 0f;
            EnsureSessionConnection(CancellationToken.None).RunTask();
        }

        private void SendHeartbeatIfDue() {
            if (!CanSendHeartbeat || Time.realtimeSinceStartup < _nextHeartbeatAt) {
                return;
            }

            _outgoing.Heartbeat(_connectionId);
            _nextHeartbeatAt = Time.realtimeSinceStartup + _heartbeatIntervalSeconds;
        }

        private void SendConnect() {
            GameSession session = _gameSessionService.GameSession;

            _outgoing.Connect(
                session,
                LocalPlatform(),
                _runtimeInfo.GameVersion.ToString(),
                _runtimeInfo.PluginVersion.ToString(),
                DefaultSessionRoomContext(),
                _settings.Current.publicLivePresenceOptOut);
        }

        private void PrepareConnectionAttempt(CancellationToken cancellationToken) {
            _active = true;
            _outgoing.ResetSequences();
            _connectionId = null;
            _heartbeatIntervalSeconds = 5f;
            _nextHeartbeatAt = 0f;
            _reconnectScheduled = false;
            _nextReconnectAt = 0f;
            _transport.Prepare(cancellationToken);
        }

        private void ScheduleReconnect(string reason, float? delayOverrideSeconds) {
            if (!_active || _reconnectScheduled) {
                return;
            }

            PreserveTournamentRoomForReconnect();
            ResetSocketSessionContext();
            float delay = delayOverrideSeconds ?? Mathf.Min(ReconnectMaxDelaySeconds, ReconnectMinDelaySeconds * Mathf.Pow(2f, _reconnectAttempt));
            if (!delayOverrideSeconds.HasValue) {
                _reconnectAttempt++;
            }

            _nextReconnectAt = Time.realtimeSinceStartup + delay;
            _reconnectScheduled = true;
            string status = string.IsNullOrEmpty(reason) ? $"Ludus reconnecting in {delay:0.#}s" : $"Ludus reconnecting in {delay:0.#}s: {reason}";
            Plugin.Log.Warn(status);
        }

        private void PreserveTournamentRoomForReconnect() {
            if (_tournamentRoom != null && _roomContext == LudusRoomContextType.LudusRoomContextTypeTournament) {
                _pendingTournamentRoom = _tournamentRoom;
            }
        }

        private void ResetSocketSessionContext() {
            _transport.DisposeSocket();
            _connectionId = null;
            _clientType = LudusClientType.LudusClientTypePlayer;
            _roomContext = LudusRoomContextType.LudusRoomContextTypeUnspecified;
            UpdateViewerList(null);
        }

        private void ApplyDefaultSessionRoomContext() {
            if (!IsConnectedToLudus) {
                EnsureSessionConnection(CancellationToken.None).RunTask();
                return;
            }

            LudusRoomContextType roomContext = DefaultSessionRoomContext();
            _outgoing.SetRoomContext(roomContext, string.Empty, _connectionId);
            RequestClientType(LudusClientType.LudusClientTypePlayer);
            ApplyRoomContext(roomContext, string.Empty, roomContext == LudusRoomContextType.LudusRoomContextTypePublicPresence ? PublicPresenceMatchId() : string.Empty);
            if (IsInPublicPresence) {
                SendPresence(LudusPlayState.LudusPlayStateInMenus, LudusDownloadState.LudusDownloadStateNone, string.Empty);
            }
        }

        private LudusRoomContextType DefaultSessionRoomContext() {
            return _settings.Current.publicLivePresenceOptOut
                ? LudusRoomContextType.LudusRoomContextTypeCore
                : LudusRoomContextType.LudusRoomContextTypePublicPresence;
        }

        private string ResolveCurrentMatchId(LudusRoomContextType roomContext, string currentMatchId) {
            if (!string.IsNullOrEmpty(currentMatchId)) {
                return currentMatchId;
            }

            if (roomContext == LudusRoomContextType.LudusRoomContextTypePublicPresence) {
                return PublicPresenceMatchId();
            }

            if (roomContext == LudusRoomContextType.LudusRoomContextTypeTournament) {
                return _tournamentRoom?.Id ?? string.Empty;
            }

            return string.Empty;
        }

        private bool CanSendHeartbeat => _active && IsConnectedToLudus;

        private void ClearChatMessages() {
            if (_chatMessages.Clear()) {
                ChatMessagesChanged?.Invoke(CurrentChatMessages);
            }
        }

        private string LocalPlayerDisplayName() {
            string name = CleanDisplayName(_gameSessionService.LocalPlayerInfo?.playerName);
            return string.IsNullOrWhiteSpace(name)
                ? "Player"
                : name;
        }

        private static string CleanDisplayName(string value) {
            if (string.IsNullOrEmpty(value)) {
                return string.Empty;
            }

            return string.Join(" ", DisplayMarkupTagPattern.Replace(value, string.Empty).Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private LivePlayerPlatform LocalPlatform() => _gameSessionService.LocalPlayerInfo?.authType switch {
            "0" => LivePlayerPlatform.LivePlayerPlatformSteam,
            "1" => LivePlayerPlatform.LivePlayerPlatformOculus,
            _ => LivePlayerPlatform.LivePlayerPlatformUnspecified
        };

        private static LudusClientType NormalizeClientType(LudusClientType clientType) {
            return clientType == LudusClientType.LudusClientTypeUnspecified
                ? LudusClientType.LudusClientTypePlayer
                : clientType;
        }

        private static string NormalizeLudusUrl(string configuredUrl) {
            string value = configuredUrl.Trim();
            if (!value.Contains("://")) {
                value = $"wss://{value}";
            } else if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
                value = $"wss://{value.Substring("https://".Length)}";
            } else if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) {
                value = $"ws://{value.Substring("http://".Length)}";
            }

            var uri = new Uri(value);
            if (string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/") {
                var builder = new UriBuilder(uri) {
                    Path = "v1/connect"
                };
                return builder.Uri.ToString();
            }

            return value;
        }
    }
}
