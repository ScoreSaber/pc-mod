namespace ScoreSaber.Core.Api.Generated {
    public partial class GameAuthenticateRequest {
        private bool _serializeUploadTrustMetadata;

        internal void SerializeUploadTrustMetadata() {
            _serializeUploadTrustMetadata = true;
        }

        public bool ShouldSerializeClientBuildId() {
            return _serializeUploadTrustMetadata;
        }

        public bool ShouldSerializeUploadProtocolVersion() {
            return _serializeUploadTrustMetadata;
        }

        public bool ShouldSerializePluginVersion() {
            return _serializeUploadTrustMetadata;
        }

        public bool ShouldSerializeGameVersion() {
            return _serializeUploadTrustMetadata;
        }

        public bool ShouldSerializeUploadVersionHash() {
            return _serializeUploadTrustMetadata;
        }

        public bool ShouldSerializeClientKind() {
            return _serializeUploadTrustMetadata;
        }

        public bool ShouldSerializeArtifactSha256() {
            return _serializeUploadTrustMetadata && !string.IsNullOrEmpty(ArtifactSha256);
        }

        public bool ShouldSerializeDevUploadToken() {
            return _serializeUploadTrustMetadata && !string.IsNullOrEmpty(DevUploadToken);
        }
    }
}
