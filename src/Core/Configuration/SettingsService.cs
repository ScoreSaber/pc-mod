using Newtonsoft.Json;
using System;
using System.IO;

namespace ScoreSaber.Core.Configuration {
    internal class SettingsService {
        private const int CurrentVersion = 11;

        internal string DataPath => "UserData";
        internal string ConfigPath => DataPath + @"\ScoreSaber";
        internal string ReplayPath => ConfigPath + @"\Replays";
        private string SettingsPath => ConfigPath + @"\ScoreSaber.json";

        internal Settings Current { get; private set; } = CreateDefaultSettings();

        internal void Load() {
            try {
                EnsureDirectories();

                if (!File.Exists(SettingsPath)) {
                    Current = CreateDefaultSettings();
                    Save();
                    return;
                }

                Current = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingsPath)) ?? CreateDefaultSettings();
                if (Current.fileVersion < CurrentVersion) {
                    Upgrade(Current);
                    Save();
                }
            } catch (Exception ex) {
                Plugin.Log.Error("Failed to load settings " + ex.ToString());
                Current = CreateDefaultSettings();
            }
        }

        internal void Save() {
            try {
                EnsureDirectories();
                Current.fileVersion = CurrentVersion;

                var serializerSettings = new JsonSerializerSettings {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                string serialized = JsonConvert.SerializeObject(Current, serializerSettings);
                File.WriteAllText(SettingsPath, serialized);
            } catch (Exception ex) {
                Plugin.Log.Error("Failed to save settings " + ex.ToString());
            }
        }

        internal string ReplayPathFor(string playerId, string songHash, BeatmapKey beatmapKey) => $@"{ReplayPath}\{playerId}-{songHash}-{beatmapKey.difficulty.SerializedName()}-{beatmapKey.beatmapCharacteristic.serializedName}.dat";

        internal string LegacyReplayPathFor(string playerId, string songName, string difficulty, string characteristic, string songHash) => $@"{ReplayPath}\{playerId}-{songName}-{difficulty}-{characteristic}-{songHash}.dat";

        internal string LegacyReplayPathFor(string playerId, string songName, string songHash) => $@"{ReplayPath}\{playerId}-{songName}-{songHash}.dat";

        private void EnsureDirectories() {
            Directory.CreateDirectory(DataPath);
            Directory.CreateDirectory(ConfigPath);
            Directory.CreateDirectory(ReplayPath);
        }

        private static Settings CreateDefaultSettings() {
            var settings = new Settings();
            settings.SetDefaults();
            return settings;
        }

        private static void Upgrade(Settings settings) {
            if (settings.spectatorPositions == null) {
                settings.SetDefaultSpectatorPositions();
            }
            if (settings.locationFilterMode == null) {
                settings.locationFilterMode = "Country";
            }
            if (settings.fileVersion < 8) {
                settings.replayCameraSmoothing = true;
            }
            if (settings.fileVersion < 9) {
                settings.replayOverrideHandedness = false;
            }
            if (settings.fileVersion < 10) {
                settings.publicLivePresenceOptOut = false;
                settings.liveChatOverlayEnabled = true;
                settings.liveChatOverlayGameplayEnabled = true;
                settings.liveChatOverlayScale = 1.15f;
                settings.liveChatOverlayTextScale = 1.25f;
            }
            if (settings.fileVersion < 11) {
                settings.useRecordedPlayerSettings = true;
            }
        }
    }
}
