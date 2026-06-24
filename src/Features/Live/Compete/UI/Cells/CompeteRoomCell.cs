using ScoreSaber.Features.Live.Compete.Domain;

namespace ScoreSaber.Features.Live.Compete.UI.Cells {
    internal class CompeteRoomCell : CompeteListRowCell {
        internal CompeteRoom Room { get; }

        internal CompeteRoomCell(CompeteRoom room)
            : base(room.DisplayName, $"{room.Round} - {PlayerCountText(room)}", room.State) {
            Room = room;
        }

        private static string PlayerCountText(CompeteRoom room) {
            return room.PlayerCount == 1 ? "1 player" : $"{room.PlayerCount} players";
        }
    }
}
