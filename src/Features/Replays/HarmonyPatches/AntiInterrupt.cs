using HarmonyLib;

namespace ScoreSaber.Features.Replays.HarmonyPatches {
    [HarmonyPatch(typeof(PauseController), nameof(PauseController.HandleHMDUnmounted))]
    internal class PatchHandleHMDUnmounted {
        internal static bool Prefix() => !ReplayStateRegistry.IsPlaybackEnabled;
    }
}
