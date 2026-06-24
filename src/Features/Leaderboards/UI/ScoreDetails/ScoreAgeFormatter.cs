using System;
using System.Collections.Generic;

namespace ScoreSaber.Features.Leaderboards.UI.ScoreDetails {
    internal static class ScoreAgeFormatter {
        internal static string FormatAgo(DateTime createdAt) {
            return ToNaturalTime(new TimeSpan(DateTime.UtcNow.Ticks - createdAt.Ticks), 2) + " ago";
        }

        private static string ToNaturalTime(TimeSpan period, int precisionParts) {
            var parts = new List<string>();

            if (TryReduceDays(ref period, 365, out double years)) {
                AddPart(parts, years, "year");
            }
            if (TryReduceDays(ref period, 30, out double months)) {
                AddPart(parts, months, "month");
            }
            if (TryReduceDays(ref period, 7, out double weeks)) {
                AddPart(parts, weeks, "week");
            }
            if (period.TotalDays >= 1) {
                AddPart(parts, period.Days, "day");
                period -= TimeSpan.FromDays(period.Days);
            }
            if (period.TotalHours >= 1 && period.Hours > 0) {
                AddPart(parts, period.Hours, "hour");
                period -= TimeSpan.FromHours(period.Hours);
            }
            if (period.TotalMinutes >= 1 && period.Minutes > 0) {
                AddPart(parts, period.Minutes, "minute");
                period -= TimeSpan.FromMinutes(period.Minutes);
            }
            if (period.TotalSeconds >= 1 && period.Seconds > 0) {
                AddPart(parts, period.Seconds, "second");
                period -= TimeSpan.FromSeconds(period.Seconds);
            } else if (period.TotalSeconds > 0) {
                AddPart(parts, Round(period.TotalSeconds, 3), "second");
            }

            return JoinParts(parts, precisionParts);
        }

        private static void AddPart(List<string> parts, double value, string unit) => parts.Add($"{value} {unit}{(value > 1 ? "s" : string.Empty)}");

        private static string JoinParts(List<string> parts, int precisionParts) {
            int count = Math.Min(parts.Count, precisionParts);
            if (count == 0) {
                return string.Empty;
            }
            if (count == 1) {
                return parts[0];
            }

            string prefix = count == 2 ? parts[0] : string.Join(", ", parts.GetRange(0, count - 1));
            return $"{prefix} and {parts[count - 1]}";
        }

        private static bool TryReduceDays(ref TimeSpan period, int days, out double result) {
            if (period.TotalDays < days) {
                result = 0;
                return false;
            }

            result = (int)Math.Floor(period.TotalDays / days);
            period -= TimeSpan.FromDays(days * result);
            return true;
        }

        private static double Round(double value, int digits) {
            if (double.IsNaN(value)) {
                return double.NaN;
            }

            return (double)Math.Round((decimal)value, digits, MidpointRounding.AwayFromZero);
        }
    }
}
