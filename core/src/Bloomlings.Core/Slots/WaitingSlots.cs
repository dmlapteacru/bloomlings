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

        private readonly SlotState[] _state;
        private readonly int[] _pod;
        private readonly int[] _age;
        private int _nextAge;

        /// <param name="lockedSlotIndex">A slot that starts locked, or -1 (FR-039).</param>
        public WaitingSlots(int lockedSlotIndex = -1)
        {
            _state = new SlotState[Capacity];
            _pod = new int[Capacity];
            _age = new int[Capacity];
            for (int i = 0; i < Capacity; i++)
            {
                _state[i] = i < DefaultCount ? SlotState.Free : SlotState.Absent;
                _pod[i] = -1;
                _age[i] = -1;
            }

            if (lockedSlotIndex >= 0)
            {
                if (lockedSlotIndex >= DefaultCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(lockedSlotIndex), lockedSlotIndex, "Only one of the 5 default slots can be locked.");
                }

                _state[lockedSlotIndex] = SlotState.Locked;
            }
        }

        private WaitingSlots(WaitingSlots source)
        {
            _state = (SlotState[])source._state.Clone();
            _pod = (int[])source._pod.Clone();
            _age = (int[])source._age.Clone();
            _nextAge = source._nextAge;
        }

        public WaitingSlots Clone() => new WaitingSlots(this);

        public SlotState StateOf(int slot) => _state[slot];

        /// <summary>The pod index in the slot, or -1.</summary>
        public int PodIn(int slot) => _pod[slot];

        public int FreeCount => Count(SlotState.Free);

        public int OccupiedCount => Count(SlotState.Occupied);

        /// <summary>Free plus occupied slots.</summary>
        public int UsableCount => FreeCount + OccupiedCount;

        /// <summary>The leftmost free usable slot, or -1.</summary>
        public int FirstFree()
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (_state[i] == SlotState.Free)
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

            _state[slot] = SlotState.Occupied;
            _pod[slot] = pod;
            _age[slot] = _nextAge++;
            return slot;
        }

        /// <summary>Frees a slot at once (FR-022).</summary>
        public void Release(int slot)
        {
            if (_state[slot] != SlotState.Occupied)
            {
                throw new InvalidOperationException($"Slot {slot} is {_state[slot]}, not occupied.");
            }

            _state[slot] = SlotState.Free;
            _pod[slot] = -1;
            _age[slot] = -1;
        }

        /// <summary>Makes a locked slot usable (key collected, FR-039).</summary>
        public void Unlock(int slot)
        {
            if (_state[slot] != SlotState.Locked)
            {
                throw new InvalidOperationException($"Slot {slot} is {_state[slot]}, not locked.");
            }

            _state[slot] = SlotState.Free;
        }

        /// <summary>Adds the sixth slot (Extra Slot, FR-043).</summary>
        public void AddExtra()
        {
            if (_state[ExtraSlotIndex] != SlotState.Absent)
            {
                throw new InvalidOperationException("The extra slot is already present.");
            }

            _state[ExtraSlotIndex] = SlotState.Free;
        }

        /// <summary>Occupied slots ordered by age, oldest first: the allocation order of FR-020.</summary>
        public int[] OccupiedByAge()
        {
            var slots = new int[OccupiedCount];
            int n = 0;
            for (int i = 0; i < Capacity; i++)
            {
                if (_state[i] == SlotState.Occupied)
                {
                    // Insertion sort by age; at most 6 entries.
                    int j = n++;
                    while (j > 0 && _age[slots[j - 1]] > _age[i])
                    {
                        slots[j] = slots[j - 1];
                        j--;
                    }

                    slots[j] = i;
                }
            }

            return slots;
        }

        /// <summary>0 for the oldest occupied slot, 1 for the next, …; -1 when the slot is not occupied.</summary>
        public int AgeRank(int slot)
        {
            if (_state[slot] != SlotState.Occupied)
            {
                return -1;
            }

            int rank = 0;
            for (int i = 0; i < Capacity; i++)
            {
                if (_state[i] == SlotState.Occupied && _age[i] < _age[slot])
                {
                    rank++;
                }
            }

            return rank;
        }

        private int Count(SlotState state)
        {
            int count = 0;
            foreach (SlotState s in _state)
            {
                if (s == state)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
