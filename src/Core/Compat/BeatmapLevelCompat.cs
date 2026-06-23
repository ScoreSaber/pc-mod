using SongCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ScoreSaber.Core.Compat {
    internal static class BeatmapLevelCompat {
        internal static IEnumerable<BeatmapLevel> GetLoadedBeatmapLevels(BeatmapLevelsModel beatmapLevelsModel) {
            if (beatmapLevelsModel == null) {
                return Enumerable.Empty<BeatmapLevel>();
            }

#if BEAT_SABER_1_29_0
            IBeatmapLevelPackCollection packCollection = beatmapLevelsModel.allLoadedBeatmapLevelPackCollection;
            return packCollection?.beatmapLevelPacks == null
                ? Enumerable.Empty<BeatmapLevel>()
                : packCollection.beatmapLevelPacks
                    .SelectMany(pack => pack.beatmapLevelCollection.beatmapLevels)
                    .OfType<IBeatmapLevel>()
                    .Select(level => new BeatmapLevel(level))
                    .Where(level => level.GetBeatmapKeys().Any());
#else
            return beatmapLevelsModel
                .GetAllPacks()
                .SelectMany(AllBeatmapLevels)
                .Where(level => level.beatmapBasicData.Count > 0);
#endif
        }

        internal static Task<BeatmapLevel> GetLevelByHash(BeatmapLevelsModel beatmapLevelsModel, string hash, CancellationToken cancellationToken) {
            if (string.IsNullOrEmpty(hash)) {
                return Task.FromResult<BeatmapLevel>(null);
            }

#if BEAT_SABER_1_29_0
            return GetLevelByHash1_29_0(beatmapLevelsModel, hash, cancellationToken);
#else
            return Task.FromResult(Loader.GetLevelByHash(hash.ToUpperInvariant()));
#endif
        }

#if BEAT_SABER_1_29_0
        private static async Task<BeatmapLevel> GetLevelByHash1_29_0(BeatmapLevelsModel beatmapLevelsModel, string hash, CancellationToken cancellationToken) {
            if (beatmapLevelsModel == null) {
                return null;
            }

            CustomPreviewBeatmapLevel previewLevel = Loader.GetLevelByHash(hash.ToUpperInvariant());
            if (previewLevel == null) {
                return null;
            }

            BeatmapLevelsModel.GetBeatmapLevelResult result = await beatmapLevelsModel.GetBeatmapLevelAsync(previewLevel.levelID, cancellationToken);
            return result.isError || result.beatmapLevel == null ? null : new BeatmapLevel(result.beatmapLevel);
        }
#endif

        internal static Task<Sprite> GetCoverSpriteAsync(BeatmapLevel level, CancellationToken cancellationToken) {
#if BEAT_SABER_1_37_1
            return level.previewMediaData.GetCoverSpriteAsync(cancellationToken);
#else
            return level.previewMediaData.GetCoverSpriteAsync();
#endif
        }

        internal static bool TryGetDifficultyDetails(BeatmapLevel level, BeatmapKey key, out BeatmapDifficultyDetails details) {
            if (level == null) {
                details = default;
                return false;
            }

#if BEAT_SABER_1_29_0
            IDifficultyBeatmap difficultyBeatmap = key.difficultyBeatmap;
            if (difficultyBeatmap == null) {
                details = default;
                return false;
            }

            details = new BeatmapDifficultyDetails(
                level.allMappers,
                null,
                null,
                null,
                null,
                difficultyBeatmap.noteJumpMovementSpeed,
                difficultyBeatmap.noteJumpStartBeatOffset);
            return true;
#else
            BeatmapBasicData beatmapData = level.GetDifficultyBeatmapData(key.beatmapCharacteristic, key.difficulty);
            if (beatmapData == null) {
                details = default;
                return false;
            }

            details = new BeatmapDifficultyDetails(
                MappersFor(level, beatmapData.mappers),
                beatmapData.notesCount,
#if BEAT_SABER_1_37_1
                beatmapData.notesCount,
#else
                beatmapData.cuttableObjectsCount,
#endif
                beatmapData.obstaclesCount,
                beatmapData.bombsCount,
                beatmapData.noteJumpMovementSpeed,
                beatmapData.noteJumpStartBeatOffset);
            return true;
#endif
        }

        internal static float GetNoteJumpMovementSpeed(BeatmapDifficulty difficulty, float noteJumpMovementSpeed) {
#if BEAT_SABER_1_40_0 || BEAT_SABER_1_42_0
            return difficulty.NoteJumpMovementSpeed(noteJumpMovementSpeed, false);
#else
            return noteJumpMovementSpeed > 0f ? noteJumpMovementSpeed : difficulty.NoteJumpMovementSpeed();
#endif
        }

#if !BEAT_SABER_1_29_0
        private static IEnumerable<BeatmapLevel> AllBeatmapLevels(BeatmapLevelPack pack) {
#if BEAT_SABER_1_37_1
            return pack.beatmapLevels;
#else
            return pack.AllBeatmapLevels();
#endif
        }
#endif

        private static string[] MappersFor(BeatmapLevel level, IEnumerable<string> beatmapMappers) {
            string[] mappers = (beatmapMappers ?? Array.Empty<string>())
                .Where(mapper => !string.IsNullOrWhiteSpace(mapper))
                .ToArray();
            return mappers.Length == 0 ? level.allMappers : mappers;
        }
    }

    internal readonly struct BeatmapDifficultyDetails {
        internal readonly string[] Mappers;
        internal readonly int? NotesCount;
        internal readonly int? CuttableObjectsCount;
        internal readonly int? ObstaclesCount;
        internal readonly int? BombsCount;
        internal readonly float NoteJumpMovementSpeed;
        internal readonly float NoteJumpStartBeatOffset;

        internal BeatmapDifficultyDetails(
            string[] mappers,
            int? notesCount,
            int? cuttableObjectsCount,
            int? obstaclesCount,
            int? bombsCount,
            float noteJumpMovementSpeed,
            float noteJumpStartBeatOffset) {

            Mappers = mappers;
            NotesCount = notesCount;
            CuttableObjectsCount = cuttableObjectsCount;
            ObstaclesCount = obstaclesCount;
            BombsCount = bombsCount;
            NoteJumpMovementSpeed = noteJumpMovementSpeed;
            NoteJumpStartBeatOffset = noteJumpStartBeatOffset;
        }
    }
}
