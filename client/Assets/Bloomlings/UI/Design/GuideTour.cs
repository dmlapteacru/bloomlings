using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>What a guided step shows in its spotlight (spec 005 FR-035).</summary>
    public enum GuideKind
    {
        /// <summary>The Garden Entry's arch: "Bloomlings come in through this gate" (Level 1, once).</summary>
        Entry,

        /// <summary>The tiles in front of the entry that keep the others out of reach (the first level that starts so, once).</summary>
        Blocked,

        /// <summary>Level 1's guided first tap on one pod (spec 001 US1).</summary>
        FirstTap,

        /// <summary>A booster's button at its unlock: the player must tap it, and this use is free (spec 001 FR-042).</summary>
        Booster,

        /// <summary>Return's slot or Bloom Burst's tiles: the player must tap one to finish the free use.</summary>
        BoosterTarget,

        /// <summary>After the free use: the free charge of the unlock is still there for later.</summary>
        BoosterKept,
    }

    /// <summary>One step of a guided spotlight: what it lights, its message keys, and whether only the lit place takes the tap.</summary>
    public sealed class GuideStep
    {
        public GuideStep(GuideKind kind, string demoId, BoosterKind? booster, bool forced, params string[] messageKeys)
        {
            Kind = kind;
            DemoId = demoId;
            Booster = booster;
            Forced = forced;
            MessageKeys = messageKeys;
        }

        public GuideKind Kind { get; }

        /// <summary>Stored in the save's seen demos once the step's demo is done, so it never shows again.</summary>
        public string DemoId { get; }

        public BoosterKind? Booster { get; }

        /// <summary>
        /// A forced step takes no tap but the one on its lit place (the owner, 2026-10-05: "make the player tap the
        /// item"); any other step goes on with a tap anywhere.
        /// </summary>
        public bool Forced { get; }

        /// <summary>The message lines (<c>Strings_en.csv</c> keys): the first in the strong style, the next in the soft one.</summary>
        public IReadOnlyList<string> MessageKeys { get; }
    }

    /// <summary>
    /// The guided spotlights of the onboarding (spec 005 FR-035; the owner, 2026-10-05): which steps a level shows, in
    /// both builds. Hosts draw each step with <see cref="Spotlight"/> and run it; this only decides.
    /// <list type="bullet">
    /// <item><description>Level 1: the Garden Entry's arch (tap to continue), then the forced first tap.</description></item>
    /// <item><description>The first level (from Level 2) where an exposed pod's variant has no reachable tile at the start:
    /// the tiles in front of the entry that block the way (<see cref="BlockingCells"/>; Level 2 in the launch
    /// content).</description></item>
    /// <item><description>Each booster at its unlock: its button lit, the player must tap it (and for Return a slot, for
    /// Bloom Burst a tile), and that use is free, so the unlock's free charge stays (spec 001 FR-042). Extra Slot,
    /// Shuffle and Bloom Burst start as the level opens; Return waits until a pod waits in a slot (Level 6: after the
    /// first tap), as it has nothing to send back before.</description></item>
    /// </list>
    /// Never in the Daily Challenge. Engine-free.
    /// </summary>
    public static class GuideTour
    {
        public const string EntryId = "demo.entry";

        public const string BlockedId = "demo.entry_blocked";

        /// <summary>The Level 1 guided first tap: its id is the roadmap's core-play unlock.</summary>
        public const string FirstTapId = "system.core";

        /// <summary>Every guided demo's id: the entry, the blocked entry, the first tap and the four boosters.</summary>
        public static IReadOnlyList<string> DemoIds { get; } = new[]
        {
            EntryId, BlockedId, FirstTapId, "booster.extra_slot", "booster.shuffle", "booster.return", "booster.bloom_burst",
        };

        /// <summary>The boosters whose demo starts as their level opens, in roadmap order (Return waits, <see cref="WhenSettled"/>).</summary>
        private static readonly BoosterKind[] AtStartBoosters = { BoosterKind.ExtraSlot, BoosterKind.Shuffle, BoosterKind.BloomBurst };

        /// <summary>A booster's demo id: its roadmap unlock id (<c>booster.extra_slot</c>, …).</summary>
        public static string BoosterId(BoosterKind kind) => "booster." + Key(kind);

        /// <summary>A booster's key in strings and icons: <c>extra_slot</c>, <c>shuffle</c>, <c>return</c>, <c>bloom_burst</c>.</summary>
        public static string Key(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => "extra_slot",
            BoosterKind.Shuffle => "shuffle",
            BoosterKind.Return => "return",
            _ => "bloom_burst",
        };

        /// <summary>
        /// The steps before play on <paramref name="level"/>: Level 1's entry and first tap, else a booster's demo, else the
        /// blocked-entry spotlight, else none (the other demos keep their cards).
        /// </summary>
        public static IReadOnlyList<GuideStep> AtStart(int level, bool daily, Func<string, bool> seen, Func<BoosterKind, bool> unlocked, LevelSession session)
        {
            if (daily || session.Status != LevelStatus.Playing)
            {
                return Array.Empty<GuideStep>();
            }

            if (level == 1 && !seen(FirstTapId))
            {
                var steps = new List<GuideStep>();
                if (!seen(EntryId))
                {
                    steps.Add(new GuideStep(GuideKind.Entry, EntryId, null, false, "demo.entry"));
                }

                if (FirstTapPod(session) != null)
                {
                    steps.Add(new GuideStep(GuideKind.FirstTap, FirstTapId, null, true, "demo.first_tap"));
                }

                return steps;
            }

            foreach (BoosterKind kind in AtStartBoosters)
            {
                if (unlocked(kind) && !seen(BoosterId(kind)) && CanUse(session, kind))
                {
                    return BoosterSteps(kind);
                }
            }

            if (level >= 2 && !seen(BlockedId) && BlockedStart(session.View))
            {
                return new[] { new GuideStep(GuideKind.Blocked, BlockedId, null, false, "demo.entry_blocked.1", "demo.entry_blocked.2") };
            }

            return Array.Empty<GuideStep>();
        }

        /// <summary>
        /// The steps once the board has settled after a command: Return's demo the first time a pod waits in a slot after
        /// Return's unlock (Level 6: right after the first tap). Empty otherwise.
        /// </summary>
        public static IReadOnlyList<GuideStep> WhenSettled(bool daily, Func<string, bool> seen, Func<BoosterKind, bool> unlocked, LevelSession session)
        {
            if (daily || session.Status != LevelStatus.Playing || !unlocked(BoosterKind.Return) || seen(BoosterId(BoosterKind.Return)) || ReturnSlot(session) < 0)
            {
                return Array.Empty<GuideStep>();
            }

            return BoosterSteps(BoosterKind.Return);
        }

        /// <summary>A booster's demo: tap its button (free), then for Return a slot and for Bloom Burst a tile, then "it stays".</summary>
        public static IReadOnlyList<GuideStep> BoosterSteps(BoosterKind kind)
        {
            string id = BoosterId(kind);
            string key = Key(kind);
            var steps = new List<GuideStep> { new GuideStep(GuideKind.Booster, id, kind, true, "demo." + key + ".1", "demo.try_free") };
            if (kind == BoosterKind.Return || kind == BoosterKind.BloomBurst)
            {
                steps.Add(new GuideStep(GuideKind.BoosterTarget, id, kind, true, "demo." + key + ".2"));
            }

            steps.Add(new GuideStep(GuideKind.BoosterKept, id, kind, false, "demo.booster_kept"));
            return steps;
        }

        /// <summary>Whether the booster would do something now (its forced demo needs a real effect, spec 001 FR-046).</summary>
        public static bool CanUse(LevelSession session, BoosterKind kind)
        {
            switch (kind)
            {
                case BoosterKind.ExtraSlot:
                    return session.Check(new UseExtraSlot()).IsAllowed;
                case BoosterKind.Shuffle:
                    return session.Check(new UseShuffle()).IsAllowed;
                case BoosterKind.Return:
                    return ReturnSlot(session) >= 0;
                default:
                    return BurstVariants(session).Count > 0;
            }
        }

        /// <summary>The first slot whose pod Return can send back now, or -1.</summary>
        public static int ReturnSlot(LevelSession session)
        {
            for (int slot = 0; slot < session.View.SlotCapacity; slot++)
            {
                if (session.Check(new UseReturn(slot)).IsAllowed)
                {
                    return slot;
                }
            }

            return -1;
        }

        /// <summary>The variants Bloom Burst can take now (each visible on the board).</summary>
        public static IReadOnlyList<VariantId> BurstVariants(LevelSession session)
        {
            var variants = new List<VariantId>();
            LevelView view = session.View;
            for (int y = 0; y < view.Height; y++)
            {
                for (int x = 0; x < view.Width; x++)
                {
                    VariantId? v = view.Cell(new CellPos(x, y)).Visible;
                    if (v.HasValue && !variants.Contains(v.Value) && session.Check(new UseBloomBurst(v.Value)).IsAllowed)
                    {
                        variants.Add(v.Value);
                    }
                }
            }

            return variants;
        }

        /// <summary>An exposed pod that clears tiles at once: the guided first tap's pod (null when none does).</summary>
        public static string? FirstTapPod(LevelSession session)
        {
            foreach (string id in session.View.PodIds)
            {
                if (!session.View.IsExposed(id))
                {
                    continue;
                }

                LevelSession probe = session.Clone();
                foreach (GameEvent e in probe.Apply(new TapPod(id)).Events)
                {
                    if (e is TileCleared)
                    {
                        return id;
                    }
                }
            }

            return null;
        }

        /// <summary>Whether an exposed pod's variant has no tile Bloomlings can reach now: the entry is blocked for it.</summary>
        public static bool BlockedStart(LevelView view)
        {
            var reachable = new HashSet<VariantId>();
            foreach (CellPos cell in view.ReachableTargets())
            {
                VariantId? v = view.Cell(cell).Visible;
                if (v.HasValue)
                {
                    reachable.Add(v.Value);
                }
            }

            if (reachable.Count == 0)
            {
                return false;
            }

            foreach (string id in view.PodIds)
            {
                PodInfo pod = view.Pod(id);
                if (view.IsExposed(id) && pod.Variant.HasValue && !reachable.Contains(pod.Variant.Value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The tiles in front of the entry that keep the others out of reach: every reachable tile and the tiles of its
        /// variant joined to it side by side (Level 5: the whole pot of logs).
        /// </summary>
        public static IReadOnlyList<CellPos> BlockingCells(LevelView view)
        {
            var cells = new List<CellPos>();
            var seen = new HashSet<CellPos>();
            var queue = new Queue<CellPos>();
            foreach (CellPos start in view.ReachableTargets())
            {
                if (seen.Add(start))
                {
                    queue.Enqueue(start);
                }
            }

            while (queue.Count > 0)
            {
                CellPos cell = queue.Dequeue();
                cells.Add(cell);
                VariantId? v = view.Cell(cell).Visible;
                if (!v.HasValue)
                {
                    continue;
                }

                foreach ((int dx, int dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int x = cell.X + dx;
                    int y = cell.Y + dy;
                    if (x < 0 || y < 0 || x >= view.Width || y >= view.Height)
                    {
                        continue;
                    }

                    var next = new CellPos(x, y);
                    if (!seen.Contains(next) && view.Cell(next).Visible == v)
                    {
                        seen.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }

            return cells;
        }

        /// <summary>The variant of the tiles that block the way (the blocked spotlight's icon): the nearest reachable tile's.</summary>
        public static VariantId? BlockingVariant(LevelView view)
        {
            foreach (CellPos cell in view.ReachableTargets())
            {
                VariantId? v = view.Cell(cell).Visible;
                if (v.HasValue)
                {
                    return v;
                }
            }

            return null;
        }
    }
}
