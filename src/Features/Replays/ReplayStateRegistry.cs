namespace ScoreSaber.Features.Replays {
    internal static class ReplayStateRegistry {
        internal static ReplayState Current { get; private set; } = new ReplayState();
        internal static bool IsPlaybackEnabled => Current.IsPlaybackEnabled;
        internal static bool IsModernPlaybackEnabled => Current.IsPlaybackEnabled && !Current.IsLegacyReplay;
        internal static bool IsLegacyPlaybackEnabled => Current.IsPlaybackEnabled && Current.IsLegacyReplay;

        internal static void Use(ReplayState replayState) {
            Current = replayState;
        }
    }
}
