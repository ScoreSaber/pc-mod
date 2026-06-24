using ScoreSaber.Features.Live.Compete.Packets;
using ScoreSaber.Live.V1;

namespace ScoreSaber.Features.Live.Compete.Packets.Handlers {
    internal sealed class ReturnToMenuCommandHandler : ILudusServerCommandHandler {
        public LudusCommandType Type => LudusCommandType.LudusCommandTypeReturnToMenu;

        public void Handle(ILudusServerCommandSession session, ServerCommand command) {
            if (session.TryCancelPendingMapStart(command.MatchId)) {
                session.NotifyStatusChanged("Map start cancelled.");
                Plugin.Log.Info("Ludus: Pending live map start cancelled.");
                return;
            }

            if (session.GameplayControl.TryStopMap(command.MatchId)) {
                session.NotifyStatusChanged("Stopping map...");
                Plugin.Log.Info("Ludus: Stopping live map.");
                return;
            }

            Plugin.Log.Info("Ludus: Stop map requested, but no live map is active.");
        }
    }
}
