using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bloomlings.Client.Services.Backend
{
    /// <summary>
    /// Draft arithmetic only; not enabled, wired to gameplay, or a proof verifier. Callers must obtain wins from
    /// a trusted replay verifier before using these methods for an authoritative leaderboard. Constructing these
    /// objects locally proves nothing. See product/plans/leaderboard-strategy-2026-10-23.md.
    /// </summary>
    public static class CompetitionRules
    {
        public const string ProposalVersion = "weekly-daily-v0-draft";
        public const bool EnabledByDefault = false;
        public const int DaysCounted = 5;
        public const int FreeAssistanceCharges = 2;
        public const int WinPoints = 100;
        public const int UnusedChargePoints = 25;
        public const int MaximumDailyPoints = WinPoints + FreeAssistanceCharges * UnusedChargePoints;
        public const int MaximumWeeklyPoints = DaysCounted * MaximumDailyPoints;

        /// <summary>Only a winning replay already checked against the pinned rules may reach this function.</summary>
        public static int ScoreValidatedWin(string rulesVersion, int assistanceUsed)
        {
            RequireVersion(rulesVersion);
            if (assistanceUsed < 0 || assistanceUsed > FreeAssistanceCharges)
                throw new ArgumentOutOfRangeException(nameof(assistanceUsed));
            return WinPoints + (FreeAssistanceCharges - assistanceUsed) * UnusedChargePoints;
        }

        /// <summary>Monday 00:00 UTC inclusive through the following Monday exclusive; device offset is irrelevant.</summary>
        public static CompetitionWeek WeekContaining(DateTimeOffset instant)
        {
            DateTime utcDay = instant.UtcDateTime.Date;
            int daysSinceMonday = ((int)utcDay.DayOfWeek + 6) % 7;
            var start = new DateTimeOffset(utcDay.AddDays(-daysSinceMonday), TimeSpan.Zero);
            return new CompetitionWeek(start, start.AddDays(7));
        }

        /// <summary>
        /// Best result per puzzle date, then the five best dates. Pending/rejected/local estimates must never be
        /// supplied. A wrong owner, week, or version fails closed instead of silently mixing scopes.
        /// </summary>
        public static CompetitionWeeklyTotal TotalValidatedWins(string playerId, CompetitionWeek week,
            IEnumerable<CompetitionValidatedWin> wins)
        {
            RequirePlayer(playerId);
            if (week == null) throw new ArgumentNullException(nameof(week));
            if (wins == null) throw new ArgumentNullException(nameof(wins));
            var byDay = new Dictionary<DateTimeOffset, int>();
            foreach (CompetitionValidatedWin win in wins)
            {
                if (win == null) throw new ArgumentException("A verified win is required.", nameof(wins));
                if (!string.Equals(playerId, win.PlayerId, StringComparison.Ordinal))
                    throw new ArgumentException("Wins must belong to the authenticated owner.", nameof(wins));
                if (!week.Contains(win.PuzzleDayUtc))
                    throw new ArgumentException("The puzzle date belongs to another week.", nameof(wins));
                int points = ScoreValidatedWin(win.RulesVersion, win.AssistanceUsed);
                if (!byDay.TryGetValue(win.PuzzleDayUtc, out int previous) || points > previous)
                    byDay[win.PuzzleDayUtc] = points;
            }

            var dailyPoints = new List<int>(byDay.Values);
            dailyPoints.Sort((a, b) => b.CompareTo(a));
            int count = Math.Min(DaysCounted, dailyPoints.Count);
            int total = 0;
            for (int i = 0; i < count; i++) total += dailyPoints[i];
            return new CompetitionWeeklyTotal(playerId, week, total, count);
        }

        /// <summary>
        /// Shared competition ranks (1, 1, 3). Player IDs only stabilize row order, never break a scoring tie.
        /// Requires the full verified population; do not use this to infer global ranks from a page of neighbours.
        /// Players without a verified win are unranked and omitted.
        /// </summary>
        public static IReadOnlyList<CompetitionRankedEntry> RankValidatedTotals(CompetitionWeek week,
            IEnumerable<CompetitionWeeklyTotal> totals)
        {
            if (week == null) throw new ArgumentNullException(nameof(week));
            if (totals == null) throw new ArgumentNullException(nameof(totals));
            var players = new HashSet<string>(StringComparer.Ordinal);
            var sorted = new List<CompetitionWeeklyTotal>();
            foreach (CompetitionWeeklyTotal total in totals)
            {
                if (total == null) throw new ArgumentException("A verified total is required.", nameof(totals));
                if (total.Week.StartUtc != week.StartUtc)
                    throw new ArgumentException("Totals must belong to one week.", nameof(totals));
                if (!players.Add(total.PlayerId))
                    throw new ArgumentException("A player may appear only once.", nameof(totals));
                if (total.CountedDays > 0) sorted.Add(total);
            }
            sorted.Sort((a, b) =>
            {
                int byPoints = b.Points.CompareTo(a.Points);
                return byPoints != 0 ? byPoints : StringComparer.Ordinal.Compare(a.PlayerId, b.PlayerId);
            });
            var result = new List<CompetitionRankedEntry>(sorted.Count);
            int rank = 0;
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i == 0 || sorted[i].Points != sorted[i - 1].Points) rank = i + 1;
                result.Add(new CompetitionRankedEntry(rank, sorted[i]));
            }
            return result.AsReadOnly();
        }

        internal static void RequireVersion(string rulesVersion)
        {
            if (!string.Equals(rulesVersion, ProposalVersion, StringComparison.Ordinal))
                throw new ArgumentException("Unsupported competition rules version.", nameof(rulesVersion));
        }

        internal static void RequirePlayer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                throw new ArgumentException("An authenticated player ID is required.", nameof(playerId));
        }
    }

    public sealed class CompetitionWeek
    {
        internal CompetitionWeek(DateTimeOffset startUtc, DateTimeOffset endUtc)
        {
            StartUtc = startUtc;
            EndUtc = endUtc;
        }

        public DateTimeOffset StartUtc { get; }
        public DateTimeOffset EndUtc { get; }
        public string Id => StartUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public bool Contains(DateTimeOffset instant) => instant >= StartUtc && instant < EndUtc;
    }

    /// <summary>
    /// Trusted-verifier output contract, NOT a receipt or a security boundary. The future verifier must establish
    /// owner, canonical puzzle/content/rules, allowed commands, win, day eligibility and receipt idempotency first.
    /// Do not deserialize client claims into this type and treat them as verified.
    /// </summary>
    public sealed class CompetitionValidatedWin
    {
        public CompetitionValidatedWin(string playerId, string rulesVersion, DateTimeOffset puzzleDayUtc, int assistanceUsed)
        {
            CompetitionRules.RequirePlayer(playerId);
            CompetitionRules.ScoreValidatedWin(rulesVersion, assistanceUsed);
            if (puzzleDayUtc.Offset != TimeSpan.Zero || puzzleDayUtc.TimeOfDay != TimeSpan.Zero)
                throw new ArgumentException("Puzzle date must be midnight UTC.", nameof(puzzleDayUtc));
            PlayerId = playerId;
            RulesVersion = rulesVersion;
            PuzzleDayUtc = puzzleDayUtc;
            AssistanceUsed = assistanceUsed;
        }

        public string PlayerId { get; }
        public string RulesVersion { get; }
        public DateTimeOffset PuzzleDayUtc { get; }
        public int AssistanceUsed { get; }
    }

    public sealed class CompetitionWeeklyTotal
    {
        internal CompetitionWeeklyTotal(string playerId, CompetitionWeek week, int points, int countedDays)
        {
            PlayerId = playerId;
            Week = week;
            Points = points;
            CountedDays = countedDays;
        }

        public string PlayerId { get; }
        public CompetitionWeek Week { get; }
        public string RulesVersion => CompetitionRules.ProposalVersion;
        public int Points { get; }
        public int CountedDays { get; }
    }

    public sealed class CompetitionRankedEntry
    {
        internal CompetitionRankedEntry(int rank, CompetitionWeeklyTotal total) { Rank = rank; Total = total; }
        public int Rank { get; }
        public CompetitionWeeklyTotal Total { get; }
    }
}
