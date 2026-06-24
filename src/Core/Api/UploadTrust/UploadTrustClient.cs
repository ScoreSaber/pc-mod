using ScoreSaber.Core.Api.Generated;
using System;

namespace ScoreSaber.Core.Api.UploadTrust {
    internal sealed class UploadTrustClient {
        private readonly ScoreSaberRuntimeInfo _runtimeInfo;
        private readonly UploadTrustBuildMetadata _buildMetadata;

        internal UploadTrustClient(ScoreSaberRuntimeInfo runtimeInfo)
            : this(runtimeInfo, UploadTrustBuildMetadata.FromAssembly(typeof(Plugin).Assembly)) {
        }

        internal UploadTrustClient(ScoreSaberRuntimeInfo runtimeInfo, UploadTrustBuildMetadata buildMetadata) {
            _runtimeInfo = runtimeInfo;
            _buildMetadata = buildMetadata;
        }

        internal void ApplyAuthMetadata(GameAuthenticateRequest request) {
            if (!_buildMetadata.IsOfficial && !_buildMetadata.IsDevelopment) {
                return;
            }

            request.ClientKind = _buildMetadata.IsOfficial
                ? GameAuthenticateRequestClientKind.Official
                : GameAuthenticateRequestClientKind.Development;
            request.UploadProtocolVersion = UploadTrustSession.ProtocolVersion;
            if (_buildMetadata.IsOfficial) {
                request.ClientBuildId = _buildMetadata.BuildId;
            }
            request.PluginVersion = _runtimeInfo.PluginVersion.ToString();
            request.GameVersion = _runtimeInfo.GameVersion.ToString();
            request.UploadVersionHash = _runtimeInfo.UploadVersionHash;
            request.ArtifactSha256 = _buildMetadata.ArtifactSha256;
            request.DevUploadToken = _buildMetadata.DevelopmentUploadToken;
            request.SerializeUploadTrustMetadata();
        }

        internal UploadTrustSession CreateSession(GameAuthenticateResponse response) {
            if (!_buildMetadata.IsOfficial && !_buildMetadata.IsDevelopment) {
                return null;
            }

            if (response == null) {
                return null;
            }

            if (response.UploadProtocolVersion != UploadTrustSession.ProtocolVersion) {
                return null;
            }

            if (_buildMetadata.IsOfficial && string.IsNullOrEmpty(response.BuildId)) {
                return null;
            }

            if (string.IsNullOrEmpty(response.UploadVersionHash)) {
                return null;
            }

            bool trustedOfficial = _buildMetadata.IsOfficial &&
                response.ClientTrust == GameAuthenticateResponseClientTrust.Official &&
                string.Equals(response.BuildId, _buildMetadata.BuildId, StringComparison.Ordinal);

            bool trustedDevelopment = _buildMetadata.IsDevelopment &&
                response.ClientTrust == GameAuthenticateResponseClientTrust.Development;

            if (!trustedOfficial && !trustedDevelopment) {
                return null;
            }

            if (!string.Equals(response.UploadVersionHash, _runtimeInfo.UploadVersionHash, StringComparison.OrdinalIgnoreCase)) {
                return null;
            }

            return new UploadTrustSession(
                response.BuildId,
                _buildMetadata.IsOfficial ? _buildMetadata.Credential : _buildMetadata.DevelopmentUploadToken,
                response.UploadVersionHash,
                _buildMetadata.IsOfficial);
        }
    }
}
