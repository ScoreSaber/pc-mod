namespace ScoreSaber.Features.Live.Compete.Services {
    internal class CompeteGameplayState {
        internal bool IsLiveGameplayActive { get; private set; }
        internal string TournamentId { get; private set; } = string.Empty;
        internal string MatchId { get; private set; } = string.Empty;
        internal string MapHash { get; private set; } = string.Empty;

        internal void Begin(string tournamentId, string matchId, string mapHash) {
            IsLiveGameplayActive = true;
            TournamentId = tournamentId ?? string.Empty;
            MatchId = matchId ?? string.Empty;
            MapHash = mapHash ?? string.Empty;
        }

        internal void End() {
            IsLiveGameplayActive = false;
            TournamentId = string.Empty;
            MatchId = string.Empty;
            MapHash = string.Empty;
        }
    }
}
