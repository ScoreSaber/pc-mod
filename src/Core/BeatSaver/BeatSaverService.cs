using Newtonsoft.Json;
using ScoreSaber.Core.Compat;
using SongCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ScoreSaber.Core.BeatSaver {
    internal class BeatSaverService {
        private const int DownloadAttemptCount = 3;
        private const int DownloadRetryDelayMs = 750;
        private const int DownloadTimeoutSeconds = 45;
        private static readonly TimeSpan DownloadStallTimeout = TimeSpan.FromSeconds(30);

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

            if (!IsBeatSaverHash(normalizedHash)) {
                throw new ArgumentException("BeatSaver hash must be a 40-character SHA1", nameof(hash));
            }

            string lowerHash = normalizedHash.ToLowerInvariant();
            string songUrl = string.IsNullOrEmpty(version?.DownloadUrl)
                ? $"https://cdn.beatsaver.com/{lowerHash}.zip"
                : version.DownloadUrl;
            string customSongsPath = Path.GetFullPath(CustomLevelPathHelper.customLevelsDirectoryPath);
            string customSongPath = Path.Combine(customSongsPath, lowerHash);
            string tempRootPath = Path.Combine(customSongsPath, $"{lowerHash}.{Guid.NewGuid():N}.download");
            string tempSongPath = Path.Combine(tempRootPath, "song");
            string zipPath = Path.Combine(tempRootPath, $"{lowerHash}.zip");

            try {
                Directory.CreateDirectory(customSongsPath);
                Directory.CreateDirectory(tempRootPath);
                TrySetHidden(tempRootPath, true);

                await DownloadAndExtractMap(BuildDownloadUrls(songUrl, lowerHash), zipPath, tempSongPath, cancellationToken);
                TrySetHidden(tempRootPath, false);
                ReplaceDirectory(tempSongPath, customSongPath);
            } finally {
                TryDelete(zipPath);
                TryDeleteDirectory(tempRootPath);
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

        private async Task DownloadAndExtractMap(IReadOnlyList<string> urls, string zipPath, string tempSongPath, CancellationToken cancellationToken) {
            Exception lastError = null;

            foreach (string url in urls) {
                for (int attempt = 1; attempt <= DownloadAttemptCount; attempt++) {
                    cancellationToken.ThrowIfCancellationRequested();
                    TryDelete(zipPath);
                    TryDeleteDirectory(tempSongPath);

                    try {
                        await DownloadZipToFile(url, zipPath, cancellationToken);
                        EnsureDownloadedZip(zipPath);
                        Directory.CreateDirectory(tempSongPath);
                        ExtractZip(zipPath, tempSongPath, cancellationToken);
                        TryDelete(zipPath);
                        return;
                    } catch (OperationCanceledException) {
                        throw;
                    } catch (Exception ex) {
                        lastError = ex;
                        Plugin.Log.Warn($"BeatSaver map download failed from {url} (attempt {attempt}/{DownloadAttemptCount}): {ex.Message}");
                        TryDelete(zipPath);
                        TryDeleteDirectory(tempSongPath);

                        if (attempt >= DownloadAttemptCount || !ShouldRetry(ex)) {
                            break;
                        }

                        await Task.Delay(DownloadRetryDelayMs * attempt, cancellationToken);
                    }
                }
            }

            throw new IOException("Failed to download BeatSaver map zip", lastError);
        }

        private async Task DownloadZipToFile(string url, string zipPath, CancellationToken cancellationToken) {
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)) {
                foreach (var header in _http.PersistentRequestHeaders) {
                    request.SetRequestHeader(header.Key, header.Value);
                }

                request.timeout = DownloadTimeoutSeconds;
                request.downloadHandler = new DownloadHandlerFile(zipPath) {
                    removeFileOnAbort = true
                };

                AsyncOperation asyncOperation = request.SendWebRequest();
                ulong downloadedBytes = 0;
                DateTime lastProgressAt = DateTime.UtcNow;

                try {
                    while (!asyncOperation.isDone) {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (request.downloadedBytes > downloadedBytes) {
                            downloadedBytes = request.downloadedBytes;
                            lastProgressAt = DateTime.UtcNow;
                        } else if (DateTime.UtcNow - lastProgressAt > DownloadStallTimeout) {
                            request.Abort();
                            throw new BeatSaverDownloadException("download stalled", true);
                        }

                        await Task.Delay(100, cancellationToken);
                    }
                } catch (OperationCanceledException) {
                    request.Abort();
                    throw;
                }

                if (request.IsConnectionError() || request.IsProtocolError()) {
                    throw CreateDownloadException(request);
                }
            }
        }

        private static IReadOnlyList<string> BuildDownloadUrls(string songUrl, string lowerHash) {
            var urls = new List<string>();
            AddDownloadUrl(urls, songUrl);
            AddDownloadUrl(urls, $"https://cdn.beatsaver.com/{lowerHash}.zip");
            return urls;
        }

        private static void AddDownloadUrl(List<string> urls, string url) {
            if (string.IsNullOrWhiteSpace(url)) {
                return;
            }

            string trimmed = url.Trim();
            if (urls.Any(existing => string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase))) {
                return;
            }

            urls.Add(trimmed);
        }

        private static void EnsureDownloadedZip(string zipPath) {
            var zipFile = new FileInfo(zipPath);
            if (!zipFile.Exists || zipFile.Length == 0) {
                throw new BeatSaverDownloadException("downloaded zip was empty", true);
            }
        }

        private static bool IsBeatSaverHash(string hash) {
            return hash.Length == 40 && hash.All(Uri.IsHexDigit);
        }

        private static void ExtractZip(string zipPath, string tempSongPath, CancellationToken cancellationToken) {
            int extractedFiles = 0;
            string rootPath = Path.GetFullPath(tempSongPath);
            if (!rootPath.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                rootPath += Path.DirectorySeparatorChar;
            }

            using (ZipArchive archive = ZipFile.OpenRead(zipPath)) {
                foreach (ZipArchiveEntry entry in archive.Entries) {
                    cancellationToken.ThrowIfCancellationRequested();

                    string destinationPath = Path.GetFullPath(Path.Combine(tempSongPath, entry.FullName));
                    if (!destinationPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)) {
                        throw new BeatSaverDownloadException("downloaded zip contained an unsafe path", false);
                    }

                    if (string.IsNullOrEmpty(entry.Name)) {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    entry.ExtractToFile(destinationPath);
                    extractedFiles++;
                }
            }

            if (extractedFiles == 0) {
                throw new BeatSaverDownloadException("downloaded zip did not contain map files", true);
            }
        }

        private static void ReplaceDirectory(string sourcePath, string destinationPath) {
            string backupPath = null;
            bool replaced = false;

            if (Directory.Exists(destinationPath)) {
                backupPath = $"{destinationPath}.{Guid.NewGuid():N}.backup";
                Directory.Move(destinationPath, backupPath);
            }

            try {
                Directory.Move(sourcePath, destinationPath);
                replaced = true;
            } catch {
                TryRestoreDirectory(backupPath, destinationPath);
                throw;
            } finally {
                if (replaced && backupPath != null) {
                    TryDeleteDirectory(backupPath);
                }
            }
        }

        private static void TryRestoreDirectory(string backupPath, string destinationPath) {
            if (backupPath == null || Directory.Exists(destinationPath) || !Directory.Exists(backupPath)) {
                return;
            }

            try {
                Directory.Move(backupPath, destinationPath);
            } catch (IOException ex) {
                Plugin.Log.Warn($"Unable to restore previous BeatSaver map folder: {ex.Message}");
            } catch (UnauthorizedAccessException ex) {
                Plugin.Log.Warn($"Unable to restore previous BeatSaver map folder: {ex.Message}");
            }
        }

        private static bool ShouldRetry(Exception ex) {
            BeatSaverDownloadException downloadException = ex as BeatSaverDownloadException;
            if (downloadException != null) {
                return downloadException.Retryable;
            }

            return ex is IOException || ex is InvalidDataException;
        }

        private static BeatSaverDownloadException CreateDownloadException(UnityWebRequest request) {
            int statusCode = (int)request.responseCode;
            string message = !string.IsNullOrEmpty(request.error)
                ? request.error
                : statusCode > 0
                    ? $"HTTP {statusCode}"
                    : "download request failed";
            bool retryable = request.IsConnectionError() || statusCode == 0 || statusCode == 408 || statusCode == 429 || statusCode >= 500;

            return new BeatSaverDownloadException(message, retryable);
        }

        private static void TryDelete(string path) {
            try {
                File.Delete(path);
            } catch (IOException ex) {
                Plugin.Log.Warn($"Unable to delete BeatSaver map zip: {ex.Message}");
            } catch (UnauthorizedAccessException ex) {
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
                Plugin.Log.Warn($"Unable to delete BeatSaver map folder: {ex.Message}");
            } catch (UnauthorizedAccessException ex) {
                Plugin.Log.Warn($"Unable to delete BeatSaver map folder: {ex.Message}");
            }
        }

        private sealed class BeatSaverDownloadException : Exception {
            internal bool Retryable { get; }

            internal BeatSaverDownloadException(string message, bool retryable) : base(message) {
                Retryable = retryable;
            }
        }
    }
}
