using ScoreSaber.Core;
using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Compete.Packets;
using ScoreSaber.Features.Live.Compete.Services;
using ScoreSaber.Live.V1;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Live.Compete.Packets.Handlers {
    internal sealed class StartMapCommandHandler : ILudusServerCommandHandler {
        public LudusCommandType Type => LudusCommandType.LudusCommandTypeStartMap;

        public void Handle(ILudusServerCommandSession session, ServerCommand command) {
            StartMap(session, command).RunTask();
        }

        private static async Task StartMap(ILudusServerCommandSession session, ServerCommand command) {
            if (!HasTournamentRoom(session, command.MatchId)) {
                return;
            }

            CancellationToken cancellationToken = session.ConnectionCancellationToken;
            try {
                if (session.TournamentRoom.Song == null || session.TournamentRoom.Song.BeatmapLevel == null) {
                    LiveSongCommand song = command.Song ?? LoadSongCommandHandler.SongCommandFromSelection(session.TournamentRoom.Song);
                    if (song != null) {
                        await LoadSongCommandHandler.EnsureSongReady(session, song);
                    }
                }

                if (session.TournamentRoom?.Song?.BeatmapLevel == null) {
                    throw new InvalidOperationException("Live room song is not ready");
                }

                CompeteRoom room = session.TournamentRoom;
                int delayMs = CompeteGameplayLauncher.StartDelayMs(command);
                cancellationToken = session.BeginMapStartCountdown(command.MatchId, delayMs, cancellationToken);
                session.NotifyStatusChanged(delayMs > 0 ? "Map starting soon..." : "Starting map...");
                Plugin.Log.Info($"Ludus: Starting room map {room.Song.Name} for {room.Id}.");
                await session.GameplayLauncher.Start(room, command, cancellationToken);
                if (!await session.GameplayLauncher.WaitForMapStartReady(room.Id, room.Song.MapHash, cancellationToken)) {
                    return;
                }

                session.SendPresence(LudusPlayState.LudusPlayStateInGame, LudusDownloadState.LudusDownloadStateNone, room.Song.MapHash);
            } catch (OperationCanceledException) {
            } catch (Exception ex) {
                session.NotifyStatusChanged($"Failed to start map: {ex.Message}");
                Plugin.Log.Warn($"Ludus: Failed to start map: {ex.Message}");
            } finally {
                session.CompletePendingMapStart(command.MatchId);
            }
        }

        private static bool HasTournamentRoom(ILudusServerCommandSession session, string matchId) {
            return session.TournamentRoom != null && (string.IsNullOrEmpty(matchId) || string.Equals(matchId, session.TournamentRoom.Id, StringComparison.Ordinal));
        }
    }
}
