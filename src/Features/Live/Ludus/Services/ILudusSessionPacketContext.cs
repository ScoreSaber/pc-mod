using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Ludus.Domain;
using ScoreSaber.Features.Live.Ludus.Packets;
using ScoreSaber.Features.Live.Protocol;
using ScoreSaber.Live.V1;
using System.Collections.Generic;

namespace ScoreSaber.Features.Live.Ludus.Services {
    internal interface ILudusSessionPacketContext : ILudusPacketSession {
        string ConnectionId { get; set; }
        float HeartbeatIntervalSeconds { get; set; }
        LudusClientType ClientType { get; }
        LudusRoomContextType RoomContext { get; }
        string CurrentLudusMatchId { get; }
        CompeteRoom PendingTournamentRoom { get; }

        void ApplyClientContext(DecodedLudusEnvelope envelope);
        void CloseTournamentRoom();
        void Disconnect();
        void EnterTournamentRoom(CompeteRoom room);
        bool RequestAuthenticationRefresh();
        void ScheduleNextHeartbeat();
        void ScheduleReconnect(string reason, float? delayOverrideSeconds);
        void SendPresence(LudusPlayState playState, LudusDownloadState downloadState, string currentMapHash);
        void SetReconnectUrl(string url);
        void NotifyChatMessagesChanged(IReadOnlyList<LiveChatEntry> messages);
        void NotifyStatusChanged(string status);
    }
}
