namespace ScoreSaber.Features.Live.Compete.Domain {
    internal class CompeteOrganizerPrompt {
        internal string Title { get; }
        internal string Message { get; }
        internal string PrimaryText { get; }
        internal string SecondaryText { get; }
        internal string CommandId { get; }
        internal string MatchId { get; }

        internal CompeteOrganizerPrompt(string title, string message, string primaryText, string secondaryText, string commandId = "", string matchId = "") {
            Title = title;
            Message = message;
            PrimaryText = primaryText;
            SecondaryText = secondaryText;
            CommandId = commandId ?? string.Empty;
            MatchId = matchId ?? string.Empty;
        }
    }
}
