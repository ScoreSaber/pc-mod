namespace ScoreSaber.Features.Live.Compete.Domain {
    internal class CompeteTournament {
        internal string Id { get; }
        internal string Name { get; }
        internal string RoomSummary { get; }

        internal CompeteTournament(string id, string name, string roomSummary) {
            Id = id;
            Name = name;
            RoomSummary = roomSummary;
        }
    }
}
