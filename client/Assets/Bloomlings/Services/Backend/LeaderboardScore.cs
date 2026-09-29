using System;

namespace Bloomlings.Client.Services.Backend
{
    /// <summary>
    /// The leaderboard score (contracts/backend-services.md, FR-062, T136):
    /// <c>level × 10_000_000 + (9_999_999 − minutesSince(2026-01-01T00:00Z))</c>. UGS Leaderboards break ties by player
    /// id, so the completion time is folded into the score: for the same level, the earlier completion scores higher.
    /// The value is an integer below 2^53 up to about level 900 million, so it is exact in a double. Two completions in
    /// the same minute tie (accepted by the contract).
    /// </summary>
    public static class LeaderboardScore
    {
        public const long LevelFactor = 10_000_000;
        public const long MaxMinutes = 9_999_999;

        public static readonly DateTime Epoch = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>The score as the exact integer; <see cref="Encode"/> is the same value as a double.</summary>
        public static long EncodeExact(int level, DateTime completedUtc)
        {
            if (level < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "Levels are never negative.");
            }

            return (level * LevelFactor) + (MaxMinutes - MinutesSinceEpoch(completedUtc));
        }

        public static double Encode(int level, DateTime completedUtc) => EncodeExact(level, completedUtc);

        /// <summary>The highest completed level a score encodes.</summary>
        public static int LevelOf(double score) => (int)((long)score / LevelFactor);

        /// <summary>The completion minute a score encodes.</summary>
        public static DateTime CompletedAt(double score) => Epoch.AddMinutes(MaxMinutes - ((long)score % LevelFactor));

        /// <summary>Whole minutes since the epoch, clamped to 0..9_999_999 (about 19 years).</summary>
        public static long MinutesSinceEpoch(DateTime utc)
        {
            // An unspecified kind is taken as UTC, never as device-local time, so every device encodes the same score.
            DateTime time = utc.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(utc, DateTimeKind.Utc) : utc.ToUniversalTime();
            long minutes = (time.Ticks - Epoch.Ticks) / TimeSpan.TicksPerMinute;
            return Math.Min(MaxMinutes, Math.Max(0L, minutes));
        }
    }
}
