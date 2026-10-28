using System;
using System.Collections.Generic;

namespace Bloomlings.Core.Rounds
{
    /// <summary>The kinds of round timeline events (contract <c>conformance-fixture.md</c>, plus the booster events).</summary>
    public enum RoundEventType
    {
        /// <summary>A pod took a place.</summary>
        Commit,

        /// <summary>A hidden pod showed its material and count (depth: on reaching the top; sealed: on commit).</summary>
        Reveal,

        /// <summary>The Sprig bridge was built.</summary>
        Bridge,

        /// <summary>A courier left the entry for its reserved cell.</summary>
        Depart,

        /// <summary>A courier reached its cell and started to act.</summary>
        Arrive,

        /// <summary>A courier's cell cleared.</summary>
        Clear,

        /// <summary>A courier came back to the entry; its job ended.</summary>
        Return,

        /// <summary>A pod's place emptied (its link released together).</summary>
        Complete,

        /// <summary>Extra Slot added a place (booster).</summary>
        ExtraSlot,

        /// <summary>Return sent a pod back to the top of its column (booster).</summary>
        Recall,

        /// <summary>Shuffle reordered the columns (booster).</summary>
        Shuffle,

        /// <summary>Bloom Burst cleared a material's free cells (booster); one <see cref="BurstClear"/> per cell follows.</summary>
        Burst,

        /// <summary>One cell cleared by Bloom Burst.</summary>
        BurstClear,
    }

    /// <summary>
    /// One event of the round timeline at model time <see cref="At"/> (µs). Which fields are meaningful depends on the
    /// type; unused ones are -1. A depart carries its courier's whole plan (arrive, clear, done and the route), so an
    /// animator can draw the walk without asking the timeline again.
    /// </summary>
    public readonly struct RoundEvent
    {
        private RoundEvent(RoundEventType type, long at, int pod = -1, int slot = -1, int cell = -1, int id = -1,
            RoundMystery mode = RoundMystery.None, long arrive = -1, long clear = -1, long done = -1, int[]? route = null,
            int column = -1, char code = '\0', int count = -1)
        {
            Type = type;
            At = at;
            Pod = pod;
            Slot = slot;
            Cell = cell;
            Id = id;
            Mode = mode;
            ArriveAt = arrive;
            ClearAt = clear;
            DoneAt = done;
            Route = route;
            Column = column;
            Code = code;
            Count = count;
        }

        public RoundEventType Type { get; }

        /// <summary>Model time in µs.</summary>
        public long At { get; }

        public int Pod { get; }

        public int Slot { get; }

        public int Cell { get; }

        /// <summary>The courier id of a depart, arrive, clear or return.</summary>
        public int Id { get; }

        /// <summary>The reveal mode of a reveal.</summary>
        public RoundMystery Mode { get; }

        public long ArriveAt { get; }

        public long ClearAt { get; }

        public long DoneAt { get; }

        /// <summary>A depart's route, entry first, target cell last (shared with the courier; do not change it).</summary>
        public IReadOnlyList<int>? Route { get; }

        /// <summary>The column of a recall.</summary>
        public int Column { get; }

        /// <summary>The material of a burst.</summary>
        public char Code { get; }

        /// <summary>The cleared cell count of a burst, or 1 for a changed shuffle (0 when it kept the order).</summary>
        public int Count { get; }

        internal static RoundEvent Commit(long at, int pod, int slot) => new RoundEvent(RoundEventType.Commit, at, pod: pod, slot: slot);

        internal static RoundEvent Reveal(long at, int pod, RoundMystery mode) => new RoundEvent(RoundEventType.Reveal, at, pod: pod, mode: mode);

        internal static RoundEvent BridgeBuilt(long at, int cell) => new RoundEvent(RoundEventType.Bridge, at, cell: cell);

        internal static RoundEvent Depart(RoundCourier job) => new RoundEvent(RoundEventType.Depart, job.Start, pod: job.Pod, cell: job.Cell,
            id: job.Id, arrive: job.Arrive, clear: job.Clear, done: job.Done, route: job.RouteArray);

        internal static RoundEvent ArriveAtCell(long at, RoundCourier job) => new RoundEvent(RoundEventType.Arrive, at, pod: job.Pod, cell: job.Cell, id: job.Id);

        internal static RoundEvent ClearCell(long at, RoundCourier job) => new RoundEvent(RoundEventType.Clear, at, pod: job.Pod, cell: job.Cell, id: job.Id);

        internal static RoundEvent ReturnHome(long at, RoundCourier job) => new RoundEvent(RoundEventType.Return, at, pod: job.Pod, cell: job.Cell, id: job.Id);

        internal static RoundEvent Complete(long at, int pod, int slot) => new RoundEvent(RoundEventType.Complete, at, pod: pod, slot: slot);

        internal static RoundEvent ExtraSlotAdded(long at, int slot) => new RoundEvent(RoundEventType.ExtraSlot, at, slot: slot);

        internal static RoundEvent Recall(long at, int pod, int slot, int column) => new RoundEvent(RoundEventType.Recall, at, pod: pod, slot: slot, column: column);

        internal static RoundEvent Shuffled(long at, bool changed) => new RoundEvent(RoundEventType.Shuffle, at, count: changed ? 1 : 0);

        internal static RoundEvent Burst(long at, char code, int count) => new RoundEvent(RoundEventType.Burst, at, code: code, count: count);

        internal static RoundEvent BurstCell(long at, int cell) => new RoundEvent(RoundEventType.BurstClear, at, cell: cell);

        /// <summary>The canonical line of this event (<see cref="RoundEvents.Line"/>).</summary>
        public override string ToString() => RoundEvents.Line(this);
    }

    /// <summary>
    /// A courier of the round timeline (reference <c>timeline.cjs</c> job): it left at <see cref="Start"/>, reaches its
    /// reserved cell at <see cref="Arrive"/>, clears it at <see cref="Clear"/> and is back at <see cref="Done"/>, walking
    /// <see cref="Route"/> (entry first) out and back.
    /// </summary>
    public sealed class RoundCourier
    {
        internal RoundCourier(int id, int pod, int cell, long start, long arrive, long clear, long done, int[] route, int frontier)
        {
            Id = id;
            Pod = pod;
            Cell = cell;
            Start = start;
            Arrive = arrive;
            Clear = clear;
            Done = done;
            RouteArray = route;
            Frontier = frontier;
        }

        public int Id { get; }

        public int Pod { get; }

        public int Cell { get; }

        public long Start { get; }

        public long Arrive { get; }

        public long Clear { get; }

        public long Done { get; }

        public IReadOnlyList<int> Route => RouteArray;

        /// <summary>How many cells of its material were free on the frontier when it left (itself included).</summary>
        public int Frontier { get; }

        internal int[] RouteArray { get; }

        internal RoundCourier Copy() => new RoundCourier(Id, Pod, Cell, Start, Arrive, Clear, Done, (int[])RouteArray.Clone(), Frontier);
    }

    /// <summary>The kinds of scheduled steps, in the order they run within one moment (research R3).</summary>
    public enum RoundStepType
    {
        Clear = 0,
        Arrive = 1,
        Return = 2,
        Release = 3,
        Check = 4,
    }

    /// <summary>
    /// A scheduled step of the timeline: a courier's arrive, clear or return (by courier id), a link's release (by its
    /// first pod) or a pod's departure check (by pod). Steps run by time, then type, then id or pod.
    /// </summary>
    public readonly struct RoundStep : IComparable<RoundStep>
    {
        public RoundStep(RoundStepType type, long at, int key)
        {
            Type = type;
            At = at;
            Key = key;
        }

        public RoundStepType Type { get; }

        public long At { get; }

        /// <summary>The courier id (arrive, clear, return) or the pod (release, check).</summary>
        public int Key { get; }

        public int CompareTo(RoundStep other)
        {
            if (At != other.At)
            {
                return At < other.At ? -1 : 1;
            }

            if (Type != other.Type)
            {
                return ((int)Type).CompareTo((int)other.Type);
            }

            return Key.CompareTo(other.Key);
        }

        /// <summary>The step's word in the extended signature: clear, arrive, return, release, check.</summary>
        public static string TypeText(RoundStepType type) => type switch
        {
            RoundStepType.Clear => "clear",
            RoundStepType.Arrive => "arrive",
            RoundStepType.Return => "return",
            RoundStepType.Release => "release",
            _ => "check",
        };

        /// <summary>A courier step (arrive, return), not one that changes the rules state.</summary>
        public bool IsCourierMove => Type == RoundStepType.Arrive || Type == RoundStepType.Return;

        public override string ToString() => At + ":" + TypeText(Type) + ":" + Key;
    }
}
