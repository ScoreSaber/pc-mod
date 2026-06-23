namespace ScoreSaber.Features.Live.Compete.Domain {
    internal class CompeteTeam {
        internal string Id { get; }
        internal string Name { get; }

        internal CompeteTeam(string id, string name) {
            Id = id;
            Name = name;
        }
    }
}
