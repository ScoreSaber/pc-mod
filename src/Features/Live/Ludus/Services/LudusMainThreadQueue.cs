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

        internal void Drain() {
            while (true) {
                Action action;
                lock (_lock) {
                    if (_actions.Count == 0) {
                        return;
                    }

                    action = _actions.Dequeue();
                }

                action();
            }
        }
    }
}
