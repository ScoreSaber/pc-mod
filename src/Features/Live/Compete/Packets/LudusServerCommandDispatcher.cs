using ScoreSaber.Features.Live.Compete.Packets.Handlers;
using ScoreSaber.Live.V1;
using System.Collections.Generic;

namespace ScoreSaber.Features.Live.Compete.Packets {
    internal interface ILudusServerCommandHandler {
        LudusCommandType Type { get; }
        void Handle(ILudusServerCommandSession session, ServerCommand command);
    }

    internal sealed class LudusServerCommandDispatcher {
        private readonly Dictionary<LudusCommandType, ILudusServerCommandHandler> _handlers;

        private LudusServerCommandDispatcher(IEnumerable<ILudusServerCommandHandler> handlers) {
            _handlers = new Dictionary<LudusCommandType, ILudusServerCommandHandler>();
            foreach (ILudusServerCommandHandler handler in handlers) {
                _handlers[handler.Type] = handler;
            }
        }

        internal static LudusServerCommandDispatcher CreateDefault() {
            return new LudusServerCommandDispatcher(new ILudusServerCommandHandler[] {
                new CreateRoomCommandHandler(),
                new PromptCommandHandler(),
                new LoadSongCommandHandler(),
                new StartMapCommandHandler(),
                new ReturnToMenuCommandHandler(),
                new FollowPlayerCommandHandler()
            });
        }

        internal void Handle(ILudusServerCommandSession session, ServerCommand command) {
            if (command == null) {
                return;
            }

            if (!TargetsLocalPlayer(session, command)) {
                Plugin.Log.Debug($"Ludus: Ignored server command {command.Type} for match {command.MatchId}.");
                return;
            }

            ILudusServerCommandHandler handler;
            if (_handlers.TryGetValue(command.Type, out handler)) {
                Plugin.Log.Info($"Ludus: Handling server command {command.Type} for match {command.MatchId}.");
                handler.Handle(session, command);
            } else {
                Plugin.Log.Warn($"Ludus: No handler registered for server command {command.Type}.");
            }
        }

        private static bool TargetsLocalPlayer(ILudusServerCommandSession session, ServerCommand command) {
            return command.TargetPlayerIds.Count == 0 || command.TargetPlayerIds.Contains(session.LocalPlayerId);
        }
    }
}
