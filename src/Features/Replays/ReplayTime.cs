using ScoreSaber.Features.Replays.Format;
using System;
using System.Collections.Generic;
using Zenject;

namespace ScoreSaber.Features.Replays {
    internal abstract class TimeSynchronizer {
        [Inject]
        protected readonly AudioTimeSyncController audioTimeSyncController = null;
    }
}

namespace ScoreSaber.Features.Replays.Playback {
    internal interface IScroller {
        void TimeUpdate(float newTime);
    }

    internal static class ReplayTimeSearch {
        internal static int CountAtOrBefore<T>(IReadOnlyList<T> values, float time, Func<T, float> timeSelector) {
            return UpperBound(values.Count, index => timeSelector(values[index]), time, true);
        }

        internal static int CountAtOrBefore(float[] times, float time) {
            return UpperBound(times.Length, index => times[index], time, true);
        }

        internal static int CountBefore(float[] times, float time) {
            return UpperBound(times.Length, index => times[index], time, false);
        }

        internal static bool IsScoringNoteEvent(NoteEvent noteEvent) {
            NoteEventType eventType = noteEvent.EventType;
            return eventType == NoteEventType.GoodCut || eventType == NoteEventType.BadCut || eventType == NoteEventType.Miss;
        }

        private static int UpperBound(int count, Func<int, float> timeAt, float time, bool inclusive) {
            int low = 0;
            int high = count;
            while (low < high) {
                int mid = low + ((high - low) / 2);
                if (inclusive ? timeAt(mid) <= time : timeAt(mid) < time) {
                    low = mid + 1;
                } else {
                    high = mid;
                }
            }

            return low;
        }
    }
}
