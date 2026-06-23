using ScoreSaber.Core;
using ScoreSaber.Features.Live.Ludus.Services;
using ScoreSaber.Features.Live.Protocol;

namespace ScoreSaber.Features.Live.Ludus.Packets.Handlers {
    internal sealed class RoomContextUpdatedEnvelopeHandler<TSession> : ILudusEnvelopeHandler<TSession>
        where TSession : ILudusSessionPacketContext {
        public LudusEnvelopeType Type => LudusEnvelopeType.RoomContextUpdated;

        public void Handle(TSession session, DecodedLudusEnvelope envelope) {
            session.ApplyClientContext(envelope);
            Plugin.Log.Info($"Ludus: Room context changed to {session.ClientType} {session.RoomContext} {session.CurrentLudusMatchId}");
        }
    }
}
