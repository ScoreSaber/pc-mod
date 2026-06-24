using ScoreSaber.Core.Api;
using ScoreSaber.Features.Leaderboards.Domain;
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

        public async Task<byte[]> GetReplayData(ScoreMap scoreMap) {
            Exception downloadError = null;
            try {
                byte[] response = await _apiClient.DownloadReplay(scoreMap.Score.Id, CancellationToken.None);
                if (response != null) {
                    return response;
                }
            } catch (Exception ex) {
                downloadError = ex;
                Plugin.Log.Debug($"Failed to download ScoreSaber replay, checking local fallback: {ex.Message}");
            }

            if (scoreMap.HasLocalReplay) {
                byte[] replay = _replayStorageService.ReadLocalReplay(scoreMap.Parent.BeatmapLevel, scoreMap.Parent.BeatmapKey, scoreMap);
                if (replay != null) {
                    return replay;
                }
            }

            throw new Exception("Failed to download replay", downloadError);
        }
    }
}
