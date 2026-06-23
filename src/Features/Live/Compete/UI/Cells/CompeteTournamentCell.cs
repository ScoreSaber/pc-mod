using ScoreSaber.Features.Live.Compete.Domain;

namespace ScoreSaber.Features.Live.Compete.UI.Cells {
    internal class CompeteTournamentCell : CompeteListRowCell {
        internal CompeteTournament Tournament { get; }

        internal CompeteTournamentCell(CompeteTournament tournament)
            : base(tournament.Name, tournament.RoomSummary, string.Empty) {
            Tournament = tournament;
        }
    }
}
