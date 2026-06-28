using ScoreSaber.Core.Timing;
using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Protocol;
using ScoreSaber.Features.Players.Domain;
using ScoreSaber.Live.V1;
using System;
using System.Collections.Generic;

namespace ScoreSaber.Features.Live.Ludus.Packets {
    internal sealed class LudusPacketSender {
        private readonly Action<byte[]> _send;
        private readonly Func<Func<byte[]>, bool> _sendDeferred;
        private readonly ScoreSaberClock _clock;
        private ulong _outgoingSequence = 1;

        internal LudusPacketSender(Action<byte[]> send, Func<Func<byte[]>, bool> sendDeferred, ScoreSaberClock clock) {
            _send = send;
            _sendDeferred = sendDeferred;
            _clock = clock;
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
            bool publicLivePresenceOptOut,
            List<LiveMod> mods) {

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
                mods,
                NowUnixMs(),
                NextSequence()));
        }

        internal void Heartbeat(string connectionId) {
            _send(LudusProto.EncodeHeartbeat(LastReceivedSequence, NowUnixMs(), NextSequence(), connectionId));
        }

        internal void SetRoomContext(LudusRoomContextType roomContext, string tournamentId, List<LiveMod> mods, string connectionId) {
            _send(LudusProto.EncodeSetRoomContext(roomContext, tournamentId, mods, NowUnixMs(), NextSequence(), connectionId));
        }

        internal void SetClientType(LudusClientType clientType, string connectionId) {
            _send(LudusProto.EncodeSetClientType(clientType, NowUnixMs(), NextSequence(), connectionId));
        }

        internal void JoinRoom(string matchId, List<LiveMod> mods, string connectionId) {
            _send(LudusProto.EncodeJoinRoom(matchId, string.Empty, mods, NowUnixMs(), NextSequence(), connectionId));
        }

        internal void ReadyState(string matchId, bool ready, string connectionId) {
            _send(LudusProto.EncodeReadyState(matchId, ready, NowUnixMs(), NextSequence(), connectionId));
        }

        internal void DownloadState(string matchId, LudusDownloadState state, string errorMessage, string connectionId) {
            _send(LudusProto.EncodeDownloadState(matchId, state, errorMessage, NowUnixMs(), NextSequence(), connectionId));
        }

        internal void PromptResponse(CompeteOrganizerPrompt prompt, string matchId, string playerId, bool accepted, string connectionId) {
            _send(LudusProto.EncodePromptResponse(prompt.CommandId, matchId, playerId, accepted, NowUnixMs(), NextSequence(), connectionId));
        }

        internal void ChatMessage(string matchId, string text, string senderDisplayName, string connectionId) {
            _send(LudusProto.EncodeChatMessage(matchId, text, senderDisplayName, NowUnixMs(), NextSequence(), connectionId));
        }

        internal void Presence(LudusPlayState playState, LudusDownloadState downloadState, string currentMatchId, string currentMapHash, string connectionId) {
            _send(LudusProto.EncodePresence(playState, downloadState, currentMatchId, currentMapHash, NowUnixMs(), NextSequence(), connectionId));
        }

        internal bool ReplayPacket(ReplayStreamPacket packet, string connectionId) {
            ulong sequence = _outgoingSequence;
            long clientTimeUnixMs = NowUnixMs();
            if (!_sendDeferred(() => LudusProto.EncodeReplayPacket(packet, clientTimeUnixMs, sequence, connectionId))) {
                return false;
            }

            _outgoingSequence++;
            return true;
        }

        private ulong NextSequence() => _outgoingSequence++;

        private long NowUnixMs() => _clock.UnixTimeMilliseconds();
    }
}
