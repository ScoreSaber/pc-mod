using System;

namespace ScoreSaber.Features.Live.Compete.Services {
    internal class CompeteGameplayState {
        internal bool IsLiveGameplayActive { get; private set; }
        internal string TournamentId { get; private set; } = string.Empty;
        internal string MatchId { get; private set; } = string.Empty;
        internal string MapHash { get; private set; } = string.Empty;
        private bool _hostStopRequested;
        private string _hostStopMapHash = string.Empty;

        internal void Begin(string tournamentId, string matchId, string mapHash) {
            IsLiveGameplayActive = true;
            TournamentId = tournamentId ?? string.Empty;
            MatchId = matchId ?? string.Empty;
            MapHash = mapHash ?? string.Empty;
            _hostStopRequested = false;
            _hostStopMapHash = string.Empty;
        }

        internal void End() {
            IsLiveGameplayActive = false;
            TournamentId = string.Empty;
            MatchId = string.Empty;
            MapHash = string.Empty;
        }

        internal void MarkHostStopRequested() {
            if (!IsLiveGameplayActive) {
                return;
            }

            _hostStopRequested = true;
            _hostStopMapHash = MapHash;
        }

        internal bool TryConsumeHostStop(string mapHash) {
            if (!_hostStopRequested) {
                return false;
            }

            bool matches = string.IsNullOrEmpty(_hostStopMapHash) ||
                string.Equals(_hostStopMapHash, mapHash ?? string.Empty, StringComparison.OrdinalIgnoreCase);

            _hostStopRequested = false;
            _hostStopMapHash = string.Empty;
            return matches;
        }
    }
}
