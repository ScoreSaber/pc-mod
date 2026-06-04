using System;
using System.Threading.Tasks;

namespace ScoreSaber.Core {
    internal static class TaskExtensions {
        internal static async Task WaitWhile(Func<bool> condition, int frequency = 25, int timeout = -1) {
            var waitTask = Task.Run(async () => {
                while (condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
                throw new TimeoutException();
        }

        internal static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1) {
            var waitTask = Task.Run(async () => {
                while (!condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask,
                    Task.Delay(timeout)))
                throw new TimeoutException();
        }

        internal static void RunTask(this Task discarded) {
            discarded.ContinueWith(t => { Plugin.Log.Error(t.Exception); }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
