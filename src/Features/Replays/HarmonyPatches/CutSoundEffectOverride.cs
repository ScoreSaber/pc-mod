using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScoreSaber.Features.Replays.HarmonyPatches {
    [HarmonyPatch(typeof(NoteCutSoundEffectManager), nameof(NoteCutSoundEffectManager.HandleNoteWasSpawned))]
    internal class CutSoundEffectOverride {
        private static IEnumerator _buffer;
        private static NoteCutSoundEffectManager _spawnEffectManager;
        private static readonly Queue<NoteController> _effects = new Queue<NoteController>();
        internal static bool Buffer { get; set; }

        internal static bool Prefix(NoteCutSoundEffectManager __instance, NoteController noteController) {

            if (ReplayStateRegistry.IsModernPlaybackEnabled) {
                if (_spawnEffectManager == null || _spawnEffectManager != __instance) {
                    _spawnEffectManager = __instance;
                    _effects.Clear();
                    _buffer = null;
                    Buffer = false;
                    return true;
                }

                if (!Buffer)
                    return true;

                if (!_effects.Contains(noteController)) {
                    _effects.Enqueue(noteController);
                    if (_buffer == null) {
                        _buffer = BufferNoteSpawn(__instance);
                        __instance.StartCoroutine(_buffer);
                    }
                    return false;
                }
                return true;
            }
            return true;
        }

        private static IEnumerator BufferNoteSpawn(NoteCutSoundEffectManager manager) {

            while (_effects.Count > 0) {
                var effect = _effects.Peek();
                manager.HandleNoteWasSpawned(effect);
                _effects.Dequeue();
                yield return new WaitForEndOfFrame();
            }
            Buffer = false;
            _buffer = null;
        }
    }
}
