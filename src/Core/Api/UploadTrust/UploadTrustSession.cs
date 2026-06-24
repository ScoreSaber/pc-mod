namespace ScoreSaber.Core.Api.UploadTrust {
    internal sealed class UploadTrustSession {
        internal const string ProtocolHeaderValue = "scoresaber-upload-v2";
        internal const int ProtocolVersion = 2;

        internal UploadTrustSession(
            string buildId,
            string buildCredential,
            string uploadVersionHash,
            bool requiresBuildId) {

            BuildId = buildId ?? string.Empty;
            BuildCredential = buildCredential ?? string.Empty;
            UploadVersionHash = uploadVersionHash ?? string.Empty;
            RequiresBuildId = requiresBuildId;
        }

        internal string BuildId { get; }
        internal string BuildCredential { get; }
        internal string UploadVersionHash { get; }
        internal bool RequiresBuildId { get; }

        internal bool IsUploadProtocolV2 {
            get {
                return (!RequiresBuildId || !string.IsNullOrEmpty(BuildId)) &&
                    !string.IsNullOrEmpty(BuildCredential) &&
                    !string.IsNullOrEmpty(UploadVersionHash);
            }
        }
    }
}
