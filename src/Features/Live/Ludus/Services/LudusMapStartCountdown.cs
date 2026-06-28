using ScoreSaber.Core;
using ScoreSaber.Core.Timing;
using ScoreSaber.Features.Live.Compete.Domain;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Live.Ludus.Services {
    internal sealed class LudusMapStartCountdown {
        private readonly LudusMainThreadQueue _mainThread;
        private readonly Func<string> _defaultMatchId;
        private readonly ScoreSaberClock _clock;

        private CancellationTokenSource _cancellation;
        private string _matchId = string.Empty;
        private int _version;

        internal LudusMapStartCountdown(LudusMainThreadQueue mainThread, Func<string> defaultMatchId, ScoreSaberClock clock) {
            _mainThread = mainThread;
            _defaultMatchId = defaultMatchId;
            _clock = clock;
        }

        internal event Action<CompeteMapStartCountdown> Changed;

        internal CancellationToken Begin(string matchId, int delayMs, CancellationToken cancellationToken) {
            Cancel();

            _matchId = MatchIdOrDefault(matchId);
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (delayMs > 0) {
                int version = ++_version;
                long startDeadlineMs = _clock.MonotonicMilliseconds() + delayMs;
                Run(_matchId, startDeadlineMs, version, _cancellation.Token).RunTask();
            }

            return _cancellation.Token;
        }

        internal bool TryCancel(string matchId) {
            if (_cancellation == null || !Matches(matchId)) {
                return false;
            }

            Cancel();
            return true;
        }

        internal void Complete(string matchId) {
            if (_cancellation != null && Matches(matchId)) {
                Cancel();
            }
        }

        internal void Cancel() {
            _version++;
            try {
                _cancellation?.Cancel();
                _cancellation?.Dispose();
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to cancel pending live map start: {ex.Message}");
            }

            _cancellation = null;
            _matchId = string.Empty;
            Changed?.Invoke(null);
        }

        private async Task Run(string matchId, long startDeadlineMs, int version, CancellationToken cancellationToken) {
            int lastSeconds = -1;

            try {
                while (true) {
                    int remainingSeconds = RemainingSeconds(startDeadlineMs);
                    if (remainingSeconds != lastSeconds) {
                        lastSeconds = remainingSeconds;
                        EnqueueChanged(matchId, remainingSeconds, version);
                    }

                    if (remainingSeconds == 0) {
                        return;
                    }

                    await Task.Delay(200, cancellationToken);
                }
            } catch (OperationCanceledException) {
            }
        }

        private void EnqueueChanged(string matchId, int remainingSeconds, int version) {
            _mainThread.Enqueue(() => {
                if (version == _version) {
                    Changed?.Invoke(new CompeteMapStartCountdown(matchId, remainingSeconds));
                }
            });
        }

        private bool Matches(string matchId) {
            return string.IsNullOrEmpty(matchId) || string.Equals(MatchIdOrDefault(matchId), _matchId, StringComparison.Ordinal);
        }

        private string MatchIdOrDefault(string matchId) {
            return !string.IsNullOrEmpty(matchId) ? matchId : _defaultMatchId();
        }

        private int RemainingSeconds(long startDeadlineMs) {
            long remainingMs = startDeadlineMs - _clock.MonotonicMilliseconds();
            return remainingMs <= 0 ? 0 : Math.Max(1, (int)Math.Ceiling(remainingMs / 1000d));
        }
    }
}
