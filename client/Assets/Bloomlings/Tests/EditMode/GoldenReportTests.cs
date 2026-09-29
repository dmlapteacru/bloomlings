using System.Collections.Generic;
using Bloomlings.Client.Tests.Device;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The device determinism report (T151): the corpus digest and the CI result line.</summary>
    public class GoldenReportTests
    {
        private static readonly GoldenResult A = new GoldenResult("a.golden.json", true, "h1", "d1", "won", null);
        private static readonly GoldenResult B = new GoldenResult("b.golden.json", true, "h2", "d2", "jammed", null);

        [Test]
        public void CorpusDigest_IsOrderIndependent_AndSeesEveryHash()
        {
            string digest = GoldenReport.CorpusDigest(new[] { A, B });

            Assert.That(GoldenReport.CorpusDigest(new[] { B, A }), Is.EqualTo(digest));
            Assert.That(GoldenReport.CorpusDigest(new[] { A, B with { StateHash = "h3" } }), Is.Not.EqualTo(digest));
        }

        [Test]
        public void SummaryLine_PassesOnlyWhenEveryCasePasses()
        {
            Assert.That(GoldenReport.SummaryLine(new[] { A, B }, "Android", "IL2CPP"), Does.StartWith("[GoldenReplay] RESULT PASS 2/2 corpus="));
            Assert.That(GoldenReport.SummaryLine(new[] { A, B with { Passed = false } }, "Android", "IL2CPP"), Does.StartWith("[GoldenReplay] RESULT FAIL 1/2"));
            Assert.That(GoldenReport.SummaryLine(new List<GoldenResult>(), "IPhonePlayer", "IL2CPP"), Does.StartWith("[GoldenReplay] RESULT FAIL 0/0"));
        }
    }
}
