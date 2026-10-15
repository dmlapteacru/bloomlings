using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Generator;

namespace Bloomlings.Pipeline.Launch
{
    /// <summary>Repair a blocked fixed-art slot without discarding the owner's certified catalog.</summary>
    public static class LaunchRecovery
    {
        public static IReadOnlyList<GeneratedLevel>? RepairMechanicConflict(int number, LaunchArtDocument doc,
            Func<LevelGenerator> create, IReadOnlyDictionary<int, LevelDefinition> history)
        {
            // A clean variant introduction cannot change its mechanics. Its optional neighbour can:
            // regenerate that neighbour and the blocked slot together, then commit only a checked pair.
            foreach (int neighbour in new[] { number - 1, number + 1, number - 2, number + 2 })
            {
                if (neighbour <= 10 || neighbour > 2500 || !history.ContainsKey(neighbour)) continue;
                var slot = doc.Plan.Slots[neighbour - 1];
                if (slot.RequiredMechanic != null || VariantPool.Default.JoiningAt(neighbour, UnlockRoadmap.Default).Count > 0) continue;
                foreach (string mechanic in slot.AllowedMechanics)
                {
                    var trial = new SortedDictionary<int, LevelDefinition>(history.ToDictionary(p => p.Key, p => p.Value));
                    trial.Remove(neighbour);
                    var adjust = create();
                    adjust.Progress = null;
                    var originalMechanics = adjust.MechanicsOf;
                    adjust.MechanicsOf = n => n == neighbour ? new[] { mechanic } : originalMechanics?.Invoke(n);
                    ulong seed = doc.Plan.Seed ^ (0xC0111DEUL + (ulong)number);
                    var changed = adjust.Regenerate(new[] { neighbour }, seed, trial).Accepted.SingleOrDefault();
                    if (changed == null) continue;
                    var fill = create();
                    fill.Progress = null;
                    var repaired = fill.Regenerate(new[] { number }, seed, trial).Accepted.SingleOrDefault();
                    if (repaired == null) continue;
                    var pair = new[] { changed, repaired };
                    if (doc.CheckDefinitions(pair.Select(p => p.Definition), false).Count != 0
                        || pair.Any(p => LevelGenerator.Conflicts(p.Definition.LevelNumber, trial, fill.MechanicsUsed).Count != 0)) continue;
                    return pair;
                }
            }
            return null;
        }
    }
}
