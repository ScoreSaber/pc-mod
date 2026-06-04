using ScoreSaber.Core.Api.UploadTrust;

namespace ScoreSaber.Features.Players.Domain {
    internal class GameSession {
        internal string PlayerId { get; set; } = string.Empty;
        internal string PlayerName { get; set; } = string.Empty;
        internal string SessionId { get; set; } = string.Empty;
        internal string SessionKey { get; set; } = string.Empty;
        internal UploadTrustSession UploadTrust { get; set; }
        internal PlayerSummary Player { get; set; }

        internal bool IsAuthenticated => !string.IsNullOrEmpty(SessionId) && !string.IsNullOrEmpty(SessionKey);

        internal bool UsesUploadProtocolV2 => UploadTrust != null && UploadTrust.IsUploadProtocolV2;
    }
}
