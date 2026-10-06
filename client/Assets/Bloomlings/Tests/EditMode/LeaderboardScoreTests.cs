using System;
using Bloomlings.Client.Services.Backend;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The leaderboard score <c>level × 10_000_000 + (9_999_999 − minutesSince(2026-01-01T00:00Z))</c>
    /// (contracts/backend-services.md, FR-062; T136).
    /// </summary>
    public class LeaderboardScoreTests
    {
        private static readonly DateTime Early = new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc);

        [Test]
        public void SameLevel_EarlierCompletionRanksHigher()
        {
            double earlier = LeaderboardScore.Encode(120, Early);
            double later = LeaderboardScore.Encode(120, Early.AddMinutes(1));

            Assert.That(earlier, Is.GreaterThan(later));
            Assert.That(LeaderboardScore.Encode(121, Early.AddYears(10)), Is.GreaterThan(earlier), "a higher level always ranks higher");
            Assert.That(LeaderboardScore.Encode(120, Early.AddSeconds(59)), Is.EqualTo(earlier), "the same minute ties (accepted)");
        }

        [Test]
        public void Encoding_MatchesTheContractFormula()
        {
            long minutes = (long)(Early - LeaderboardScore.Epoch).TotalMinutes;
            Assert.That(LeaderboardScore.EncodeExact(120, Early), Is.EqualTo((120L * 10_000_000L) + (9_999_999L - minutes)));
            Assert.That(LeaderboardScore.EncodeExact(1, LeaderboardScore.Epoch), Is.EqualTo(19_999_999L));
            Assert.That(LeaderboardScore.EncodeExact(1, LeaderboardScore.Epoch.AddDays(-3)), Is.EqualTo(19_999_999L), "times before the epoch clamp to 0 minutes");
            Assert.That(LeaderboardScore.LevelOf(LeaderboardScore.Encode(5000, Early)), Is.EqualTo(5000));
            Assert.That(LeaderboardScore.CompletedAt(LeaderboardScore.Encode(5000, Early)), Is.EqualTo(Early));
        }

        [TestCase(1)]
        [TestCase(5000)]
        [TestCase(1_000_000)]
        [TestCase(900_000_000)]
        public void Value_IsExactInADouble(int level)
        {
            DateTime at = LeaderboardScore.Epoch.AddMinutes(1_234_567);
            long exact = LeaderboardScore.EncodeExact(level, at);
            double score = LeaderboardScore.Encode(level, at);

            Assert.That(exact, Is.LessThan(1L << 53));
            Assert.That((long)score, Is.EqualTo(exact));
            Assert.That(score, Is.EqualTo((double)exact));
            Assert.That(LeaderboardScore.Encode(level, at.AddMinutes(1)), Is.LessThan(score), "one minute is still visible in the double");
        }

        [Test]
        public void UnspecifiedKind_IsReadAsUtc()
        {
            var unspecified = new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Unspecified);
            Assert.That(LeaderboardScore.Encode(7, unspecified), Is.EqualTo(LeaderboardScore.Encode(7, Early)));
        }

        [Test]
        public void AnEntry_KnowsWhenItsLevelWasReached()
        {
            var reached = new DateTime(2026, 5, 4, 10, 15, 0, DateTimeKind.Utc);
            var entry = new Bloomlings.Client.Services.Backend.LeaderboardEntry(3, "Ada", 120, false, LeaderboardScore.Encode(120, reached));

            Assert.That(entry.ReachedAtUtc, Is.EqualTo(reached));
            Assert.That(new Bloomlings.Client.Services.Backend.LeaderboardEntry(3, "Ada", 120, false).ReachedAtUtc, Is.Null, "unknown without a score");
        }
    }
}
