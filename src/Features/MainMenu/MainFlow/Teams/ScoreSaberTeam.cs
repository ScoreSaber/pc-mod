#pragma warning disable IDE1006
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace ScoreSaber.Features.MainMenu.MainFlow.Teams {

    internal class ScoreSaberTeam {
        [JsonProperty("TeamMembers")]
        public Dictionary<TeamType, List<TeamMember>> TeamMembers { get; set; }
    }

    internal class TeamMember {
        [JsonProperty("Name")]
        internal string Name { get; set; }
        [JsonProperty("ProfilePicture")]
        internal string ProfilePicture { get; set; }
        [JsonProperty("Discord")]
        internal string Discord { get; set; }
        [JsonProperty("GitHub")]
        internal string GitHub { get; set; }
        [JsonProperty("Twitch")]
        internal string Twitch { get; set; }
        [JsonProperty("Twitter")]
        internal string Twitter { get; set; }
        [JsonProperty("YouTube")]
        internal string YouTube { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum TeamType {
        Backend,
        Frontend,
        Mod,
        PPv3,
        Admin,
        RT,
        NAT,
        QAT,
        CAT,
        CCT
    }
}
