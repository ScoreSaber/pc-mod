using System;
using System.Collections.Generic;

namespace ScoreSaber.Features.Live.Ludus.Services {
    internal sealed class LudusMainThreadQueue {
        private readonly Queue<Action> _actions = new Queue<Action>();
        private readonly object _lock = new object();

        internal void Enqueue(Action action) {
            lock (_lock) {
                _actions.Enqueue(action);
            }
        }

        internal int Drain(int maxActions) {
            int processed = 0;
            while (true) {
                Action action;
                lock (_lock) {
                    if (_actions.Count == 0 || processed >= maxActions) {
                        return _actions.Count;
                    }

                    action = _actions.Dequeue();
                }

                try {
                    action();
                } catch (Exception ex) {
                    Plugin.Log.Error($"Ludus main thread action failed: {ex}");
                }
                processed++;
            }
        }
    }
}
