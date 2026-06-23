using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Compete.Packets;
using ScoreSaber.Features.Live.Ludus.Services;
using ScoreSaber.Features.Live.Ludus.Packets;
using ScoreSaber.Features.Live.Protocol;
using ScoreSaber.Live.V1;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScoreSaber.Features.Live.Compete.Packets.Handlers {
    internal sealed class RoomSnapshotEnvelopeHandler : ILudusEnvelopeHandler<ILudusSessionPacketContext> {
        private readonly ILudusServerCommandSession _commandSession;

        internal RoomSnapshotEnvelopeHandler(ILudusServerCommandSession commandSession) {
            _commandSession = commandSession;
        }

        public LudusEnvelopeType Type => LudusEnvelopeType.RoomSnapshot;

        public void Handle(ILudusSessionPacketContext session, DecodedLudusEnvelope envelope) {
            ApplyRoomSnapshot(_commandSession, envelope.Rooms);
        }

        private static void ApplyRoomSnapshot(ILudusServerCommandSession session, IEnumerable<LiveMatchRoomState> rooms) {
            if (rooms == null) {
                session.NotifyViewersUpdated(null);
                return;
            }

            List<LiveMatchRoomState> roomList = rooms as List<LiveMatchRoomState> ?? rooms.ToList();
            LiveMatchRoomState room = FindSessionRoom(session, roomList);
            session.NotifyViewersUpdated(room?.Viewers);
            if (room == null || session.TournamentRoom == null) {
                return;
            }

            Dictionary<string, LiveRoomPlayerState> states = room.PlayerStates.ToDictionary(state => state.PlayerId, state => state);
            var players = new List<CompetePlayer>();
            bool localReady = session.TournamentRoom.LocalPlayerReady;

            foreach (CompetePlayer player in session.TournamentRoom.Players) {
                LiveRoomPlayerState state;
                if (!states.TryGetValue(player.PlayerId, out state)) {
                    players.Add(player);
                    continue;
                }

                bool isLocal = string.Equals(player.PlayerId, session.LocalPlayerId, StringComparison.Ordinal);
                if (isLocal) {
                    localReady = state.ReadyState == LudusReadyState.LudusReadyStateReady;
                }

                players.Add(new CompetePlayer(
                    player.Name,
                    FormatPlayerStatus(state),
                    player.TeamId,
                    player.Rank,
                    isLocal,
                    player.PlayerId,
                    state.IsBot,
                    player.AvatarUrl));
            }

            session.TournamentRoom = new CompeteRoom(
                session.TournamentRoom.Id,
                session.TournamentRoom.TournamentId,
                session.TournamentRoom.Name,
                session.TournamentRoom.Code,
                session.TournamentRoom.Round,
                session.TournamentRoom.State,
                session.TournamentRoom.PlayerListMode,
                session.TournamentRoom.Teams,
                session.TournamentRoom.Song,
                players,
                localReady,
                Math.Max(session.TournamentRoom.PlayerCount, players.Count));
            session.NotifyRoomUpdated(session.TournamentRoom);
        }

        private static LiveMatchRoomState FindSessionRoom(ILudusServerCommandSession session, IList<LiveMatchRoomState> rooms) {
            if (rooms == null || rooms.Count == 0) {
                return null;
            }

            if (session.TournamentRoom != null) {
                return rooms.FirstOrDefault(item => item.MatchId == session.TournamentRoom.Id || item.RoomId == session.TournamentRoom.Id);
            }

            string localPlayerId = session.LocalPlayerId;
            if (!string.IsNullOrEmpty(localPlayerId)) {
                LiveMatchRoomState playerRoom = rooms.FirstOrDefault(item =>
                    item.MatchId == $"player:{localPlayerId}" ||
                    item.PlayerIds.Contains(localPlayerId));
                if (playerRoom != null) {
                    return playerRoom;
                }
            }

            return rooms.Count == 1 ? rooms[0] : null;
        }

        private static string FormatPlayerStatus(LiveRoomPlayerState state) {
            if (state.ReadyState == LudusReadyState.LudusReadyStateReady) {
                return "Ready";
            }

            if (state.DownloadState == LudusDownloadState.LudusDownloadStateDownloading) {
                return "Downloading";
            }

            if (state.DownloadState == LudusDownloadState.LudusDownloadStateError) {
                return "Download Error";
            }

            if (state.PlayState == LudusPlayState.LudusPlayStateInGame) {
                return "In Game";
            }

            return "Waiting";
        }
    }
}
