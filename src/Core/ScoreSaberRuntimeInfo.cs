using System;
using System.Security.Cryptography;
using System.Text;

namespace ScoreSaber.Core {
    internal class ScoreSaberRuntimeInfo {
        internal Version PluginVersion { get; }
        internal Hive.Versioning.Version GameVersion { get; }
        internal string UploadVersionHash { get; }

        internal ScoreSaberRuntimeInfo(Version pluginVersion, Hive.Versioning.Version gameVersion, string uploadGameVersion) {
            PluginVersion = pluginVersion;
            GameVersion = gameVersion;
            UploadVersionHash = BuildVersionHash(pluginVersion, uploadGameVersion);
        }

        private static string BuildVersionHash(Version pluginVersion, string uploadGameVersion) {
            using (var md5 = MD5.Create()) {
                string versionString = $"{pluginVersion}{uploadGameVersion}";
                return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(versionString))).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
