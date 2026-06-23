using System.Collections.Generic;
using System.Linq;

namespace ScoreSaber.Features.Live.Compete.Domain {
    internal class CompeteRoom {
        internal string Id { get; }
        internal string TournamentId { get; }
        internal string Name { get; }
        internal string Code { get; }
        internal string Round { get; }
        internal string State { get; }
        internal CompetePlayerListMode PlayerListMode { get; }
        internal IReadOnlyList<CompeteTeam> Teams { get; }
        internal CompeteSongSelection Song { get; }
        internal string SongStatus { get; }
        internal IReadOnlyList<CompetePlayer> Players { get; }
        internal bool LocalPlayerReady { get; }
        internal int PlayerCount { get; }

        internal CompeteRoom(
            string id,
            string tournamentId,
            string name,
            string code,
            string round,
            string state,
            CompetePlayerListMode playerListMode,
            IEnumerable<CompeteTeam> teams,
            CompeteSongSelection song,
            IEnumerable<CompetePlayer> players,
            bool localPlayerReady,
            int playerCount = -1,
            string songStatus = "") {

            Id = id;
            TournamentId = tournamentId;
            Name = name;
            Code = code;
            Round = round;
            State = state;
            PlayerListMode = playerListMode;
            Teams = teams.ToArray();
            Song = song;
            SongStatus = songStatus ?? string.Empty;
            Players = players.ToArray();
            LocalPlayerReady = localPlayerReady;
            PlayerCount = playerCount >= 0 ? playerCount : Players.Count;
        }

        internal string DisplayName => string.IsNullOrEmpty(Code) ? Name : $"{Name} - {Code}";

        internal CompeteRoom WithPlayers(IEnumerable<CompetePlayer> players, bool localPlayerReady) {
            return new CompeteRoom(Id, TournamentId, Name, Code, Round, State, PlayerListMode, Teams, Song, players, localPlayerReady, PlayerCount, SongStatus);
        }

        internal CompeteRoom WithSong(CompeteSongSelection song) {
            return new CompeteRoom(Id, TournamentId, Name, Code, Round, State, PlayerListMode, Teams, song, Players, LocalPlayerReady, PlayerCount);
        }

        internal CompeteRoom WithSongStatus(CompeteSongSelection song, string songStatus) {
            return new CompeteRoom(Id, TournamentId, Name, Code, Round, State, PlayerListMode, Teams, song, Players, LocalPlayerReady, PlayerCount, songStatus);
        }
    }
}
