using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;
using Bloomlings.Core.Progression;

namespace Bloomlings.Client.App.Progression
{
    /// <summary>
    /// Linear progression (FR-057, T060): the current level is always the highest completed level + 1. A win advances
    /// it and saves at once; every roadmap unlock passed on the way raises <see cref="UnlockReached"/> exactly once
    /// (FR-031), and its flag is stored in the save. Demos are due on the unlock level or within its 0–2 level window.
    /// </summary>
    public sealed class ProgressionService
    {
        private readonly PlayerSave _save;
        private readonly UnlockRoadmap _roadmap;
        private readonly Action _persist;

        public ProgressionService(PlayerSave save, UnlockRoadmap roadmap, Action persist)
        {
            _save = save;
            _roadmap = roadmap;
            _persist = persist;
        }

        public event Action<UnlockEntry>? UnlockReached;

        public UnlockRoadmap Roadmap => _roadmap;

        public int HighestCompletedLevel => _save.Progression.HighestCompletedLevel;

        public int CurrentLevel => _save.Progression.CurrentLevel;

        /// <summary>Raises the unlocks already reached but not yet recorded (Level 1 on first launch).</summary>
        public void Initialize()
        {
            if (RaiseUnlocks(0, CurrentLevel))
            {
                _persist();
            }
        }

        /// <summary>Records a win. Only the current level can be completed; returns false otherwise.</summary>
        public bool CompleteLevel(int level)
        {
            if (level != CurrentLevel)
            {
                return false;
            }

            int before = CurrentLevel;
            _save.Progression.HighestCompletedLevel = level;
            _save.Stats.Increment("levelsWon");
            RaiseUnlocks(before, CurrentLevel);
            _persist();
            return true;
        }

        public bool IsUnlocked(string unlockId) => _save.Unlocks.IsSet(unlockId);

        /// <summary>
        /// The first unlock whose demo should play on <paramref name="level"/>: reached, not yet seen, and
        /// <paramref name="level"/> lies within [unlock level, unlock level + demoWithin] (FR-031).
        /// </summary>
        public UnlockEntry? DemoDue(int level)
        {
            foreach (UnlockEntry entry in _roadmap.Entries)
            {
                if (entry.Level > level)
                {
                    break;
                }

                if (IsUnlocked(entry.UnlockId) && !_save.Unlocks.HasSeenDemo(entry.UnlockId) && level <= entry.Level + entry.DemoWithin)
                {
                    return entry;
                }
            }

            return null;
        }

        public bool HasSeenDemo(string demoId) => _save.Unlocks.HasSeenDemo(demoId);

        public void MarkDemoSeen(string demoId)
        {
            if (_save.Unlocks.MarkDemoSeen(demoId))
            {
                _persist();
            }
        }

        /// <summary>Editor tool: completes every level up to <paramref name="highestCompleted"/>, firing each unlock.</summary>
        public IReadOnlyList<UnlockEntry> FastForward(int highestCompleted)
        {
            var reached = new List<UnlockEntry>();
            void Collect(UnlockEntry entry) => reached.Add(entry);
            UnlockReached += Collect;
            try
            {
                while (HighestCompletedLevel < highestCompleted)
                {
                    CompleteLevel(CurrentLevel);
                }
            }
            finally
            {
                UnlockReached -= Collect;
            }

            return reached;
        }

        private bool RaiseUnlocks(int fromLevel, int toLevel)
        {
            bool changed = false;
            foreach (UnlockEntry entry in _roadmap.ReachedBetween(fromLevel, toLevel))
            {
                if (_save.Unlocks.IsSet(entry.UnlockId))
                {
                    continue;
                }

                _save.Unlocks.Flags[entry.UnlockId] = true;
                changed = true;
                UnlockReached?.Invoke(entry);
            }

            return changed;
        }
    }
}
