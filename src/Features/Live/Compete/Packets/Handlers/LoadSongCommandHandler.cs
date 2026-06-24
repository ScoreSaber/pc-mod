using ScoreSaber.Core;
using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Live.Compete.Packets;
using ScoreSaber.Live.V1;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Live.Compete.Packets.Handlers {
    internal sealed class LoadSongCommandHandler : ILudusServerCommandHandler {
        public LudusCommandType Type => LudusCommandType.LudusCommandTypeLoadSong;

        public void Handle(ILudusServerCommandSession session, ServerCommand command) {
            LoadSong(session, command).RunTask();
        }

        internal static async Task LoadSong(ILudusServerCommandSession session, ServerCommand command) {
            if (session.TournamentRoom == null || command.Song == null) {
                return;
            }

            CancellationToken cancellationToken = session.ConnectionCancellationToken;
            CompeteSongSelection installed = await session.SongService.ResolveInstalled(command.Song, cancellationToken);
            if (installed != null) {
                if (session.TournamentRoom == null) {
                    return;
                }

                session.TournamentRoom = session.TournamentRoom.WithSong(installed);
                session.NotifyRoomUpdated(session.TournamentRoom);
                session.SendDownloadState(LudusDownloadState.LudusDownloadStateDownloaded);
                return;
            }

            CompeteSongSelection preview = await session.SongService.CreatePreview(command.Song, cancellationToken);
            if (session.TournamentRoom == null) {
                return;
            }

            session.TournamentRoom = session.TournamentRoom.WithSongStatus(preview ?? session.TournamentRoom.Song, "Downloading map...");
            session.NotifyRoomUpdated(session.TournamentRoom);
            session.SendDownloadState(LudusDownloadState.LudusDownloadStateDownloading);

            try {
                CompeteSongSelection song = await session.SongService.ResolveOrDownload(command.Song, cancellationToken);
                if (song == null) {
                    throw new InvalidOperationException("SongCore could not resolve the downloaded song");
                }

                if (session.TournamentRoom == null) {
                    return;
                }

                session.TournamentRoom = session.TournamentRoom.WithSong(song);
                session.NotifyRoomUpdated(session.TournamentRoom);
                session.SendDownloadState(LudusDownloadState.LudusDownloadStateDownloaded);
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to load live room song: {ex.Message}");
                if (session.TournamentRoom == null) {
                    return;
                }

                session.TournamentRoom = session.TournamentRoom.WithSongStatus(preview ?? session.TournamentRoom.Song, "Map download failed.");
                session.NotifyRoomUpdated(session.TournamentRoom);
                session.SendDownloadState(LudusDownloadState.LudusDownloadStateError, ex.Message);
            }
        }
    }
}
