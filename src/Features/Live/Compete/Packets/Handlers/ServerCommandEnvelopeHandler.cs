using ScoreSaber.Features.Live.Ludus.Services;
using ScoreSaber.Features.Live.Ludus.Packets;
using ScoreSaber.Features.Live.Protocol;

namespace ScoreSaber.Features.Live.Compete.Packets.Handlers {
    internal sealed class ServerCommandEnvelopeHandler : ILudusEnvelopeHandler<ILudusSessionPacketContext> {
        private readonly LudusServerCommandDispatcher _commandDispatcher;
        private readonly ILudusServerCommandSession _commandSession;

        internal ServerCommandEnvelopeHandler(LudusServerCommandDispatcher commandDispatcher, ILudusServerCommandSession commandSession) {
            _commandDispatcher = commandDispatcher;
            _commandSession = commandSession;
        }

        public LudusEnvelopeType Type => LudusEnvelopeType.ServerCommand;

        public void Handle(ILudusSessionPacketContext session, DecodedLudusEnvelope envelope) {
            _commandDispatcher.Handle(_commandSession, envelope.ServerCommand);
        }
    }
}
