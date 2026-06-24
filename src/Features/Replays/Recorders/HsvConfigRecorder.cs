using IPA.Utilities;
using Newtonsoft.Json.Linq;
using ScoreSaber.Features.Replays.Format;
using System;
using System.IO;
using System.Linq;

namespace ScoreSaber.Features.Replays.Recorders {
    internal class HsvConfigRecorder {
        private const int MaxSelectorBytes = 8 * 1024;
        private const string PluginConfigFileName = "HitScoreVisualizer.json";
        private const string ConfigDirectoryName = "HitScoreVisualizer";

        private static readonly string[] AllowedConfigExtensions = { ".json", ".hsv", ".hsvconfig" };

        internal byte[] Export() {

            try {
                string userDataPath = UnityGame.UserDataPath ?? "UserData";
                string selectedPath = SelectedHsvConfigPath(userDataPath);
                if (string.IsNullOrEmpty(selectedPath)) {
                    return null;
                }

                var fileInfo = new FileInfo(selectedPath);
                if (!fileInfo.Exists || fileInfo.Length > HsvReplayConfigCodec.MaxJsonBytes) {
                    return null;
                }

                string configJson = File.ReadAllText(selectedPath);
                if (!HsvReplayConfigCodec.TryEncodeJson(configJson, out byte[] payload, out string failure)) {
                    Plugin.Log.Debug("Skipping HSV replay config: " + failure);
                    return null;
                }

                return payload;
            } catch (Exception ex) {
                Plugin.Log.Debug("Failed to record HSV config: " + ex.Message);
                return null;
            }
        }

        private static string SelectedHsvConfigPath(string userDataPath) {

            string selectorPath = FindPluginConfigPath(userDataPath);
            if (string.IsNullOrEmpty(selectorPath)) {
                return null;
            }

            var selectorInfo = new FileInfo(selectorPath);
            if (!selectorInfo.Exists || selectorInfo.Length > MaxSelectorBytes) {
                return null;
            }

            string relativeConfigPath = ReadSelectedConfigPath(selectorPath);
            if (string.IsNullOrWhiteSpace(relativeConfigPath)) {
                return null;
            }

            string configRoot = Path.GetFullPath(Path.Combine(userDataPath, ConfigDirectoryName));
            string fullConfigPath = Path.GetFullPath(Path.Combine(configRoot, relativeConfigPath));
            string configRootWithSeparator = configRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullConfigPath.StartsWith(configRootWithSeparator, StringComparison.OrdinalIgnoreCase)) {
                return null;
            }

            string extension = Path.GetExtension(fullConfigPath);
            if (!AllowedConfigExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) {
                return null;
            }

            return fullConfigPath;
        }

        private static string FindPluginConfigPath(string userDataPath) {

            string primary = Path.Combine(userDataPath, PluginConfigFileName);
            if (File.Exists(primary)) {
                return primary;
            }

            if (!Directory.Exists(userDataPath)) {
                return null;
            }

            foreach (string path in Directory.EnumerateFiles(userDataPath, "*.json", SearchOption.TopDirectoryOnly)) {
                var info = new FileInfo(path);
                if (info.Length <= MaxSelectorBytes && !string.IsNullOrEmpty(ReadSelectedConfigPath(path))) {
                    return path;
                }
            }

            return null;
        }

        private static string ReadSelectedConfigPath(string selectorPath) {

            try {
                JObject root = JObject.Parse(File.ReadAllText(selectorPath));
                JProperty property = root
                    .DescendantsAndSelf()
                    .OfType<JProperty>()
                    .FirstOrDefault(candidate => string.Equals(candidate.Name, "ConfigFilePath", StringComparison.OrdinalIgnoreCase));
                return property?.Value?.Type == JTokenType.String ? property.Value.Value<string>() : null;
            } catch {
                return null;
            }
        }
    }
}
