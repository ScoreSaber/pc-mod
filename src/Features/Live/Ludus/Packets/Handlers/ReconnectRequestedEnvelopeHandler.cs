using ScoreSaber.Features.Live.Ludus.Services;
using ScoreSaber.Features.Live.Protocol;
using System;

namespace ScoreSaber.Features.Live.Ludus.Packets.Handlers {
    internal sealed class ReconnectRequestedEnvelopeHandler<TSession> : ILudusEnvelopeHandler<TSession>
        where TSession : ILudusSessionPacketContext {
        public LudusEnvelopeType Type => LudusEnvelopeType.ReconnectRequested;

        public void Handle(TSession session, DecodedLudusEnvelope envelope) {
            if (envelope == null || string.IsNullOrEmpty(envelope.ReconnectWebSocketUrl)) {
                return;
            }

            session.SetReconnectUrl(envelope.ReconnectWebSocketUrl);
            session.ScheduleReconnect(
                string.IsNullOrEmpty(envelope.ReconnectReason) ? "redirected" : envelope.ReconnectReason,
                Math.Max(0.05f, envelope.ReconnectRetryAfterMs / 1000f));
        }
    }
}
