using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Json;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Bloomlings.Client.Tests.EditMode
{
    /// <summary>
    /// The guided spotlights of the onboarding (spec 005 FR-034, FR-035; the owner, 2026-10-05): which steps the launch
    /// levels show, that a booster's demo is forced and free, where the spotlight's pieces go, and the entry arch.
    /// </summary>
    public sealed class GuideTourTests
    {
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        private static readonly Dictionary<string, BasePicture> Pictures = Directory
            .GetFiles(Path.Combine(Root, "content", "pictures", "lib"), "*.json")
            .Select(f => BasePictureJson.Read(File.ReadAllText(f)))
            .ToDictionary(p => p.Id);

        private static LevelSession Level(int n)
        {
            LevelDefinition definition = DefinitionJson.Read(File.ReadAllText(Path.Combine(Root, "content", "curated", "level-" + n.ToString("0000") + ".json")));
            return LevelSession.Load(definition, Pictures[definition.Picture.Id], new SessionOptions(0, 20000));
        }

        private static Func<string, bool> Seen(params string[] ids) => id => ids.Contains(id);

        private static Func<BoosterKind, bool> Unlocked(params BoosterKind[] kinds) => kind => kinds.Contains(kind);

        [Test]
        public void Level1_ShowsTheEntry_ThenTheForcedFirstTap()
        {
            LevelSession one = Level(1);
            IReadOnlyList<GuideStep> steps = GuideTour.AtStart(1, false, Seen(), Unlocked(), one);
            Assert.That(steps.Select(s => s.Kind), Is.EqualTo(new[] { GuideKind.Entry, GuideKind.FirstTap }));
            Assert.That(steps[0].Forced, Is.False, "the entry goes on with a tap anywhere");
            Assert.That(steps[1].Forced, Is.True, "the first tap is forced");
            Assert.That(GuideTour.FirstTapPod(one), Is.Not.Null);
            Assert.That(GuideTour.AtStart(1, false, Seen(GuideTour.EntryId), Unlocked(), one).Select(s => s.Kind), Is.EqualTo(new[] { GuideKind.FirstTap }));
            Assert.That(GuideTour.AtStart(1, false, Seen(GuideTour.EntryId, GuideTour.FirstTapId), Unlocked(), one), Is.Empty);
            Assert.That(GuideTour.AtStart(1, true, Seen(), Unlocked(), one), Is.Empty, "never in the Daily Challenge");
        }

        [Test]
        public void TheBlockedEntry_ShowsOnce_WhereAPodsTilesAreOutOfReach_Level2()
        {
            Assert.That(GuideTour.BlockedStart(Level(1).View), Is.False, "Level 1's pods all reach their tiles");
            LevelSession two = Level(2);
            Assert.That(GuideTour.BlockedStart(two.View), Is.True, "Level 2 starts with a pod whose tiles are behind others");
            string[] early = { GuideTour.EntryId, GuideTour.FirstTapId };
            IReadOnlyList<GuideStep> steps = GuideTour.AtStart(2, false, Seen(early), Unlocked(), two);
            Assert.That(steps.Select(s => s.Kind), Is.EqualTo(new[] { GuideKind.Blocked }));
            Assert.That(GuideTour.AtStart(2, false, Seen(early.Append(GuideTour.BlockedId).ToArray()), Unlocked(), two), Is.Empty);

            // The lit tiles are the reachable ones and their variant's tiles joined to them: all of one variant.
            IReadOnlyList<CellPos> cells = GuideTour.BlockingCells(two.View);
            Assert.That(cells, Is.Not.Empty);
            Assert.That(cells.Select(c => two.View.Cell(c).Visible).Distinct().Single(), Is.EqualTo(GuideTour.BlockingVariant(two.View)));
            Assert.That(cells, Is.SupersetOf(two.View.ReachableTargets()));

            // Level 5 (the owner's case): the whole pot of logs blocks the water and the leaves (38 logs on its 12×12 board).
            LevelSession five = Level(5);
            Assert.That(GuideTour.BlockingCells(five.View).Count, Is.EqualTo(38));
        }

        [TestCase(3, BoosterKind.ExtraSlot)]
        [TestCase(4, BoosterKind.Shuffle)]
        [TestCase(9, BoosterKind.BloomBurst)]
        public void ABooster_AtItsUnlockLevel_OpensOnItsForcedDemo(int level, BoosterKind kind)
        {
            LevelSession session = Level(level);
            var unlocked = Enum.GetValues(typeof(BoosterKind)).Cast<BoosterKind>().ToArray();
            var seen = GuideTour.DemoIds.Where(id => id != GuideTour.BoosterId(kind)).ToArray();
            IReadOnlyList<GuideStep> steps = GuideTour.AtStart(level, false, Seen(seen), Unlocked(unlocked), session);
            Assert.That(steps.First().Kind, Is.EqualTo(GuideKind.Booster));
            Assert.That(steps.All(s => s.Booster == kind && s.DemoId == GuideTour.BoosterId(kind)), Is.True);
            Assert.That(steps.First().Forced && steps.First().MessageKeys.Contains("demo.try_free"), Is.True, "forced, and it says the use is free");
            Assert.That(steps.Last().Kind == GuideKind.BoosterKept && !steps.Last().Forced, Is.True, "then the kept step");
            Assert.That(steps.Any(s => s.Kind == GuideKind.BoosterTarget), Is.EqualTo(kind == BoosterKind.BloomBurst), "Bloom Burst also has its tile to tap");
            Assert.That(GuideTour.AtStart(level, false, Seen(GuideTour.DemoIds.ToArray()), Unlocked(unlocked), session), Is.Empty, "once only");
        }

        [Test]
        public void Return_WaitsUntilAPodWaitsInASlot_Level6()
        {
            LevelSession six = Level(6);
            var unlocked = Unlocked(BoosterKind.ExtraSlot, BoosterKind.Shuffle, BoosterKind.Return);
            Func<string, bool> seen = Seen(GuideTour.EntryId, GuideTour.FirstTapId, GuideTour.BlockedId, "booster.extra_slot", "booster.shuffle");
            Assert.That(GuideTour.AtStart(6, false, seen, unlocked, six), Is.Empty, "nothing to send back at the start");
            Assert.That(GuideTour.WhenSettled(false, seen, unlocked, six), Is.Empty);

            six.Apply(new TapPod(six.View.Stack(0)[0]));
            IReadOnlyList<GuideStep> steps = GuideTour.WhenSettled(false, seen, unlocked, six);
            Assert.That(steps.Select(s => s.Kind), Is.EqualTo(new[] { GuideKind.Booster, GuideKind.BoosterTarget, GuideKind.BoosterKept }));
            Assert.That(steps.All(s => s.Booster == BoosterKind.Return), Is.True);
            Assert.That(GuideTour.ReturnSlot(six), Is.GreaterThanOrEqualTo(0));
            Assert.That(GuideTour.WhenSettled(false, id => seen(id) || id == "booster.return", unlocked, six), Is.Empty, "once only");
            Assert.That(GuideTour.WhenSettled(true, seen, unlocked, six), Is.Empty, "never in the Daily Challenge");
        }

        [Test]
        public void EveryGuideMessage_IsAString()
        {
            string csv = File.ReadAllText(Path.Combine(Application.dataPath, "Bloomlings", "UI", "Localization", "Resources", "Strings_en.csv"));
            var keys = new HashSet<string>(csv.Split('\n').Select(l => l.Split(',')[0].Trim()));
            var steps = new List<GuideStep>();
            foreach (BoosterKind kind in Enum.GetValues(typeof(BoosterKind)))
            {
                steps.AddRange(GuideTour.BoosterSteps(kind));
            }

            steps.AddRange(GuideTour.AtStart(1, false, Seen(), Unlocked(), Level(1)));
            steps.AddRange(GuideTour.AtStart(2, false, Seen(GuideTour.EntryId, GuideTour.FirstTapId), Unlocked(), Level(2)));
            foreach (string key in steps.SelectMany(s => s.MessageKeys))
            {
                Assert.That(keys, Does.Contain(key));
            }

            Assert.That(AssetSlots.Has("ui.spotlight") && AssetSlots.Has("board.entry.arch"), Is.True);
        }

        [Test]
        public void TheSpotlight_PutsItsBubbleAboveTheHole_OrBelowIt_InsideTheSafeArea()
        {
            foreach ((float w, float h, Insets insets) in new[] { (1080f, 1920f, new Insets(63f, 0f)), (1080f, 2340f, new Insets(110f, 63f)), (720f, 1600f, new Insets(48f, 30f)) })
            {
                Box safe = ScreenLayout.SafeArea(w, h, insets);
                var low = Box.FromCenter(w * 0.5f, h * 0.7f, w * 0.12f, w * 0.12f);
                SpotlightLayout above = Spotlight.Layout(w, h, insets, low, 2, true, true, true);
                Assert.That(above.TailDown && above.Bubble.Bottom < low.Top && above.Bubble.Within(safe), Is.True, w + "x" + h + " above");
                Assert.That(above.TailX, Is.InRange(above.Bubble.Left, above.Bubble.Right));
                Assert.That(above.Hand.Top, Is.LessThan(low.Bottom).And.GreaterThan(low.Top), "the fingertip touches the hole's bottom edge");

                var high = new Box(safe.Left + 20f, safe.Top + 40f, safe.Right - 20f, safe.Top + (h * 0.4f));
                SpotlightLayout below = Spotlight.Layout(w, h, insets, high, 1, false, true, false);
                Assert.That(!below.TailDown && below.Bubble.Top > high.Bottom && below.Bubble.Within(safe), Is.True, w + "x" + h + " below");
                Assert.That(below.Hand.IsEmpty, Is.True, "no hand on a step that is not forced");
            }
        }

        [Test]
        public void TheScrim_IsClearInTheHole_AndDimElsewhere()
        {
            var holes = new[] { new Box(100f, 200f, 300f, 400f) };
            byte[] scrim = Spotlight.Scrim(holes, 1080f, 2340f, 270, 585);
            Assert.That(scrim.Length, Is.EqualTo(270 * 585 * 4));
            int Alpha(int x, int y) => scrim[(((y * 270) + x) * 4) + 3];
            Assert.That(Alpha(50, 75), Is.EqualTo(0), "the hole's middle is clear");
            Assert.That(Alpha(260, 575), Is.EqualTo((int)Math.Round(Spotlight.ScrimAlpha * 255f)).Within(1), "far from it, the scrim");
            Assert.That(Spotlight.Scrim(holes, 1080f, 2340f, 270, 585), Is.EqualTo(scrim), "deterministic");
            Assert.That(Spotlight.ScrimKey(holes, 1080f, 2340f), Is.Not.EqualTo(Spotlight.ScrimKey(new[] { new Box(100f, 200f, 300f, 500f) }, 1080f, 2340f)));
        }

        [Test]
        public void TheEntryArch_StandsInTheBorder_TurnedToItsSide()
        {
            var cell = new Box(400f, 1000f, 500f, 1100f);
            (Box bottom, float b) = BoardLayout.ArchOf(cell, EntrySide.Bottom);
            (float dx, float dy) = BoardLayout.DoorOf(cell, EntrySide.Bottom);
            Assert.That(b, Is.EqualTo(0f));
            Assert.That(bottom.CenterX, Is.EqualTo(dx).Within(0.01f));
            Assert.That(bottom.CenterY, Is.EqualTo(dy - (BoardLayout.ArchInset * 100f)).Within(0.01f), "a little toward the board");
            Assert.That(bottom.Top, Is.LessThan(cell.Bottom).And.GreaterThan(cell.CenterY), "it reaches over the entry cell's foot only");
            Assert.That(BoardLayout.ArchOf(cell, EntrySide.Top).Degrees, Is.EqualTo(180f));
            Assert.That(BoardLayout.ArchOf(cell, EntrySide.Left).Degrees, Is.EqualTo(90f));
            Assert.That(BoardLayout.ArchOf(cell, EntrySide.Right).Degrees, Is.EqualTo(-90f));
            Box left = BoardLayout.ArchBounds(BoardLayout.ArchOf(cell, EntrySide.Left));
            Assert.That(left.Width, Is.EqualTo(BoardLayout.ArchHeight * 100f).Within(0.01f), "a side arch lies across");
            Assert.That(left.Right, Is.GreaterThan(cell.Left).And.LessThan(cell.CenterX));

            byte[] arch = UiRaster.EntryArch(112, 88);
            Assert.That(arch.Length, Is.EqualTo(112 * 88 * 4));
            Assert.That(UiRaster.EntryArch(112, 88), Is.EqualTo(arch), "deterministic");
            int A(int x, int y) => arch[(((y * 112) + x) * 4) + 3];
            Assert.That(A(56, 70), Is.EqualTo(255), "the opening is solid");
            Assert.That(A(2, 2), Is.LessThan(40), "the top corners are clear");
        }
    }
}
