using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Protocol;
using ScoreSaber.Features.Players.Domain;
using ScoreSaber.Live.V1;
using System;

namespace ScoreSaber.Features.Live.Ludus.Packets {
    internal sealed class LudusPacketSender {
        private readonly Action<byte[]> _send;
        private readonly Action<Func<byte[]>> _sendDeferred;
        private ulong _outgoingSequence = 1;

        internal LudusPacketSender(Action<byte[]> send, Action<Func<byte[]>> sendDeferred) {
            _send = send;
            _sendDeferred = sendDeferred;
        }

        internal ulong LastReceivedSequence { get; set; }

        internal void ResetSequences() {
            _outgoingSequence = 1;
            LastReceivedSequence = 0;
        }

        internal void Connect(
            GameSession session,
            LivePlayerPlatform platform,
            string gameVersion,
            string clientVersion,
            LudusRoomContextType initialRoomContext,
            bool publicLivePresenceOptOut) {

            _send(LudusProto.EncodeConnect(
                string.Empty,
                session.SessionId,
                session.SessionKey,
                string.Empty,
                session.PlayerId,
                platform,
                gameVersion,
                clientVersion,
                initialRoomContext,
                publicLivePresenceOptOut,
                NextSequence()));
        }

        internal void Heartbeat(string connectionId) {
            _send(LudusProto.EncodeHeartbeat(LastReceivedSequence, NextSequence(), connectionId));
        }

        internal void SetRoomContext(LudusRoomContextType roomContext, string tournamentId, string connectionId) {
            _send(LudusProto.EncodeSetRoomContext(roomContext, tournamentId, NextSequence(), connectionId));
        }

        internal void SetClientType(LudusClientType clientType, string connectionId) {
            _send(LudusProto.EncodeSetClientType(clientType, NextSequence(), connectionId));
        }

        internal void JoinRoom(string matchId, string connectionId) {
            _send(LudusProto.EncodeJoinRoom(matchId, string.Empty, NextSequence(), connectionId));
        }

        internal void ReadyState(string matchId, bool ready, string connectionId) {
            _send(LudusProto.EncodeReadyState(matchId, ready, NextSequence(), connectionId));
        }

        internal void DownloadState(string matchId, LudusDownloadState state, string errorMessage, string connectionId) {
            _send(LudusProto.EncodeDownloadState(matchId, state, errorMessage, NextSequence(), connectionId));
        }

        internal void PromptResponse(CompeteOrganizerPrompt prompt, string matchId, string playerId, bool accepted, string connectionId) {
            _send(LudusProto.EncodePromptResponse(prompt.CommandId, matchId, playerId, accepted, NextSequence(), connectionId));
        }

        internal void ChatMessage(string matchId, string text, string senderDisplayName, string connectionId) {
            _send(LudusProto.EncodeChatMessage(matchId, text, senderDisplayName, NextSequence(), connectionId));
        }

        internal void Presence(LudusPlayState playState, LudusDownloadState downloadState, string currentMatchId, string currentMapHash, string connectionId) {
            _send(LudusProto.EncodePresence(playState, downloadState, currentMatchId, currentMapHash, NextSequence(), connectionId));
        }

        internal void ReplayPacket(ReplayStreamPacket packet, string connectionId) {
            ulong sequence = NextSequence();
            _sendDeferred(() => LudusProto.EncodeReplayPacket(packet, sequence, connectionId));
        }

        private ulong NextSequence() => _outgoingSequence++;
    }
}
