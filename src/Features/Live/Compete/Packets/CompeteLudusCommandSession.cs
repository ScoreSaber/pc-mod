using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Compete.Services;
using ScoreSaber.Live.V1;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ScoreSaber.Features.Live.Compete.Packets {
    internal sealed class CompeteLudusCommandSession : ILudusServerCommandSession {
        private readonly Func<string> _localPlayerId;
        private readonly Func<CompeteRoom> _getTournamentRoom;
        private readonly Action<CompeteRoom> _setTournamentRoom;
        private readonly Func<CancellationToken> _connectionCancellationToken;
        private readonly Action<int> _playerFollowRequested;
        private readonly Action<IReadOnlyList<LiveRoomViewerState>> _viewersUpdated;
        private readonly Action<CompeteRoom> _roomUpdated;
        private readonly Action<CompeteOrganizerPrompt> _promptReceived;
        private readonly Action<string> _statusChanged;
        private readonly Action _closeTournamentRoom;
        private readonly Action<LudusDownloadState, string> _sendDownloadState;
        private readonly Action<LudusPlayState, LudusDownloadState, string> _sendPresence;
        private readonly Func<string, int, CancellationToken, CancellationToken> _beginMapStartCountdown;
        private readonly Func<string, bool> _tryCancelPendingMapStart;
        private readonly Action<string> _completePendingMapStart;

        internal CompeteLudusCommandSession(
            Func<string> localPlayerId,
            Func<CompeteRoom> getTournamentRoom,
            Action<CompeteRoom> setTournamentRoom,
            Func<CancellationToken> connectionCancellationToken,
            CompeteSongService songService,
            CompeteDirectoryService directoryService,
            CompeteGameplayLauncher gameplayLauncher,
            CompeteGameplayControl gameplayControl,
            Action<int> playerFollowRequested,
            Action<IReadOnlyList<LiveRoomViewerState>> viewersUpdated,
            Action<CompeteRoom> roomUpdated,
            Action<CompeteOrganizerPrompt> promptReceived,
            Action<string> statusChanged,
            Action closeTournamentRoom,
            Action<LudusDownloadState, string> sendDownloadState,
            Action<LudusPlayState, LudusDownloadState, string> sendPresence,
            Func<string, int, CancellationToken, CancellationToken> beginMapStartCountdown,
            Func<string, bool> tryCancelPendingMapStart,
            Action<string> completePendingMapStart) {

            _localPlayerId = localPlayerId;
            _getTournamentRoom = getTournamentRoom;
            _setTournamentRoom = setTournamentRoom;
            _connectionCancellationToken = connectionCancellationToken;
            SongService = songService;
            DirectoryService = directoryService;
            GameplayLauncher = gameplayLauncher;
            GameplayControl = gameplayControl;
            _playerFollowRequested = playerFollowRequested;
            _viewersUpdated = viewersUpdated;
            _roomUpdated = roomUpdated;
            _promptReceived = promptReceived;
            _statusChanged = statusChanged;
            _closeTournamentRoom = closeTournamentRoom;
            _sendDownloadState = sendDownloadState;
            _sendPresence = sendPresence;
            _beginMapStartCountdown = beginMapStartCountdown;
            _tryCancelPendingMapStart = tryCancelPendingMapStart;
            _completePendingMapStart = completePendingMapStart;
        }

        public string LocalPlayerId => _localPlayerId();

        public CompeteRoom TournamentRoom {
            get => _getTournamentRoom();
            set => _setTournamentRoom(value);
        }

        public CancellationToken ConnectionCancellationToken => _connectionCancellationToken();
        public CompeteSongService SongService { get; }
        public CompeteDirectoryService DirectoryService { get; }
        public CompeteGameplayLauncher GameplayLauncher { get; }
        public CompeteGameplayControl GameplayControl { get; }
        public void NotifyPlayerFollowRequested(int viewerCount) => _playerFollowRequested(viewerCount);
        public void NotifyViewersUpdated(IReadOnlyList<LiveRoomViewerState> viewers) => _viewersUpdated(viewers);
        public void NotifyRoomUpdated(CompeteRoom room) => _roomUpdated(room);
        public void NotifyPromptReceived(CompeteOrganizerPrompt prompt) => _promptReceived(prompt);
        public void NotifyStatusChanged(string status) => _statusChanged(status);
        public void CloseTournamentRoom() => _closeTournamentRoom();
        public void SendDownloadState(LudusDownloadState state, string errorMessage = "") => _sendDownloadState(state, errorMessage);
        public void SendPresence(LudusPlayState playState, LudusDownloadState downloadState, string currentMapHash) => _sendPresence(playState, downloadState, currentMapHash);
        public CancellationToken BeginMapStartCountdown(string matchId, int delayMs, CancellationToken cancellationToken) =>
            _beginMapStartCountdown(matchId, delayMs, cancellationToken);
        public bool TryCancelPendingMapStart(string matchId) => _tryCancelPendingMapStart(matchId);
        public void CompletePendingMapStart(string matchId) => _completePendingMapStart(matchId);
    }
}
