namespace ScoreSaber.Features.ScoreSubmission.Services {
    internal static class ScoreSubmissionRegistry {
        private static ScoreSubmissionService _service;
        internal static bool IsEnabled { get; private set; } = true;

        internal static void Use(ScoreSubmissionService service) {
            _service = service;
            _service.SetEnabled(IsEnabled);
        }

        internal static void SetEnabled(bool enabled) {
            IsEnabled = enabled;
            _service?.SetEnabled(enabled);
        }
    }
}
