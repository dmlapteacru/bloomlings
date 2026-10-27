using System;
using System.Collections.Generic;

namespace Bloomlings.Core.Rounds
{
    /// <summary>The kind of a player action on a round level.</summary>
    public enum RoundActionType
    {
        Tap,
        Bridge,
    }

    /// <summary>
    /// A player action of the round rules: a tap on a pod (for a link, any member launches the whole link; legal moves
    /// name its first member) or building the Sprig bridge at one of its sites.
    /// </summary>
    public readonly struct RoundAction : IEquatable<RoundAction>
    {
        private RoundAction(RoundActionType type, int value)
        {
            Type = type;
            Value = value;
        }

        public RoundActionType Type { get; }

        /// <summary>The pod of a tap or the site index of a bridge.</summary>
        public int Value { get; }

        public int Pod => Type == RoundActionType.Tap ? Value : -1;

        public int Choice => Type == RoundActionType.Bridge ? Value : -1;

        public static RoundAction Tap(int pod) => new RoundAction(RoundActionType.Tap, pod);

        public static RoundAction BuildBridge(int choice) => new RoundAction(RoundActionType.Bridge, choice);

        public bool Equals(RoundAction other) => Type == other.Type && Value == other.Value;

        public override bool Equals(object? obj) => obj is RoundAction other && Equals(other);

        public override int GetHashCode() => ((int)Type * 397) ^ Value;

        public override string ToString() => Type == RoundActionType.Tap ? "tap pod=" + Value : "bridge choice=" + Value;
    }

    /// <summary>
    /// Why a round action is refused (reference <c>check</c> / <c>canTap</c>). The reference's reason strings are given by
    /// <see cref="RoundRules.ReasonText"/>.
    /// </summary>
    public enum RoundRefusal
    {
        None,
        Finished,
        BridgeNotReady,
        UnknownAction,
        UnknownPod,
        NotExposed,
        PartnerNotExposed,
        PairNeedsTwoSlots,
        NoSlot,

        /// <summary>A booster: the level is won.</summary>
        LevelWon,

        /// <summary>Extra Slot: already used in this attempt.</summary>
        AlreadyUsed,

        /// <summary>Return: the pod (or a member of its link) is not waiting in a place with unsent work.</summary>
        NotReturnable,

        /// <summary>Return: a courier of the pod (or of a member of its link) is still on the way.</summary>
        CourierInFlight,

        /// <summary>Shuffle: fewer than two pods in the columns may move.</summary>
        NothingToShuffle,

        /// <summary>Bloom Burst: an unknown material, or none of its pods is revealed and still to do.</summary>
        MaterialNotRevealed,

        /// <summary>Bloom Burst: no cell of the material is free of a courier.</summary>
        NoFreeCells,
    }

    /// <summary>
    /// The round rules, a line-by-line port of the reference <c>tools/rounds/engine/rules.cjs</c> (spec 006 FR-001 to
    /// FR-010, research R1, R3). Pure functions of a <see cref="RoundState"/>; every list comes out in the reference's
    /// order (column order, index order, legal taps then bridges), and no hash-ordered collection is iterated.
    /// The settled (instant) work of the reference's <c>settle</c> is not ported: round levels play on
    /// <see cref="RoundTimeline"/>, and the patient model of <see cref="RoundSolver"/> uses it too.
    /// </summary>
    public static class RoundRules
    {
        /// <summary>The default number of places.</summary>
        public const int Capacity = 5;

        /// <summary>A fresh state: reveals the top pods (depth mode) and sets the status (reference <c>create</c>).</summary>
        public static RoundState Create(RoundLevel level, int capacity = 0)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (capacity == 0)
            {
                capacity = level.Capacity > 0 ? level.Capacity : Capacity;
            }

            if (capacity < 3 || capacity > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be 3, 4 or 5.");
            }

            var state = new RoundState(level, capacity);
            RevealTops(state, null);
            state.Status = StatusOf(state);
            return state;
        }

        /// <summary>
        /// Reveals every top pod that is still hidden and not sealed (depth mode), in column order, adding a reveal event
        /// at <paramref name="at"/> for each when <paramref name="events"/> is given.
        /// </summary>
        public static void RevealTops(RoundState state, List<RoundEvent>? events, long at = 0)
        {
            for (int c = 0; c < state.Columns.Length; c++)
            {
                int pod = state.TopOf(c);
                if (pod >= 0 && !state.Revealed[pod] && state.Level.Pods[pod].Mystery != RoundMystery.Sealed)
                {
                    state.Revealed[pod] = true;
                    events?.Add(RoundEvent.Reveal(at, pod, RoundMystery.Depth));
                }
            }
        }

        /// <summary>The path from an entry to <paramref name="cell"/> (reference <c>routeTo</c>): entry first, target last.</summary>
        public static int[] RouteTo(RoundState state, int cell)
        {
            RoundReach reach = state.Reach();
            RoundPrepared meta = state.Prepared;
            if (meta.IsEntry[cell])
            {
                return new[] { cell };
            }

            // The open neighbour with the smallest distance; the first one in neighbour order wins a tie.
            int best = -1;
            for (int d = 0; d < 4; d++)
            {
                int j = meta.Neighbours[cell * 4 + d];
                if (j >= 0 && reach.Distance[j] > 0 && (best < 0 || reach.Distance[j] < reach.Distance[best]))
                {
                    best = j;
                }
            }

            if (best < 0)
            {
                throw new InvalidOperationException("No route to target.");
            }

            var path = new List<int> { cell };
            for (int i = best; i >= 0; i = reach.Parent[i])
            {
                path.Add(i);
            }

            path.Reverse();
            return path.ToArray();
        }

        /// <summary>The bridge sites that can be built now: a wall whose anchor is reached, none once a bridge stands.</summary>
        public static List<int> BridgeChoices(RoundState state)
        {
            var result = new List<int>();
            IReadOnlyList<RoundBridgeChoice>? sites = state.Level.Bridge;
            if (sites == null || state.Bridge >= 0)
            {
                return result;
            }

            RoundReach reach = state.Reach();
            for (int i = 0; i < sites.Count; i++)
            {
                if (state.Board[sites[i].Cell] == RoundLevel.Wall && reach.Distance[sites[i].Anchor] > 0)
                {
                    result.Add(i);
                }
            }

            return result;
        }

        private static bool AnyBridgeChoice(RoundState state)
        {
            IReadOnlyList<RoundBridgeChoice>? sites = state.Level.Bridge;
            if (sites == null || state.Bridge >= 0)
            {
                return false;
            }

            RoundReach reach = state.Reach();
            foreach (RoundBridgeChoice site in sites)
            {
                if (state.Board[site.Cell] == RoundLevel.Wall && reach.Distance[site.Anchor] > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The top pods in column order, empty columns skipped (reference <c>topPods</c>).</summary>
        public static List<int> TopPods(RoundState state)
        {
            var result = new List<int>(state.Columns.Length);
            for (int c = 0; c < state.Columns.Length; c++)
            {
                int pod = state.TopOf(c);
                if (pod >= 0)
                {
                    result.Add(pod);
                }
            }

            return result;
        }

        /// <summary>
        /// The settled status (reference <c>status</c>): Won when no material cell remains; Playing while a bridge can be
        /// built; Jammed when no place is free; else Playing when some top pod can be tapped, else Stuck. The timeline
        /// reports Playing while steps are pending.
        /// </summary>
        public static RoundStatus StatusOf(RoundState state)
        {
            bool anyWork = false;
            foreach (sbyte c in state.Board)
            {
                if (c >= 0)
                {
                    anyWork = true;
                    break;
                }
            }

            if (!anyWork)
            {
                return RoundStatus.Won;
            }

            if (AnyBridgeChoice(state))
            {
                return RoundStatus.Playing;
            }

            if (Array.IndexOf(state.Slots, -1) < 0)
            {
                return RoundStatus.Jammed;
            }

            foreach (int pod in TopPods(state))
            {
                if (CanTap(state, pod) == RoundRefusal.None)
                {
                    return RoundStatus.Playing;
                }
            }

            return RoundStatus.Stuck;
        }

        /// <summary>
        /// Packs the occupied places to the left, keeping their order (reference <c>compactSlots</c>). Only an accepted
        /// link admission repacks.
        /// </summary>
        public static void CompactSlots(RoundState state)
        {
            int[] slots = state.Slots;
            int k = 0;
            for (int s = 0; s < slots.Length; s++)
            {
                if (slots[s] >= 0)
                {
                    slots[k++] = slots[s];
                }
            }

            for (; k < slots.Length; k++)
            {
                slots[k] = -1;
            }
        }

        /// <summary>
        /// Whether a pod can be tapped now (reference <c>canTap</c>): it must be a top pod; each member of its link must be
        /// on top of a column, or below its column's top with only members of the same link above it; and there must be a
        /// free place for each member.
        /// </summary>
        public static RoundRefusal CanTap(RoundState state, int pod)
        {
            if (pod < 0 || pod >= state.Level.Pods.Count)
            {
                return RoundRefusal.UnknownPod;
            }

            List<int> heads = TopPods(state);
            if (!heads.Contains(pod))
            {
                return RoundRefusal.NotExposed;
            }

            int[] group = state.Prepared.Groups[pod];
            foreach (int member in group)
            {
                if (!heads.Contains(member) && !UnderOwnGroup(state, member, group))
                {
                    return RoundRefusal.PartnerNotExposed;
                }
            }

            if (state.FreeSlots < group.Length)
            {
                return group.Length > 1 ? RoundRefusal.PairNeedsTwoSlots : RoundRefusal.NoSlot;
            }

            return RoundRefusal.None;
        }

        // Some column holds the member at or below its head with only members of the group between the head and it.
        private static bool UnderOwnGroup(RoundState state, int member, int[] group)
        {
            for (int c = 0; c < state.Columns.Length; c++)
            {
                List<int> stack = state.Columns[c];
                int at = stack.IndexOf(member);
                int head = state.Heads[c];
                if (at < head)
                {
                    continue;
                }

                bool clear = true;
                for (int k = head; k < at; k++)
                {
                    if (Array.IndexOf(group, stack[k]) < 0)
                    {
                        clear = false;
                        break;
                    }
                }

                if (clear)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The legal actions (reference <c>legal</c>): none unless Playing; one tap per tappable link, named by its first
        /// member, in top-pod order; then each buildable bridge site.
        /// </summary>
        public static List<RoundAction> Legal(RoundState state)
        {
            var moves = new List<RoundAction>();
            if (state.Status != RoundStatus.Playing)
            {
                return moves;
            }

            var seen = new List<int>();
            foreach (int pod in TopPods(state))
            {
                if (CanTap(state, pod) == RoundRefusal.None)
                {
                    int first = state.Prepared.Groups[pod][0];
                    if (!seen.Contains(first))
                    {
                        seen.Add(first);
                        moves.Add(RoundAction.Tap(first));
                    }
                }
            }

            foreach (int choice in BridgeChoices(state))
            {
                moves.Add(RoundAction.BuildBridge(choice));
            }

            return moves;
        }

        /// <summary>Why an action is refused, or <see cref="RoundRefusal.None"/> (reference <c>check</c>).</summary>
        public static RoundRefusal Check(RoundState state, RoundAction action)
        {
            if (state.Status != RoundStatus.Playing)
            {
                return RoundRefusal.Finished;
            }

            if (action.Type == RoundActionType.Bridge)
            {
                return BridgeChoices(state).Contains(action.Choice) ? RoundRefusal.None : RoundRefusal.BridgeNotReady;
            }

            if (action.Type != RoundActionType.Tap)
            {
                return RoundRefusal.UnknownAction;
            }

            return CanTap(state, action.Pod);
        }

        /// <summary>The reference's reason string of a refusal ("not-exposed", "no-slot" …), or null for none.</summary>
        public static string? ReasonText(RoundRefusal refusal) => refusal switch
        {
            RoundRefusal.None => null,
            RoundRefusal.Finished => "finished",
            RoundRefusal.BridgeNotReady => "bridge-not-ready",
            RoundRefusal.UnknownAction => "unknown-action",
            RoundRefusal.UnknownPod => "unknown-pod",
            RoundRefusal.NotExposed => "not-exposed",
            RoundRefusal.PartnerNotExposed => "partner-not-exposed",
            RoundRefusal.PairNeedsTwoSlots => "pair-needs-two-slots",
            RoundRefusal.NoSlot => "no-slot",
            RoundRefusal.LevelWon => "level-won",
            RoundRefusal.AlreadyUsed => "already-used",
            RoundRefusal.NotReturnable => "not-returnable",
            RoundRefusal.CourierInFlight => "courier-in-flight",
            RoundRefusal.NothingToShuffle => "nothing-to-shuffle",
            RoundRefusal.MaterialNotRevealed => "material-not-revealed",
            RoundRefusal.NoFreeCells => "no-free-cells",
            _ => "unknown",
        };
    }
}
