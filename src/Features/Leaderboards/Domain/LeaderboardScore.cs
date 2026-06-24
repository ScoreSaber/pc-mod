using ScoreSaber.Core.Gameplay;
using ScoreSaber.Features.Players.Domain;
using System;
using System.Collections.Generic;

namespace ScoreSaber.Features.Leaderboards.Domain {
    internal class LeaderboardScore {
        internal int Id { get; set; }
        internal int Rank { get; set; }
        internal int UnmodifiedScore { get; set; }
        internal int ModifiedScore { get; set; }
        internal double Accuracy { get; set; }
        internal double PP { get; set; }
        internal double Weight { get; set; }
        internal List<string> Mods { get; set; } = new List<string>();
        internal int BadCuts { get; set; }
        internal int MissedNotes { get; set; }
        internal int MaxCombo { get; set; }
        internal bool FullCombo { get; set; }
        internal bool HasReplay { get; set; }
        internal bool PersonalBest { get; set; }
        internal ScoreSaberPlayOutcome PlayOutcome { get; set; }
        internal double? PlayOutcomeTime { get; set; }
        internal int LegacyHMDId { get; set; }
        internal string Version { get; set; } = string.Empty;
        internal DateTime CreatedAt { get; set; }
        internal PlayerSummary Player { get; set; } = new PlayerSummary();
        internal PlayerDevice Device { get; set; } = new PlayerDevice();
    }
}
