namespace ScoreSaber.Features.Players.Domain {
    internal class LocalPlayerInfo {

        internal string playerId { get; set; }
        internal string playerName { get; set; }
        internal string playerKey { get; set; }
        internal string playerFriends { get; set; }
        internal string playerNonce { get; set; }
        internal string authType { get; set; }
        internal LocalPlayerInfo(string playerId, string playerName, string playerFriends, string authType, string playerNonce) {

            this.playerId = playerId;
            this.playerName = playerName;
            this.playerFriends = playerFriends;
            this.authType = authType;
            this.playerNonce = playerNonce;
        }

    }
}
