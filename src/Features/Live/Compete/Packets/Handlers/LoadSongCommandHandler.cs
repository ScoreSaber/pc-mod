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
            await EnsureSongReady(session, command?.Song);
        }

        internal static async Task<bool> EnsureSongReady(ILudusServerCommandSession session, LiveSongCommand song) {
            if (session.TournamentRoom == null || song == null) {
                return false;
            }

            if (session.TournamentRoom.Song?.BeatmapLevel != null && MatchesSong(session.TournamentRoom.Song, song)) {
                return true;
            }

            CancellationToken cancellationToken = session.ConnectionCancellationToken;
            CompeteSongSelection installed = await session.SongService.ResolveInstalled(song, cancellationToken);
            if (installed != null) {
                if (session.TournamentRoom == null) {
                    return false;
                }

                session.TournamentRoom = session.TournamentRoom.WithSong(installed);
                session.NotifyRoomUpdated(session.TournamentRoom);
                session.SendDownloadState(LudusDownloadState.LudusDownloadStateDownloaded);
                return true;
            }

            CompeteSongSelection preview = await session.SongService.CreatePreview(song, cancellationToken);
            if (session.TournamentRoom == null) {
                return false;
            }

            session.TournamentRoom = session.TournamentRoom.WithSongStatus(preview ?? session.TournamentRoom.Song, "Downloading map...");
            session.NotifyRoomUpdated(session.TournamentRoom);
            session.SendDownloadState(LudusDownloadState.LudusDownloadStateDownloading);

            try {
                CompeteSongSelection resolved = await session.SongService.ResolveOrDownload(song, cancellationToken);
                if (resolved == null || resolved.BeatmapLevel == null) {
                    throw new InvalidOperationException("SongCore could not resolve the downloaded song");
                }

                if (session.TournamentRoom == null) {
                    return false;
                }

                session.TournamentRoom = session.TournamentRoom.WithSong(resolved);
                session.NotifyRoomUpdated(session.TournamentRoom);
                session.SendDownloadState(LudusDownloadState.LudusDownloadStateDownloaded);
                return true;
            } catch (Exception ex) {
                Plugin.Log.Warn($"Failed to load live room song: {ex.Message}");
                if (session.TournamentRoom == null) {
                    return false;
                }

                session.TournamentRoom = session.TournamentRoom.WithSongStatus(preview ?? session.TournamentRoom.Song, "Map download failed.");
                session.NotifyRoomUpdated(session.TournamentRoom);
                session.SendDownloadState(LudusDownloadState.LudusDownloadStateError, ex.Message);
                return false;
            }
        }

        internal static LiveSongCommand SongCommandFromSelection(CompeteSongSelection song) {
            if (song == null || string.IsNullOrEmpty(song.MapHash)) {
                return null;
            }

            return new LiveSongCommand {
                Hash = song.MapHash,
                Difficulty = song.Difficulty,
                Characteristic = song.Characteristic
            };
        }

        private static bool MatchesSong(CompeteSongSelection selection, LiveSongCommand song) {
            if (selection == null || song == null) {
                return false;
            }

            return string.Equals(selection.MapHash, song.Hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
