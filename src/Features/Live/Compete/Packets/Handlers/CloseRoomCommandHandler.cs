using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Compete.Packets;
using ScoreSaber.Live.V1;
using System;

namespace ScoreSaber.Features.Live.Compete.Packets.Handlers {
    internal sealed class CloseRoomCommandHandler : ILudusServerCommandHandler {
        public LudusCommandType Type => LudusCommandType.LudusCommandTypeCloseRoom;

        public void Handle(ILudusServerCommandSession session, ServerCommand command) {
            CompeteRoom room = session.TournamentRoom;
            if (room == null) {
                return;
            }

            if (!string.IsNullOrEmpty(command.MatchId) && !string.Equals(command.MatchId, room.Id, StringComparison.Ordinal)) {
                return;
            }

            session.NotifyStatusChanged("Room closed.");
            session.CloseTournamentRoom();
            Plugin.Log.Info($"Ludus: Closed room {room.Id}.");
        }
    }
}
