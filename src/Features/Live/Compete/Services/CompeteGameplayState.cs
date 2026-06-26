using System;

namespace ScoreSaber.Features.Live.Compete.Services {
    internal class CompeteGameplayState {
        internal event Action<bool> LiveGameplayActiveChanged;

        internal bool IsLiveGameplayActive { get; private set; }
        internal bool IsMapStartReady { get; private set; }
        internal bool IsWaitingForMapStartReady => IsLiveGameplayActive && !IsMapStartReady;
        internal string TournamentId { get; private set; } = string.Empty;
        internal string MatchId { get; private set; } = string.Empty;
        internal string MapHash { get; private set; } = string.Empty;
        private bool _hostStopRequested;
        private string _hostStopMapHash = string.Empty;

        internal void Begin(string tournamentId, string matchId, string mapHash) {
            bool wasActive = IsLiveGameplayActive;
            IsLiveGameplayActive = true;
            TournamentId = tournamentId ?? string.Empty;
            MatchId = matchId ?? string.Empty;
            MapHash = mapHash ?? string.Empty;
            IsMapStartReady = false;
            _hostStopRequested = false;
            _hostStopMapHash = string.Empty;
            if (!wasActive) {
                LiveGameplayActiveChanged?.Invoke(true);
            }
        }

        internal void End() {
            bool wasActive = IsLiveGameplayActive;
            IsLiveGameplayActive = false;
            IsMapStartReady = false;
            TournamentId = string.Empty;
            MatchId = string.Empty;
            MapHash = string.Empty;
            if (wasActive) {
                LiveGameplayActiveChanged?.Invoke(false);
            }
        }

        internal void MarkMapStartReady() {
            if (!IsLiveGameplayActive) {
                return;
            }

            IsMapStartReady = true;
        }

        internal bool IsCurrentMap(string matchId, string mapHash) {
            if (!IsLiveGameplayActive) {
                return false;
            }

            if (!string.IsNullOrEmpty(matchId) && !string.Equals(MatchId, matchId, StringComparison.Ordinal)) {
                return false;
            }

            return string.IsNullOrEmpty(mapHash) || string.Equals(MapHash, mapHash, StringComparison.OrdinalIgnoreCase);
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
