using System;

namespace Bloomlings.Core.Slots
{
    public enum SlotState
    {
        Free,
        Occupied,

        /// <summary>Unusable until its key is collected (FR-039).</summary>
        Locked,

        /// <summary>The sixth slot before Extra Slot is used (FR-043).</summary>
        Absent,
    }

    /// <summary>
    /// The Waiting Buffer: 5 slots by default (FR-015) plus an absent sixth one. A committed pod takes the leftmost
    /// free usable slot (FR-014) and gets a monotonic age stamp; allocation serves the oldest slot first (FR-020).
    /// Pods in slots are never reordered.
    /// </summary>
    public sealed class WaitingSlots
    {
        public const int DefaultCount = 5;
        public const int Capacity = 6;
        public const int ExtraSlotIndex = 5;

        // One array, so a clone copies once: each slot's state, then its pod (or -1), then its age stamp (or -1).
        private const int PodAt = Capacity;
        private const int AgeAt = 2 * Capacity;

        private readonly int[] _slots;
        private int _nextAge;

        /// <param name="lockedSlotIndex">A slot that starts locked, or -1 (FR-039).</param>
        public WaitingSlots(int lockedSlotIndex = -1)
        {
            _slots = new int[3 * Capacity];
            for (int i = 0; i < Capacity; i++)
            {
                _slots[i] = (int)(i < DefaultCount ? SlotState.Free : SlotState.Absent);
                _slots[PodAt + i] = -1;
                _slots[AgeAt + i] = -1;
            }

            if (lockedSlotIndex >= 0)
            {
                if (lockedSlotIndex >= DefaultCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(lockedSlotIndex), lockedSlotIndex, "Only one of the 5 default slots can be locked.");
                }

                _slots[lockedSlotIndex] = (int)SlotState.Locked;
            }
        }

        private WaitingSlots(WaitingSlots source)
        {
            _slots = (int[])source._slots.Clone();
            _nextAge = source._nextAge;
        }

        public WaitingSlots Clone() => new WaitingSlots(this);

        /// <summary>Makes these slots the same as <paramref name="source"/>, as <see cref="Clone"/> would, in place.</summary>
        internal void CopyFrom(WaitingSlots source)
        {
            Array.Copy(source._slots, _slots, _slots.Length);
            _nextAge = source._nextAge;
        }

        public SlotState StateOf(int slot) => (SlotState)_slots[slot];

        /// <summary>The pod index in the slot, or -1.</summary>
        public int PodIn(int slot) => _slots[PodAt + slot];

        public int FreeCount => Count(SlotState.Free);

        public int OccupiedCount => Count(SlotState.Occupied);

        /// <summary>Free plus occupied slots.</summary>
        public int UsableCount => FreeCount + OccupiedCount;

        /// <summary>The leftmost free usable slot, or -1.</summary>
        public int FirstFree()
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (StateOf(i) == SlotState.Free)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Places a pod in the leftmost free slot, stamps its age and returns the slot index.</summary>
        public int Commit(int pod)
        {
            int slot = FirstFree();
            if (slot < 0)
            {
                throw new InvalidOperationException("No free usable slot.");
            }

            _slots[slot] = (int)SlotState.Occupied;
            _slots[PodAt + slot] = pod;
            _slots[AgeAt + slot] = _nextAge++;
            return slot;
        }

        /// <summary>Frees a slot at once (FR-022).</summary>
        public void Release(int slot)
        {
            if (StateOf(slot) != SlotState.Occupied)
            {
                throw new InvalidOperationException($"Slot {slot} is {StateOf(slot)}, not occupied.");
            }

            _slots[slot] = (int)SlotState.Free;
            _slots[PodAt + slot] = -1;
            _slots[AgeAt + slot] = -1;
        }

        /// <summary>Makes a locked slot usable (key collected, FR-039).</summary>
        public void Unlock(int slot)
        {
            if (StateOf(slot) != SlotState.Locked)
            {
                throw new InvalidOperationException($"Slot {slot} is {StateOf(slot)}, not locked.");
            }

            _slots[slot] = (int)SlotState.Free;
        }

        /// <summary>Adds the sixth slot (Extra Slot, FR-043).</summary>
        public void AddExtra()
        {
            if (StateOf(ExtraSlotIndex) != SlotState.Absent)
            {
                throw new InvalidOperationException("The extra slot is already present.");
            }

            _slots[ExtraSlotIndex] = (int)SlotState.Free;
        }

        /// <summary>Occupied slots ordered by age, oldest first: the allocation order of FR-020.</summary>
        public int[] OccupiedByAge()
        {
            var slots = new int[OccupiedCount];
            int n = 0;
            for (int i = 0; i < Capacity; i++)
            {
                if (StateOf(i) == SlotState.Occupied)
                {
                    // Insertion sort by age; at most 6 entries.
                    int j = n++;
                    while (j > 0 && _slots[AgeAt + slots[j - 1]] > _slots[AgeAt + i])
                    {
                        slots[j] = slots[j - 1];
                        j--;
                    }

                    slots[j] = i;
                }
            }

            return slots;
        }

        /// <summary>As <see cref="OccupiedByAge()"/>, into <paramref name="slots"/> (room for <see cref="Capacity"/>); returns how many.</summary>
        internal int OccupiedByAge(int[] slots)
        {
            int n = 0;
            for (int i = 0; i < Capacity; i++)
            {
                if (StateOf(i) == SlotState.Occupied)
                {
                    // Insertion sort by age; at most 6 entries.
                    int j = n++;
                    while (j > 0 && _slots[AgeAt + slots[j - 1]] > _slots[AgeAt + i])
                    {
                        slots[j] = slots[j - 1];
                        j--;
                    }

                    slots[j] = i;
                }
            }

            return n;
        }

        /// <summary>0 for the oldest occupied slot, 1 for the next, …; -1 when the slot is not occupied.</summary>
        public int AgeRank(int slot)
        {
            if (StateOf(slot) != SlotState.Occupied)
            {
                return -1;
            }

            int rank = 0;
            int age = _slots[AgeAt + slot];
            for (int i = 0; i < Capacity; i++)
            {
                if (StateOf(i) == SlotState.Occupied && _slots[AgeAt + i] < age)
                {
                    rank++;
                }
            }

            return rank;
        }

        private int Count(SlotState state)
        {
            int count = 0;
            for (int i = 0; i < Capacity; i++)
            {
                if (_slots[i] == (int)state)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
