using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The walkers chosen in Settings (the owner, 2026-11-04: Classic, Creatures, Heroes), Munchers' aside eating and the Head
    /// Bonk style; presentation only.
    /// </summary>
    public class WalkerLookTests
    {
        private static readonly DateTime Today = new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void BackToClassic() => WalkerLook.Use(WalkerKind.Classic, ClearStyles.Default);

        [Test]
        public void Classic_IsTheGamesLook_AndKeepsItsTimingInEveryStyleButBonk()
        {
            Assert.That(WalkerLook.For(WalkerKind.Classic), Is.SameAs(WalkerLook.Default));
            foreach (ClearStyle style in Enum.GetValues(typeof(ClearStyle)).Cast<ClearStyle>())
            {
                WalkerLook.Use(WalkerKind.Classic, style);
                Assert.That(WalkerLook.Current.IsDefault, Is.True);
                Assert.That(ClearStyles.Linger, Is.EqualTo(style == ClearStyle.Bonk ? WalkerLook.HandsLinger : ClearStyles.LiveLinger), style.ToString());
            }
        }

        [Test]
        public void CreaturesAndHeroes_EatAsideInMunchers_AndKeepTheOtherStylesActs()
        {
            foreach (WalkerKind kind in new[] { WalkerKind.Creatures, WalkerKind.Heroes })
            {
                WalkerLook look = WalkerLook.For(kind);
                Assert.That(look.AsideMunch, Is.True, kind.ToString());
                Assert.That(look.UsesHands(ClearStyle.Munchers), Is.True);
                Assert.That(look.UsesHands(ClearStyle.Bonk), Is.True);
                Assert.That(look.UsesHands(ClearStyle.Blossom), Is.False, "the other bought styles keep their acts");
                Assert.That(look.BoardShare, Is.EqualTo(WalkerLook.MockBoardShare), "a fixed size on screen");
            }

            Assert.That(WalkerLook.GameCreatures.DrawsCreature(RoundMaterial.Of('A').Index), Is.True, "every family is a creature");
            Assert.That(WalkerLook.GameHeroes.Heroes, Is.True);
        }

        [Test]
        public void TheSetting_RoundTripsThroughTheSave_AndOlderSavesAreClassic()
        {
            PlayerSave save = PlayerSave.CreateNew("p", Today);
            Assert.That(save.Settings.Walkers, Is.EqualTo("classic"));
            string classic = SaveSerializer.Write(save);
            Assert.That(classic, Does.Not.Contain("walkers"), "a classic save stays as it was");
            Assert.That(WalkerKinds.Parse(SaveSerializer.Read(classic).Settings.Walkers), Is.EqualTo(WalkerKind.Classic));

            save.Settings.Walkers = WalkerKinds.Id(WalkerKind.Heroes);
            PlayerSave read = SaveSerializer.Read(SaveSerializer.Write(save));
            Assert.That(WalkerKinds.Parse(read.Settings.Walkers), Is.EqualTo(WalkerKind.Heroes));
            Assert.That(WalkerKinds.Next(WalkerKind.Classic), Is.EqualTo(WalkerKind.Creatures));
            Assert.That(WalkerKinds.Next(WalkerKind.Heroes), Is.EqualTo(WalkerKind.Classic));
            Assert.That(WalkerKinds.Parse("glitter"), Is.EqualTo(WalkerKind.Classic));
        }

        [Test]
        public void Bonk_TakesTheCubeInItsHands_AndBonksItOnItsHead_WithEveryWalkerKind()
        {
            var points = new List<(float X, float Y)> { (0.5f, 6.5f), (1.5f, 6.5f), (2.5f, 6.5f), (3.5f, 6.5f) };
            foreach (WalkerKind kind in WalkerKinds.All)
            {
                WalkerLook.Use(kind, ClearStyle.Bonk);
                ClearWalk walk = ClearWalk.Live(points, 0, RoundLook.WalkerVariant(Bloomlings.Core.Variants.Family.Sprig), 0f, 0.4f, 0.5f, 0.9f, null, 40f);
                bool lifted = false;
                for (float t = 0.4f; t < 1.5f; t += 0.05f)
                {
                    var list = new FxList();
                    ClearLook.Walker(list, ClearStyle.Bonk, walk, t);
                    lifted |= list.Items.Any(i => i.Kind == FxKind.Tile && i.Lifted && i.Material == 0);
                    Assert.That(list.Items.Any(i => i.Kind == FxKind.Character), Is.True, kind + " at " + t);
                }

                Assert.That(lifted, Is.True, kind + ": the cube held, tossed and bonked over the walkers");
            }
        }

        [Test]
        public void AsideSpots_SpreadTheEatersApart_OnOpenGroundOnly()
        {
            WalkerLook.Use(WalkerKind.Creatures, ClearStyle.Munchers);
            var spots = new AsideSpots();
            var walkers = new List<RoundWalk>();
            var ways = new Dictionary<int, List<(float X, float Y)>>();

            // A 20 × 20 open field (cells 0..399, a cell per unit), its left column a lane every walker walks up; cubes at x >= 12.
            for (int id = 0; id < 6; id++)
            {
                var route = new List<int>();
                var way = new List<(float X, float Y)>();
                for (int y = 19; y >= 10 - id; y--)
                {
                    route.Add((y * 20) + 5);
                    way.Add((5.5f, y + 0.5f));
                }

                walkers.Add(new RoundWalk(id, 0, 0, route[route.Count - 1], 0, 1_000_000 + (id * 10_000), 1_100_000 + (id * 10_000), 2_000_000, route));
                ways[id] = way;
            }

            spots.Update(walkers, 1.5f, 1.5f, w => ways[w.Id], (x, y) => x < 0 || y < 0 || x >= 20 || y >= 20 ? -1 : ((int)y * 20) + (int)x, cell => cell % 20 < 12);
            var chosen = walkers.Select(w => spots.Of(w.Id)).Where(s => s.HasValue).Select(s => s!.Value).ToList();
            Assert.That(chosen.Count, Is.EqualTo(walkers.Count), "every walker found a spot");
            for (int i = 0; i < chosen.Count; i++)
            {
                Assert.That(chosen[i].X, Is.LessThan(12f), "never over the cubes");
                for (int j = i + 1; j < chosen.Count; j++)
                {
                    float d = (float)Math.Sqrt(Math.Pow(chosen[i].X - chosen[j].X, 2) + Math.Pow(chosen[i].Y - chosen[j].Y, 2));
                    Assert.That(d, Is.GreaterThanOrEqualTo(WalkerLook.AsideSpacing * 1.5f * 0.99f), "never two in one place");
                }
            }
        }
    }
}
