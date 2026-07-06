using ScoreSaber.Core.Api;
using ScoreSaber.Core.Api.Generated;
using ScoreSaber.Features.Live.Compete.Domain;
using ScoreSaber.Features.Players.Domain;
using ScoreSaber.Features.Players.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScoreSaber.Features.Live.Compete.Services {
    internal class CompeteDirectoryService {
        private readonly IScoreSaberApiClient _apiClient;
        private readonly GameSessionService _gameSessionService;

        internal CompeteDirectoryService(IScoreSaberApiClient apiClient, GameSessionService gameSessionService) {
            _apiClient = apiClient;
            _gameSessionService = gameSessionService;
        }

        internal async Task<IReadOnlyList<CompeteTournament>> GetActiveTournaments(CancellationToken cancellationToken) {
            GameSession session = await GetSession(cancellationToken);
            List<LivePlayerTournamentSummary> tournaments = await _apiClient.ListLivePlayerTournaments(session, cancellationToken);
            Plugin.Log.Info($"Live tournaments loaded for player {session.PlayerId}: {tournaments.Count}");
            return tournaments
                .Select(tournament => new CompeteTournament(
                    tournament.TournamentId ?? string.Empty,
                    tournament.Name ?? tournament.TournamentId ?? "Tournament",
                    tournament.RoomSummary ?? string.Empty))
                .ToArray();
        }

        internal async Task<IReadOnlyList<CompeteRoom>> GetJoinableRooms(string tournamentId, CancellationToken cancellationToken) {
            GameSession session = await GetSession(cancellationToken);
            List<LivePlayerRoomSummary> rooms = await _apiClient.ListLivePlayerRooms(tournamentId, session, cancellationToken);
            Plugin.Log.Info($"Live rooms loaded for player {session.PlayerId} in {tournamentId}: {rooms.Count}");
            return rooms.Select(ToDomain).ToArray();
        }

        internal async Task<CompeteRoom> GetRoom(string tournamentId, string matchId, CancellationToken cancellationToken) {
            GameSession session = await GetSession(cancellationToken);
            LivePlayerRoomDetails room = await _apiClient.GetLivePlayerRoom(tournamentId, matchId, session, cancellationToken);
            return await ToDomain(room, cancellationToken);
        }

        internal async Task<CompeteRoom> GetRoomByInviteCode(string inviteCode, CancellationToken cancellationToken) {
            GameSession session = await GetSession(cancellationToken);
            LivePlayerRoomDetails room = await _apiClient.GetLivePlayerRoomByInviteCode(inviteCode, session, cancellationToken);
            return await ToDomain(room, cancellationToken);
        }

        private async Task<GameSession> GetSession(CancellationToken cancellationToken) {
            bool authenticated = await _gameSessionService.EnsureAuthenticated(false, cancellationToken);
            if (!authenticated || !_gameSessionService.HasAuthenticatedSession) {
                throw new InvalidOperationException("ScoreSaber game session is not available");
            }

            return _gameSessionService.GameSession;
        }

        private static CompeteRoom ToDomain(LivePlayerRoomSummary room) {
            return new CompeteRoom(
                room.MatchId ?? string.Empty,
                room.TournamentId ?? string.Empty,
                RoomName(room.MatchId),
                room.InviteCode ?? string.Empty,
                room.MatchId ?? string.Empty,
                FormatRoomState(room.State.ToString()),
                room.RosterMode == LivePlayerRoomSummaryRosterMode.TEAM ? CompetePlayerListMode.Teams : CompetePlayerListMode.Regular,
                Array.Empty<CompeteTeam>(),
                null,
                Array.Empty<CompetePlayer>(),
                false,
                ToInt(room.PlayerCount));
        }

        private async Task<CompeteRoom> ToDomain(LivePlayerRoomDetails room, CancellationToken cancellationToken) {
            CompeteTeam[] teams = BuildTeams(room.Members);
            CompetePlayer[] players = room.Members
                .Where(member => member.Role == LivePlayerRoomDetailsMembersItemRole.PLAYER)
                .Select(ToDomain)
                .ToArray();

            return new CompeteRoom(
                room.MatchId ?? string.Empty,
                room.TournamentId ?? string.Empty,
                RoomName(room.MatchId),
                room.InviteCode ?? string.Empty,
                room.MatchId ?? string.Empty,
                FormatRoomState(room.State.ToString()),
                room.RosterMode == LivePlayerRoomDetailsRosterMode.TEAM ? CompetePlayerListMode.Teams : CompetePlayerListMode.Regular,
                teams,
                await ToSong(room.SelectedSong, cancellationToken),
                players,
                false,
                ToInt(room.PlayerCount));
        }

        private CompetePlayer ToDomain(LivePlayerRoomDetailsMembersItem member) {
            string teamId = member.TeamId.HasValue ? TeamId(member.TeamId.Value) : string.Empty;
            string playerId = member.PlayerId ?? member.Player?.Id ?? string.Empty;
            bool isLocalPlayer = _gameSessionService.LocalPlayerInfo != null &&
                string.Equals(_gameSessionService.LocalPlayerInfo.playerId, playerId, StringComparison.Ordinal);

            return new CompetePlayer(
                member.Player?.Name ?? playerId ?? "Player",
                FormatMemberStatus(member),
                teamId,
                string.Empty,
                isLocalPlayer,
                playerId,
                member.IsBot,
                member.Player?.Avatar,
                member.Connected);
        }

        private async Task<CompeteSongSelection> ToSong(LivePlayerRoomDetailsSelectedSong song, CancellationToken cancellationToken) {
            if (song == null) {
                return null;
            }

            string stars = await FetchSongStars(song, cancellationToken);
            return new CompeteSongSelection(
                null,
                default,
                DisplaySongName(song.SongName, song.SongSubName),
                song.LevelAuthorName ?? song.SongAuthorName ?? "Unknown",
                FormatDifficulty(song.Difficulty.ToString()),
                song.Characteristic.ToString(),
                song.CoverUrl ?? string.Empty,
                FormatDuration(song.DurationSeconds),
                Math.Round(song.Bpm).ToString(CultureInfo.InvariantCulture),
                song.Nps <= 0 ? "--" : song.Nps.ToString("0.00", CultureInfo.InvariantCulture),
                "--",
                "--",
                "--",
                "--",
                "--",
                stars,
                song.MapHash ?? string.Empty,
                song.DownloadUrl ?? string.Empty);
        }

        private async Task<string> FetchSongStars(LivePlayerRoomDetailsSelectedSong song, CancellationToken cancellationToken) {
            string hash = (song?.MapHash ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(hash)) {
                return "--";
            }

            try {
                MapDetailsResponse map = await _apiClient.GetMapByHash(hash, cancellationToken);
                return FormatStars(SelectLeaderboard(map, song)?.Realm?.Stars);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception ex) {
                Plugin.Log.Warn($"Unable to fetch ScoreSaber live room song stars: {ex.Message}");
                return "--";
            }
        }

        private static CompeteTeam[] BuildTeams(IEnumerable<LivePlayerRoomDetailsMembersItem> members) {
            return members
                .Where(member => member.TeamId.HasValue)
                .GroupBy(member => member.TeamId.Value)
                .OrderBy(group => group.Key)
                .Select((group, index) => new CompeteTeam(
                    TeamId(group.Key),
                    group.Select(member => member.TeamName).FirstOrDefault(name => !string.IsNullOrEmpty(name)) ?? $"Team {index + 1}"))
                .ToArray();
        }

        private static string TeamId(double teamId) {
            return teamId.ToString("0", CultureInfo.InvariantCulture);
        }

        private static string FormatMemberStatus(LivePlayerRoomDetailsMembersItem member) {
            if (!member.Connected) {
                return "Offline";
            }

            if (member.DownloadState == LivePlayerRoomDetailsMembersItemDownloadState.DOWNLOADING) {
                return "Downloading";
            }

            if (member.DownloadState == LivePlayerRoomDetailsMembersItemDownloadState.ERROR) {
                return "Download Error";
            }

            return FormatRoomState(member.PlayState.ToString());
        }

        private static string RoomName(string matchId) {
            return string.IsNullOrEmpty(matchId) ? "Room" : matchId;
        }

        private static string FormatRoomState(string value) {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase((value ?? string.Empty).Replace("_", " ").ToLowerInvariant());
        }

        private static string DisplaySongName(string songName, string songSubName) {
            return string.IsNullOrEmpty(songSubName) ? songName ?? string.Empty : $"{songName} {songSubName}";
        }

        private static string FormatDifficulty(string difficulty) {
            return string.Equals(difficulty, "ExpertPlus", StringComparison.OrdinalIgnoreCase) ? "Expert+" : difficulty;
        }

        private static MapDetailsResponseLeaderboardsItem SelectLeaderboard(MapDetailsResponse map, LivePlayerRoomDetailsSelectedSong song) {
            if (map?.Leaderboards == null || map.Leaderboards.Count == 0) {
                return null;
            }

            if (song?.LeaderboardId.HasValue == true) {
                MapDetailsResponseLeaderboardsItem idMatch = map.Leaderboards.FirstOrDefault(
                    leaderboard => Math.Abs(leaderboard.Id - song.LeaderboardId.Value) < 0.1d);
                if (idMatch != null) {
                    return idMatch;
                }
            }

            string difficulty = NormalizeDifficultyName(song == null ? string.Empty : song.Difficulty.ToString());
            string characteristic = NormalizeCharacteristicName(song == null ? string.Empty : song.Characteristic.ToString());
            List<MapDetailsResponseLeaderboardsItem> leaderboards = map.Leaderboards
                .Where(leaderboard => string.IsNullOrEmpty(difficulty) || NormalizeDifficultyName(DifficultyName(leaderboard)) == difficulty)
                .ToList();

            if (leaderboards.Count == 0) {
                leaderboards = map.Leaderboards;
            }

            if (!string.IsNullOrEmpty(characteristic)) {
                MapDetailsResponseLeaderboardsItem characteristicMatch = leaderboards.FirstOrDefault(
                    leaderboard => NormalizeCharacteristicName(CharacteristicName(leaderboard)) == characteristic);
                if (characteristicMatch != null) {
                    return characteristicMatch;
                }
            }

            return leaderboards.FirstOrDefault();
        }

        private static string DifficultyName(MapDetailsResponseLeaderboardsItem leaderboard) {
            string rawDifficulty = RawDifficultyPart(leaderboard?.RawDifficulty, 0);
            if (!string.IsNullOrEmpty(rawDifficulty)) {
                return rawDifficulty;
            }

            switch ((int)(leaderboard?.Difficulty ?? 0)) {
                case 1:
                    return "Easy";
                case 3:
                    return "Normal";
                case 5:
                    return "Hard";
                case 7:
                    return "Expert";
                case 9:
                    return "ExpertPlus";
                default:
                    return string.Empty;
            }
        }

        private static string CharacteristicName(MapDetailsResponseLeaderboardsItem leaderboard) {
            string gameMode = leaderboard?.GameMode;
            if (string.IsNullOrWhiteSpace(gameMode)) {
                gameMode = RawDifficultyPart(leaderboard?.RawDifficulty, 1);
            }

            if (string.IsNullOrWhiteSpace(gameMode)) {
                return string.Empty;
            }

            return gameMode.StartsWith("Solo", StringComparison.OrdinalIgnoreCase)
                ? gameMode.Substring(4)
                : gameMode;
        }

        private static string RawDifficultyPart(string rawDifficulty, int index) {
            if (string.IsNullOrWhiteSpace(rawDifficulty)) {
                return string.Empty;
            }

            string[] parts = rawDifficulty.Trim('_').Split('_');
            return index >= 0 && index < parts.Length ? parts[index] : string.Empty;
        }

        private static string NormalizeDifficultyName(string difficulty) {
            return (difficulty ?? string.Empty)
                .Replace("+", "Plus")
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();
        }

        private static string NormalizeCharacteristicName(string characteristic) {
            string normalized = (characteristic ?? string.Empty)
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();

            if (normalized.StartsWith("solo", StringComparison.Ordinal)) {
                normalized = normalized.Substring(4);
            }

            switch (normalized) {
                case "90degree":
                case "generated90degree":
                    return "ninetydegree";
                case "360degree":
                case "generated360degree":
                    return "threesixtydegree";
                default:
                    return normalized;
            }
        }

        private static string FormatStars(double? value) {
            return value.HasValue && value.Value > 0d ? value.Value.ToString("0.00", CultureInfo.InvariantCulture) : "--";
        }

        private static string FormatDuration(double seconds) {
            if (seconds <= 0) {
                return "--";
            }

            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return $"{(int)time.TotalMinutes}:{time.Seconds:00}";
        }

        private static int ToInt(double value) {
            return (int)Math.Round(value);
        }
    }
}
