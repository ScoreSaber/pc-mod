using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Live.Ludus.Services;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace ScoreSaber.Features.MainMenu.Settings.ViewControllers {
    [HotReload(RelativePathToLayout = @"./MainSettingsViewController.bsml")]
    internal partial class MainSettingsViewController : BSMLAutomaticViewController {
        private SettingsService _settings;
        private LudusSessionService _ludusSession;

        [Inject]
        internal void Construct(SettingsService settings, LudusSessionService ludusSession) {
            _settings = settings;
            _ludusSession = ludusSession;
        }

        // NORMAL SETTINGS
        [UIValue("showScorePP")]
        public bool ShowScorePP {
            get => _settings.Current.showScorePP;
            set => _settings.Current.showScorePP = value;
        }

        [UIValue("showLocalPlayerRank")]
        public bool ShowLocalPlayerRank {
            get => _settings.Current.showLocalPlayerRank;
            set => _settings.Current.showLocalPlayerRank = value;
        }

        [UIValue("hideNAScores")]
        public bool HideNAScores {
            get => _settings.Current.hideNAScoresFromLeaderboard;
            set => _settings.Current.hideNAScoresFromLeaderboard = value;
        }

        [UIValue("locationFilterMode")]
        public string LocationFilterMode {
            get => _settings.Current.locationFilterMode;
            set => _settings.Current.locationFilterMode = value;
        }

        [UIValue("enableCountryLeaderboards")]
        public bool EnableCountryLeaderboards {
            get => _settings.Current.enableCountryLeaderboards;
            set => _settings.Current.enableCountryLeaderboards = value;
        }

        // LIVE SETTINGS
        [UIValue("publicLivePresenceEnabled")]
        public bool PublicLivePresenceEnabled {
            get => !_settings.Current.publicLivePresenceOptOut;
            set {
                bool optOut = !value;
                if (_settings.Current.publicLivePresenceOptOut == optOut) {
                    return;
                }

                _settings.Current.publicLivePresenceOptOut = optOut;
                _settings.Save();
                _ludusSession.ApplyPublicLivePresencePreference();
            }
        }

        [UIValue("liveChatOverlayEnabled")]
        public bool LiveChatOverlayEnabled {
            get => _settings.Current.liveChatOverlayEnabled;
            set {
                if (_settings.Current.liveChatOverlayEnabled == value) {
                    return;
                }

                _settings.Current.liveChatOverlayEnabled = value;
                _settings.Save();
            }
        }

        [UIValue("liveChatOverlayGameplayEnabled")]
        public bool LiveChatOverlayGameplayEnabled {
            get => _settings.Current.liveChatOverlayGameplayEnabled;
            set {
                if (_settings.Current.liveChatOverlayGameplayEnabled == value) {
                    return;
                }

                _settings.Current.liveChatOverlayGameplayEnabled = value;
                _settings.Save();
            }
        }

        [UIValue("liveChatOverlayScale")]
        public float LiveChatOverlayScale {
            get => _settings.Current.liveChatOverlayScale;
            set {
                _settings.Current.liveChatOverlayScale = Clamp(value, 0.85f, 1.75f);
                _settings.Save();
            }
        }

        [UIValue("liveChatOverlayTextScale")]
        public float LiveChatOverlayTextScale {
            get => _settings.Current.liveChatOverlayTextScale;
            set {
                _settings.Current.liveChatOverlayTextScale = Clamp(value, 0.9f, 1.8f);
                _settings.Save();
            }
        }

        [UIValue("shareHsvProfiles")]
        public bool ShareHsvProfiles {
            get => _settings.Current.shareHsvProfiles;
            set {
                if (_settings.Current.shareHsvProfiles == value) {
                    return;
                }

                _settings.Current.shareHsvProfiles = value;
                _settings.Save();
            }
        }

        [UIValue("locationFilterOptions")]
        public List<object> LocationFilterOptions = new object[] {
            "Country",
            "Region",
        }.ToList();

        // REPLAY SETTINGS

        [UIValue("saveLocalReplays")]
        public bool SaveLocalReplays {
            get => _settings.Current.saveLocalReplays;
            set => _settings.Current.saveLocalReplays = value;
        }

        [UIValue("replayCameraSmoothing")]
        public bool ReplayCameraSmoothing {
            get => _settings.Current.replayCameraSmoothing;
            set => _settings.Current.replayCameraSmoothing = value;
        }

        [UIValue("replayOverrideHandedness")]
        public bool ReplayOverrideHandedness {
            get => _settings.Current.replayOverrideHandedness;
            set => _settings.Current.replayOverrideHandedness = value;
        }

        [UIValue("useRecordedPlayerSettings")]
        public bool UseRecordedPlayerSettings {
            get => _settings.Current.useRecordedPlayerSettings;
            set => _settings.Current.useRecordedPlayerSettings = value;
        }

        [UIValue("replayCameraFOV")]
        public float ReplayCameraFOV {
            get => _settings.Current.replayCameraFOV;
            set => _settings.Current.replayCameraFOV = value;
        }

        [UIValue("currentXValueRotation")]
        public float currentXValueRotation {
            get => _settings.Current.replayCameraXRotation;
            set => _settings.Current.replayCameraXRotation = value;
        }


        [UIValue("currentYValueRotation")]
        public float currentYValueRotation {
            get => _settings.Current.replayCameraYRotation;
            set => _settings.Current.replayCameraYRotation = value;
        }

        [UIValue("currentZValueRotation")]
        public float currentZValueRotation {
            get => _settings.Current.replayCameraZRotation;
            set => _settings.Current.replayCameraZRotation = value;
        }

        [UIValue("currentXValueOffset")]
        public float currentXValueOffset {
            get => _settings.Current.replayCameraXOffset;
            set => _settings.Current.replayCameraXOffset = value;
        }


        [UIValue("currentYValueOffset")]
        public float currentYValueOffset {
            get => _settings.Current.replayCameraYOffset;
            set => _settings.Current.replayCameraYOffset = value;
        }

        [UIValue("currentZValueOffset")]
        public float currentZValueOffset {
            get => _settings.Current.replayCameraZOffset;
            set => _settings.Current.replayCameraZOffset = value;
        }

        private static float Clamp(float value, float min, float max) {
            if (value < min) {
                return min;
            }

            if (value > max) {
                return max;
            }

            return value;
        }
    }
}
