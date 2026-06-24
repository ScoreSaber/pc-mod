using ScoreSaber.Features.Live.Protocol;
using System.Collections.Generic;

namespace ScoreSaber.Features.Live.Ludus.Packets {
    internal interface ILudusPacketSession {
        ulong LastReceivedSequence { get; set; }
    }

    internal interface ILudusEnvelopeHandler<TSession> where TSession : ILudusPacketSession {
        LudusEnvelopeType Type { get; }
        void Handle(TSession session, DecodedLudusEnvelope envelope);
    }

    internal sealed class LudusPacketDispatcher<TSession> where TSession : ILudusPacketSession {
        private readonly Dictionary<LudusEnvelopeType, ILudusEnvelopeHandler<TSession>> _handlers;

        internal LudusPacketDispatcher(IEnumerable<ILudusEnvelopeHandler<TSession>> handlers) {
            _handlers = new Dictionary<LudusEnvelopeType, ILudusEnvelopeHandler<TSession>>();
            foreach (ILudusEnvelopeHandler<TSession> handler in handlers) {
                _handlers[handler.Type] = handler;
            }
        }

        internal void Handle(TSession session, byte[] bytes) {
            DecodedLudusEnvelope envelope = LudusProto.Decode(bytes);
            if (envelope == null) {
                return;
            }

            if (envelope.Sequence > session.LastReceivedSequence) {
                session.LastReceivedSequence = envelope.Sequence;
            }

            ILudusEnvelopeHandler<TSession> handler;
            if (_handlers.TryGetValue(envelope.Type, out handler)) {
                handler.Handle(session, envelope);
            }
        }
    }
}
