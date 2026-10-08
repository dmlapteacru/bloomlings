using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The board's clearing styles (spec 005 FR-038; spec 001 research R4, amendment of 2026-10-06).</summary>
    public class ClearStylesTests
    {
        private static IEnumerable<ClearStyle> Styles => Enum.GetValues(typeof(ClearStyle)).Cast<ClearStyle>();

        [Test]
        public void EveryStyle_TakesTheSameTimeForATile()
        {
            foreach (ClearStyle style in Styles)
            {
                for (int cells = 1; cells <= 24; cells++)
                {
                    ClearLegs legs = ClearStyles.LegsOf(style, cells);
                    Assert.That(legs.Total, Is.EqualTo(ClearStyles.TripSeconds(cells)).Within(1e-4f), style + " " + cells);
                    Assert.That(legs.Out, Is.GreaterThan(0f), style.ToString());
                    Assert.That(legs.GoneShare, Is.InRange(0f, 1f), style.ToString());
                }
            }

            // Colony Flow's ants (the owner's video of 2026-10-08, Level 11 at 1×): about 9 cells a second, one out of each
            // slot every 0.30 s; the acts keep the calm pace 1.5 times as fast (the owner, 2026-10-06).
            Assert.That(1f / ClearStyles.PerCell, Is.InRange(8.5f, 9.5f), "cells a second");
            Assert.That(ClearStyles.TripSeconds(6), Is.EqualTo((6f * 1.1f / 10f) + (1.4f / 1.5f)).Within(1e-4f));
            Assert.That(ClearStyles.LineGap, Is.EqualTo(0.3f).Within(1e-4f));
            Assert.That(ClearStyles.TripSeconds(49), Is.LessThan(7f), "a straight route across the biggest board");
        }

        [Test]
        public void TheFreePair_AlternatesByLevel_AndAChosenBoughtStyleWins()
        {
            for (int level = 1; level <= ClearStyles.OnboardingLevels; level++)
            {
                Assert.That(ClearStyles.ForLevel(level), Is.EqualTo(ClearStyle.Blossom), "the onboarding plays Blossom");
            }

            Assert.That(ClearStyles.ForLevel(11), Is.EqualTo(ClearStyle.Blossom));
            Assert.That(ClearStyles.ForLevel(12), Is.EqualTo(ClearStyle.Munchers));
            Assert.That(ClearStyles.ForLevel(13), Is.EqualTo(ClearStyle.Blossom));
            Assert.That(ClearStyles.ForLevel(500), Is.EqualTo(ClearStyle.Munchers));
            Assert.That(ClearStyles.ForLevel(12, ClearStyle.Fireflies), Is.EqualTo(ClearStyle.Fireflies));
            Assert.That(ClearStyles.ForLevel(3, ClearStyle.Parade), Is.EqualTo(ClearStyle.Parade));
            Assert.That(ClearStyles.ForLevel(12, ClearStyle.Blossom), Is.EqualTo(ClearStyle.Munchers), "a free style is never chosen: the pair plays");
        }

        [Test]
        public void TheCatalog_HasTwoFreeAndFiveBoughtStyles_WithIdsAndSlots()
        {
            Assert.That(ClearStyles.Free, Is.EquivalentTo(new[] { ClearStyle.Blossom, ClearStyle.Munchers }));
            Assert.That(ClearStyles.Bought.Count, Is.EqualTo(5));
            Assert.That(ClearStyles.Free.Concat(ClearStyles.Bought), Is.EquivalentTo(Styles));
            foreach (ClearStyle style in Styles)
            {
                Assert.That(ClearStyles.Parse(ClearStyles.Id(style)), Is.EqualTo(style));
                Assert.That(ClearStyles.Id(style), Does.StartWith("clear."));
                Assert.That(AssetSlots.Has(ClearLook.SlotOf(style)), Is.True, ClearLook.SlotOf(style));
            }

            Assert.That(ClearStyles.Parse("hat.cap"), Is.Null);
            Assert.That(ClearStyles.Parse(null), Is.Null);
            Assert.That(AssetSlots.Has("ui.card.clearing"), Is.True);
        }

        [Test]
        public void FxList_KeepsATurnAndASquashAboutTheItemsCenter()
        {
            var list = new FxList();
            list.PushTurn(30f, 5f, 5f);
            list.PushSquash(2f, 0.5f, 5f, 5f);
            list.Circle(5f, 5f, 1f, Rgba.White);
            list.Circle(6f, 5f, 1f, Rgba.White);
            list.Pop();
            list.Pop();
            list.PushAlpha(0.5f);
            list.Round(Box.FromCenter(1f, 1f, 2f, 1f), 0.2f, Rgba.White);
            list.Pop();
            list.Round(Box.FromCenter(1f, 1f, 2f, 1f), 0.2f, Rgba.White);

            FxItem centered = list.Items[0];
            Assert.That(centered.X, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(centered.Y, Is.EqualTo(5f).Within(1e-4f));
            Assert.That(centered.Turn, Is.EqualTo(30f).Within(1e-3f));
            Assert.That(centered.Sx, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(centered.Sy, Is.EqualTo(0.5f).Within(1e-4f));

            // One cell right of the pivot: squashed to 2 cells, then turned 30° clockwise (y down).
            FxItem moved = list.Items[1];
            Assert.That(moved.X, Is.EqualTo(5f + (2f * (float)Math.Cos(Math.PI / 6))).Within(1e-4f));
            Assert.That(moved.Y, Is.EqualTo(5f + (2f * (float)Math.Sin(Math.PI / 6))).Within(1e-4f));
            Assert.That(list.Items[2].Alpha, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(list.Items[3].Alpha, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(list.Items[3].Turn, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void AWalker_IsDrawnOnlyOnItsTrip_AndHoldsItsTileFromItsAct()
        {
            var points = new List<(float X, float Y)> { (2.5f, 6f), (2.5f, 5.5f), (2.5f, 4.5f), (3.5f, 4.5f) };
            var list = new FxList();
            foreach (ClearStyle style in Styles)
            {
                ClearLegs legs = ClearStyles.LegsOf(style, 3);
                var walk = new ClearWalk(points, VariantId.Leaf, 10f, legs.Total, Box.FromCenter(1f, 8f, 0.7f, 0.7f));
                list.Clear();
                ClearLook.Walker(list, style, walk, 9.9f);
                Assert.That(list.Items, Is.Empty, style + " before it sets off");
                ClearLook.Walker(list, style, walk, 10f + legs.Total + 0.01f);
                Assert.That(list.Items, Is.Empty, style + " after its clear");

                float mid = 10f + (legs.Out / 2f);
                ClearLook.Walker(list, style, walk, mid);
                Assert.That(list.Items.Any(i => i.Kind == FxKind.Character && i.Variant == VariantId.Leaf), Is.True, style + " walks");
                Assert.That(ClearLook.Holds(style, walk, mid), Is.False, style + " leaves the tile on the board while walking out");

                float acting = 10f + legs.Out + (legs.Act / 2f);
                Assert.That(ClearLook.Holds(style, walk, acting), Is.True, style + " holds the tile in its act");
                list.Clear();
                ClearLook.Walker(list, style, walk, acting);
                Assert.That(list.Items, Is.Not.Empty, style + " acts");
                Assert.That(list.Items.All(i => i.Slot == ClearLook.SlotOf(style)), Is.True, style.ToString());
                Assert.That(list.Items.All(i => i.Kind != FxKind.Shape || ShapeLibrary.Has(i.Shape!)), Is.True, style.ToString());
                Assert.That(ClearLook.Holds(style, walk, 10f + legs.Total + 0.01f), Is.False, style + " gives the cell back at the clear");
            }
        }

        [Test]
        public void ThePreview_ClearsItsBoardInALine_AndLoops()
        {
            foreach (ClearStyle style in Styles)
            {
                var preview = new ClearPreview(style);
                int tiles = ClearPreview.Columns * ClearPreview.Rows;
                Assert.That(preview.Count(0f), Is.EqualTo(tiles), style.ToString());
                Assert.That(preview.Count(preview.Period - 0.01f), Is.EqualTo(0), style.ToString());
                Assert.That(preview.Period, Is.InRange(5f, 90f), style.ToString());
                Assert.That(preview.Local(preview.Period + 1f), Is.EqualTo(1f).Within(1e-3f));

                // The tiles clear one by one over the loop, nearest first (the entry cell first).
                Assert.That(preview.Cleared(ClearPreview.EntryColumn, ClearPreview.Rows - 1, preview.Period - 0.01f), Is.True);
                int last = tiles;
                for (float t = 0f; t < preview.Period; t += 0.25f)
                {
                    int count = preview.Count(t);
                    Assert.That(count, Is.LessThanOrEqualTo(last), style.ToString());
                    last = count;
                }

                var list = new FxList();
                preview.Draw(list, ClearStyles.TripSeconds(1) * 0.5f);
                Assert.That(list.Items.Any(i => i.Kind == FxKind.Character), Is.True, style + " shows a walker early on");
            }
        }

        [Test]
        public void TheFreeCard_ShowsBlossomAndMunchersByTurns_ABoughtCardItsOwnLoop()
        {
            ClearPreview blossom = ClearPreview.Of(ClearStyle.Blossom);
            ClearPreview munchers = ClearPreview.Of(ClearStyle.Munchers);
            Assert.That(ClearPreview.Of(ClearStyle.Blossom), Is.SameAs(blossom), "made once");
            float both = blossom.Period + munchers.Period;
            foreach (float cycle in new[] { 0f, both, 7f * both })
            {
                (ClearPreview first, float t1) = ClearPreview.At(ClearStyle.Blossom, true, cycle + 1f);
                Assert.That(first, Is.SameAs(blossom));
                Assert.That(t1, Is.EqualTo(1f).Within(0.01f));
                (ClearPreview second, float t2) = ClearPreview.At(ClearStyle.Blossom, true, cycle + blossom.Period + 2f);
                Assert.That(second, Is.SameAs(munchers), "Munchers' loop follows Blossom's");
                Assert.That(t2, Is.EqualTo(2f).Within(0.01f));
            }

            (ClearPreview own, float t) = ClearPreview.At(ClearStyle.Bubbles, false, ClearPreview.Of(ClearStyle.Bubbles).Period + 3f);
            Assert.That(own.Style, Is.EqualTo(ClearStyle.Bubbles));
            Assert.That(t, Is.EqualTo(3f).Within(0.01f));
        }

        [Test]
        public void BlossomsRestore_SparklesOrSplashesDropletsForTheDropFamily()
        {
            var flower = new FxList();
            ClearLook.Restore(flower, ClearStyle.Blossom, new ClearFade((2.5f, 2.5f), VariantId.Flower, 10f, null), 10.2f);
            Assert.That(flower.Items.Any(i => i.Shape == "fx.sparkle") && flower.Items.All(i => i.Shape != "fx.droplet"), Is.True);

            var dew = new FxList();
            ClearLook.Restore(dew, ClearStyle.Blossom, new ClearFade((2.5f, 2.5f), VariantId.Dew, 10f, null), 10.2f);
            Assert.That(dew.Items.Count(i => i.Shape == "fx.droplet"), Is.EqualTo(2), "a Drop-family tile splashes");
            Assert.That(dew.Items.All(i => i.Shape != "fx.sparkle"), Is.True);
        }

        [Test]
        public void Blossom_SwaysTheNeighboursOfAJustOpenedFlower()
        {
            var fades = new List<ClearFade> { new ClearFade((2.5f, 2.5f), VariantId.Flower, 10f, null) };
            float sway = 0f;
            for (float t = 10f; t < 11f; t += 0.05f)
            {
                sway = Math.Max(sway, Math.Abs(ClearLook.Sway(ClearStyle.Blossom, fades, (3.5f, 2.5f), t)));
            }

            Assert.That(sway, Is.GreaterThan(1f));
            Assert.That(ClearLook.Sway(ClearStyle.Blossom, fades, (2.5f, 2.5f), 10.3f), Is.EqualTo(0f), "not the cleared cell itself");
            Assert.That(ClearLook.Sway(ClearStyle.Munchers, fades, (3.5f, 2.5f), 10.3f), Is.EqualTo(0f));
        }
    }
}
