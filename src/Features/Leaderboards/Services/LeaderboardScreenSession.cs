using ScoreSaber.Core;
using ScoreSaber.Features.Leaderboards.Domain;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Leaderboards.Services {
    internal class LeaderboardScreenSession : IDisposable {
        internal event Action<LeaderboardScreenState> StateChanged;

        private readonly LeaderboardScreenLoader _leaderboardLoader;

        private BeatmapKey? _beatmapKey;
        private LeaderboardScreenScope _scope = LeaderboardScreenScope.Global;
        private int _page = 1;
        private CancellationTokenSource _refreshCancellation;

        internal LeaderboardScreenState CurrentState { get; private set; } = LeaderboardScreenState.Failed(LeaderboardScreenStatus.Error, string.Empty, false, null, string.Empty, false, 1);

        public LeaderboardScreenSession(
            LeaderboardScreenLoader leaderboardLoader) {
            _leaderboardLoader = leaderboardLoader;
        }

        internal void SetBeatmap(BeatmapKey beatmapKey) {
            if (!ScoreSaberBeatmapKey.IsSupported(beatmapKey)) {
                ClearBeatmap();
                return;
            }

            _beatmapKey = beatmapKey;
            _page = 1;
            Refresh();
        }

        internal void ClearBeatmap() {
            _beatmapKey = null;
            _page = 1;
            CancelRefresh();
        }

        internal void SelectScope(LeaderboardScreenScope scope) {
            if (_scope == scope && _page == 1) {
                Refresh();
                return;
            }

            _scope = scope;
            _page = 1;
            Refresh();
        }

        internal void PageUp() {
            if (_page <= 1) {
                return;
            }

            _page--;
            Refresh();
        }

        internal void PageDown() {
            _page++;
            Refresh();
        }

        internal void RefreshFromFirstPage() {
            _page = 1;
            Refresh();
        }

        internal ScoreMap GetScore(int index) {
            if (CurrentState?.Leaderboard == null || index < 0 || index >= CurrentState.Leaderboard.Scores.Length) {
                return null;
            }

            return CurrentState.Leaderboard.Scores[index];
        }

        private void Refresh() => LoadCurrent().RunTask();

        private async Task LoadCurrent() {
            if (!_beatmapKey.HasValue) {
                return;
            }

            CancelRefresh();
            _refreshCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = _refreshCancellation.Token;

            Publish(LeaderboardScreenState.Loading(_page));

            try {
                LeaderboardScreenState state = await _leaderboardLoader.Load(_beatmapKey.Value, _scope, _page, cancellationToken);
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                Publish(state);
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Plugin.Log.Error($"Failed to load LeaderboardCore ScoreSaber leaderboard: {ex}");
                Publish(LeaderboardScreenState.Failed(LeaderboardScreenStatus.Error, "Failed to load leaderboard, score won't upload", true, null, string.Empty, false, _page));
            }
        }

        private void CancelRefresh() {
            _refreshCancellation?.Cancel();
            _refreshCancellation?.Dispose();
            _refreshCancellation = null;
        }

        private void Publish(LeaderboardScreenState state) {
            CurrentState = state;
            StateChanged?.Invoke(state);
        }

        public void Dispose() {
            CancelRefresh();
        }
    }
}
