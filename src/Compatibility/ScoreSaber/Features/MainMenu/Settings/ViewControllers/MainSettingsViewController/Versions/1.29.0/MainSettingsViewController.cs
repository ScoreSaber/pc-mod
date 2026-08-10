using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using IPA.Utilities;
using UnityEngine;

namespace ScoreSaber.Features.MainMenu.Settings.ViewControllers {
    internal partial class MainSettingsViewController {
        [UIComponent("replay-settings-scroll")]
        private readonly ScrollView _replaySettingsScroll = null;

        [UIAction("#post-parse")]
        private void FixLegacyReplaySettingsScroll() {
            if (_replaySettingsScroll == null) {
                return;
            }

            RectTransform scrollRect = _replaySettingsScroll.transform as RectTransform;
            if (scrollRect != null) {
                Vector2 center = new Vector2(0.5f, 0.5f);
                scrollRect.anchorMin = center;
                scrollRect.anchorMax = center;
                scrollRect.pivot = center;
                scrollRect.anchoredPosition = Vector2.zero;
                scrollRect.sizeDelta = new Vector2(100f, 44f);
            }

            RectTransform viewport = _replaySettingsScroll.GetField<RectTransform, ScrollView>("_viewport");
            if (viewport == null) {
                return;
            }

            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.anchoredPosition = Vector2.zero;
            viewport.sizeDelta = Vector2.zero;
        }
    }
}
