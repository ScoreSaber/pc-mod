using System;
using System.Collections.Generic;

namespace ScoreSaber.Features.Players.Domain {
    internal class PlayerProfile : PlayerSummary {
        internal string Bio { get; set; } = string.Empty;
        internal DateTimeOffset CreatedAt { get; set; }
        internal DateTimeOffset LastSeenAt { get; set; }
        internal List<PlayerBadge> Badges { get; set; } = new List<PlayerBadge>();
        internal int Followers { get; set; }
        internal int Following { get; set; }
    }

    internal class PlayerBadge {
        internal string Image { get; set; } = string.Empty;
        internal string Description { get; set; } = string.Empty;
    }
}
