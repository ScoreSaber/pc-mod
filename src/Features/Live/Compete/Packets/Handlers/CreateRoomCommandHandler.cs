using ScoreSaber.Core;
using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Compete.Packets;
using ScoreSaber.Live.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Live.Compete.Packets.Handlers {
    internal sealed class CreateRoomCommandHandler : ILudusServerCommandHandler {
        public LudusCommandType Type => LudusCommandType.LudusCommandTypeCreateRoom;

        public void Handle(ILudusServerCommandSession session, ServerCommand command) {
            RefreshRoomDetails(session, command.MatchId).RunTask();
        }

        private static async Task RefreshRoomDetails(ILudusServerCommandSession session, string matchId) {
            CompeteRoom currentRoom = session.TournamentRoom;
            if (currentRoom == null) {
                return;
            }

            if (!string.IsNullOrEmpty(matchId) && !string.Equals(matchId, currentRoom.Id, StringComparison.Ordinal)) {
                return;
            }

            try {
                CompeteRoom details = await session.DirectoryService.GetRoom(currentRoom.TournamentId, currentRoom.Id, session.ConnectionCancellationToken);
                if (details == null || session.TournamentRoom == null) {
                    return;
                }

                session.TournamentRoom = MergeRoomDetails(details, session.TournamentRoom);
                session.NotifyRoomUpdated(session.TournamentRoom);
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to refresh live room details: {ex.Message}");
            }
        }

        private static CompeteRoom MergeRoomDetails(CompeteRoom details, CompeteRoom current) {
            Dictionary<string, CompetePlayer> livePlayers = current.Players
                .Where(player => !string.IsNullOrEmpty(player.PlayerId))
                .ToDictionary(player => player.PlayerId, player => player);

            CompetePlayer[] players = details.Players.Select(player => {
                CompetePlayer livePlayer;
                if (!string.IsNullOrEmpty(player.PlayerId) && livePlayers.TryGetValue(player.PlayerId, out livePlayer)) {
                    return new CompetePlayer(
                        player.Name,
                        livePlayer.Status,
                        player.TeamId,
                        player.Rank,
                        player.IsLocalPlayer,
                        player.PlayerId,
                        livePlayer.IsBot,
                        player.AvatarUrl,
                        player.IsActive || livePlayer.IsActive);
                }

                return player;
            }).ToArray();

            CompeteSongSelection song = ShouldKeepCurrentSong(current.Song, details.Song) ? current.Song : details.Song;
            string songStatus = ReferenceEquals(song, current.Song) ? current.SongStatus : details.SongStatus;

            return new CompeteRoom(
                details.Id,
                details.TournamentId,
                details.Name,
                details.Code,
                details.Round,
                details.State,
                details.PlayerListMode,
                details.Teams,
                song,
                players,
                current.LocalPlayerReady,
                details.PlayerCount,
                songStatus);
        }

        private static bool ShouldKeepCurrentSong(CompeteSongSelection current, CompeteSongSelection details) {
            if (current == null) {
                return false;
            }

            if (details == null) {
                return true;
            }

            if (!string.IsNullOrEmpty(current.MapHash) && !string.IsNullOrEmpty(details.MapHash)) {
                return string.Equals(current.MapHash, details.MapHash, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(current.Name, details.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(current.Difficulty, details.Difficulty, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(current.Characteristic, details.Characteristic, StringComparison.OrdinalIgnoreCase);
        }
    }
}
