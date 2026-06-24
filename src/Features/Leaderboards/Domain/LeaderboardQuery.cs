namespace ScoreSaber.Features.Leaderboards.Domain {
    internal enum LeaderboardQueryScope {
        Global,
        AroundPlayer,
        Friends,
        Country,
        Region,
        Countries
    }

    internal class LeaderboardQuery {
        internal string SongHash { get; set; } = string.Empty;
        internal string GameMode { get; set; } = string.Empty;
        internal int Difficulty { get; set; }
        internal int Page { get; set; } = 1;
        internal int Limit { get; set; } = 10;
        internal LeaderboardQueryScope Scope { get; set; } = LeaderboardQueryScope.Global;
        internal string Countries { get; set; } = string.Empty;
        internal bool HideNoArrows { get; set; }
        internal int? RealmId { get; set; }
    }
}
