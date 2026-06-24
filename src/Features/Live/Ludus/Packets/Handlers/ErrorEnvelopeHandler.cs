using ScoreSaber.Core;
using ScoreSaber.Features.Live.Ludus.Services;
using ScoreSaber.Features.Live.Protocol;
using ScoreSaber.Live.V1;
using System;

namespace ScoreSaber.Features.Live.Ludus.Packets.Handlers {
    internal sealed class ErrorEnvelopeHandler<TSession> : ILudusEnvelopeHandler<TSession>
        where TSession : ILudusSessionPacketContext {
        public LudusEnvelopeType Type => LudusEnvelopeType.Error;

        public void Handle(TSession session, DecodedLudusEnvelope envelope) {
            string status = $"Ludus error {envelope.ErrorCode}: {envelope.ErrorMessage}";
            if (IsHiddenClientStatus(envelope)) {
                Plugin.Log.Debug(status);
                return;
            }

            if (string.Equals(envelope.ErrorCode, "auth_failed", StringComparison.OrdinalIgnoreCase)) {
                session.NotifyStatusChanged(status);
                Plugin.Log.Warn(status);
                if (envelope.Retryable) {
                    if (session.RequestAuthenticationRefresh()) {
                        session.ScheduleReconnect("authentication failed", 0.5f);
                    } else {
                        session.ScheduleReconnect("authentication failed with a fresh game session", null);
                    }
                    return;
                }

                session.Disconnect();
                return;
            }

            if (string.Equals(envelope.ErrorCode, "denied_mods", StringComparison.OrdinalIgnoreCase)) {
                session.NotifyStatusChanged(envelope.ErrorMessage);
                Plugin.Log.Warn(status);
                session.CloseTournamentRoom();
                return;
            }

            if (IsUnavailableTournamentRoom(envelope, session)) {
                session.NotifyStatusChanged("Room closed.");
                Plugin.Log.Warn(status);
                session.CloseTournamentRoom();
                return;
            }

            session.NotifyStatusChanged(status);
            Plugin.Log.Warn(status);
        }

        private static bool IsHiddenClientStatus(DecodedLudusEnvelope envelope) {
            return string.Equals(envelope?.ErrorCode, "packet_rejected", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUnavailableTournamentRoom(DecodedLudusEnvelope envelope, TSession session) {
            if (session.RoomContext != LudusRoomContextType.LudusRoomContextTypeTournament) {
                return false;
            }

            string error = $"{envelope.ErrorCode} {envelope.ErrorMessage}".ToLowerInvariant();
            bool roomError = error.Contains("room") || error.Contains("match");
            if (!roomError) {
                return false;
            }

            return error.Contains("closed") ||
                error.Contains("gone") ||
                error.Contains("missing") ||
                error.Contains("not found") ||
                error.Contains("not_found") ||
                error.Contains("not-in") ||
                error.Contains("not_in") ||
                error.Contains("unavailable");
        }
    }
}
