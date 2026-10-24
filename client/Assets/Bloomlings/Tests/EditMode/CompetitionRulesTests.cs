using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Backend;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    public sealed class CompetitionRulesTests
    {
        private static readonly DateTimeOffset Monday = new DateTimeOffset(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);
        private static CompetitionWeek Week => CompetitionRules.WeekContaining(Monday);

        private static CompetitionValidatedWin Win(string player, int day, int assists = 0) =>
            new CompetitionValidatedWin(player, CompetitionRules.ProposalVersion, Monday.AddDays(day), assists);

        [Test]
        public void Proposal_IsVersionedAndDisabled()
        {
            Assert.That(CompetitionRules.EnabledByDefault, Is.False);
            Assert.That(CompetitionRules.ProposalVersion, Is.EqualTo("weekly-daily-v0-draft"));
        }

        [TestCase(0, 150)]
        [TestCase(1, 125)]
        [TestCase(2, 100)]
        public void VerifiedWin_UsesOnlyTheEqualFreeAssistanceBudget(int assists, int expected)
        {
            Assert.That(CompetitionRules.ScoreValidatedWin(CompetitionRules.ProposalVersion, assists), Is.EqualTo(expected));
        }

        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(int.MaxValue)]
        public void InvalidAssistanceCount_IsRejected(int assists)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Win("a", 0, assists));
        }

        [Test]
        public void UnknownRulesVersion_CannotBeCounted()
        {
            Assert.Throws<ArgumentException>(() => new CompetitionValidatedWin("a", "future-v1", Monday, 0));
        }

        [Test]
        public void Week_UsesUtcMondayAndAnExclusiveEnd()
        {
            Assert.That(Week.Id, Is.EqualTo("2026-10-19"));
            Assert.That(Week.StartUtc, Is.EqualTo(Monday));
            Assert.That(Week.EndUtc, Is.EqualTo(Monday.AddDays(7)));
            Assert.That(Week.Contains(Monday.AddTicks(-1)), Is.False);
            Assert.That(Week.Contains(Monday), Is.True);
            Assert.That(Week.Contains(Monday.AddDays(7).AddTicks(-1)), Is.True);
            Assert.That(Week.Contains(Monday.AddDays(7)), Is.False);
            Assert.That(CompetitionRules.WeekContaining(Monday.AddDays(7)).StartUtc, Is.EqualTo(Monday.AddDays(7)));
        }

        [Test]
        public void LocalOffsetAndYearBoundary_DoNotChangeUtcWeek()
        {
            var localMonday = new DateTimeOffset(2026, 10, 26, 1, 0, 0, TimeSpan.FromHours(3));
            Assert.That(CompetitionRules.WeekContaining(localMonday).StartUtc, Is.EqualTo(Monday));
            var january = new DateTimeOffset(2027, 1, 1, 23, 0, 0, TimeSpan.Zero);
            Assert.That(CompetitionRules.WeekContaining(january).Id, Is.EqualTo("2026-12-28"));
        }

        [Test]
        public void PuzzleDate_MustBeUnambiguousMidnightUtc()
        {
            Assert.Throws<ArgumentException>(() => new CompetitionValidatedWin("a", CompetitionRules.ProposalVersion, Monday.AddHours(1), 0));
            Assert.Throws<ArgumentException>(() => new CompetitionValidatedWin("a", CompetitionRules.ProposalVersion, Monday.ToOffset(TimeSpan.FromHours(3)), 0));
            Assert.Throws<ArgumentException>(() => Win(" ", 0));
        }

        [Test]
        public void RepeatedReceiptsAndRetries_KeepOnlyTheBestResultForEachDate()
        {
            var results = new[] { Win("a", 0, 2), Win("a", 0), Win("a", 0), Win("a", 1, 1) };
            CompetitionWeeklyTotal total = CompetitionRules.TotalValidatedWins("a", Week, results);
            Assert.That(total.Points, Is.EqualTo(275));
            Assert.That(total.CountedDays, Is.EqualTo(2));
            Array.Reverse(results);
            Assert.That(CompetitionRules.TotalValidatedWins("a", Week, results).Points, Is.EqualTo(275));
        }

        [Test]
        public void SevenDailyWins_CountOnlyFiveBestDays()
        {
            var results = new List<CompetitionValidatedWin> { Win("a", 0, 2), Win("a", 1, 1) };
            for (int day = 2; day < 7; day++) results.Add(Win("a", day));
            CompetitionWeeklyTotal total = CompetitionRules.TotalValidatedWins("a", Week, results);
            Assert.That(total.Points, Is.EqualTo(750));
            Assert.That(total.Points, Is.EqualTo(CompetitionRules.MaximumWeeklyPoints));
            Assert.That(total.CountedDays, Is.EqualTo(5));
        }

        [Test]
        public void ForeignOwnerOrPuzzleWeek_FailsClosed()
        {
            Assert.Throws<ArgumentException>(() => CompetitionRules.TotalValidatedWins("a", Week, new[] { Win("b", 0) }));
            Assert.Throws<ArgumentException>(() => CompetitionRules.TotalValidatedWins("a", Week, new[] { Win("a", -1) }));
            Assert.Throws<ArgumentException>(() => CompetitionRules.TotalValidatedWins("a", Week, new[] { Win("a", 7) }));
        }

        [Test]
        public void Ties_ShareRankAndSkipTheFollowingRank_WithoutInventingCompetitors()
        {
            var a = CompetitionRules.TotalValidatedWins("a", Week, new[] { Win("a", 0) });
            var b = CompetitionRules.TotalValidatedWins("b", Week, new[] { Win("b", 1) });
            var c = CompetitionRules.TotalValidatedWins("c", Week, new[] { Win("c", 2, 1) });
            var pendingOnly = CompetitionRules.TotalValidatedWins("pending", Week, Array.Empty<CompetitionValidatedWin>());
            var ranked = CompetitionRules.RankValidatedTotals(Week, new[] { c, b, pendingOnly, a });
            Assert.That(ranked.Count, Is.EqualTo(3));
            Assert.That(ranked[0].Rank, Is.EqualTo(1));
            Assert.That(ranked[1].Rank, Is.EqualTo(1));
            Assert.That(ranked[2].Rank, Is.EqualTo(3));
            Assert.That(ranked[0].Total.PlayerId, Is.EqualTo("a"));
            Assert.That(ranked[1].Total.PlayerId, Is.EqualTo("b"));
        }

        [Test]
        public void Ranking_RejectsDuplicateAccountsAndDifferentWeeks()
        {
            var total = CompetitionRules.TotalValidatedWins("a", Week, new[] { Win("a", 0) });
            Assert.Throws<ArgumentException>(() => CompetitionRules.RankValidatedTotals(Week, new[] { total, total }));
            Assert.Throws<ArgumentException>(() => CompetitionRules.RankValidatedTotals(CompetitionRules.WeekContaining(Monday.AddDays(7)), new[] { total }));
        }

        [Test]
        public void EmptyVerifiedPopulation_HasNoRowsAndNoPlayerRank()
        {
            var empty = CompetitionRules.TotalValidatedWins("a", Week, Array.Empty<CompetitionValidatedWin>());
            Assert.That(empty.Points, Is.Zero);
            Assert.That(empty.CountedDays, Is.Zero);
            Assert.That(CompetitionRules.RankValidatedTotals(Week, new[] { empty }), Is.Empty);
        }
    }
}
