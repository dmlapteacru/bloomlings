using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Rounds
{
    /// <summary>The round level validator (spec 006 FR-023, data model RoundLevel validation).</summary>
    public class RoundLevelValidatorTests
    {
        private static IEnumerable<int> Placed() => Enumerable.Range(1, 20);

        [TestCaseSource(nameof(Placed))]
        public void EveryPlacedLevelIsValid(int number)
        {
            RoundLevel level = RoundTestLevels.Content(number);
            Assert.That(RoundLevelValidator.Validate(level), Is.Empty);
        }

        // A valid 7 × 8 level: a frame, G and P cells, two columns, a link pair and a hidden pod.
        private static RoundLevel Make(
            string[]? rows = null,
            RoundPod[]? pods = null,
            int[][]? tray = null,
            RoundBridgeChoice[]? bridge = null,
            int capacity = 5,
            int number = 14,
            RoundClass cls = RoundClass.Normal,
            RoundIntroduction? intro = null,
            string[]? mechanics = null,
            RoundException? exception = null)
        {
            rows ??= new[]
            {
                "###.###",
                "#.....#",
                "#GGGGG#",
                "#PPPPP#",
                "#GGGGG#",
                "#.....#",
                "#.....#",
                "#######",
            };
            pods ??= new[]
            {
                new RoundPod('G', 4), new RoundPod('P', 2, group: "A"), new RoundPod('G', 6, group: "A"), new RoundPod('P', 3, RoundMystery.Depth),
            };
            tray ??= new[] { new[] { 0, 1 }, new[] { 3, 2 } };
            mechanics ??= new[] { "hidden", "links" };
            return new RoundLevel(number, rows[0].Length, rows.Length, rows, pods, tray.Select(c => (IReadOnlyList<int>)c).ToArray(), new[] { 3 }, bridge, capacity,
                cls, introduction: intro, mechanics: mechanics, exception: exception);
        }

        [Test]
        public void TheSampleIsValid()
        {
            Assert.That(RoundLevelValidator.Validate(Make()), Is.Empty);
        }

        [Test]
        public void TheReferenceChecksAreReported()
        {
            Assert.That(RoundLevelValidator.Validate(Make(rows: new[] { "###.##", "#....#", "#GGGG#", "#....#", "#....#", "#....#", "#....#", "######" },
                pods: new[] { new RoundPod('G', 4) }, tray: new[] { new[] { 0 }, new int[0] }, mechanics: new string[0])), Does.Contain(RoundLevelValidator.BoardSize));
            string[] badRow = Make().RowsBottomFirst.ToArray();
            badRow[3] = "#GGxGG#";
            Assert.That(RoundLevelValidator.Validate(Make(rows: badRow.Reverse().ToArray())), Does.Contain(RoundLevelValidator.Rows));
            Assert.That(RoundLevelValidator.Validate(Make(tray: new[] { new[] { 0, 1 }, new[] { 3 } })), Does.Contain(RoundLevelValidator.TrayPartition));
            Assert.That(RoundLevelValidator.Validate(Make(tray: new[] { new[] { 0, 1 }, new[] { 3, 2, 1 } })), Does.Contain(RoundLevelValidator.TrayPartition));
            Assert.That(RoundLevelValidator.Validate(Make(tray: new[] { new[] { 0, 1, 3, 2 } })), Does.Contain(RoundLevelValidator.Columns));
            Assert.That(RoundLevelValidator.Validate(Make(capacity: 4)), Does.Contain(RoundLevelValidator.CapacityFive));
            Assert.That(RoundLevelValidator.Validate(Make(pods: new[]
            {
                new RoundPod('G', 4), new RoundPod('P', 2, group: "A"), new RoundPod('G', 6, group: "A"), new RoundPod('P', 2, RoundMystery.Depth),
            })), Does.Contain(RoundLevelValidator.Accounting));
            Assert.That(RoundLevelValidator.Validate(Make(pods: new[]
            {
                new RoundPod('G', 4), new RoundPod('P', 2, group: "A"), new RoundPod('G', 6, group: "A"), new RoundPod('P', 61, RoundMystery.Depth),
            })), Does.Contain(RoundLevelValidator.PodCount));
        }

        [Test]
        public void LinksArePairsInDifferentColumnsAtEqualDepth()
        {
            Assert.That(RoundLevelValidator.Validate(Make(tray: new[] { new[] { 0, 1, 2 }, new[] { 3 } })), Does.Contain(RoundLevelValidator.Groups), "same column");
            Assert.That(RoundLevelValidator.Validate(Make(tray: new[] { new[] { 1, 0 }, new[] { 3, 2 } })), Does.Contain(RoundLevelValidator.Groups), "different depths");
            Assert.That(RoundLevelValidator.Validate(Make(pods: new[]
            {
                new RoundPod('G', 4, group: "A"), new RoundPod('P', 2, group: "A"), new RoundPod('G', 6, group: "A"), new RoundPod('P', 3, RoundMystery.Depth),
            })), Does.Contain(RoundLevelValidator.Groups), "three members");
        }

        [Test]
        public void HiddenPodsAreDepthModeAtMostSixAndNeverLinked()
        {
            Assert.That(RoundLevelValidator.Validate(Make(pods: new[]
            {
                new RoundPod('G', 4), new RoundPod('P', 2, RoundMystery.Depth, "A"), new RoundPod('G', 6, group: "A"), new RoundPod('P', 3),
            })), Does.Contain(RoundLevelValidator.Hidden));
            Assert.That(RoundLevelValidator.Validate(Make(pods: new[]
            {
                new RoundPod('G', 4), new RoundPod('P', 2, group: "A"), new RoundPod('G', 6, group: "A"), new RoundPod('P', 3, RoundMystery.Sealed),
            })), Does.Contain(RoundLevelValidator.HiddenMode));
        }

        [Test]
        public void BridgeSitesAreWallsWithAnOrthogonalNeighbourAsAnchor()
        {
            string[] rows = { "###.###", "#.....#", "#GGGGG#", "#PPPPP#", "#GGGGG#", "#######", "#.....#", "#######" };
            int w = 7;
            int stream = 5 * w + 3; // rows are bottom first: the stream is row 5
            var good = new[] { new RoundBridgeChoice(stream, stream - w, "left"), new RoundBridgeChoice(stream + 1, stream + 1 - w, "right") };
            string[] withBridge = { "hidden", "links", "bridge" };
            Assert.That(RoundLevelValidator.ValidateDefinition(Make(rows: rows, bridge: good, mechanics: withBridge)), Is.Empty);

            // 17 §8 as amended on 2026-10-28: the anchor may be above or beside the site too.
            var above = new[] { new RoundBridgeChoice(stream, stream + w, "down"), good[1] };
            Assert.That(RoundLevelValidator.ValidateDefinition(Make(rows: rows, bridge: above, mechanics: withBridge)), Is.Empty);
            var beside = new[] { new RoundBridgeChoice(stream, stream - 1, "right"), new RoundBridgeChoice(stream + 1, stream + 2, "left") };
            Assert.That(RoundLevelValidator.ValidateDefinition(Make(rows: rows, bridge: beside, mechanics: withBridge)), Is.Empty);

            var diagonal = new[] { new RoundBridgeChoice(stream, stream - w - 1, "diagonal"), good[1] };
            Assert.That(RoundLevelValidator.ValidateDefinition(Make(rows: rows, bridge: diagonal, mechanics: withBridge)), Does.Contain(RoundLevelValidator.BridgeSites));
            var distant = new[] { new RoundBridgeChoice(stream, stream - (2 * w), "two rows"), good[1] };
            Assert.That(RoundLevelValidator.ValidateDefinition(Make(rows: rows, bridge: distant, mechanics: withBridge)), Does.Contain(RoundLevelValidator.BridgeSites));
            var twoAside = new[] { new RoundBridgeChoice(stream, stream + 2, "two aside"), good[1] };
            Assert.That(RoundLevelValidator.ValidateDefinition(Make(rows: rows, bridge: twoAside, mechanics: withBridge)), Does.Contain(RoundLevelValidator.BridgeSites));
            var notWall = new[] { new RoundBridgeChoice(stream - w, stream - 2 * w, "open"), good[1] };
            Assert.That(RoundLevelValidator.ValidateDefinition(Make(rows: rows, bridge: notWall, mechanics: withBridge)), Does.Contain(RoundLevelValidator.BridgeSites));
        }

        [Test]
        public void ASideAnchor_MustBeInTheSameRow()
        {
            // The last cell of a row and the first of the next differ by one, but they are not neighbours.
            Assert.Multiple(() =>
            {
                Assert.That(RoundLevelValidator.Orthogonal(13, 12, 7), Is.True, "beside, same row");
                Assert.That(RoundLevelValidator.Orthogonal(13, 14, 7), Is.False, "the end of row 1 and the start of row 2");
                Assert.That(RoundLevelValidator.Orthogonal(14, 13, 7), Is.False);
                Assert.That(RoundLevelValidator.Orthogonal(10, 3, 7), Is.True, "below");
                Assert.That(RoundLevelValidator.Orthogonal(10, 17, 7), Is.True, "above");
                Assert.That(RoundLevelValidator.Orthogonal(10, 10, 7), Is.False, "the site itself");
            });
        }

        [Test]
        public void MechanicsMustMatchThePodsAndTheHero()
        {
            Assert.That(RoundLevelValidator.Validate(Make(mechanics: new[] { "links" })), Does.Contain(RoundLevelValidator.Mechanics));
            Assert.That(RoundLevelValidator.Validate(Make(mechanics: new[] { "hidden", "links", "latch" })), Does.Contain(RoundLevelValidator.UnknownMechanic));
        }

        [Test]
        public void TheCalendarAndTheIntroductionsAreChecked()
        {
            Assert.That(RoundLevelValidator.ClassOf(15), Is.EqualTo(RoundClass.SuperHard));
            Assert.That(RoundLevelValidator.ClassOf(10), Is.EqualTo(RoundClass.Hard));
            Assert.That(RoundLevelValidator.ClassOf(11), Is.EqualTo(RoundClass.Normal));
            Assert.That(RoundLevelValidator.Validate(Make(cls: RoundClass.Hard)), Does.Contain(RoundLevelValidator.Calendar));
            // L12 may not use links yet (introduced at 11 and 13); L14 may.
            Assert.That(RoundLevelValidator.Validate(Make(number: 12)), Does.Contain(RoundLevelValidator.Introductions));
            Assert.That(RoundLevelValidator.Validate(Make(number: 14)), Is.Empty);
            // L13 is the second meeting of links: only links, marked second.
            RoundPod[] linksOnly = { new RoundPod('G', 4), new RoundPod('P', 2, group: "A"), new RoundPod('G', 6, group: "A"), new RoundPod('P', 3) };
            Assert.That(RoundLevelValidator.Validate(Make(number: 13, pods: linksOnly, mechanics: new[] { "links" }, intro: new RoundIntroduction("links", true))), Is.Empty);
            Assert.That(RoundLevelValidator.Validate(Make(number: 13, pods: linksOnly, mechanics: new[] { "links" }, intro: new RoundIntroduction("links", false))),
                Does.Contain(RoundLevelValidator.Introductions));
        }

        [Test]
        public void AHiddenIntroductionKeepsHiddenPodsOutOfTheFirstRow()
        {
            RoundPod[] pods = { new RoundPod('G', 4), new RoundPod('P', 2), new RoundPod('G', 6), new RoundPod('P', 3, RoundMystery.Depth) };
            var intro = new RoundIntroduction("hidden", false);
            Assert.That(RoundLevelValidator.Validate(Make(number: 6, pods: pods, tray: new[] { new[] { 0, 3 }, new[] { 1, 2 } }, mechanics: new[] { "hidden" }, intro: intro)), Is.Empty);
            Assert.That(RoundLevelValidator.Validate(Make(number: 6, pods: pods, tray: new[] { new[] { 3, 0 }, new[] { 1, 2 } }, mechanics: new[] { "hidden" }, intro: intro)),
                Does.Contain(RoundLevelValidator.HiddenFirstRow));
        }

        [Test]
        public void AnExceptionWaivesOnlyTheRuleItNames()
        {
            var calendar = new RoundException(RoundLevelValidator.Calendar, "owner's choice", "owner", "2026-10-27");
            Assert.That(RoundLevelValidator.Validate(Make(number: 14, cls: RoundClass.Hard, exception: calendar)), Is.Empty);
            var other = new RoundException(RoundLevelValidator.BridgeDecides, "both bridges win", "owner", "2026-10-27");
            Assert.That(RoundLevelValidator.Validate(Make(number: 14, cls: RoundClass.Hard, exception: other)), Is.EqualTo(new[] { RoundLevelValidator.Calendar }));
            Assert.That(RoundLevelValidator.IsExcepted(Make(exception: other), RoundLevelValidator.BridgeDecides), Is.True);
            var structural = new RoundException(RoundLevelValidator.Accounting, "no", "owner", "2026-10-27");
            Assert.That(RoundLevelValidator.Validate(Make(number: 14, exception: structural)), Is.EqualTo(new[] { RoundLevelValidator.UnknownException }));
            Assert.That(RoundLevelValidator.IsExcepted(RoundTestLevels.Content(18), RoundLevelValidator.BridgeDecides), Is.True, "L18 keeps both winning bridges");
        }

        [Test]
        public void TheCalmRule_IsExceptable_AndL30KeepsTheOwnersException()
        {
            var calm = new RoundException(RoundLevelValidator.CalmTest, "kept as it is", "owner", "2026-10-31");
            Assert.That(RoundLevelValidator.Validate(Make(number: 14, exception: calm)), Is.Empty);
            RoundLevel l30 = RoundTestLevels.Content(30);
            Assert.That(RoundLevelValidator.IsExcepted(l30, RoundLevelValidator.CalmTest), Is.True, "L30: Super Hard calm above 25 %, kept by the owner");
            Assert.That(RoundLevelValidator.Validate(l30), Is.Empty);
        }
    }
}
