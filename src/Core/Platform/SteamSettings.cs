using Microsoft.Win32;
using System;
using System.IO;

namespace ScoreSaber.Core.Platform {
    internal class SteamSettings {

        internal class PartialSteamVRSettings {
            public LastKnown LastKnown { get; set; }
        }

        internal class LastKnown {
            public string HMDManufacturer { get; set; }
            public string HMDModel { get; set; }
        }

        internal static string HMDName = null;

        internal static void Initialize() {
            HMDName = AttemptGetHMD();
        }

        private static string AttemptGetHMD() {
            try {
                string steamDir = GetSteamDir();
                if (string.IsNullOrEmpty(steamDir)) return null;

                string steamVRConfigPath = Path.Combine(steamDir, "config", "steamvr.vrsettings");

                if (!File.Exists(steamVRConfigPath)) return null;

                var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<PartialSteamVRSettings>(File.ReadAllText(steamVRConfigPath));
                return $"{settings.LastKnown.HMDManufacturer}:{settings.LastKnown.HMDModel}";
            } catch (Exception ex) {
                Plugin.Log.Info($"Failed to get HMD from SteamSettings {ex}");
                return null;
            }
        }

        private static string GetSteamDir() {
            string SteamInstall = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)?.OpenSubKey("SOFTWARE")?.OpenSubKey("WOW6432Node")?.OpenSubKey("Valve")?.OpenSubKey("Steam")?.GetValue("InstallPath").ToString();
            if (string.IsNullOrEmpty(SteamInstall)) {
                SteamInstall = Registry.LocalMachine.OpenSubKey("SOFTWARE")?.OpenSubKey("WOW6432Node")?.OpenSubKey("Valve")?.OpenSubKey("Steam")?.GetValue("InstallPath").ToString();
            }
            return SteamInstall;
        }
    }
}
