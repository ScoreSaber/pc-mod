using Newtonsoft.Json;
using SongCore;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Core.BeatSaver {
    internal class BeatSaverService {
        private readonly Http _http;

        internal BeatSaverService(Http http) {
            _http = http;
        }

        internal async Task<BeatSaverMap> GetMapByHash(string hash, CancellationToken cancellationToken) {
            string normalizedHash = NormalizeHash(hash);
            if (string.IsNullOrEmpty(normalizedHash)) {
                throw new ArgumentException("BeatSaver hash is required", nameof(hash));
            }

            string response = await _http.GetRawAsync($"https://api.beatsaver.com/maps/hash/{normalizedHash.ToLowerInvariant()}");
            cancellationToken.ThrowIfCancellationRequested();
            return JsonConvert.DeserializeObject<BeatSaverMap>(response);
        }

        internal async Task<BeatSaverMap> GetMapById(string id, CancellationToken cancellationToken) {
            if (string.IsNullOrWhiteSpace(id)) {
                throw new ArgumentException("BeatSaver id is required", nameof(id));
            }

            string response = await _http.GetRawAsync($"https://api.beatsaver.com/maps/id/{id.Trim()}");
            cancellationToken.ThrowIfCancellationRequested();
            return JsonConvert.DeserializeObject<BeatSaverMap>(response);
        }

        internal async Task DownloadMapByHash(string hash, BeatSaverVersion version, CancellationToken cancellationToken) {
            string normalizedHash = NormalizeHash(hash);
            if (string.IsNullOrEmpty(normalizedHash)) {
                throw new ArgumentException("BeatSaver hash is required", nameof(hash));
            }

            string lowerHash = normalizedHash.ToLowerInvariant();
            string songUrl = string.IsNullOrEmpty(version?.DownloadUrl)
                ? $"https://cdn.beatsaver.com/{lowerHash}.zip"
                : version.DownloadUrl;
            string customSongsPath = Path.GetFullPath(CustomLevelPathHelper.customLevelsDirectoryPath);
            string customSongPath = Path.Combine(customSongsPath, lowerHash);
            string tempSongPath = Path.Combine(customSongsPath, $"{lowerHash}.{Guid.NewGuid():N}.download");
            string zipPath = Path.Combine(tempSongPath, $"{lowerHash}.zip");

            try {
                Directory.CreateDirectory(customSongsPath);
                Directory.CreateDirectory(tempSongPath);
                TrySetHidden(tempSongPath, true);

                byte[] data = await _http.DownloadRawAsync(songUrl);
                cancellationToken.ThrowIfCancellationRequested();
                File.WriteAllBytes(zipPath, data);
                ZipFile.ExtractToDirectory(zipPath, tempSongPath);
                cancellationToken.ThrowIfCancellationRequested();
                TryDelete(zipPath);

                TrySetHidden(tempSongPath, false);
                if (Directory.Exists(customSongPath)) {
                    Directory.Delete(customSongPath, true);
                }
                Directory.Move(tempSongPath, customSongPath);
            } finally {
                TryDelete(zipPath);
                TryDeleteDirectory(tempSongPath);
            }
        }

        internal BeatSaverVersion SelectVersion(BeatSaverMap map, string hash) {
            string normalizedHash = NormalizeHash(hash);
            BeatSaverVersion[] versions = map?.Versions ?? Array.Empty<BeatSaverVersion>();
            return versions.FirstOrDefault(version => string.Equals(version.Hash, normalizedHash, StringComparison.OrdinalIgnoreCase)) ??
                versions.FirstOrDefault();
        }

        internal BeatSaverDifficulty SelectDifficulty(BeatSaverVersion version, string difficulty) {
            BeatSaverDifficulty[] diffs = version?.Diffs ?? Array.Empty<BeatSaverDifficulty>();
            string normalizedDifficulty = NormalizeDifficulty(difficulty);
            return diffs.FirstOrDefault(diff => string.Equals(NormalizeDifficulty(diff.Difficulty), normalizedDifficulty, StringComparison.OrdinalIgnoreCase)) ??
                diffs.FirstOrDefault();
        }

        internal static string NormalizeHash(string hash) {
            return hash?.Trim() ?? string.Empty;
        }

        internal static string NormalizeDifficulty(string difficulty) {
            if (string.IsNullOrEmpty(difficulty)) {
                return string.Empty;
            }

            return difficulty.Replace("+", "Plus");
        }

        private static void TryDelete(string path) {
            try {
                File.Delete(path);
            } catch (IOException ex) {
                Plugin.Log.Warn($"Unable to delete BeatSaver map zip: {ex.Message}");
            }
        }

        private static void TrySetHidden(string path, bool hidden) {
            try {
                FileAttributes attributes = File.GetAttributes(path);
                File.SetAttributes(path, hidden ? attributes | FileAttributes.Hidden : attributes & ~FileAttributes.Hidden);
            } catch (IOException ex) {
                Plugin.Log.Warn($"Unable to update BeatSaver map temp folder attributes: {ex.Message}");
            } catch (UnauthorizedAccessException ex) {
                Plugin.Log.Warn($"Unable to update BeatSaver map temp folder attributes: {ex.Message}");
            }
        }

        private static void TryDeleteDirectory(string path) {
            try {
                if (Directory.Exists(path)) {
                    Directory.Delete(path, true);
                }
            } catch (IOException ex) {
                Plugin.Log.Warn($"Unable to delete BeatSaver map temp folder: {ex.Message}");
            } catch (UnauthorizedAccessException ex) {
                Plugin.Log.Warn($"Unable to delete BeatSaver map temp folder: {ex.Message}");
            }
        }
    }
}
