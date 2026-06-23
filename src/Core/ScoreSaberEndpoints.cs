using System.Linq;
using System.Reflection;

namespace ScoreSaber.Core {
    internal static class ScoreSaberEndpoints {
        private const string DefaultWebsiteBaseUrl = "https://scoresaber.com";
        private const string DefaultApiBaseUrl = "https://scoresaber.com";
        private const string DefaultCdnBaseUrl = "https://cdn.scoresaber.com";
        private const string DefaultLudusUrl = "wss://ludus-1.scoresaber.com/v1/connect";

        internal static readonly string WebsiteBaseUrl = ConfiguredUrl("ScoreSaberWebsiteBaseUrl", DefaultWebsiteBaseUrl);
        internal static readonly string ApiBaseUrl = ConfiguredUrl("ScoreSaberApiBaseUrl", DefaultApiBaseUrl);
        internal static readonly string CdnBaseUrl = ConfiguredUrl("ScoreSaberCdnBaseUrl", DefaultCdnBaseUrl);
        internal static readonly string LudusUrl = ConfiguredUrl("ScoreSaberLudusUrl", DefaultLudusUrl);

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

        private static string ConfiguredUrl(string key, string fallback) {
            string value = typeof(Plugin)
                .Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(x => x.Key == key)
                ?.Value;

            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().TrimEnd('/');
        }
    }
}
