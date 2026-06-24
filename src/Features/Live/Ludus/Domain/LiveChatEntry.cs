using ScoreSaber.Live.V1;
using System;

namespace ScoreSaber.Features.Live.Ludus.Domain {
    internal sealed class LiveChatEntry {
        internal string MessageId { get; }
        internal string MatchId { get; }
        internal string SenderName { get; }
        internal string SenderPlayerId { get; }
        internal LiveChatMessageKind Kind { get; }
        internal string Text { get; }
        internal long CreatedAtUnixMs { get; }
        internal ulong RoomSequence { get; }

        internal bool IsChat => Kind == LiveChatMessageKind.LiveChatMessageKindChat;

        private LiveChatEntry(
            string messageId,
            string matchId,
            string senderName,
            string senderPlayerId,
            LiveChatMessageKind kind,
            string text,
            long createdAtUnixMs,
            ulong roomSequence) {

            MessageId = messageId ?? string.Empty;
            MatchId = matchId ?? string.Empty;
            SenderName = senderName ?? string.Empty;
            SenderPlayerId = senderPlayerId ?? string.Empty;
            Kind = kind;
            Text = text ?? string.Empty;
            CreatedAtUnixMs = createdAtUnixMs;
            RoomSequence = roomSequence;
        }

        internal static LiveChatEntry FromProto(LiveChatMessage message) {
            if (message == null) {
                return null;
            }

            return new LiveChatEntry(
                message.MessageId,
                message.MatchId,
                message.SenderDisplayName,
                message.SenderPlayerId,
                message.Kind,
                message.Text,
                message.CreatedAtUnixMs,
                message.RoomSequence);
        }

        internal string Key => string.IsNullOrEmpty(MessageId) ? $"{MatchId}:{RoomSequence}" : MessageId;

        internal string DisplayTime {
            get {
                if (CreatedAtUnixMs <= 0) {
                    return "--:--";
                }

                return DateTimeOffset.FromUnixTimeMilliseconds(CreatedAtUnixMs).ToLocalTime().ToString("HH:mm");
            }
        }
    }
}
