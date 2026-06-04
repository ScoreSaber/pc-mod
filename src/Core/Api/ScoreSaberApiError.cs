namespace ScoreSaber.Core.Api {
    internal class ScoreSaberApiError {
        internal int StatusCode { get; set; }
        internal bool NetworkError { get; set; }
        internal string Code { get; set; } = string.Empty;
        internal string Message { get; set; } = string.Empty;
        internal string RawBody { get; set; } = string.Empty;

        internal static ScoreSaberApiError FromMessage(string message) {
            return new ScoreSaberApiError {
                Message = message
            };
        }
    }
}
