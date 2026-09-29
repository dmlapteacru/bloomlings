using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Progression
{
    /// <summary>
    /// Which expansion variant joins the global pool at each pool-expansion row of the roadmap (FR-060: "New variants
    /// join the global pool at milestones (for example L45 and L200) without any player choice"). Launch variants are in
    /// the pool from L1. A new variant is introduced with one clean level, then one mixed level (FR-031).
    /// The spec leaves the choice open ("Variants entering at L45/L200: to be decided"); <see cref="Default"/> is a
    /// proposal until the product owner decides: Vine at L45 and Berry at L200.
    /// </summary>
    public sealed class VariantPool
    {
        private readonly Dictionary<VariantId, string> _unlockOf = new Dictionary<VariantId, string>();

        public VariantPool(IEnumerable<(string UnlockId, VariantId Variant)> expansions)
        {
            foreach ((string unlockId, VariantId variant) in expansions)
            {
                if (!unlockId.StartsWith("variant.", StringComparison.Ordinal))
                {
                    throw new ArgumentException($"'{unlockId}' is not a variant unlock.", nameof(expansions));
                }

                _unlockOf.Add(variant, unlockId);
            }
        }

        /// <summary>The proposed default: Vine with <c>variant.pool_expansion_1</c> (L45), Berry with <c>variant.pool_expansion_2</c> (L200).</summary>
        public static VariantPool Default { get; } = new VariantPool(new[]
        {
            ("variant.pool_expansion_1", VariantId.Vine),
            ("variant.pool_expansion_2", VariantId.Berry),
        });

        /// <summary>The level a variant joins the pool: 1 for launch variants, its unlock row for an expansion, null if never.</summary>
        public int? IntroducedAt(VariantId variant, UnlockRoadmap roadmap, VariantCatalog? catalog = null)
        {
            VariantInfo info = (catalog ?? VariantCatalog.Default).Get(variant);
            if (info.Status == VariantStatus.Launch)
            {
                return 1;
            }

            return _unlockOf.TryGetValue(variant, out string? unlockId) ? roadmap.LevelOf(unlockId) : null;
        }

        public bool IsAvailable(VariantId variant, int level, UnlockRoadmap roadmap)
        {
            int? at = IntroducedAt(variant, roadmap);
            return at != null && at.Value <= level;
        }

        /// <summary>The expansion variants that join the pool exactly at <paramref name="level"/>.</summary>
        public IReadOnlyList<VariantId> JoiningAt(int level, UnlockRoadmap roadmap)
        {
            var joining = new List<VariantId>();
            foreach (KeyValuePair<VariantId, string> pair in _unlockOf)
            {
                if (roadmap.LevelOf(pair.Value) == level)
                {
                    joining.Add(pair.Key);
                }
            }

            joining.Sort();
            return joining;
        }

        /// <summary>The expansion variants available at <paramref name="level"/>.</summary>
        public IReadOnlyList<VariantId> ExpansionsAt(int level, UnlockRoadmap roadmap)
        {
            var available = new List<VariantId>();
            foreach (KeyValuePair<VariantId, string> pair in _unlockOf)
            {
                int? at = roadmap.LevelOf(pair.Value);
                if (at != null && at.Value <= level)
                {
                    available.Add(pair.Key);
                }
            }

            available.Sort();
            return available;
        }
    }
}
