using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Boosters;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Search;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using static Bloomlings.Core.Tests.Fixtures.RuleLevels;

namespace Bloomlings.Core.Tests.Rules
{
    /// <summary>
    /// The owner, 2026-10-06: "I press Shuffle and they don't shuffle". A tray dealt round-robin in definition order (as
    /// the generator deals it) used to come back the same: the first relaxed line was that order, dealt the same way. A
    /// shuffle now shows a new tray whenever a winnable one that looks different exists (research R10 as amended).
    /// </summary>
    public class ShuffleLookTests
    {
        private static readonly string[] Ids = { "l1", "m1", "w1", "d1", "f1", "o1", "v1", "l2", "m2", "w2", "d2", "f2", "o2", "v2" };

        [Test]
        public void Shuffle_ShowsANewTray_NotTheSameColumnsBack()
        {
            LevelSession session = Dealt();
            IReadOnlyList<int>[] before = Tray(session);
            Assert.That(session.Apply(new UseShuffle()).Accepted, Is.True);

            Assert.That(ShufflePlanner.LooksShuffled(session.State, before, Tray(session)), Is.True, "the player sees a new tray");
            Assert.That(StateSearch.Find(session, StateSearch.IsWon, MoveOrder.ProgressFirst, 20_000).Outcome, Is.EqualTo(SearchOutcome.Found), "and it can still be won");
        }

        [Test]
        public void Shuffle_IsTheSameOnEveryRun()
        {
            LevelSession a = Dealt();
            LevelSession b = Dealt();
            a.Apply(new UseShuffle());
            b.Apply(new UseShuffle());
            Assert.That(Tray(a).Select(s => string.Join(",", s)), Is.EqualTo(Tray(b).Select(s => string.Join(",", s))));
        }

        [Test]
        public void ATrayWhoseColumnsOnlyMovedOver_DoesNotLookShuffled()
        {
            LevelSession session = Dealt();
            IReadOnlyList<int>[] before = Tray(session);
            IReadOnlyList<int>[] moved = before.Skip(1).Concat(before.Take(1)).ToArray();
            Assert.That(ShufflePlanner.LooksShuffled(session.State, before, moved), Is.False);
            Assert.That(ShufflePlanner.LooksShuffled(session.State, before, before), Is.False);
        }

        /// <summary>Two tiles of each variant in its column over the open row: every order wins, so only the look decides.</summary>
        private static LevelSession Dealt()
        {
            string[] rows = Enumerable.Repeat("#######", 5).Concat(new[] { "lmwdfov", "lmwdfov", "......." }).ToArray();
            var variants = new Dictionary<char, VariantId>
            {
                ['l'] = VariantId.Leaf, ['m'] = VariantId.Moss, ['w'] = VariantId.Water, ['d'] = VariantId.Dew,
                ['f'] = VariantId.Flower, ['o'] = VariantId.Wood, ['v'] = VariantId.VioletBud,
            };
            PodDef[] pods = Ids.Select(id => Pod(id, variants[id[0]], 1)).ToArray();
            string[][] stacks = Enumerable.Range(0, 4).Select(s => Ids.Where((_, i) => i % 4 == s).ToArray()).ToArray();
            return Session(rows, pods, stacks);
        }

        private static IReadOnlyList<int>[] Tray(LevelSession session) =>
            Enumerable.Range(0, session.State.Tray.StackCount).Select(s => (IReadOnlyList<int>)session.State.Tray.StackTopFirst(s).ToArray()).ToArray();
    }
}
