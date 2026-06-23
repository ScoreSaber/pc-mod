using ScoreSaber.Core;
using ScoreSaber.Features.Live.Ludus.Services;
using ScoreSaber.Features.Live.Protocol;
using ScoreSaber.Live.V1;

namespace ScoreSaber.Features.Live.Ludus.Packets.Handlers {
    internal sealed class ConnectAcceptedEnvelopeHandler<TSession> : ILudusEnvelopeHandler<TSession>
        where TSession : ILudusSessionPacketContext {
        public LudusEnvelopeType Type => LudusEnvelopeType.ConnectAccepted;

        public void Handle(TSession session, DecodedLudusEnvelope envelope) {
            session.ConnectionId = envelope.ConnectionId;
            if (envelope.HeartbeatIntervalMs > 0) {
                session.HeartbeatIntervalSeconds = envelope.HeartbeatIntervalMs / 1000f;
            }

            session.ScheduleNextHeartbeat();
            session.ApplyClientContext(envelope);
            Plugin.Log.Info($"Ludus: Connected as {session.ConnectionId} {session.ClientType} in {envelope.RoomContext} {session.CurrentLudusMatchId}");
            session.SendPresence(LudusPlayState.LudusPlayStateInMenus, LudusDownloadState.LudusDownloadStateNone, string.Empty);
            if (session.PendingTournamentRoom != null) {
                session.EnterTournamentRoom(session.PendingTournamentRoom);
            }
        }
    }
}
