using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using UnityEngine;

namespace ScoreSaber.Features.Live.Compete.UI.ViewControllers.CodeEntry {
    [HotReload]
    internal class CompeteCodeEntryViewController : BSMLAutomaticViewController {
        internal event Action<string> JoinRequested;

        private string _joinCode = string.Empty;
        private string _status = string.Empty;

        [UIValue("join-code")]
        private string joinCode {
            get => _joinCode;
            set => SetValue(ref _joinCode, value, nameof(joinCode));
        }

        [UIValue("join-code-display")]
        private string joinCodeDisplay => string.IsNullOrEmpty(joinCode) ? "--------" : joinCode;

        [UIValue("status")]
        private string status {
            get => _status;
            set => SetValue(ref _status, value, nameof(status));
        }

        internal void Reset() {
            joinCode = string.Empty;
            status = string.Empty;
        }

        internal void SetStatus(string value) {
            status = value;
        }

        [UIAction("code-entered")]
        private void CodeEntered(string value) {
            joinCode = value.Trim().ToLowerInvariant();
            NotifyPropertyChanged(nameof(joinCodeDisplay));
        }

        [UIAction("paste-code")]
        private void PasteCode() {
            string clipboard = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrWhiteSpace(clipboard)) {
                status = "Clipboard is empty.";
                return;
            }

            joinCode = clipboard.Trim().ToLowerInvariant();
            status = string.Empty;
        }

        [UIAction("join-code")]
        private void JoinCode() {
            if (string.IsNullOrWhiteSpace(joinCode)) {
                status = "Enter a room code first.";
                return;
            }

            status = string.Empty;
            JoinRequested?.Invoke(joinCode);
        }

        private void SetValue<T>(ref T field, T value, string propertyName) {
            field = value;
            NotifyPropertyChanged(propertyName);
            if (propertyName == nameof(joinCode)) {
                NotifyPropertyChanged(nameof(joinCodeDisplay));
            }
        }
    }
}
