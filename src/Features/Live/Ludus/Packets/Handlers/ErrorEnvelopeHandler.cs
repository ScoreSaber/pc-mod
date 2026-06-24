using ScoreSaber.Core;
using ScoreSaber.Features.Live.Ludus.Services;
using ScoreSaber.Features.Live.Protocol;
using System;

namespace ScoreSaber.Features.Live.Ludus.Packets.Handlers {
    internal sealed class ErrorEnvelopeHandler<TSession> : ILudusEnvelopeHandler<TSession>
        where TSession : ILudusSessionPacketContext {
        public LudusEnvelopeType Type => LudusEnvelopeType.Error;

        public void Handle(TSession session, DecodedLudusEnvelope envelope) {
            string status = $"Ludus error {envelope.ErrorCode}: {envelope.ErrorMessage}";
            session.NotifyStatusChanged(status);
            Plugin.Log.Warn(status);
            if (string.Equals(envelope.ErrorCode, "auth_failed", StringComparison.OrdinalIgnoreCase)) {
                session.RequestAuthenticationRefresh();
                session.ScheduleReconnect("authentication failed", 0.5f);
            }
        }
    }
}
