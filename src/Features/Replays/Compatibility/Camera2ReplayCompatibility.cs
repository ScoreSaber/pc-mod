namespace ScoreSaber.Core.ReplaySystem.HarmonyPatches {
    internal static class PatchHandleHMDUnmounted {
        internal static bool Prefix() => !ScoreSaber.Features.Replays.ReplayStateRegistry.IsPlaybackEnabled;
    }
}
