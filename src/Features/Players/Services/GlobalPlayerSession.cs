using ScoreSaber.Features.Players.Domain;
using System;

namespace ScoreSaber.Features.Players.Services {
    internal class GlobalPlayerSession {
        internal GlobalPlayerScope Scope { get; private set; }
        internal int Page { get; private set; } = 1;
        private string _requestId = string.Empty;

        internal string BeginRequest() {
            _requestId = Guid.NewGuid().ToString();
            return _requestId;
        }

        internal bool IsCurrentRequest(string requestId) => _requestId == requestId;

        internal bool SelectScope(GlobalPlayerScope scope) {
            if (Scope == scope) {
                return false;
            }

            Scope = scope;
            Page = 1;
            return true;
        }

        internal void MovePage(bool down) {
            if (down) {
                Page++;
                return;
            }

            if (Page > 1) {
                Page--;
            }
        }
    }
}
