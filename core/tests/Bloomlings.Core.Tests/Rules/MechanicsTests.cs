using System;
using System.Linq;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Tray;
using Bloomlings.Core.Variants;
using NUnit.Framework;
using static Bloomlings.Core.Tests.Fixtures.RuleLevels;

namespace Bloomlings.Core.Tests.Rules
{
    /// <summary>The remaining doc 01 §20 tests and the US4 acceptance scenarios, one mechanic at a time (T096).</summary>
    public class MechanicsTests
    {
        private static readonly string[] Walls6 = { "#######", "#######", "#######", "#######", "#######", "#######" };

        private static string[] Rows(params string[] bottomRows) => Walls6.Take(8 - bottomRows.Length).Concat(bottomRows).ToArray();

        private static LevelSession Load(string[] rows, LevelDefinition definition)
        {
            LevelSession session = LevelSession.Load(definition, Picture(rows), Options);
            Assert.That(session.StateHash, Is.EqualTo(session.ComputeFullStateHash()));
            return session;
        }

        private static CommandResult TapChecked(LevelSession session, string podId)
        {
            CommandResult result = session.Tap(podId);
            Assert.That(session.StateHash, Is.EqualTo(session.ComputeFullStateHash()), "incremental hash after tap:" + podId);
            return result;
        }

        private static PodDef Locked(string id, VariantId variant, int count, string key) => new PodDef(id, variant, count, false, key, null);

        private static PodDef Connected(string id, VariantId variant, int count, string group) => new PodDef(id, variant, count, false, null, group);

        private static PodDef MysteryPod(string id, VariantId variant, int count) => new PodDef(id, variant, count, true, null, null);

        private static CellOverlay Key(int x, int y, string key) => new CellOverlay(new CellPos(x, y), Array.Empty<VariantId>(), false, false, false, key);

        private static SpecialDef Special(string id, SpecialType type, CellPos[] cells, SpecialCondition condition, SpecialEffect effect) =>
            new SpecialDef(id, type, cells, condition, effect);

        [Test]
        public void LayeredCrossFamilyReveal_WakesAnActiveVioletPod()
        {
            string[] rows = Rows("###l###", ".......");
            LevelDefinition level = Definition(
                rows,
                new[] { Pod("violet", VariantId.VioletBud, 1), Pod("leaf", VariantId.Leaf, 1) },
                overlays: new[] { new CellOverlay(new CellPos(3, 1), new[] { VariantId.VioletBud }, false, false, false, null) });
            LevelSession session = Load(rows, level);
            Assert.That(session.View.Cell(new CellPos(3, 1)).Next, Is.EqualTo(VariantId.VioletBud), "layer peek (FR-036)");

            CommandResult waiting = TapChecked(session, "violet");
            CommandResult result = TapChecked(session, "leaf");

            Assert.That(waiting.Clears(), Is.Empty);
            GameEvent[] events = result.Events.ToArray();
            int revealed = Array.FindIndex(events, e => e is LayerRevealed r && r.NewTopVariant == VariantId.VioletBud);
            int woke = Array.FindIndex(events, e => e is TileCleared c && c.PodId == "violet" && c.Variant == VariantId.VioletBud);
            Assert.That(revealed, Is.GreaterThanOrEqualTo(0));
            Assert.That(woke, Is.GreaterThan(revealed), "the waiting Violet pod wakes on the revealed layer");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void Key_IsCollectedWithItsLayerAndOpensExactlyOneLock()
        {
            // Leaf carries k1 (opens pod "locked"); Water carries k2 (opens slot 4).
            string[] rows = Rows("##mlw##", ".......");
            LevelDefinition level = Definition(
                rows,
                new[] { Pod("leaf", VariantId.Leaf, 1), Pod("water", VariantId.Water, 1), Locked("locked", VariantId.Moss, 1, "k1") },
                overlays: new[] { Key(3, 1, "k1"), Key(4, 1, "k2") }) with
            {
                Locks = new[] { new LockDef("k1", LockTargetKind.Pod, "locked"), new LockDef("k2", LockTargetKind.Slot, "4") },
                Slots = new SlotsDef(SlotsDef.DefaultCount, new LockedSlotDef(4, "k2")),
            };
            LevelSession session = Load(rows, level);
            Assert.That(session.View.SlotStateOf(4), Is.EqualTo(SlotState.Locked));
            Assert.That(session.Tap("locked").Reason, Is.EqualTo(RejectReason.Locked));

            CommandResult leaf = TapChecked(session, "leaf");

            Assert.That(leaf.ClearedBy("leaf").Count(), Is.EqualTo(1), "the key costs no extra work");
            Assert.That(leaf.Events.OfType<KeyCollected>().Single().KeyId, Is.EqualTo("k1"));
            Assert.That(leaf.Events.OfType<LockOpened>().Single(), Is.EqualTo(new LockOpened(1, LockTargetKind.Pod, "locked")));
            Assert.That(session.View.Pod("locked").Locked, Is.False);
            Assert.That(session.View.SlotStateOf(4), Is.EqualTo(SlotState.Locked), "k2 is still on the board");
            Assert.That(session.View.Cell(new CellPos(3, 1)).KeyId, Is.Null);

            TapChecked(session, "locked");
            CommandResult water = TapChecked(session, "water");

            Assert.That(water.Events.OfType<LockOpened>().Single(), Is.EqualTo(new LockOpened(1, LockTargetKind.Slot, "4")));
            Assert.That(session.View.SlotStateOf(4), Is.EqualTo(SlotState.Free));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void LockedPod_IsRefusedAndBuriesThePodsBelowIt()
        {
            string[] rows = Rows("##mlw##", ".......");
            LevelDefinition level = Definition(
                rows,
                new[] { Locked("locked", VariantId.Leaf, 1, "k1"), Pod("below", VariantId.Moss, 1), Pod("water", VariantId.Water, 1) },
                stacks: new[] { new[] { "locked", "below" }, new[] { "water" } },
                overlays: new[] { Key(4, 1, "k1") }) with
            {
                Locks = new[] { new LockDef("k1", LockTargetKind.Pod, "locked") },
            };
            LevelSession session = Load(rows, level);

            Assert.That(session.Tap("locked").Reason, Is.EqualTo(RejectReason.Locked));
            Assert.That(session.Tap("below").Reason, Is.EqualTo(RejectReason.NotExposed));

            TapChecked(session, "water");
            Assert.That(session.Tap("locked").Accepted, Is.True);
            Assert.That(session.Tap("below").Accepted, Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void MissingOrExtraKeys_AreInvalidLevels()
        {
            string[] rows = Rows("##mlw##", ".......");
            PodDef[] pods = { Pod("leaf", VariantId.Leaf, 1), Pod("water", VariantId.Water, 1), Locked("locked", VariantId.Moss, 1, "k1") };

            LevelDefinition noLock = Definition(rows, pods, overlays: new[] { Key(3, 1, "k1") });
            LevelDefinition noKey = Definition(rows, pods) with { Locks = new[] { new LockDef("k1", LockTargetKind.Pod, "locked") } };

            Assert.Throws<InvalidLevelException>(() => LevelSession.Load(noLock, Picture(rows), Options));
            Assert.Throws<InvalidLevelException>(() => LevelSession.Load(noKey, Picture(rows), Options));
        }

        /// <summary>A Leaf pair and a Water tile at the bottom; a Flower column behind the Water tile.</summary>
        private static readonly string[] ConnectedBoard = Rows("###f###", "###f###", "###f###", "###f###", "##lwl##", ".......");

        private static LevelDefinition ConnectedLevel() => Definition(
            ConnectedBoard,
            new[]
            {
                Pod("f1", VariantId.Flower, 1), Pod("f2", VariantId.Flower, 1), Pod("f3", VariantId.Flower, 1), Pod("f4", VariantId.Flower, 1),
                Connected("cLeaf", VariantId.Leaf, 2, "g"), Connected("cWater", VariantId.Water, 1, "g"),
            });

        [Test]
        public void ConnectedPods_WithMixedVariants_CommitTogetherIntoTheirOwnSlots()
        {
            LevelSession session = Load(ConnectedBoard, ConnectedLevel());

            CommandResult result = TapChecked(session, "cWater");

            PodCommitted[] commits = result.Events.OfType<PodCommitted>().ToArray();
            Assert.That(commits.Select(c => c.PodId), Is.EqualTo(new[] { "cWater", "cLeaf" }), "the tapped member first");
            Assert.That(commits.Select(c => c.SlotIndex), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(result.ClearedBy("cLeaf").Count(), Is.EqualTo(2), "each member then works on its own variant");
            Assert.That(result.ClearedBy("cWater").Count(), Is.EqualTo(1));
            Assert.That(session.View.ConnectedGroup("cLeaf"), Is.EqualTo(new[] { "cLeaf", "cWater" }));
        }

        [Test]
        public void ConnectedPods_AreRefusedWhenSlotsAreShort()
        {
            LevelSession session = Load(ConnectedBoard, ConnectedLevel());
            foreach (string flower in new[] { "f1", "f2", "f3", "f4" })
            {
                TapChecked(session, flower);
            }

            CheckRefused(session.Check(new TapPod("cLeaf")), RejectReason.NotEnoughSlotsForGroup);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Stuck), "one free slot, and the only exposed pods form a pair");
        }

        [Test]
        public void ConnectedTriple_NeedsThreeFreeSlots_AndCommitsTogether()
        {
            // Three members (Leaf, Water, Leaf) at the same depth; three Flower pods first leave only two free slots.
            string[] rows = Rows("###f###", "###f###", "###f###", "##lwl##", ".......");
            LevelDefinition level = Definition(
                rows,
                new[]
                {
                    Pod("f1", VariantId.Flower, 1), Pod("f2", VariantId.Flower, 1), Pod("f3", VariantId.Flower, 1),
                    Connected("a", VariantId.Leaf, 1, "t"), Connected("b", VariantId.Water, 1, "t"), Connected("c", VariantId.Leaf, 1, "t"),
                },
                stacks: new[] { new[] { "a", "f1" }, new[] { "b", "f2" }, new[] { "c", "f3" } });
            LevelSession session = Load(rows, level);
            Assert.That(LevelMechanics.UnlocksUsed(level, Picture(rows)), Does.Contain("mechanic.connected_triple").And.Not.Contain("mechanic.connected_pair"));

            CommandResult result = TapChecked(session, "b");

            Assert.That(result.Events.OfType<PodCommitted>().Select(c => c.PodId), Is.EqualTo(new[] { "b", "a", "c" }), "the tapped member first, then the group");
            Assert.That(session.View.ConnectedGroup("a"), Is.EqualTo(new[] { "a", "b", "c" }));

            // The same pods with the Flowers on top: three wait behind the Water tile, and two free slots are too few.
            LevelSession buried = Load(rows, level with { Tray = new TrayDef(new[] { new[] { "f1", "a" }, new[] { "f2", "b" }, new[] { "f3", "c" } }) });
            foreach (string flower in new[] { "f1", "f2", "f3" })
            {
                TapChecked(buried, flower);
            }

            CheckRefused(buried.Check(new TapPod("a")), RejectReason.NotEnoughSlotsForGroup);
        }

        private static void CheckRefused(CommandCheck check, RejectReason reason)
        {
            Assert.That(check.IsAllowed, Is.False);
            Assert.That(check.Reason, Is.EqualTo(reason));
        }

        [Test]
        public void ConnectedPods_NeedEveryMemberExposed()
        {
            PodDef[] pods = ConnectedLevel().Pods.ToArray();
            LevelDefinition level = Definition(
                ConnectedBoard,
                pods,
                stacks: new[] { new[] { "f1", "cLeaf" }, new[] { "f2", "cWater" }, new[] { "f3" }, new[] { "f4" } });
            LevelSession session = Load(ConnectedBoard, level);

            TapChecked(session, "f1");

            CheckRefused(session.Check(new TapPod("cLeaf")), RejectReason.NotExposed);
            TapChecked(session, "f2");
            Assert.That(session.Tap("cLeaf").Events.OfType<PodCommitted>().Count(), Is.EqualTo(2));
        }

        [Test]
        public void ConnectedPods_MustSitAtTheSameDepth()
        {
            LevelDefinition bad = Definition(
                ConnectedBoard,
                ConnectedLevel().Pods.ToArray(),
                stacks: new[] { new[] { "f1", "cLeaf" }, new[] { "cWater" }, new[] { "f2" }, new[] { "f3" }, new[] { "f4" } });

            Assert.Throws<InvalidLevelException>(() => LevelSession.Load(bad, Picture(ConnectedBoard), Options));
        }

        [Test]
        public void Stone_IsNeverWalkable()
        {
            // The Leaf tile sits behind a stone; nothing else leads to it.
            string[] rows = Rows("###l###", "###m###", "..m#m..", ".......");
            LevelDefinition level = Definition(rows, new[] { Pod("leaf", VariantId.Leaf, 1), Pod("moss", VariantId.Moss, 3) });
            LevelSession session = Load(rows, level);

            TapChecked(session, "moss");
            Assert.That(session.View.Pod("moss").Remaining, Is.EqualTo(1), "the Moss tile above the stone stays out of reach");
            Assert.That(session.View.Cell(new CellPos(3, 1)).Kind, Is.EqualTo(CellKind.Stone));
        }

        [Test]
        public void Gate_OpensWhenItsRegionIsRestored()
        {
            // The gate at (3,2) seals the Leaf tile above it; it opens when the Moss row is restored.
            string[] rows = Rows("###l###", "###.###", "mmmmmmm", ".......");
            LevelDefinition level = Definition(rows, new[] { Pod("leaf", VariantId.Leaf, 1), Pod("moss", VariantId.Moss, 7) }) with
            {
                Specials = new[]
                {
                    Special(
                        "gate",
                        SpecialType.Gate,
                        new[] { new CellPos(3, 2) },
                        new SpecialCondition(SpecialConditionKind.ClearRegion, null, null, null, Enumerable.Range(0, 7).Select(x => new CellPos(x, 1)).ToArray()),
                        new SpecialEffect(SpecialEffectKind.OpenCells, Array.Empty<CellPos>())),
                },
            };
            LevelSession session = Load(rows, level);
            Assert.That(session.View.Specials.Single().Total, Is.EqualTo(7));

            TapChecked(session, "leaf");
            CommandResult result = TapChecked(session, "moss");

            Assert.That(result.Events.OfType<SpecialProgressed>().Last(), Is.EqualTo(new SpecialProgressed(1, "gate", 7, 7)));
            Assert.That(result.Events.OfType<SpecialTriggered>().Single().EffectCells, Is.EqualTo(new[] { new CellPos(3, 2) }));
            Assert.That(result.ClearedBy("leaf"), Is.EqualTo(new[] { new CellPos(3, 3) }));
            Assert.That(session.View.Specials.Single().Triggered, Is.True);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void Gate_WithAKeyCondition_OpensWhenTheKeyIsCollected()
        {
            string[] rows = Rows("###l###", "###.###", "..mmm..", ".......");
            LevelDefinition level = Definition(rows, new[] { Pod("leaf", VariantId.Leaf, 1), Pod("moss", VariantId.Moss, 3) }, overlays: new[] { Key(4, 1, "gk") }) with
            {
                Locks = new[] { new LockDef("gk", LockTargetKind.Special, "gate") },
                Specials = new[]
                {
                    Special("gate", SpecialType.Gate, new[] { new CellPos(3, 2) }, new SpecialCondition(SpecialConditionKind.Key, "gk", null, null, Array.Empty<CellPos>()), new SpecialEffect(SpecialEffectKind.OpenCells, Array.Empty<CellPos>())),
                },
            };
            LevelSession session = Load(rows, level);

            CommandResult result = TapChecked(session, "moss");

            Assert.That(result.Events.OfType<LockOpened>().Single().TargetKind, Is.EqualTo(LockTargetKind.Special));
            Assert.That(result.Events.OfType<SpecialTriggered>().Single().SpecialId, Is.EqualTo("gate"));
            TapChecked(session, "leaf");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void Fountain_CountsOnlyItsExactVariant_ThenRemovesAStone()
        {
            // Fountain at (3,2): "restore 2 Water around it". Dew below it does not count (FR-038).
            string[] rows = Rows("###l###", "#######", "#.....#", "#ww.ww#", "#wwdww#", ".......");
            LevelDefinition level = Definition(
                rows,
                new[] { Pod("leaf", VariantId.Leaf, 1), Pod("dew", VariantId.Dew, 1), Pod("water", VariantId.Water, 8) }) with
            {
                Specials = new[]
                {
                    Special(
                        "fountain",
                        SpecialType.Fountain,
                        new[] { new CellPos(3, 2) },
                        new SpecialCondition(SpecialConditionKind.ClearCountAdjacent, null, VariantId.Water, 2, Array.Empty<CellPos>()),
                        new SpecialEffect(SpecialEffectKind.RemoveStones, new[] { new CellPos(3, 4) })),
                },
            };
            LevelSession session = Load(rows, level);

            TapChecked(session, "leaf");
            CommandResult dew = TapChecked(session, "dew");
            Assert.That(dew.Events.OfType<SpecialProgressed>(), Is.Empty, "Dew is not Water");

            CommandResult water = TapChecked(session, "water");

            SpecialTriggered triggered = water.Events.OfType<SpecialTriggered>().Single();
            Assert.That(triggered.EffectCells, Is.EqualTo(new[] { new CellPos(3, 4) }), "the Fountain itself stays; the stone goes");
            Assert.That(session.View.Cell(new CellPos(3, 2)).Kind, Is.EqualTo(CellKind.Special));
            Assert.That(water.ClearedBy("leaf"), Is.EqualTo(new[] { new CellPos(3, 5) }));
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void LockedSlot_LeavesFourUsableSlotsUntilItsKey()
        {
            // Four Flower pods wait behind a Water tile; with slot 0 locked, the fourth fills the buffer: a jam.
            string[] rows = Rows("###f###", "###f###", "###f###", "###f###", "##mwm##", ".......");
            LevelDefinition level = Definition(
                rows,
                new[]
                {
                    Pod("f1", VariantId.Flower, 1), Pod("f2", VariantId.Flower, 1), Pod("f3", VariantId.Flower, 1), Pod("f4", VariantId.Flower, 1),
                    Pod("water", VariantId.Water, 1), Pod("moss", VariantId.Moss, 2),
                },
                overlays: new[] { Key(2, 1, "sk") }) with
            {
                Locks = new[] { new LockDef("sk", LockTargetKind.Slot, "0") },
                Slots = new SlotsDef(SlotsDef.DefaultCount, new LockedSlotDef(0, "sk")),
            };
            LevelSession session = Load(rows, level);

            CommandResult first = TapChecked(session, "f1");
            Assert.That(first.Events.OfType<PodCommitted>().Single().SlotIndex, Is.EqualTo(1), "the locked slot is skipped");
            TapChecked(session, "f2");
            TapChecked(session, "f3");
            CommandResult jam = TapChecked(session, "f4");

            Assert.That(jam.Events.OfType<LevelJammed>(), Is.Not.Empty);
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Jammed));
        }

        [Test]
        public void LockedSlot_OpensWhenItsKeyIsCollected()
        {
            string[] rows = Rows("###f###", "###f###", "###f###", "###f###", "##mwm##", ".......");
            LevelDefinition level = Definition(
                rows,
                new[]
                {
                    Pod("f1", VariantId.Flower, 1), Pod("f2", VariantId.Flower, 1), Pod("f3", VariantId.Flower, 1), Pod("f4", VariantId.Flower, 1),
                    Pod("water", VariantId.Water, 1), Pod("moss", VariantId.Moss, 2),
                },
                overlays: new[] { Key(2, 1, "sk") }) with
            {
                Locks = new[] { new LockDef("sk", LockTargetKind.Slot, "0") },
                Slots = new SlotsDef(SlotsDef.DefaultCount, new LockedSlotDef(0, "sk")),
            };
            LevelSession session = Load(rows, level);

            TapChecked(session, "moss");
            Assert.That(session.View.SlotStateOf(0), Is.EqualTo(SlotState.Free));
            foreach (string pod in new[] { "f1", "f2", "f3", "f4" })
            {
                TapChecked(session, pod);
            }

            Assert.That(session.Status, Is.EqualTo(LevelStatus.Playing), "five usable slots now");
            TapChecked(session, "water");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void MysteryPod_RevealsItsFixedVariantOnCommit()
        {
            string[] rows = Rows("###l###", "..mmm..", ".......");
            LevelDefinition level = Definition(rows, new[] { MysteryPod("mystery", VariantId.Moss, 3), Pod("leaf", VariantId.Leaf, 1) });
            LevelSession session = Load(rows, level);
            Assert.That(session.View.Pod("mystery").Variant, Is.Null, "shown as ? + count");
            Assert.That(session.View.Pod("mystery").Remaining, Is.EqualTo(3));

            CommandResult result = TapChecked(session, "mystery");

            Assert.That(result.Events.OfType<MysteryPodRevealed>().Single(), Is.EqualTo(new MysteryPodRevealed(0, "mystery", VariantId.Moss)));
            Assert.That(result.ClearedBy("mystery").Count(), Is.EqualTo(3));
        }

        [Test]
        public void MysteryTile_RevealsWhenItBecomesReachable()
        {
            // (3,1) is reachable from the start and reveals at load; (3,2) reveals once (3,1) opens.
            string[] rows = Rows("###m###", "###l###", ".......");
            LevelDefinition level = Definition(
                rows,
                new[] { Pod("leaf", VariantId.Leaf, 1), Pod("moss", VariantId.Moss, 1) },
                overlays: new[]
                {
                    new CellOverlay(new CellPos(3, 1), Array.Empty<VariantId>(), true, false, false, null),
                    new CellOverlay(new CellPos(3, 2), Array.Empty<VariantId>(), true, false, false, null),
                });
            LevelSession session = Load(rows, level);
            Assert.That(session.View.Cell(new CellPos(3, 1)).MysteryHidden, Is.False);
            Assert.That(session.View.Cell(new CellPos(3, 2)).MysteryHidden, Is.True);
            Assert.That(session.View.Cell(new CellPos(3, 2)).Visible, Is.Null);

            TapChecked(session, "moss");
            CommandResult result = TapChecked(session, "leaf");

            GameEvent[] events = result.Events.ToArray();
            int reveal = Array.FindIndex(events, e => e is MysteryTileRevealed m && m.Cell == new CellPos(3, 2) && m.Variant == VariantId.Moss);
            int clear = Array.FindIndex(events, e => e is TileCleared c && c.Cell == new CellPos(3, 2));
            Assert.That(reveal, Is.GreaterThanOrEqualTo(0));
            Assert.That(clear, Is.GreaterThan(reveal), "the revealed tile is claimed by the waiting Moss pod");
            Assert.That(session.Status, Is.EqualTo(LevelStatus.Won));
        }

        [Test]
        public void Restart_RebuildsMechanicsState()
        {
            string[] rows = Rows("##mlw##", ".......");
            LevelDefinition level = Definition(
                rows,
                new[] { Pod("leaf", VariantId.Leaf, 1), Pod("water", VariantId.Water, 1), Locked("locked", VariantId.Moss, 1, "k1") },
                overlays: new[] { Key(3, 1, "k1") }) with
            {
                Locks = new[] { new LockDef("k1", LockTargetKind.Pod, "locked") },
            };
            LevelSession session = Load(rows, level);
            ulong start = session.StateHash;

            TapChecked(session, "leaf");
            session.Apply(new Restart());

            Assert.That(session.StateHash, Is.EqualTo(start));
            Assert.That(session.View.Pod("locked").Locked, Is.True);
            Assert.That(session.View.Cell(new CellPos(3, 1)).KeyId, Is.EqualTo("k1"));
        }
    }
}
