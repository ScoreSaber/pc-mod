namespace ScoreSaber.Features.Leaderboards.Domain {
    internal enum LeaderboardScreenScope {
        Global,
        AroundPlayer,
        Friends,
        Country
    }

    internal enum LeaderboardScreenStatus {
        Loading,
        Loaded,
        Empty,
        NoLeaderboard,
        NoPlayerScore,
        Error
    }

    internal class LeaderboardScreenState {
        internal LeaderboardScreenStatus Status { get; private set; }
        internal LeaderboardMap Leaderboard { get; private set; }
        internal int PlayerScoreIndex { get; private set; }
        internal string RankedStatus { get; private set; }
        internal string ErrorText { get; private set; }
        internal bool ShowRefreshButton { get; private set; }
        internal bool CanPageUp { get; private set; }
        internal bool CanPageDown { get; private set; }

        internal bool IsLoaded => Status != LeaderboardScreenStatus.Loading;

        private LeaderboardScreenState() {
            PlayerScoreIndex = -1;
            RankedStatus = string.Empty;
            ErrorText = string.Empty;
            ShowRefreshButton = true;
        }

        internal static LeaderboardScreenState Loading(int page) {
            return new LeaderboardScreenState {
                Status = LeaderboardScreenStatus.Loading,
                CanPageUp = page > 1,
                CanPageDown = true
            };
        }

        internal static LeaderboardScreenState Loaded(LeaderboardMap leaderboard, int playerScoreIndex, string rankedStatus, bool canPage, int page) {
            return new LeaderboardScreenState {
                Status = LeaderboardScreenStatus.Loaded,
                Leaderboard = leaderboard,
                PlayerScoreIndex = playerScoreIndex,
                RankedStatus = rankedStatus,
                CanPageUp = canPage && page > 1,
                CanPageDown = canPage
            };
        }

        internal static LeaderboardScreenState Failed(LeaderboardScreenStatus status, string errorText, bool showRefreshButton, LeaderboardMap leaderboard, string rankedStatus, bool canPage, int page) {
            return new LeaderboardScreenState {
                Status = status,
                Leaderboard = leaderboard,
                RankedStatus = rankedStatus,
                ErrorText = errorText,
                ShowRefreshButton = showRefreshButton,
                CanPageUp = canPage && page > 1,
                CanPageDown = canPage
            };
        }
    }
}
