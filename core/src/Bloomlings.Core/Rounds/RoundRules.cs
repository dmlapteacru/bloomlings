using System;
using System.Collections.Generic;

namespace Bloomlings.Core.Rounds
{
    /// <summary>The kind of a player action on a round level.</summary>
    public enum RoundActionType
    {
        Tap,
        Bridge,

        /// <summary>Choosing an exit of the water channel (rules 17 §8 «Drop Водный канал», 2026-11-05).</summary>
        Channel,
    }

    /// <summary>
    /// A player action of the round rules: a tap on a pod (for a link, any member launches the whole link; legal moves
    /// name its first member), building the Sprig bridge at one of its sites, or choosing an exit of the Drop water channel.
    /// </summary>
    public readonly struct RoundAction : IEquatable<RoundAction>
    {
        private RoundAction(RoundActionType type, int value)
        {
            Type = type;
            Value = value;
        }

        public RoundActionType Type { get; }

        /// <summary>The pod of a tap, the site index of a bridge or the exit index of a channel choice.</summary>
        public int Value { get; }

        public int Pod => Type == RoundActionType.Tap ? Value : -1;

        public int Choice => Type == RoundActionType.Bridge ? Value : -1;

        /// <summary>The exit of a channel choice, else -1.</summary>
        public int Exit => Type == RoundActionType.Channel ? Value : -1;

        public static RoundAction Tap(int pod) => new RoundAction(RoundActionType.Tap, pod);

        public static RoundAction BuildBridge(int choice) => new RoundAction(RoundActionType.Bridge, choice);

        /// <summary>Choosing exit <paramref name="exit"/> of the water channel (reference <c>{type:'channel', exit}</c>).</summary>
        public static RoundAction ChooseExit(int exit) => new RoundAction(RoundActionType.Channel, exit);

        public bool Equals(RoundAction other) => Type == other.Type && Value == other.Value;

        public override bool Equals(object? obj) => obj is RoundAction other && Equals(other);

        public override int GetHashCode() => ((int)Type * 397) ^ Value;

        public override string ToString() => Type == RoundActionType.Tap ? "tap pod=" + Value : Type == RoundActionType.Bridge ? "bridge choice=" + Value : "channel exit=" + Value;
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

        /// <summary>The channel cannot be chosen: its spring is not reached yet, an exit is already chosen, or no such exit.</summary>
        ChannelNotReady,
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

        /// <summary>
        /// A fresh state: opens the latches already open at the start (silently), reveals the top pods (depth mode), the
        /// hidden cells already on the start front (silently: part of the start state) and sets the status (reference
        /// <c>create</c>).
        /// </summary>
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

            // Shutters (17 §8, 2026-11-05): all closed at the start (each counts at least 1), before any reach is taken.
            SyncShutters(state);

            // A latch on top at the start whose key is already reached starts open (before the tops reveal).
            SyncLatches(state);
            RevealTops(state, null);
            SyncCells(state);
            SyncChannel(state);
            state.Status = StatusOf(state);
            return state;
        }

        /// <summary>
        /// Reveals every veiled cell that is a target now (reference <c>revealCells</c>, rules 17 §8 «Скрытые клетки»): a
        /// hidden cell reveals as soon as it has an orthogonal neighbour that is open and reached from an entry, or is itself
        /// an entry cell. Called after every board change (a clear, a bridge, a burst); the cells come in increasing cell
        /// order, each as a <see cref="RoundEventType.CellReveal"/> at <paramref name="at"/> when <paramref name="events"/>
        /// is given. Open cells never close, so a target stays a target until cleared: the reveal changes no other rule.
        /// </summary>
        public static void RevealCells(RoundState state, List<RoundEvent>? events, long at = 0)
        {
            bool[]? veiled = state.Veiled;
            if (veiled == null || state.VeiledLeft == 0)
            {
                return;
            }

            IReadOnlyList<int> targets = state.Reach().Targets;
            List<int>? found = null;
            foreach (int i in targets)
            {
                if (veiled[i])
                {
                    (found ??= new List<int>()).Add(i);
                }
            }

            if (found == null)
            {
                return;
            }

            found.Sort();
            foreach (int i in found)
            {
                veiled[i] = false;
                state.VeiledLeft--;
                events?.Add(RoundEvent.CellRevealed(at, i));
            }
        }

        /// <summary>
        /// Recomputes the veiled cells from the board (reference <c>syncCells</c>; a fresh state or a restored snapshot):
        /// every hidden material cell, then the silent reveal of the targets among them.
        /// </summary>
        public static void SyncCells(RoundState state)
        {
            bool[]? hidden = state.Prepared.Hidden;
            if (hidden == null)
            {
                state.Veiled = null;
                state.VeiledLeft = 0;
                return;
            }

            var veiled = new bool[hidden.Length];
            int left = 0;
            for (int i = 0; i < hidden.Length; i++)
            {
                if (hidden[i] && state.Board[i] >= 0)
                {
                    veiled[i] = true;
                    left++;
                }
            }

            state.Veiled = veiled;
            state.VeiledLeft = left;
            RevealCells(state, null);
        }

        /// <summary>
        /// Opens every closed latch that is its column's top with its key cell reached by an open path from an entry
        /// (reference <c>openLatches</c>, rules 17 §8 «Засов и жёлудь-ключ»), in increasing latch order, each as a
        /// <see cref="RoundEventType.LatchOpen"/> at <paramref name="at"/> when <paramref name="events"/> is given. Called
        /// after every change that can make it true: a tap's commits, a bridge, a clear and a burst (each after its cell
        /// reveals). Opening changes no reach, and a column's next latch stands at least one pod deeper, so one pass finds
        /// every latch that opens. Returns how many opened; the caller then reveals the tops they exposed.
        /// </summary>
        public static int OpenLatches(RoundState state, List<RoundEvent>? events, long at = 0)
        {
            bool[]? latched = state.Latched;
            if (latched == null || state.LatchedLeft == 0)
            {
                return 0;
            }

            IReadOnlyList<RoundLatch> latches = state.Level.Latches;
            IReadOnlyList<int>? distance = null;
            int opened = 0;
            for (int i = 0; i < latches.Count; i++)
            {
                RoundLatch latch = latches[i];
                if (!latched[i] || state.Heads[latch.Column] != latch.Depth)
                {
                    continue;
                }

                distance ??= state.Reach().Distance;
                if (distance[latch.Key] > 0)
                {
                    latched[i] = false;
                    state.LatchedLeft--;
                    opened++;
                    events?.Add(RoundEvent.LatchOpened(at, i, latch));
                }
            }

            return opened;
        }

        /// <summary>
        /// Recomputes the latches from the heads and the board (reference <c>syncLatches</c>; a fresh state or a restored
        /// snapshot): a latch its column's head has passed is open, the rest closed, then the opening rule silently. A latch
        /// already open in <paramref name="before"/> (a snapshot's <see cref="RoundState.Latched"/>) stays open too: latches
        /// never close, and only Return moves a head back onto an open latch (a booster the reference does not play; without
        /// boosters <paramref name="before"/> changes nothing).
        /// </summary>
        public static void SyncLatches(RoundState state, bool[]? before = null)
        {
            IReadOnlyList<RoundLatch> latches = state.Level.Latches;
            if (latches.Count == 0)
            {
                state.Latched = null;
                state.LatchedLeft = 0;
                return;
            }

            var latched = new bool[latches.Count];
            int left = 0;
            for (int i = 0; i < latches.Count; i++)
            {
                bool passed = state.Heads[latches[i].Column] > latches[i].Depth || (before != null && i < before.Length && !before[i]);
                latched[i] = !passed;
                if (!passed)
                {
                    left++;
                }
            }

            state.Latched = latched;
            state.LatchedLeft = left;
            OpenLatches(state, null);
        }

        /// <summary>
        /// Opens every closed shutter whose count is reached (reference <c>openShutters</c>, rules 17 §8 «Twig Деревянные
        /// створки»): the work done of its material (its start cells minus its cells now) is at least its count. Called
        /// after every change of the board's material cells (a clear, a burst), first of all, before the cell reveals; the
        /// shutters come in increasing order, each as a <see cref="RoundEventType.ShutterOpen"/> at <paramref name="at"/>
        /// when <paramref name="events"/> is given. Their cells become targets (the cached reach is dropped). Returns how
        /// many opened.
        /// </summary>
        public static int OpenShutters(RoundState state, List<RoundEvent>? events, long at = 0)
        {
            bool[]? shut = state.Shut;
            if (shut == null || state.ShutLeft == 0)
            {
                return 0;
            }

            int[]? left = null;
            int opened = 0;
            IReadOnlyList<RoundShutter> shutters = state.Level.Shutters;
            for (int i = 0; i < shutters.Count; i++)
            {
                if (!shut[i])
                {
                    continue;
                }

                left ??= MaterialLeft(state);
                int material = state.Prepared.ShutMaterial![i];
                if (state.Prepared.ShutStart![material] - left[material] >= shutters[i].Count)
                {
                    shut[i] = false;
                    state.ShutLeft--;
                    opened++;
                    events?.Add(RoundEvent.ShutterOpened(at, i));
                }
            }

            if (opened > 0)
            {
                state.BoardChanged();
            }

            return opened;
        }

        /// <summary>Recomputes the shutters from the board (reference <c>syncShutters</c>; a fresh state or a restored snapshot), silently.</summary>
        public static void SyncShutters(RoundState state)
        {
            IReadOnlyList<RoundShutter> shutters = state.Level.Shutters;
            if (shutters.Count == 0)
            {
                state.Shut = null;
                state.ShutLeft = 0;
                return;
            }

            int[] left = MaterialLeft(state);
            var shut = new bool[shutters.Count];
            int closed = 0;
            for (int i = 0; i < shutters.Count; i++)
            {
                int material = state.Prepared.ShutMaterial![i];
                shut[i] = state.Prepared.ShutStart![material] - left[material] < shutters[i].Count;
                if (shut[i])
                {
                    closed++;
                }
            }

            state.Shut = shut;
            state.ShutLeft = closed;
            state.BoardChanged();
        }

        private static int[] MaterialLeft(RoundState state)
        {
            var left = new int[RoundMaterial.Codes.Length];
            foreach (sbyte c in state.Board)
            {
                if (c >= 0)
                {
                    left[c]++;
                }
            }

            return left;
        }

        /// <summary>
        /// The exits that can be chosen now (reference <c>channelChoices</c>, rules 17 §8 «Drop Водный канал»): both, once
        /// the spring is reached by an open path from an active entry and no exit is chosen yet; else none.
        /// </summary>
        public static List<int> ChannelChoices(RoundState state)
        {
            var result = new List<int>();
            RoundChannel? channel = state.Level.Channel;
            if (channel == null || state.Channel >= 0 || !(state.Reach().Distance[channel.Spring] > 0))
            {
                return result;
            }

            for (int i = 0; i < channel.Exits.Count; i++)
            {
                result.Add(i);
            }

            return result;
        }

        private static bool AnyChannelChoice(RoundState state)
        {
            RoundChannel? channel = state.Level.Channel;
            return channel != null && state.Channel < 0 && state.Reach().Distance[channel.Spring] > 0;
        }

        /// <summary>
        /// Marks the spring reached the first time it is (reference <c>readyChannel</c>), adding a
        /// <see cref="RoundEventType.ChannelReady"/> at <paramref name="at"/> when <paramref name="events"/> is given and no
        /// exit is chosen. Called after every board change (a clear, a bridge, a burst), after its cell reveals and latches.
        /// </summary>
        public static bool ReadyChannel(RoundState state, List<RoundEvent>? events, long at = 0)
        {
            RoundChannel? channel = state.Level.Channel;
            if (channel == null || state.SpringReached || !(state.Reach().Distance[channel.Spring] > 0))
            {
                return false;
            }

            state.SpringReached = true;
            if (state.Channel < 0)
            {
                events?.Add(RoundEvent.ChannelReadied(at));
            }

            return true;
        }

        /// <summary>Recomputes <see cref="RoundState.SpringReached"/> from the board and the choice (reference <c>syncChannel</c>), silently.</summary>
        public static void SyncChannel(RoundState state)
        {
            if (state.Level.Channel == null)
            {
                return;
            }

            state.SpringReached = false;
            ReadyChannel(state, null);
        }

        /// <summary>
        /// The ride of a walker on <paramref name="route"/> (reference <c>rideOf</c>): the chosen exit's ride when the route
        /// starts at the chosen exit, else 0.
        /// </summary>
        public static long RideOf(RoundState state, int[] route)
        {
            RoundChannel? channel = state.Level.Channel;
            if (channel == null || state.Channel < 0)
            {
                return 0;
            }

            RoundChannelExit exit = channel.Exits[state.Channel];
            return route.Length > 0 && route[0] == exit.Cell ? exit.RideUs : 0;
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

        /// <summary>
        /// The top pods in column order, empty columns and columns whose top is a closed latch skipped (reference
        /// <c>topPods</c>).
        /// </summary>
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
        /// built or a channel exit chosen; Jammed when no place is free; else Playing when some top pod can be tapped, else Stuck. The timeline
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

            if (AnyChannelChoice(state))
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
        /// on top of a column, or below its column's top with only members of the same link above it and no closed latch
        /// before it; and there must be a free place for each member.
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

                // A partner behind a closed latch is not exposed (17 §8 «Засов и жёлудь-ключ»).
                if (at < head || at >= state.BlockAt(c))
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
        /// member, in top-pod order; then each buildable bridge site; then each channel exit once the channel is ready.
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

            foreach (int exit in ChannelChoices(state))
            {
                moves.Add(RoundAction.ChooseExit(exit));
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

            if (action.Type == RoundActionType.Channel)
            {
                return ChannelChoices(state).Contains(action.Exit) ? RoundRefusal.None : RoundRefusal.ChannelNotReady;
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
            RoundRefusal.ChannelNotReady => "channel-not-ready",
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
