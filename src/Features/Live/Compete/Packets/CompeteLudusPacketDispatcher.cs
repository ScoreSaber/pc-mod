using ScoreSaber.Core.Timing;
using ScoreSaber.Features.Live.Compete.Packets.Handlers;
using ScoreSaber.Features.Live.Ludus.Services;
using ScoreSaber.Features.Live.Ludus.Packets;
using ScoreSaber.Features.Live.Ludus.Packets.Handlers;

namespace ScoreSaber.Features.Live.Compete.Packets {
    internal static class CompeteLudusPacketDispatcher {
        internal static LudusPacketDispatcher<ILudusSessionPacketContext> CreateDefault(ILudusServerCommandSession commandSession, LudusChatMessageBuffer chatMessages, ScoreSaberClock clock) {
            LudusServerCommandDispatcher commandDispatcher = LudusServerCommandDispatcher.CreateDefault();
            return new LudusPacketDispatcher<ILudusSessionPacketContext>(new ILudusEnvelopeHandler<ILudusSessionPacketContext>[] {
                new ConnectAcceptedEnvelopeHandler<ILudusSessionPacketContext>(),
                new RoomContextUpdatedEnvelopeHandler<ILudusSessionPacketContext>(),
                new ReconnectRequestedEnvelopeHandler<ILudusSessionPacketContext>(),
                new RoomSnapshotEnvelopeHandler(commandSession),
                new ServerCommandEnvelopeHandler(commandDispatcher, commandSession),
                new ChatMessageEnvelopeHandler<ILudusSessionPacketContext>(chatMessages),
                new ChatSnapshotEnvelopeHandler<ILudusSessionPacketContext>(chatMessages),
                new ErrorEnvelopeHandler<ILudusSessionPacketContext>()
            }, clock);
        }
    }
}
