using ScoreSaber.Core.Api;
using ScoreSaber.Features.Leaderboards.Domain;
using ScoreSaber.Features.Leaderboards.Services;
using ScoreSaber.Features.Replays;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Replays.Services {
    internal class ReplayQueryService {
        private readonly IScoreSaberApiClient _apiClient;
        private readonly ReplayStorageService _replayStorageService;

        public ReplayQueryService(IScoreSaberApiClient apiClient, ReplayStorageService replayStorageService) {
            _apiClient = apiClient;
            _replayStorageService = replayStorageService;
        }

        public async Task<byte[]> GetReplayData(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, ScoreMap scoreMap) {
            if (scoreMap.HasLocalReplay) {
                byte[] replay = _replayStorageService.ReadLocalReplay(beatmapLevel, beatmapKey, scoreMap);
                if (replay != null) {
                    return replay;
                }
            }

            byte[] response = await _apiClient.DownloadReplay(scoreMap.Score.Id, CancellationToken.None);
            if (response != null) {
                return response;
            }

            throw new Exception("Failed to download replay");
        }
    }
}
