// camera2 reflects this old full name and treats Prefix() == false as replay playback
namespace ScoreSaber.Core.ReplaySystem.HarmonyPatches {
    internal static class PatchHandleHMDUnmounted {
        internal static bool Prefix() => !ScoreSaber.Features.Replays.ReplayStateRegistry.IsPlaybackEnabled;
    }
}
