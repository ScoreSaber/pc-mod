namespace ScoreSaber.Features.Live.Compete.Domain {
    internal class CompetePlayer {
        internal string Name { get; }
        internal string Status { get; }
        internal string TeamId { get; }
        internal string Rank { get; }
        internal bool IsLocalPlayer { get; }
        internal string PlayerId { get; }
        internal bool IsBot { get; }
        internal string AvatarUrl { get; }
        internal bool IsActive { get; }
        internal string DisplayName => IsBot ? $"{Name} [BOT]" : Name;

        internal CompetePlayer(string name, string status, string teamId, string rank, bool isLocalPlayer = false, string playerId = "", bool isBot = false, string avatarUrl = "", bool isActive = true) {
            Name = name;
            Status = status;
            TeamId = teamId;
            Rank = rank;
            IsLocalPlayer = isLocalPlayer;
            PlayerId = playerId ?? string.Empty;
            IsBot = isBot;
            AvatarUrl = avatarUrl ?? string.Empty;
            IsActive = isActive;
        }
    }
}
