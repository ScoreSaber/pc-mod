namespace ScoreSaber.Features.Live.Compete.Domain {
    internal class CompeteMapStartCountdown {
        internal string MatchId { get; }
        internal int RemainingSeconds { get; }

        internal CompeteMapStartCountdown(string matchId, int remainingSeconds) {
            MatchId = matchId ?? string.Empty;
            RemainingSeconds = remainingSeconds;
        }
    }
}
