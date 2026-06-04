using System;

namespace ScoreSaber.Features.Leaderboards.Domain {
    internal enum LeaderboardStatus {
        Unranked,
        Ranked,
        Qualified,
        Loved
    }

    internal class LeaderboardDetails {
        internal int Id { get; set; }
        internal string SongHash { get; set; } = string.Empty;
        internal string SongName { get; set; } = string.Empty;
        internal string SongSubName { get; set; } = string.Empty;
        internal string SongAuthorName { get; set; } = string.Empty;
        internal string LevelAuthorName { get; set; } = string.Empty;
        internal string CoverImage { get; set; } = string.Empty;
        internal int Difficulty { get; set; }
        internal string DifficultyRaw { get; set; } = string.Empty;
        internal string GameMode { get; set; } = string.Empty;
        internal int MaxScore { get; set; }
        internal int Plays { get; set; }
        internal int DailyPlays { get; set; }
        internal DateTimeOffset CreatedAt { get; set; }
        internal DateTimeOffset? RankedAt { get; set; }
        internal DateTimeOffset? QualifiedAt { get; set; }
        internal DateTimeOffset? LovedAt { get; set; }
        internal LeaderboardStatus Status { get; set; }
        internal bool PositiveModifiers { get; set; }
        internal double Stars { get; set; }
        internal int RealmId { get; set; }
        internal string RealmName { get; set; } = string.Empty;
    }
}
