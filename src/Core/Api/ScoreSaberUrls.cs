namespace ScoreSaber.Core.Api {

    internal static class ScoreSaberUrls {
        internal const string WebsiteBaseUrl = "https://scoresaber.com";
        internal const string ApiBaseUrl = "https://scoresaber.com/api";
        internal const string CdnBaseUrl = "https://cdn.scoresaber.com";

        internal static string GlobalLeaderboard() {
            return $"{WebsiteBaseUrl}/global";
        }

        internal static string Leaderboard(int leaderboardId) {
            return $"{WebsiteBaseUrl}/leaderboard/{leaderboardId}";
        }

        internal static string Player(string playerId) {
            return $"{WebsiteBaseUrl}/u/{playerId}";
        }

        internal static string Flag(string country) {
            return $"{CdnBaseUrl}/flags/{country.ToLower()}.png";
        }
    }
}
