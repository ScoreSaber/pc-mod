using ScoreSaber.Core.Configuration;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.Leaderboards.Services;
using ScoreSaber.Features.ScoreSubmission.Domain;
using System;
using System.Collections.Generic;
using System.IO;

namespace ScoreSaber.Features.Replays {

    internal class ReplayStorageService {
        private readonly SettingsService _settings;

        public ReplayStorageService(SettingsService settings) {
            _settings = settings;
        }

        internal bool LocalReplayExists(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, ScoreMap scoreMap) => GetExistingReplayPath(beatmapLevel, beatmapKey, scoreMap) != null;

        internal byte[] ReadLocalReplay(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, ScoreMap scoreMap) {
            string replayPath = GetExistingReplayPath(beatmapLevel, beatmapKey, scoreMap);
            return replayPath == null ? null : File.ReadAllBytes(replayPath);
        }

        internal void SaveLocalReplay(ScoreSaberUploadData uploadData, BeatmapKey beatmapKey, byte[] replay) {
            if (!_settings.Current.saveLocalReplays || replay == null) {
                return;
            }

            try {
                Directory.CreateDirectory(_settings.ReplayPath);
                File.WriteAllBytes(GetReplayPath(uploadData.PlayerId, uploadData.LeaderboardId, beatmapKey), replay);
            } catch (Exception ex) {
                Plugin.Log.Error($"Failed to write local replay; {ex}");
            }
        }

        internal string GetReplayPath(string playerId, string songHash, BeatmapKey beatmapKey) => _settings.ReplayPathFor(playerId, songHash, beatmapKey);

        internal string GetExistingReplayPath(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, ScoreMap scoreMap) {
            foreach (string replayPath in GetReplayPaths(beatmapLevel, beatmapKey, scoreMap)) {
                if (File.Exists(replayPath)) {
                    return replayPath;
                }
            }

            return null;
        }

        private IEnumerable<string> GetReplayPaths(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, ScoreMap scoreMap) {
            string playerId = scoreMap.Score.Player.Id;
            string songHash = scoreMap.Parent.SongHash;

            yield return GetReplayPath(playerId, songHash, beatmapKey);

            string songName = beatmapLevel.songName.ReplaceInvalidChars().Truncate(155);
            string difficulty = beatmapKey.difficulty.SerializedName();
            string characteristic = beatmapKey.CharacteristicSerializedName();

            yield return _settings.LegacyReplayPathFor(playerId, songName, difficulty, characteristic, songHash);
            yield return _settings.LegacyReplayPathFor(playerId, songName, songHash);
        }
    }
}
