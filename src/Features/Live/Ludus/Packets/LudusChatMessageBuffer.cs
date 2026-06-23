using ScoreSaber.Features.Live.Ludus.Domain;
using ScoreSaber.Live.V1;
using System;
using System.Collections.Generic;

namespace ScoreSaber.Features.Live.Ludus.Packets {
    internal sealed class LudusChatMessageBuffer {
        private const int MaxMessages = 100;
        private readonly List<LiveChatEntry> _messages = new List<LiveChatEntry>();

        internal IReadOnlyList<LiveChatEntry> CurrentMessages => _messages.ToArray();

        internal bool Apply(LiveChatMessage message, string currentMatchId) {
            LiveChatEntry entry = EntryForCurrentMatch(message, currentMatchId);
            if (entry == null) {
                return false;
            }

            Upsert(entry);
            SortAndTrim();
            return true;
        }

        internal void Replace(LiveChatSnapshot snapshot, string currentMatchId) {
            if (string.IsNullOrEmpty(currentMatchId)) {
                _messages.Clear();
                return;
            }

            _messages.RemoveAll(message => !string.Equals(message.MatchId, currentMatchId, StringComparison.Ordinal));
            if (snapshot?.Messages == null) {
                return;
            }

            foreach (LiveChatMessage message in snapshot.Messages) {
                LiveChatEntry entry = EntryForCurrentMatch(message, currentMatchId);
                if (entry != null) {
                    Upsert(entry);
                }
            }

            SortAndTrim();
        }

        internal bool Clear() {
            if (_messages.Count == 0) {
                return false;
            }

            _messages.Clear();
            return true;
        }

        private static LiveChatEntry EntryForCurrentMatch(LiveChatMessage message, string currentMatchId) {
            if (message == null || string.IsNullOrEmpty(message.MatchId) || !string.Equals(message.MatchId, currentMatchId, StringComparison.Ordinal)) {
                return null;
            }

            return LiveChatEntry.FromProto(message);
        }

        private void Upsert(LiveChatEntry entry) {
            int index = _messages.FindIndex(item => item.Key == entry.Key);
            if (index >= 0) {
                _messages[index] = entry;
            } else {
                _messages.Add(entry);
            }
        }

        private void SortAndTrim() {
            _messages.Sort((left, right) => left.RoomSequence.CompareTo(right.RoomSequence));
            if (_messages.Count > MaxMessages) {
                _messages.RemoveRange(0, _messages.Count - MaxMessages);
            }
        }
    }
}
