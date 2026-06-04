using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Leaderboards.Services {
    internal class MaxScoreCache {
        private readonly BeatmapLevelLoader _beatmapLevelLoader;
        private readonly BeatmapDataLoader _beatmapDataLoader;
        private readonly BeatmapLevelsEntitlementModel _beatmapLevelsEntitlementModel;


        private readonly Dictionary<BeatmapKey, int> _cache = new Dictionary<BeatmapKey, int>();

        public MaxScoreCache(BeatmapLevelLoader beatmapLevelLoader, BeatmapDataLoader beatmapDataLoader, BeatmapLevelsEntitlementModel beatmapLevelsEntitlementModel) {
            _beatmapLevelLoader = beatmapLevelLoader;
            _beatmapDataLoader = beatmapDataLoader;
            _beatmapLevelsEntitlementModel = beatmapLevelsEntitlementModel;
        }

        public async Task<int> GetMaxScore(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey) {
            if (_cache.TryGetValue(beatmapKey, out int cachedScore)) {
                return cachedScore;
            }

            var beatmapLevelDataVersion = await _beatmapLevelsEntitlementModel.GetLevelDataVersionAsync(beatmapKey.levelId, CancellationToken.None);
            var beatmapLevelData = (await _beatmapLevelLoader.LoadBeatmapLevelDataAsync(beatmapLevel, beatmapLevelDataVersion, CancellationToken.None)).beatmapLevelData;
            var beatmapData = await _beatmapDataLoader.LoadBeatmapDataAsync(beatmapLevelData: beatmapLevelData,
                                                                            beatmapKey: beatmapKey,
                                                                            startBpm: beatmapLevel.beatsPerMinute,
                                                                            loadingForDesignatedEnvironment: false,
                                                                            originalEnvironmentInfo: null,
                                                                            targetEnvironmentInfo: null,
                                                                            beatmapLevelDataVersion: beatmapLevelDataVersion,
                                                                            gameplayModifiers: null,
                                                                            playerSpecificSettings: null,
                                                                            enableBeatmapDataCaching: false);
            int maxScore = ScoreModel.ComputeMaxMultipliedScoreForBeatmap(beatmapData);

            _cache[beatmapKey] = maxScore;
            return maxScore;
        }
    }
}
