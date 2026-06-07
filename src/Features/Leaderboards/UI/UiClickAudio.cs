using System.Reflection;

namespace ScoreSaber.Features.Leaderboards.UI {
    internal static class UiClickAudio {
        private static readonly MethodInfo ButtonClickSound = typeof(BasicUIAudioManager).GetMethod("HandleButtonClickEvent", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void Play(BasicUIAudioManager uiAudioManager) {
            if (uiAudioManager == null) {
                return;
            }

            ButtonClickSound?.Invoke(uiAudioManager, null);
        }
    }
}
