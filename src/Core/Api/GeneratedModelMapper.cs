using ScoreSaber.Core.Api.Generated;
using ScoreSaber.Core.Api.Paging;
using ScoreSaber.Core.Gameplay;
using ScoreSaber.Features.Players.Domain;
using ScoreSaber.Features.Leaderboards.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ScoreSaber.Core.Api {

    internal static class GeneratedModelMapper {

        internal static LeaderboardDetails ToDomain(LeaderboardResponse source) {
            return new LeaderboardDetails {
                Id = ToInt(source.Id),
                SongHash = source.Map.Hash ?? string.Empty,
                SongName = source.Map.SongName ?? string.Empty,
                SongSubName = source.Map.SongSubName ?? string.Empty,
                SongAuthorName = source.Map.SongAuthorName ?? string.Empty,
                LevelAuthorName = source.Map.LevelAuthorName ?? string.Empty,
                CoverImage = source.Map.CoverUrl ?? string.Empty,
                Difficulty = ToInt(source.Difficulty.Difficulty),
                DifficultyRaw = source.Difficulty.RawDifficulty ?? string.Empty,
                GameMode = source.Difficulty.GameMode ?? string.Empty,
                MaxScore = ToInt(source.MaxScore),
                Plays = ToInt(source.TotalScores),
                DailyPlays = ToInt(source.DailyScores),
                CreatedAt = ParseOffset(source.CreatedAt),
                RankedAt = ParseNullableOffset(source.Realm.RankedAt),
                QualifiedAt = ParseNullableOffset(source.Realm.QualifiedAt),
                LovedAt = ParseNullableOffset(source.Realm.LovedAt),
                Status = ToDomain(source.Realm.LeaderboardStatus),
                PositiveModifiers = source.Realm.PositiveModifiers,
                Stars = source.Realm.Stars,
                RealmId = ToInt(source.Realm.RealmId),
                RealmName = source.Realm.RealmName ?? string.Empty
            };
        }

        internal static LeaderboardScore ToDomain(LeaderboardScoresResponseDataItem source) {
            return new LeaderboardScore {
                Id = ToInt(source.Id),
                Rank = ToInt(source.Rank),
                UnmodifiedScore = ToInt(source.UnmodifiedScore),
                ModifiedScore = ToInt(source.ModifiedScore),
                Accuracy = source.Accuracy,
                PP = source.PP,
                Weight = source.Weight,
                Mods = source.Mods ?? new List<string>(),
                BadCuts = ToInt(source.BadCuts),
                MissedNotes = ToInt(source.MissedNotes),
                MaxCombo = ToInt(source.MaxCombo),
                FullCombo = source.FullCombo,
                HasReplay = source.HasReplay,
                PersonalBest = source.PersonalBest,
                PlayOutcome = ToDomain(source.PlayOutcome),
                PlayOutcomeTime = source.PlayOutcomeTime,
                LegacyHMDId = source.LegacyHMDId.HasValue ? ToInt(source.LegacyHMDId.Value) : 0,
                Version = source.Version ?? string.Empty,
                CreatedAt = ParseDate(source.CreatedAt),
                Player = ToDomain(source.Player),
                Device = ToDomain(source.Device)
            };
        }

        internal static LeaderboardScore ToDomain(LeaderboardScoresResponsePlayerScore source) {
            if (source == null) {
                return null;
            }

            return new LeaderboardScore {
                Id = ToInt(source.Id),
                Rank = ToInt(source.Rank),
                UnmodifiedScore = ToInt(source.UnmodifiedScore),
                ModifiedScore = ToInt(source.ModifiedScore),
                Accuracy = source.Accuracy,
                PP = source.PP,
                Weight = source.Weight,
                Mods = source.Mods ?? new List<string>(),
                BadCuts = ToInt(source.BadCuts),
                MissedNotes = ToInt(source.MissedNotes),
                MaxCombo = ToInt(source.MaxCombo),
                FullCombo = source.FullCombo,
                HasReplay = source.HasReplay,
                PersonalBest = source.PersonalBest,
                PlayOutcome = ToDomain(source.PlayOutcome),
                PlayOutcomeTime = source.PlayOutcomeTime,
                LegacyHMDId = source.LegacyHMDId.HasValue ? ToInt(source.LegacyHMDId.Value) : 0,
                Version = source.Version ?? string.Empty,
                CreatedAt = ParseDate(source.CreatedAt),
                Player = ToDomain(source.Player),
                Device = ToDomain(source.Device)
            };
        }

        internal static PlayerSummary ToDomain(LeaderboardScoresResponseDataItemPlayer source) {
            return new PlayerSummary {
                Id = source.Id ?? string.Empty,
                Name = ToPlayerName(source.PlayerNameInGame, source.Name),
                Country = source.Country ?? string.Empty,
                Role = source.Role ?? string.Empty,
                Avatar = source.Avatar ?? string.Empty,
                Permissions = ToInt(source.Permissions)
            };
        }

        internal static PlayerSummary ToDomain(LeaderboardScoresResponsePlayerScorePlayer source) {
            return new PlayerSummary {
                Id = source.Id ?? string.Empty,
                Name = ToPlayerName(source.PlayerNameInGame, source.Name),
                Country = source.Country ?? string.Empty,
                Role = source.Role ?? string.Empty,
                Avatar = source.Avatar ?? string.Empty,
                Permissions = ToInt(source.Permissions)
            };
        }

        internal static PlayerSummary ToDomain(PlayerListResponseDataItem source) {
            return new PlayerSummary {
                Id = source.Id ?? string.Empty,
                Name = ToPlayerName(source.PlayerNameInGame, source.Name),
                Country = source.Country ?? string.Empty,
                Role = source.Role ?? string.Empty,
                Avatar = source.Avatar ?? string.Empty,
                Permissions = ToInt(source.Permissions),
                Banned = source.Banned,
                Inactive = source.Inactive,
                Stats = ToDomain(source.Stats)
            };
        }

        internal static PlayerProfile ToDomain(PlayerProfileResponse source) {
            var profile = new PlayerProfile {
                Id = source.Id ?? string.Empty,
                Name = ToPlayerName(source.PlayerNameInGame, source.Name),
                Country = source.Country ?? string.Empty,
                Role = source.Role ?? string.Empty,
                Avatar = source.Avatar ?? string.Empty,
                Permissions = ToInt(source.Permissions),
                Banned = source.Banned,
                Inactive = source.Inactive,
                Stats = ToDomain(source.Stats),
                Bio = source.Bio ?? string.Empty,
                CreatedAt = ParseOffset(source.CreatedAt),
                LastSeenAt = ParseOffset(source.LastSeenAt),
                Followers = ToInt(source.Followers),
                Following = ToInt(source.Following)
            };

            foreach (var badge in source.Badges) {
                profile.Badges.Add(new PlayerBadge {
                    Image = badge.Image ?? string.Empty,
                    Description = badge.Description ?? string.Empty
                });
            }

            return profile;
        }

        internal static PlayerProfile ToDomain(PlayerBasicProfileResponse source) {
            return new PlayerProfile {
                Id = source.Id ?? string.Empty,
                Name = ToPlayerName(source.PlayerNameInGame, source.Name),
                Country = source.Country ?? string.Empty,
                Role = source.Role ?? string.Empty,
                Avatar = source.Avatar ?? string.Empty,
                Permissions = ToInt(source.Permissions),
                Banned = source.Banned,
                Inactive = source.Inactive,
                Stats = ToDomain(source.Stats)
            };
        }

        internal static PlayerHistoryPoint ToDomain(GlobalPlayerHistoryEntry source) {
            return new PlayerHistoryPoint {
                Rank = ToInt(source.Rank),
                TotalPP = source.TotalPP,
                TotalScore = ToLong(source.TotalScore),
                TotalRankedScore = ToLong(source.TotalRankedScore),
                Estimated = source.Estimated,
                CreatedAt = ParseOffset(source.CreatedAt)
            };
        }

        internal static PageMetadata ToDomain(LeaderboardScoresResponseMetadata source) {
            return new PageMetadata {
                Page = ToInt(source.Page),
                ItemsPerPage = ToInt(source.ItemsPerPage),
                TotalItems = ToInt(source.TotalItems),
                TotalPages = ToInt(source.TotalPages)
            };
        }

        internal static PageMetadata ToDomain(PlayerListResponseMetadata source) {
            return new PageMetadata {
                Page = ToInt(source.Page),
                ItemsPerPage = ToInt(source.ItemsPerPage),
                TotalItems = ToInt(source.TotalItems),
                TotalPages = ToInt(source.TotalPages)
            };
        }

        private static PlayerStats ToDomain(PlayerListResponseDataItemStats source) {
            if (source == null) {
                return new PlayerStats();
            }

            return new PlayerStats {
                RealmId = ToInt(source.RealmId),
                RealmName = source.RealmName ?? string.Empty,
                Rank = ToInt(source.Rank),
                CountryRank = ToInt(source.CountryRank),
                TotalPP = source.TotalPP,
                TotalScore = ToLong(source.TotalScore),
                TotalRankedScore = ToLong(source.TotalRankedScore),
                TotalPlayedLeaderboards = ToInt(source.TotalPlayedLeaderboards),
                TotalPlayedRankedLeaderboards = ToInt(source.TotalPlayedRankedLeaderboards),
                TotalSubmittedPlays = ToInt(source.TotalSubmittedPlays),
                TotalReplayViews = ToInt(source.TotalReplayViews),
                AverageAccuracy = source.AverageAccuracy,
                WeightedAverageAccuracy = source.WeightedAverageAccuracy,
                CompletionAccuracy = source.CompletionAccuracy,
                Device = ToDomain(source.Device)
            };
        }

        private static PlayerStats ToDomain(PlayerProfileResponseStats source) {
            if (source == null) {
                return new PlayerStats();
            }

            return new PlayerStats {
                RealmId = ToInt(source.RealmId),
                RealmName = source.RealmName ?? string.Empty,
                Rank = ToInt(source.Rank),
                CountryRank = ToInt(source.CountryRank),
                TotalPP = source.TotalPP,
                TotalScore = ToLong(source.TotalScore),
                TotalRankedScore = ToLong(source.TotalRankedScore),
                TotalPlayedLeaderboards = ToInt(source.TotalPlayedLeaderboards),
                TotalPlayedRankedLeaderboards = ToInt(source.TotalPlayedRankedLeaderboards),
                TotalSubmittedPlays = ToInt(source.TotalSubmittedPlays),
                TotalReplayViews = ToInt(source.TotalReplayViews),
                AverageAccuracy = source.AverageAccuracy,
                WeightedAverageAccuracy = source.WeightedAverageAccuracy,
                CompletionAccuracy = source.CompletionAccuracy,
                Device = ToDomain(source.Device)
            };
        }

        private static PlayerStats ToDomain(PlayerBasicProfileResponseStats source) {
            if (source == null) {
                return new PlayerStats();
            }

            return new PlayerStats {
                RealmId = ToInt(source.RealmId),
                RealmName = source.RealmName ?? string.Empty,
                Rank = ToInt(source.Rank),
                CountryRank = ToInt(source.CountryRank),
                TotalPP = source.TotalPP,
                TotalScore = ToLong(source.TotalScore),
                TotalRankedScore = ToLong(source.TotalRankedScore),
                TotalPlayedLeaderboards = ToInt(source.TotalPlayedLeaderboards),
                TotalPlayedRankedLeaderboards = ToInt(source.TotalPlayedRankedLeaderboards),
                TotalSubmittedPlays = ToInt(source.TotalSubmittedPlays),
                TotalReplayViews = ToInt(source.TotalReplayViews),
                AverageAccuracy = source.AverageAccuracy,
                WeightedAverageAccuracy = source.WeightedAverageAccuracy,
                CompletionAccuracy = source.CompletionAccuracy,
                Device = ToDomain(source.Device)
            };
        }

        private static PlayerDevice ToDomain(LeaderboardScoresResponseDataItemDevice source) {
            if (source == null) {
                return new PlayerDevice();
            }

            return new PlayerDevice {
                HMD = source.HMD ?? string.Empty,
                ControllerLeft = source.ControllerLeft ?? string.Empty,
                ControllerRight = source.ControllerRight ?? string.Empty
            };
        }

        private static PlayerDevice ToDomain(LeaderboardScoresResponsePlayerScoreDevice source) {
            if (source == null) {
                return new PlayerDevice();
            }

            return new PlayerDevice {
                HMD = source.HMD ?? string.Empty,
                ControllerLeft = source.ControllerLeft ?? string.Empty,
                ControllerRight = source.ControllerRight ?? string.Empty
            };
        }

        private static PlayerDevice ToDomain(PlayerListResponseDataItemStatsDevice source) {
            if (source == null) {
                return new PlayerDevice();
            }

            return new PlayerDevice {
                HMD = source.HMD ?? string.Empty,
                ControllerLeft = source.ControllerLeft ?? string.Empty,
                ControllerRight = source.ControllerRight ?? string.Empty
            };
        }

        private static PlayerDevice ToDomain(PlayerProfileResponseStatsDevice source) {
            if (source == null) {
                return new PlayerDevice();
            }

            return new PlayerDevice {
                HMD = source.HMD ?? string.Empty,
                ControllerLeft = source.ControllerLeft ?? string.Empty,
                ControllerRight = source.ControllerRight ?? string.Empty
            };
        }

        private static PlayerDevice ToDomain(PlayerBasicProfileResponseStatsDevice source) {
            if (source == null) {
                return new PlayerDevice();
            }

            return new PlayerDevice {
                HMD = source.HMD ?? string.Empty,
                ControllerLeft = source.ControllerLeft ?? string.Empty,
                ControllerRight = source.ControllerRight ?? string.Empty
            };
        }

        private static LeaderboardStatus ToDomain(LeaderboardResponseRealmLeaderboardStatus status) {
            switch (status) {
                case LeaderboardResponseRealmLeaderboardStatus.RANKED:
                    return LeaderboardStatus.Ranked;
                case LeaderboardResponseRealmLeaderboardStatus.QUALIFIED:
                    return LeaderboardStatus.Qualified;
                case LeaderboardResponseRealmLeaderboardStatus.LOVED:
                    return LeaderboardStatus.Loved;
                default:
                    return LeaderboardStatus.Unranked;
            }
        }

        private static ScoreSaberPlayOutcome ToDomain(LeaderboardScoresResponseDataItemPlayOutcome outcome) {
            switch (outcome) {
                case LeaderboardScoresResponseDataItemPlayOutcome.FAIL:
                    return ScoreSaberPlayOutcome.Fail;
                case LeaderboardScoresResponseDataItemPlayOutcome.QUIT:
                    return ScoreSaberPlayOutcome.Quit;
                case LeaderboardScoresResponseDataItemPlayOutcome.RESTART:
                    return ScoreSaberPlayOutcome.Restart;
                default:
                    return ScoreSaberPlayOutcome.Clear;
            }
        }

        private static ScoreSaberPlayOutcome ToDomain(LeaderboardScoresResponsePlayerScorePlayOutcome outcome) {
            switch (outcome) {
                case LeaderboardScoresResponsePlayerScorePlayOutcome.FAIL:
                    return ScoreSaberPlayOutcome.Fail;
                case LeaderboardScoresResponsePlayerScorePlayOutcome.QUIT:
                    return ScoreSaberPlayOutcome.Quit;
                case LeaderboardScoresResponsePlayerScorePlayOutcome.RESTART:
                    return ScoreSaberPlayOutcome.Restart;
                default:
                    return ScoreSaberPlayOutcome.Clear;
            }
        }

        private static int ToInt(double value) {
            return Convert.ToInt32(value);
        }

        private static long ToLong(string value) {
            long parsed;
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ? parsed : 0;
        }

        private static string ToPlayerName(string playerNameInGame, string name) {
            return !string.IsNullOrEmpty(playerNameInGame) ? playerNameInGame : name ?? string.Empty;
        }

        private static DateTimeOffset ParseOffset(string value) {
            DateTimeOffset parsed;
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed)
                ? parsed
                : default(DateTimeOffset);
        }

        private static DateTimeOffset? ParseNullableOffset(string value) {
            if (string.IsNullOrEmpty(value)) {
                return null;
            }

            DateTimeOffset parsed;
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed)
                ? parsed
                : (DateTimeOffset?)null;
        }

        private static DateTime ParseDate(string value) {
            DateTime parsed;
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsed)
                ? parsed
                : default(DateTime);
        }
    }
}
