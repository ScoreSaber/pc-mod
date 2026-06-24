using HarmonyLib;
using System.Collections.Generic;

namespace ScoreSaber.Features.Replays.HarmonyPatches {
    [HarmonyPatch(typeof(BeatmapObjectManager), nameof(BeatmapObjectManager.HandleNoteControllerNoteWasMissed))]
    internal static class ReplayNoteMissEventGuard {
        private static readonly HashSet<NoteController> _allowedReplayMisses = new HashSet<NoteController>();

        internal static void Allow(NoteController noteController) {
            _allowedReplayMisses.Add(noteController);
        }

        internal static void Clear(NoteController noteController) {
            _allowedReplayMisses.Remove(noteController);
        }

        private static bool Prefix(NoteController noteController) {
            return !ReplayStateRegistry.IsModernPlaybackEnabled || _allowedReplayMisses.Contains(noteController);
        }
    }
}
