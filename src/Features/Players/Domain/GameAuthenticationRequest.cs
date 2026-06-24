namespace ScoreSaber.Features.Players.Domain {
    internal class GameAuthenticationRequest {
        internal int AuthType { get; set; }
        internal string PlayerId { get; set; } = string.Empty;
        internal string Nonce { get; set; } = string.Empty;
        internal string FriendIds { get; set; } = string.Empty;
        internal string PlayerName { get; set; } = string.Empty;
    }
}
