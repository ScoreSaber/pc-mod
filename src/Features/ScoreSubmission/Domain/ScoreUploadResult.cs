using ScoreSaber.Core.Api;

namespace ScoreSaber.Features.ScoreSubmission.Domain {
    internal enum ScoreUploadStatus {
        Packaging,
        Uploading,
        Success,
        Retrying,
        Error,
        Done
    }

    internal class ScoreUploadResult {
        internal ScoreUploadStatus Status { get; set; }
        internal bool Success { get; set; }
        internal string Message { get; set; } = string.Empty;
        internal ScoreSaberApiError Error { get; set; }
    }

    internal class ScoreSubmissionStatus {
        internal ScoreUploadStatus Status { get; private set; }
        internal string Message { get; private set; }
        internal ScoreUploadResult Result { get; private set; }

        internal static ScoreSubmissionStatus Progress(ScoreUploadStatus status, string message) {
            return new ScoreSubmissionStatus {
                Status = status,
                Message = message ?? string.Empty
            };
        }

        internal static ScoreSubmissionStatus FromResult(ScoreUploadResult result) {
            return new ScoreSubmissionStatus {
                Status = result?.Status ?? ScoreUploadStatus.Error,
                Message = result?.Message ?? string.Empty,
                Result = result
            };
        }
    }
}
