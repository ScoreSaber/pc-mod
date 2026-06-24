using ScoreSaber.Features.Live.Ludus.Services;
using ScoreSaber.Features.Live.Protocol;

namespace ScoreSaber.Features.Live.Ludus.Packets.Handlers {
    internal sealed class ChatMessageEnvelopeHandler<TSession> : ILudusEnvelopeHandler<TSession>
        where TSession : ILudusSessionPacketContext {
        private readonly LudusChatMessageBuffer _messages;

        internal ChatMessageEnvelopeHandler(LudusChatMessageBuffer messages) {
            _messages = messages;
        }

        public LudusEnvelopeType Type => LudusEnvelopeType.ChatMessage;

        public void Handle(TSession session, DecodedLudusEnvelope envelope) {
            if (_messages.Apply(envelope.ChatMessage, session.CurrentLudusMatchId)) {
                session.NotifyChatMessagesChanged(_messages.MessagesFor(session.CurrentLudusMatchId));
            }
        }
    }
}
