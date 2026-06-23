using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ScoreSaber {
    internal readonly struct BeatmapKey : IEquatable<BeatmapKey> {
        internal readonly IDifficultyBeatmap difficultyBeatmap;

        internal BeatmapKey(IDifficultyBeatmap difficultyBeatmap) {
            this.difficultyBeatmap = difficultyBeatmap;
        }

        public string levelId => difficultyBeatmap?.level?.levelID;
        public BeatmapDifficulty difficulty => difficultyBeatmap?.difficulty ?? default;
        public BeatmapCharacteristicSO beatmapCharacteristic => difficultyBeatmap?.parentDifficultyBeatmapSet?.beatmapCharacteristic;

        public bool Equals(BeatmapKey other) => levelId == other.levelId && difficulty == other.difficulty && beatmapCharacteristic == other.beatmapCharacteristic;

        public override bool Equals(object obj) => obj is BeatmapKey other && Equals(other);

        public override int GetHashCode() {
            unchecked {
                int hash = levelId != null ? levelId.GetHashCode() : 0;
                hash = (hash * 397) ^ (int)difficulty;
                hash = (hash * 397) ^ (beatmapCharacteristic != null ? beatmapCharacteristic.GetHashCode() : 0);
                return hash;
            }
        }
    }

    internal class BeatmapLevel {
        internal readonly IPreviewBeatmapLevel previewBeatmapLevel;
        private readonly IBeatmapLevel _beatmapLevel;

        internal BeatmapLevel(IPreviewBeatmapLevel previewBeatmapLevel) {
            this.previewBeatmapLevel = previewBeatmapLevel;
            _beatmapLevel = previewBeatmapLevel as IBeatmapLevel;
            previewMediaData = new PreviewMediaDataCompat(previewBeatmapLevel);
        }

        internal BeatmapLevel(IBeatmapLevel beatmapLevel) {
            previewBeatmapLevel = beatmapLevel;
            _beatmapLevel = beatmapLevel;
            previewMediaData = new PreviewMediaDataCompat(beatmapLevel);
        }

        public string levelID => previewBeatmapLevel.levelID;
        public string songName => previewBeatmapLevel.songName;
        public string songSubName => previewBeatmapLevel.songSubName;
        public string songAuthorName => previewBeatmapLevel.songAuthorName;
        public float beatsPerMinute => previewBeatmapLevel.beatsPerMinute;
        public float songDuration => previewBeatmapLevel.songDuration;
        public string[] allMappers => string.IsNullOrEmpty(previewBeatmapLevel.levelAuthorName) ? new string[0] : new[] { previewBeatmapLevel.levelAuthorName };
        public string[] allLighters => new string[0];
        public PreviewMediaDataCompat previewMediaData { get; }

        public IEnumerable<BeatmapKey> GetBeatmapKeys() {
            return _beatmapLevel?.beatmapLevelData?.difficultyBeatmapSets == null
                ? Enumerable.Empty<BeatmapKey>()
                : _beatmapLevel.beatmapLevelData.difficultyBeatmapSets
                    .SelectMany(set => set.difficultyBeatmaps)
                    .Select(difficultyBeatmap => new BeatmapKey(difficultyBeatmap));
        }
    }

    internal class PreviewMediaDataCompat {
        private readonly IPreviewBeatmapLevel _level;

        internal PreviewMediaDataCompat(IPreviewBeatmapLevel level) {
            _level = level;
        }

        public Task<Sprite> GetCoverSpriteAsync() {
            return _level.GetCoverImageAsync(CancellationToken.None);
        }
    }

    // 1.29 doesn't have this so we're just sneaking it in for DI reasons
    internal class EnvironmentsListModel {
    }

    // BGLib's ICoroutineStarter arrived with 1.38
    internal interface ICoroutineStarter {
        Coroutine StartCoroutine(IEnumerator routine);
    }

    internal static class BeatmapLevelsModelPolyfills {
        internal static BeatmapLevel GetBeatmapLevel(this BeatmapLevelsModel beatmapLevelsModel, string levelId) {
            IPreviewBeatmapLevel previewBeatmapLevel = beatmapLevelsModel.GetLevelPreviewForLevelId(levelId);
            return previewBeatmapLevel == null ? null : new BeatmapLevel(previewBeatmapLevel);
        }
    }
}
