using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Ludus.Domain;
using ScoreSaber.Features.Live.Protocol;
using ScoreSaber.Live.V1;
using System;
using System.Collections.Generic;

namespace ScoreSaber.Features.Live.Ludus.Services {
    internal sealed class LudusSessionPacketContext : ILudusSessionPacketContext {
        private readonly Func<ulong> _getLastReceivedSequence;
        private readonly Action<ulong> _setLastReceivedSequence;
        private readonly Func<string> _getConnectionId;
        private readonly Action<string> _setConnectionId;
        private readonly Func<float> _getHeartbeatIntervalSeconds;
        private readonly Action<float> _setHeartbeatIntervalSeconds;
        private readonly Func<LudusClientType> _getClientType;
        private readonly Func<LudusRoomContextType> _getRoomContext;
        private readonly Func<string> _getCurrentLudusMatchId;
        private readonly Func<CompeteRoom> _getPendingTournamentRoom;
        private readonly Action<DecodedLudusEnvelope> _applyClientContext;
        private readonly Action _closeTournamentRoom;
        private readonly Action _disconnect;
        private readonly Action<CompeteRoom> _enterTournamentRoom;
        private readonly Func<bool> _requestAuthenticationRefresh;
        private readonly Action _scheduleNextHeartbeat;
        private readonly Action<string, float?> _scheduleReconnect;
        private readonly Action<LudusPlayState, LudusDownloadState, string> _sendPresence;
        private readonly Action<string> _setReconnectUrl;
        private readonly Action<IReadOnlyList<LiveChatEntry>> _notifyChatMessagesChanged;
        private readonly Action<string> _notifyStatusChanged;

        internal LudusSessionPacketContext(
            Func<ulong> getLastReceivedSequence,
            Action<ulong> setLastReceivedSequence,
            Func<string> getConnectionId,
            Action<string> setConnectionId,
            Func<float> getHeartbeatIntervalSeconds,
            Action<float> setHeartbeatIntervalSeconds,
            Func<LudusClientType> getClientType,
            Func<LudusRoomContextType> getRoomContext,
            Func<string> getCurrentLudusMatchId,
            Func<CompeteRoom> getPendingTournamentRoom,
            Action<DecodedLudusEnvelope> applyClientContext,
            Action closeTournamentRoom,
            Action disconnect,
            Action<CompeteRoom> enterTournamentRoom,
            Func<bool> requestAuthenticationRefresh,
            Action scheduleNextHeartbeat,
            Action<string, float?> scheduleReconnect,
            Action<LudusPlayState, LudusDownloadState, string> sendPresence,
            Action<string> setReconnectUrl,
            Action<IReadOnlyList<LiveChatEntry>> notifyChatMessagesChanged,
            Action<string> notifyStatusChanged) {

            _getLastReceivedSequence = getLastReceivedSequence;
            _setLastReceivedSequence = setLastReceivedSequence;
            _getConnectionId = getConnectionId;
            _setConnectionId = setConnectionId;
            _getHeartbeatIntervalSeconds = getHeartbeatIntervalSeconds;
            _setHeartbeatIntervalSeconds = setHeartbeatIntervalSeconds;
            _getClientType = getClientType;
            _getRoomContext = getRoomContext;
            _getCurrentLudusMatchId = getCurrentLudusMatchId;
            _getPendingTournamentRoom = getPendingTournamentRoom;
            _applyClientContext = applyClientContext;
            _closeTournamentRoom = closeTournamentRoom;
            _disconnect = disconnect;
            _enterTournamentRoom = enterTournamentRoom;
            _requestAuthenticationRefresh = requestAuthenticationRefresh;
            _scheduleNextHeartbeat = scheduleNextHeartbeat;
            _scheduleReconnect = scheduleReconnect;
            _sendPresence = sendPresence;
            _setReconnectUrl = setReconnectUrl;
            _notifyChatMessagesChanged = notifyChatMessagesChanged;
            _notifyStatusChanged = notifyStatusChanged;
        }

        public ulong LastReceivedSequence {
            get => _getLastReceivedSequence();
            set => _setLastReceivedSequence(value);
        }

        public string ConnectionId {
            get => _getConnectionId();
            set => _setConnectionId(value);
        }

        public float HeartbeatIntervalSeconds {
            get => _getHeartbeatIntervalSeconds();
            set => _setHeartbeatIntervalSeconds(value);
        }

        public LudusClientType ClientType => _getClientType();
        public LudusRoomContextType RoomContext => _getRoomContext();
        public string CurrentLudusMatchId => _getCurrentLudusMatchId();
        public CompeteRoom PendingTournamentRoom => _getPendingTournamentRoom();
        public void ApplyClientContext(DecodedLudusEnvelope envelope) => _applyClientContext(envelope);
        public void CloseTournamentRoom() => _closeTournamentRoom();
        public void Disconnect() => _disconnect();
        public void EnterTournamentRoom(CompeteRoom room) => _enterTournamentRoom(room);
        public bool RequestAuthenticationRefresh() => _requestAuthenticationRefresh();
        public void ScheduleNextHeartbeat() => _scheduleNextHeartbeat();
        public void ScheduleReconnect(string reason, float? delayOverrideSeconds) => _scheduleReconnect(reason, delayOverrideSeconds);
        public void SendPresence(LudusPlayState playState, LudusDownloadState downloadState, string currentMapHash) =>
            _sendPresence(playState, downloadState, currentMapHash);
        public void SetReconnectUrl(string url) => _setReconnectUrl(url);
        public void NotifyChatMessagesChanged(IReadOnlyList<LiveChatEntry> messages) => _notifyChatMessagesChanged(messages);
        public void NotifyStatusChanged(string status) => _notifyStatusChanged(status);
    }
}
