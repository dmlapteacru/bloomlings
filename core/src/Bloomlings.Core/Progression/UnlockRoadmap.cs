using System;
using System.Collections.Generic;
using System.Linq;

namespace Bloomlings.Core.Progression
{
    public enum UnlockKind
    {
        System,
        Booster,
        Mechanic,
        Variant,
        Profile,
    }

    /// <summary>
    /// One row of the unlock roadmap: <see cref="UnlockId"/> becomes available when the player reaches
    /// <see cref="Level"/>, and its demo is shown on that level or within <see cref="DemoWithin"/> (0–2) levels (FR-031).
    /// </summary>
    public sealed record UnlockEntry(int Level, string UnlockId, UnlockKind Kind, int DemoWithin, bool Optional);

    /// <summary>
    /// The unlock roadmap (spec "Unlock Roadmap", doc 13; T057). It is data shared by the client (unlock events and
    /// demos) and by pipeline validation (no mechanic before its unlock level). <c>content/roadmap/unlock-roadmap.json</c>
    /// is its reviewable form; a test keeps it identical to <see cref="Default"/>.
    /// </summary>
    public sealed class UnlockRoadmap
    {
        private readonly List<UnlockEntry> _entries;
        private readonly Dictionary<string, UnlockEntry> _byId;

        public UnlockRoadmap(IEnumerable<UnlockEntry> entries)
        {
            _entries = new List<UnlockEntry>(entries);
            _byId = new Dictionary<string, UnlockEntry>(StringComparer.Ordinal);
            var mechanicLevels = new HashSet<int>();
            int previousLevel = 0;
            foreach (UnlockEntry entry in _entries)
            {
                if (entry.Level < 1)
                {
                    throw new ArgumentException($"Unlock '{entry.UnlockId}' has level {entry.Level}; levels start at 1.", nameof(entries));
                }

                if (entry.Level < previousLevel)
                {
                    throw new ArgumentException($"Unlock '{entry.UnlockId}' at L{entry.Level} is out of level order.", nameof(entries));
                }

                if (entry.DemoWithin < 0 || entry.DemoWithin > 2)
                {
                    throw new ArgumentException($"Unlock '{entry.UnlockId}' has demoWithin {entry.DemoWithin}; allowed is 0–2 (FR-031).", nameof(entries));
                }

                if (!IsValidId(entry.UnlockId))
                {
                    throw new ArgumentException($"Unlock id '{entry.UnlockId}' must be lowercase words joined by '.' or '_'.", nameof(entries));
                }

                if (_byId.ContainsKey(entry.UnlockId))
                {
                    throw new ArgumentException($"Duplicate unlock id '{entry.UnlockId}'.", nameof(entries));
                }

                if (entry.Kind == UnlockKind.Mechanic && !mechanicLevels.Add(entry.Level))
                {
                    throw new ArgumentException($"Two mechanics unlock at L{entry.Level}; FR-031 allows one major mechanic per level.", nameof(entries));
                }

                _byId.Add(entry.UnlockId, entry);
                previousLevel = entry.Level;
            }
        }

        /// <summary>Entries in level order.</summary>
        public IReadOnlyList<UnlockEntry> Entries => _entries;

        public bool TryGet(string unlockId, out UnlockEntry entry) => _byId.TryGetValue(unlockId, out entry!);

        /// <summary>The level at which an unlock becomes available, or null for an unknown id.</summary>
        public int? LevelOf(string unlockId) => _byId.TryGetValue(unlockId, out UnlockEntry entry) ? entry.Level : (int?)null;

        /// <summary>True once the player has reached <paramref name="currentLevel"/> ≥ the unlock level.</summary>
        public bool IsUnlockedAt(string unlockId, int currentLevel)
        {
            int? level = LevelOf(unlockId);
            return level.HasValue && currentLevel >= level.Value;
        }

        /// <summary>Unlocks reached when the current level moves from <paramref name="fromLevel"/> (exclusive) to <paramref name="toLevel"/> (inclusive).</summary>
        public IReadOnlyList<UnlockEntry> ReachedBetween(int fromLevel, int toLevel)
        {
            var reached = new List<UnlockEntry>();
            foreach (UnlockEntry entry in _entries)
            {
                if (entry.Level > fromLevel && entry.Level <= toLevel)
                {
                    reached.Add(entry);
                }
            }

            return reached;
        }

        /// <summary>
        /// The roadmap of the spec. Defaults: the Key unlocks at L8 (<c>mechanic.key</c>) and Stones at L11
        /// (<c>mechanic.stone</c>). If the Mystery Pod passes the fairness solver and takes L8, <c>mechanic.key</c> moves
        /// to L14. Milestone rows are listed as systems so that the first one can be demonstrated.
        /// </summary>
        public static UnlockRoadmap Default { get; } = new UnlockRoadmap(new[]
        {
            new UnlockEntry(1, "system.core", UnlockKind.System, 0, false),
            new UnlockEntry(2, "profile.third_variant", UnlockKind.Profile, 0, false),
            new UnlockEntry(3, "booster.extra_slot", UnlockKind.Booster, 0, false),
            new UnlockEntry(4, "booster.shuffle", UnlockKind.Booster, 0, false),
            new UnlockEntry(5, "profile.hard", UnlockKind.Profile, 0, false),
            new UnlockEntry(6, "booster.return", UnlockKind.Booster, 0, false),
            new UnlockEntry(7, "system.daily_reward", UnlockKind.System, 0, false),
            new UnlockEntry(8, "mechanic.key", UnlockKind.Mechanic, 0, false),
            new UnlockEntry(9, "booster.bloom_burst", UnlockKind.Booster, 0, false),
            new UnlockEntry(10, "system.leaderboard", UnlockKind.System, 0, false),
            new UnlockEntry(10, "profile.super_hard", UnlockKind.Profile, 0, false),
            new UnlockEntry(11, "mechanic.stone", UnlockKind.Mechanic, 0, false),
            new UnlockEntry(12, "system.store", UnlockKind.System, 0, false),
            new UnlockEntry(14, "profile.key_practice", UnlockKind.Profile, 0, false),
            new UnlockEntry(16, "mechanic.locked_pod", UnlockKind.Mechanic, 0, false),
            new UnlockEntry(18, "mechanic.connected_pair", UnlockKind.Mechanic, 0, false),
            new UnlockEntry(20, "profile.four_families", UnlockKind.Profile, 0, false),
            new UnlockEntry(25, "system.milestone_25", UnlockKind.System, 0, false),
            new UnlockEntry(28, "mechanic.layered_tile", UnlockKind.Mechanic, 0, false),
            new UnlockEntry(32, "profile.five_variants", UnlockKind.Profile, 0, false),
            new UnlockEntry(35, "mechanic.gate", UnlockKind.Mechanic, 0, false),
            new UnlockEntry(40, "system.wardrobe", UnlockKind.System, 0, false),
            new UnlockEntry(45, "variant.pool_expansion_1", UnlockKind.Variant, 1, false),
            new UnlockEntry(50, "system.daily_challenge", UnlockKind.System, 0, false),
            new UnlockEntry(60, "mechanic.fountain", UnlockKind.Mechanic, 0, false),
            new UnlockEntry(70, "profile.six_variants_hard", UnlockKind.Profile, 0, false),
            new UnlockEntry(75, "system.milestone_75", UnlockKind.System, 0, false),
            new UnlockEntry(80, "mechanic.locked_slot", UnlockKind.Mechanic, 0, false),
            new UnlockEntry(90, "mechanic.mystery_tile", UnlockKind.Mechanic, 0, true),
            new UnlockEntry(100, "system.milestone_100", UnlockKind.System, 0, false),
            new UnlockEntry(100, "system.theme_rotation", UnlockKind.System, 0, false),
            new UnlockEntry(125, "profile.layers_depth_3", UnlockKind.Profile, 0, false),
            new UnlockEntry(150, "mechanic.chest", UnlockKind.Mechanic, 0, true),
            new UnlockEntry(175, "profile.advanced_combinations", UnlockKind.Profile, 0, false),
            new UnlockEntry(200, "system.milestone_200", UnlockKind.System, 0, false),
            new UnlockEntry(200, "variant.pool_expansion_2", UnlockKind.Variant, 1, false),
            new UnlockEntry(225, "profile.advanced_hard", UnlockKind.Profile, 0, false),
            new UnlockEntry(250, "mechanic.environment_2", UnlockKind.Mechanic, 0, true),
            new UnlockEntry(300, "profile.six_variants_normal", UnlockKind.Profile, 0, false),
            new UnlockEntry(400, "mechanic.connected_triple", UnlockKind.Mechanic, 0, true),
            new UnlockEntry(500, "system.milestone_500", UnlockKind.System, 0, false),
        });

        /// <summary>
        /// The roadmap's Level 8 alternative (spec roadmap L8 and L14, open decision "Level 8: Mystery if fair, otherwise
        /// Key"): the Mystery Pod takes L8, and the Key unlocks at L14 instead of its practice row. Use it only once the
        /// fairness solver passes the Mystery Pod levels.
        /// </summary>
        public static UnlockRoadmap MysteryPodAtLevel8 { get; } = new UnlockRoadmap(Default._entries.Select(entry => entry.UnlockId switch
        {
            "mechanic.key" => entry with { UnlockId = "mechanic.mystery_pod", Optional = true },
            "profile.key_practice" => entry with { UnlockId = "mechanic.key", Kind = UnlockKind.Mechanic },
            _ => entry,
        }));

        /// <summary>The roadmap for a Level 8 choice: <c>key</c> (default) or <c>mystery_pod</c>.</summary>
        public static UnlockRoadmap ForLevel8(string choice) => choice switch
        {
            "key" => Default,
            "mystery_pod" => MysteryPodAtLevel8,
            _ => throw new ArgumentException($"Level 8 is 'key' or 'mystery_pod', not '{choice}'.", nameof(choice)),
        };

        private static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            foreach (char c in id)
            {
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '.'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
