namespace ScoreSaber.Features.Players.Domain {
    internal enum PlayerQueryScope {
        Global,
        AroundPlayer,
        Friends,
        Country,
        Region,
        Countries
    }

    internal class PlayerListQuery {
        internal int Page { get; set; } = 1;
        internal int Limit { get; set; } = 5;
        internal PlayerQueryScope Scope { get; set; } = PlayerQueryScope.Global;
        internal string Countries { get; set; } = string.Empty;
        internal int? RealmId { get; set; }
    }

    internal class GlobalPlayerPage {
        internal GlobalPlayerScope Scope { get; set; }
        internal int Page { get; set; }
        internal PlayerSummary[] Players { get; set; }
    }
}
