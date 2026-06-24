using System;
using System.Collections.Generic;

namespace ScoreSaber.Features.Players.Domain {
    internal class PlayerSummary {
        internal string Id { get; set; } = string.Empty;
        internal string Name { get; set; } = string.Empty;
        internal string Country { get; set; } = string.Empty;
        internal string Role { get; set; } = string.Empty;
        internal string Avatar { get; set; } = string.Empty;
        internal int Permissions { get; set; }
        internal bool Banned { get; set; }
        internal bool Inactive { get; set; }
        internal PlayerStats Stats { get; set; } = new PlayerStats();
        internal List<PlayerHistoryPoint> GlobalHistory { get; set; } = new List<PlayerHistoryPoint>();
    }

    internal class PlayerStats {
        internal int RealmId { get; set; }
        internal string RealmName { get; set; } = string.Empty;
        internal int Rank { get; set; }
        internal int CountryRank { get; set; }
        internal double TotalPP { get; set; }
        internal long TotalScore { get; set; }
        internal long TotalRankedScore { get; set; }
        internal int TotalPlayedLeaderboards { get; set; }
        internal int TotalPlayedRankedLeaderboards { get; set; }
        internal int TotalSubmittedPlays { get; set; }
        internal int TotalReplayViews { get; set; }
        internal double AverageAccuracy { get; set; }
        internal double WeightedAverageAccuracy { get; set; }
        internal double CompletionAccuracy { get; set; }
        internal PlayerDevice Device { get; set; } = new PlayerDevice();
    }

    internal class PlayerDevice {
        internal string HMD { get; set; } = string.Empty;
        internal string ControllerLeft { get; set; } = string.Empty;
        internal string ControllerRight { get; set; } = string.Empty;
    }

    internal class PlayerHistoryPoint {
        internal int Rank { get; set; }
        internal double TotalPP { get; set; }
        internal long TotalScore { get; set; }
        internal long TotalRankedScore { get; set; }
        internal bool Estimated { get; set; }
        internal DateTimeOffset CreatedAt { get; set; }
    }
}
