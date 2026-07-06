using IPA.Utilities.Async;
using ScoreSaber.Core.Api;
using ScoreSaber.Core.Api.Generated;
using ScoreSaber.Core.BeatSaver;
using ScoreSaber.Core.Compat;
using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Live.V1;
using SongCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Live.Compete.Services {
    internal class CompeteSongService {
        private const int SongRefreshTimeoutMs = 30000;

        private readonly BeatSaverService _beatSaver;
        private readonly IScoreSaberApiClient _apiClient;
        private readonly BeatmapLevelsModel _beatmapLevelsModel;
        private static readonly object _songsLoadedLock = new object();
        private static readonly List<TaskCompletionSource<bool>> _songsLoadedCompletions = new List<TaskCompletionSource<bool>>();
        private static readonly object _mapDownloadsLock = new object();
        private static readonly Dictionary<string, Task> _mapDownloadsByHash = new Dictionary<string, Task>(StringComparer.OrdinalIgnoreCase);

        internal CompeteSongService(BeatSaverService beatSaver, IScoreSaberApiClient apiClient, BeatmapLevelsModel beatmapLevelsModel) {
            _beatSaver = beatSaver;
            _apiClient = apiClient;
            _beatmapLevelsModel = beatmapLevelsModel;
        }

        internal async Task<CompeteSongSelection> ResolveOrDownload(LiveSongCommand song, CancellationToken cancellationToken) {
            LiveSongDetails scoreSaberDetails = await TryFetchScoreSaberSongDetails(song, cancellationToken);
            CompeteSongSelection installed = await ResolveInstalled(song, scoreSaberDetails, cancellationToken);
            if (installed != null) {
                return installed;
            }

            BeatSaverMap map = await TryFetchBeatSaverMap(song, cancellationToken);
            BeatSaverVersion version = _beatSaver.SelectVersion(map, SongHash(song));
            LiveSongDetails details = MergeSongDetails(scoreSaberDetails, BuildBeatSaverSongDetails(song, map, version));
            await DownloadAndRefresh(SongHash(song), version, cancellationToken);

            CompeteSongSelection resolved = await ResolveInstalled(song, details, cancellationToken);
            if (resolved != null) {
                return resolved;
            }

            await RefreshSongs(cancellationToken);
            resolved = await ResolveInstalled(song, details, cancellationToken);
            if (resolved == null) {
                throw new InvalidOperationException("SongCore could not resolve the downloaded song");
            }

            return resolved;
        }

        internal async Task<CompeteSongSelection> ResolveInstalled(LiveSongCommand song, CancellationToken cancellationToken) {
            LiveSongDetails scoreSaberDetails = await TryFetchScoreSaberSongDetails(song, cancellationToken);
            return await ResolveInstalled(song, scoreSaberDetails, cancellationToken);
        }

        internal async Task<CompeteSongSelection> ResolveInstalledAfterRefresh(LiveSongCommand song, CancellationToken cancellationToken) {
            LiveSongDetails scoreSaberDetails = await TryFetchScoreSaberSongDetails(song, cancellationToken);
            return await ResolveInstalledAfterRefresh(song, scoreSaberDetails, cancellationToken);
        }

        private async Task<CompeteSongSelection> ResolveInstalled(LiveSongCommand song, LiveSongDetails scoreSaberDetails, CancellationToken cancellationToken) {
            string hash = SongHash(song);
            if (string.IsNullOrEmpty(hash)) {
                return null;
            }

            BeatmapLevel level = await BeatmapLevelCompat.GetLevelByHash(_beatmapLevelsModel, hash, cancellationToken);
            if (level == null) {
                return null;
            }

            BeatmapKey? key = FindBeatmapKey(level, song);
            if (!key.HasValue) {
                return null;
            }

            return CreateSongSelection(level, key.Value, song, scoreSaberDetails);
        }

        private async Task<CompeteSongSelection> ResolveInstalledAfterRefresh(LiveSongCommand song, LiveSongDetails scoreSaberDetails, CancellationToken cancellationToken) {
            await RefreshSongs(cancellationToken);
            return await ResolveInstalled(song, scoreSaberDetails, cancellationToken);
        }

        internal async Task<CompeteSongSelection> CreatePreview(LiveSongCommand song, CancellationToken cancellationToken) {
            if (song == null) {
                return null;
            }

            LiveSongDetails scoreSaberDetails = await TryFetchScoreSaberSongDetails(song, cancellationToken);
            BeatSaverMap map = await TryFetchBeatSaverMap(song, cancellationToken);
            BeatSaverVersion version = _beatSaver.SelectVersion(map, SongHash(song));
            return CreatePreview(song, MergeSongDetails(scoreSaberDetails, BuildBeatSaverSongDetails(song, map, version)));
        }

        private async Task<LiveSongDetails> TryFetchScoreSaberSongDetails(LiveSongCommand song, CancellationToken cancellationToken) {
            string hash = SongHash(song);
            if (string.IsNullOrEmpty(hash)) {
                return null;
            }

            try {
                MapDetailsResponse map = await _apiClient.GetMapByHash(hash, cancellationToken);
                return BuildScoreSaberSongDetails(song, map);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                Plugin.Log.Warn($"Unable to fetch ScoreSaber live song details: {ex.Message}");
                return null;
            }
        }

        private async Task<BeatSaverMap> TryFetchBeatSaverMap(LiveSongCommand song, CancellationToken cancellationToken) {
            try {
                return await _beatSaver.GetMapByHash(SongHash(song), cancellationToken);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                Plugin.Log.Warn($"Unable to fetch BeatSaver live song details: {ex.Message}");
                return null;
            }
        }

        private LiveSongDetails BuildBeatSaverSongDetails(LiveSongCommand song, BeatSaverMap map, BeatSaverVersion version) {
            string hash = SongHash(song);
            BeatSaverDifficulty diff = _beatSaver.SelectDifficulty(version, song?.Difficulty);
            BeatSaverMapMetadata metadata = map?.Metadata;

            string name = DisplaySongName(metadata?.SongName, metadata?.SongSubName);
            if (string.IsNullOrEmpty(name)) {
                name = FirstNonEmpty(map?.Name, hash);
            }

            return new LiveSongDetails {
                Hash = FirstNonEmpty(version?.Hash, hash).ToUpperInvariant(),
                Name = name,
                Mapper = FirstNonEmpty(metadata?.LevelAuthorName, map?.Uploader?.Name, metadata?.SongAuthorName, "Unknown"),
                Difficulty = diff?.Difficulty ?? string.Empty,
                Characteristic = diff?.Characteristic ?? string.Empty,
                CoverUrl = version?.CoverUrl ?? string.Empty,
                DownloadUrl = version?.DownloadUrl ?? string.Empty,
                Duration = FormatDuration(metadata?.Duration),
                Bpm = FormatNumber(metadata?.Bpm, "0"),
                Nps = FormatNumber(diff?.Nps, "0.00"),
                Notes = FormatInt(diff?.Notes),
                Obstacles = FormatInt(diff?.Obstacles),
                Bombs = FormatInt(diff?.Bombs),
                Njs = FormatNumber(diff?.Njs, "0.0#"),
                JumpDistance = PreviewJumpDistance(metadata, diff),
                Stars = "--"
            };
        }

        private static LiveSongDetails BuildScoreSaberSongDetails(LiveSongCommand song, MapDetailsResponse map) {
            if (map == null) {
                return null;
            }

            string hash = FirstNonEmpty(map.Hash, SongHash(song)).ToUpperInvariant();
            MapDetailsResponseLeaderboardsItem leaderboard = SelectLeaderboard(map, song);
            string name = DisplaySongName(map.SongName, map.SongSubName);

            return new LiveSongDetails {
                Hash = hash,
                Name = FirstNonEmpty(name, hash),
                Mapper = FirstNonEmpty(map.LevelAuthorName, map.SongAuthorName, "Unknown"),
                Difficulty = DifficultyName(leaderboard),
                Characteristic = CharacteristicName(leaderboard),
                CoverUrl = map.CoverUrl ?? string.Empty,
                Duration = "--",
                Bpm = FormatNumber(map.Bpm, "0"),
                Nps = "--",
                Notes = "--",
                Obstacles = "--",
                Bombs = "--",
                Njs = "--",
                JumpDistance = "--",
                Stars = FormatStars(leaderboard?.Realm?.Stars)
            };
        }

        private static LiveSongDetails MergeSongDetails(LiveSongDetails scoreSaber, LiveSongDetails beatSaver) {
            if (scoreSaber == null) {
                return beatSaver;
            }

            if (beatSaver == null) {
                return scoreSaber;
            }

            return new LiveSongDetails {
                Hash = FirstNonEmpty(scoreSaber.Hash, beatSaver.Hash),
                Name = FirstNonEmpty(scoreSaber.Name, beatSaver.Name),
                Mapper = FirstNonEmpty(scoreSaber.Mapper, beatSaver.Mapper),
                Difficulty = FirstNonEmpty(scoreSaber.Difficulty, beatSaver.Difficulty),
                Characteristic = FirstNonEmpty(scoreSaber.Characteristic, beatSaver.Characteristic),
                CoverUrl = FirstNonEmpty(scoreSaber.CoverUrl, beatSaver.CoverUrl),
                DownloadUrl = FirstNonEmpty(beatSaver.DownloadUrl, scoreSaber.DownloadUrl),
                Duration = FirstDetailValue(scoreSaber.Duration, beatSaver.Duration),
                Bpm = FirstDetailValue(scoreSaber.Bpm, beatSaver.Bpm),
                Nps = FirstDetailValue(scoreSaber.Nps, beatSaver.Nps),
                Notes = FirstDetailValue(scoreSaber.Notes, beatSaver.Notes),
                Obstacles = FirstDetailValue(scoreSaber.Obstacles, beatSaver.Obstacles),
                Bombs = FirstDetailValue(scoreSaber.Bombs, beatSaver.Bombs),
                Njs = FirstDetailValue(scoreSaber.Njs, beatSaver.Njs),
                JumpDistance = FirstDetailValue(scoreSaber.JumpDistance, beatSaver.JumpDistance),
                Stars = FirstDetailValue(scoreSaber.Stars, beatSaver.Stars)
            };
        }

        private static CompeteSongSelection CreatePreview(LiveSongCommand song, LiveSongDetails details) {
            if (song == null) {
                return null;
            }

            string hash = SongHash(song).ToUpperInvariant();
            return new CompeteSongSelection(
                null,
                default,
                FirstNonEmpty(details?.Name, hash),
                FirstNonEmpty(details?.Mapper, "Unknown"),
                FirstNonEmpty(FormatDifficulty(song.Difficulty), FormatDifficulty(details?.Difficulty)),
                details?.Characteristic ?? string.Empty,
                details?.CoverUrl ?? string.Empty,
                details?.Duration ?? "--",
                details?.Bpm ?? "--",
                details?.Nps ?? "--",
                details?.Notes ?? "--",
                details?.Obstacles ?? "--",
                details?.Bombs ?? "--",
                details?.Njs ?? "--",
                details?.JumpDistance ?? "--",
                details?.Stars ?? "--",
                FirstNonEmpty(details?.Hash, hash),
                details?.DownloadUrl ?? string.Empty);
        }

        private async Task DownloadAndRefresh(string hash, BeatSaverVersion version, CancellationToken cancellationToken) {
            string normalizedHash = BeatSaverService.NormalizeHash(hash);
            Task downloadTask;
            lock (_mapDownloadsLock) {
                if (!_mapDownloadsByHash.TryGetValue(normalizedHash, out downloadTask)) {
                    downloadTask = DownloadAndRefreshCore(normalizedHash, version, cancellationToken);
                    _mapDownloadsByHash[normalizedHash] = downloadTask;
                }
            }

            try {
                await downloadTask;
            } finally {
                lock (_mapDownloadsLock) {
                    Task currentTask;
                    if (_mapDownloadsByHash.TryGetValue(normalizedHash, out currentTask) && ReferenceEquals(currentTask, downloadTask)) {
                        _mapDownloadsByHash.Remove(normalizedHash);
                    }
                }
            }
        }

        private async Task DownloadAndRefreshCore(string hash, BeatSaverVersion version, CancellationToken cancellationToken) {
            await _beatSaver.DownloadMapByHash(hash, version, cancellationToken);
            await RefreshSongs(cancellationToken);
        }

        private static async Task RefreshSongs(CancellationToken cancellationToken) {
            await WaitForCurrentSongRefresh(cancellationToken);
            await StartSongRefresh(cancellationToken);
        }

        private static async Task WaitForCurrentSongRefresh(CancellationToken cancellationToken) {
            if (!Loader.AreSongsLoading) {
                return;
            }

            TaskCompletionSource<bool> songsLoaded = new TaskCompletionSource<bool>();
            Delegate handler;
            EventInfo songsLoadedEvent = RegisterSongsLoadedWait(songsLoaded, out handler);
            try {
                if (Loader.AreSongsLoading) {
                    await WaitForSongsLoaded(songsLoaded, cancellationToken);
                }
            } finally {
                UnregisterSongsLoadedWait(songsLoadedEvent, handler, songsLoaded);
            }
        }

        private static async Task StartSongRefresh(CancellationToken cancellationToken) {
            var songsLoaded = new TaskCompletionSource<bool>();
            Delegate handler;
            EventInfo songsLoadedEvent = RegisterSongsLoadedWait(songsLoaded, out handler);

            try {
                await UnityMainThreadTaskScheduler.Factory.StartNew(() => Loader.Instance.RefreshSongs(false));
                await WaitForSongsLoaded(songsLoaded, cancellationToken);
            } finally {
                UnregisterSongsLoadedWait(songsLoadedEvent, handler, songsLoaded);
            }
        }

        private static async Task WaitForSongsLoaded(TaskCompletionSource<bool> songsLoaded, CancellationToken cancellationToken) {
            Task timeout = Task.Delay(SongRefreshTimeoutMs, cancellationToken);
            Task completed = await Task.WhenAny(songsLoaded.Task, timeout);
            if (completed != songsLoaded.Task) {
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException("Timed out waiting for SongCore to refresh songs");
            }
        }

        private static EventInfo RegisterSongsLoadedWait(TaskCompletionSource<bool> songsLoaded, out Delegate handler) {
            EventInfo songsLoadedEvent = typeof(Loader).GetEvent("SongsLoadedEvent");
            MethodInfo handlerMethod = typeof(CompeteSongService).GetMethod(nameof(SongsLoaded), BindingFlags.NonPublic | BindingFlags.Static);
            handler = Delegate.CreateDelegate(songsLoadedEvent.EventHandlerType, handlerMethod);
            lock (_songsLoadedLock) {
                _songsLoadedCompletions.Add(songsLoaded);
            }
            songsLoadedEvent.AddEventHandler(null, handler);
            return songsLoadedEvent;
        }

        private static void UnregisterSongsLoadedWait(EventInfo songsLoadedEvent, Delegate handler, TaskCompletionSource<bool> songsLoaded) {
            songsLoadedEvent.RemoveEventHandler(null, handler);
            lock (_songsLoadedLock) {
                _songsLoadedCompletions.Remove(songsLoaded);
            }
        }

        private static void SongsLoaded(object loader, object songs) {
            TaskCompletionSource<bool>[] completions;
            lock (_songsLoadedLock) {
                completions = _songsLoadedCompletions.ToArray();
            }

            foreach (TaskCompletionSource<bool> completion in completions) {
                completion.TrySetResult(true);
            }
        }

        private static CompeteSongSelection CreateSongSelection(BeatmapLevel level, BeatmapKey key, LiveSongCommand song, LiveSongDetails scoreSaberDetails) {
            if (!BeatmapLevelCompat.TryGetDifficultyDetails(level, key, out BeatmapDifficultyDetails difficultyDetails)) {
                return null;
            }

            float njs = BeatmapLevelCompat.GetNoteJumpMovementSpeed(key.difficulty, difficultyDetails.NoteJumpMovementSpeed);

            return new CompeteSongSelection(
                level,
                key,
                DisplaySongName(level.songName, level.songSubName),
                MapperName(difficultyDetails.Mappers),
                key.difficulty.ToString().Replace("Plus", "+"),
                key.beatmapCharacteristic.serializedName,
                string.Empty,
                FormatDuration(level.songDuration),
                Math.Round(level.beatsPerMinute).ToString(CultureInfo.InvariantCulture),
                NotesPerSecond(difficultyDetails.CuttableObjectsCount, level.songDuration),
                FormatInt(difficultyDetails.NotesCount),
                FormatInt(difficultyDetails.ObstaclesCount),
                FormatInt(difficultyDetails.BombsCount),
                njs.ToString("0.0#", CultureInfo.InvariantCulture),
                JumpDistance(level.beatsPerMinute, njs, difficultyDetails.NoteJumpStartBeatOffset).ToString("0.0#", CultureInfo.InvariantCulture),
                FirstDetailValue(scoreSaberDetails?.Stars, "--"),
                SongHash(song),
                string.Empty);
        }

        private static BeatmapKey? FindBeatmapKey(BeatmapLevel level, LiveSongCommand song) {
            BeatmapDifficulty difficulty;
            bool hasDifficulty = Enum.TryParse(BeatSaverService.NormalizeDifficulty(song?.Difficulty), true, out difficulty);
            string characteristic = NormalizeCharacteristicName(song?.Characteristic);
            BeatmapKey? difficultyMatch = null;

            foreach (BeatmapKey key in level.GetBeatmapKeys()) {
                if (hasDifficulty && key.difficulty != difficulty) {
                    continue;
                }

                if (!difficultyMatch.HasValue) {
                    difficultyMatch = key;
                }

                string keyCharacteristic = key.beatmapCharacteristic == null ? string.Empty : key.beatmapCharacteristic.serializedName;
                if (string.IsNullOrEmpty(characteristic) || NormalizeCharacteristicName(keyCharacteristic) == characteristic) {
                    return key;
                }
            }

            return difficultyMatch ?? level.GetBeatmapKeys().FirstOrDefault();
        }

        private static MapDetailsResponseLeaderboardsItem SelectLeaderboard(MapDetailsResponse map, LiveSongCommand song) {
            if (map?.Leaderboards == null || map.Leaderboards.Count == 0) {
                return null;
            }

            string difficulty = NormalizeDifficultyName(song?.Difficulty);
            string characteristic = NormalizeCharacteristicName(song?.Characteristic);
            List<MapDetailsResponseLeaderboardsItem> leaderboards = map.Leaderboards
                .Where(leaderboard => string.IsNullOrEmpty(difficulty) || NormalizeDifficultyName(DifficultyName(leaderboard)) == difficulty)
                .ToList();

            if (leaderboards.Count == 0) {
                leaderboards = map.Leaderboards;
            }

            if (!string.IsNullOrEmpty(characteristic)) {
                MapDetailsResponseLeaderboardsItem characteristicMatch = leaderboards.FirstOrDefault(
                    leaderboard => NormalizeCharacteristicName(CharacteristicName(leaderboard)) == characteristic);
                if (characteristicMatch != null) {
                    return characteristicMatch;
                }
            }

            return leaderboards.FirstOrDefault();
        }

        private static string SongHash(LiveSongCommand song) {
            return BeatSaverService.NormalizeHash(song?.Hash);
        }

        private static string DisplaySongName(string songName, string songSubName) {
            return string.IsNullOrEmpty(songSubName) ? songName ?? string.Empty : $"{songName} {songSubName}";
        }

        private static string MapperName(IEnumerable<string> mappers) {
            string[] names = mappers.Where(mapper => !string.IsNullOrWhiteSpace(mapper)).ToArray();
            return names.Length == 0 ? "Unknown" : string.Join(", ", names);
        }

        private static string FormatDifficulty(string difficulty) {
            return string.Equals(difficulty, "ExpertPlus", StringComparison.OrdinalIgnoreCase) ? "Expert+" : difficulty ?? string.Empty;
        }

        private static string DifficultyName(MapDetailsResponseLeaderboardsItem leaderboard) {
            string rawDifficulty = RawDifficultyPart(leaderboard?.RawDifficulty, 0);
            if (!string.IsNullOrEmpty(rawDifficulty)) {
                return rawDifficulty;
            }

            switch ((int)(leaderboard?.Difficulty ?? 0)) {
                case 1:
                    return "Easy";
                case 3:
                    return "Normal";
                case 5:
                    return "Hard";
                case 7:
                    return "Expert";
                case 9:
                    return "ExpertPlus";
                default:
                    return string.Empty;
            }
        }

        private static string CharacteristicName(MapDetailsResponseLeaderboardsItem leaderboard) {
            string gameMode = leaderboard?.GameMode;
            if (string.IsNullOrWhiteSpace(gameMode)) {
                gameMode = RawDifficultyPart(leaderboard?.RawDifficulty, 1);
            }

            if (string.IsNullOrWhiteSpace(gameMode)) {
                return string.Empty;
            }

            return gameMode.StartsWith("Solo", StringComparison.OrdinalIgnoreCase)
                ? gameMode.Substring(4)
                : gameMode;
        }

        private static string RawDifficultyPart(string rawDifficulty, int index) {
            if (string.IsNullOrWhiteSpace(rawDifficulty)) {
                return string.Empty;
            }

            string[] parts = rawDifficulty.Trim('_').Split('_');
            return index >= 0 && index < parts.Length ? parts[index] : string.Empty;
        }

        private static string NormalizeDifficultyName(string difficulty) {
            return (difficulty ?? string.Empty)
                .Replace("+", "Plus")
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();
        }

        private static string NormalizeCharacteristicName(string characteristic) {
            string normalized = (characteristic ?? string.Empty)
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();

            if (normalized.StartsWith("solo", StringComparison.Ordinal)) {
                normalized = normalized.Substring(4);
            }

            switch (normalized) {
                case "90degree":
                case "generated90degree":
                    return "ninetydegree";
                case "360degree":
                case "generated360degree":
                    return "threesixtydegree";
                default:
                    return normalized;
            }
        }

        private static string FormatDuration(float? duration) {
            return duration.HasValue ? FormatDuration(duration.Value) : "--";
        }

        private static string FormatDuration(float duration) {
            if (duration <= 0f) {
                return "--";
            }

            TimeSpan time = TimeSpan.FromSeconds(duration);
            return $"{(int)time.TotalMinutes}:{time.Seconds:00}";
        }

        private static string FormatNumber(float? value, string format) {
            return value.HasValue && value.Value > 0f ? value.Value.ToString(format, CultureInfo.InvariantCulture) : "--";
        }

        private static string FormatNumber(double value, string format) {
            return value > 0d ? value.ToString(format, CultureInfo.InvariantCulture) : "--";
        }

        private static string FormatStars(double? value) {
            return value.HasValue && value.Value > 0d ? value.Value.ToString("0.00", CultureInfo.InvariantCulture) : "--";
        }

        private static string FormatInt(int? value) {
            return value.HasValue && value.Value > 0 ? value.Value.ToString(CultureInfo.InvariantCulture) : "--";
        }

        private static string NotesPerSecond(int? cuttableObjectsCount, float duration) {
            return duration <= 0f || !cuttableObjectsCount.HasValue ? "--" : (cuttableObjectsCount.Value / duration).ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string PreviewJumpDistance(BeatSaverMapMetadata metadata, BeatSaverDifficulty diff) {
            if (metadata == null || diff == null || !metadata.Bpm.HasValue || !diff.Njs.HasValue || metadata.Bpm.Value <= 0f || diff.Njs.Value <= 0f) {
                return "--";
            }

            return JumpDistance(metadata.Bpm.Value, diff.Njs.Value, diff.Offset ?? 0f).ToString("0.0#", CultureInfo.InvariantCulture);
        }

        private static float JumpDistance(float bpm, float njs, float offset) {
            float oneBeatDuration = 60f / bpm;
            float halfJumpDuration = 4f;
            while (njs * oneBeatDuration * halfJumpDuration > 17.999f) {
                halfJumpDuration /= 2f;
            }

            halfJumpDuration += offset;
            return njs * oneBeatDuration * Math.Max(halfJumpDuration, 0.25f) * 2f;
        }

        private static string FirstNonEmpty(params string[] values) {
            foreach (string value in values) {
                if (!string.IsNullOrWhiteSpace(value)) {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string FirstDetailValue(params string[] values) {
            foreach (string value in values) {
                if (!string.IsNullOrWhiteSpace(value) && value != "--") {
                    return value;
                }
            }

            return FirstNonEmpty(values);
        }

        private sealed class LiveSongDetails {
            internal string Hash { get; set; } = "";
            internal string Name { get; set; } = "";
            internal string Mapper { get; set; } = "";
            internal string Difficulty { get; set; } = "";
            internal string Characteristic { get; set; } = "";
            internal string CoverUrl { get; set; } = "";
            internal string DownloadUrl { get; set; } = "";
            internal string Duration { get; set; } = "--";
            internal string Bpm { get; set; } = "--";
            internal string Nps { get; set; } = "--";
            internal string Notes { get; set; } = "--";
            internal string Obstacles { get; set; } = "--";
            internal string Bombs { get; set; } = "--";
            internal string Njs { get; set; } = "--";
            internal string JumpDistance { get; set; } = "--";
            internal string Stars { get; set; } = "--";
        }
    }
}
