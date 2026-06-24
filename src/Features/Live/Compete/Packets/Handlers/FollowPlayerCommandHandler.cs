using ScoreSaber.Features.Live.Compete.Packets;
using ScoreSaber.Live.V1;
using Newtonsoft.Json;
using System;

namespace ScoreSaber.Features.Live.Compete.Packets.Handlers {
    internal sealed class FollowPlayerCommandHandler : ILudusServerCommandHandler {
        public LudusCommandType Type => LudusCommandType.LudusCommandTypeFollowPlayer;

        public void Handle(ILudusServerCommandSession session, ServerCommand command) {
            session.NotifyPlayerFollowRequested(ViewerCount(command));
        }

        private static int ViewerCount(ServerCommand command) {
            if (string.IsNullOrEmpty(command?.PayloadJson)) {
                return 0;
            }

            try {
                return Math.Max(0, JsonConvert.DeserializeObject<FollowPlayerPayload>(command.PayloadJson)?.ViewerCount ?? 0);
            } catch (Exception ex) {
                Plugin.Log.Warn($"Ludus: Failed to read follow request payload: {ex.Message}");
                return 0;
            }
        }

        private sealed class FollowPlayerPayload {
            [JsonProperty("viewerCount")]
            public int ViewerCount { get; set; }
        }
    }
}
