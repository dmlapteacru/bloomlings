using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using Bloomlings.Generator.Profiles;

namespace Bloomlings.Generator
{
    /// <summary>
    /// R9 step 2 (T085): enumerates role→variant mappings. Each used role maps to an allowed variant of its own color
    /// group (FR-006); the number of distinct variants stays in the band range (FR-004, FR-060); roles may share a
    /// variant (merge) only if the profile allows it; and every pair of variants in the level must be a readable pair
    /// (FR-005). Enumeration order is fixed, so the result is deterministic.
    /// </summary>
    public static class RoleMapper
    {
        /// <param name="variantCount">The allowed number of distinct variants (the profile's range when null).</param>
        /// <param name="extraVariants">Variants the pool adds for this level (expansions, FR-060) on top of the profile's.</param>
        public static IReadOnlyList<SortedDictionary<string, VariantId>> Mappings(
            BasePicture picture,
            GenerationProfile profile,
            Func<VariantId, VariantId, bool> readablePair,
            int limit = 256,
            IntRange? variantCount = null,
            IReadOnlyList<VariantId>? extraVariants = null)
        {
            var allowed = new List<VariantId>(profile.AllowedVariants);
            foreach (VariantId extra in extraVariants ?? Array.Empty<VariantId>())
            {
                if (!allowed.Contains(extra))
                {
                    allowed.Add(extra);
                }
            }

            IntRange distinctRange = variantCount ?? profile.VariantCount;
            var used = new bool[picture.Roles.Count];
            foreach (IReadOnlyList<int> row in picture.Grid)
            {
                foreach (int cell in row)
                {
                    if (cell >= 0)
                    {
                        used[cell] = true;
                    }
                }
            }

            var roles = new List<PictureRole>();
            var options = new List<List<VariantId>>();
            for (int i = 0; i < picture.Roles.Count; i++)
            {
                if (!used[i])
                {
                    continue;
                }

                var choices = new List<VariantId>();
                foreach (VariantId variant in allowed)
                {
                    if (VariantCatalog.Default.Get(variant).ColorGroup == picture.Roles[i].ColorGroup)
                    {
                        choices.Add(variant);
                    }
                }

                if (choices.Count == 0)
                {
                    return Array.Empty<SortedDictionary<string, VariantId>>();
                }

                roles.Add(picture.Roles[i]);
                options.Add(choices);
            }

            var results = new List<SortedDictionary<string, VariantId>>();
            var current = new VariantId[roles.Count];
            Enumerate(0);
            return results;

            void Enumerate(int index)
            {
                if (results.Count >= limit)
                {
                    return;
                }

                if (index == roles.Count)
                {
                    var distinct = new SortedSet<VariantId>(current);
                    if (!distinctRange.Contains(distinct.Count))
                    {
                        return;
                    }

                    if (!profile.AllowRoleMerge && distinct.Count < roles.Count)
                    {
                        return;
                    }

                    var list = new List<VariantId>(distinct);
                    for (int a = 0; a < list.Count; a++)
                    {
                        for (int b = a + 1; b < list.Count; b++)
                        {
                            if (!readablePair(list[a], list[b]))
                            {
                                return;
                            }
                        }
                    }

                    var mapping = new SortedDictionary<string, VariantId>(StringComparer.Ordinal);
                    for (int r = 0; r < roles.Count; r++)
                    {
                        mapping[roles[r].RoleId] = current[r];
                    }

                    results.Add(mapping);
                    return;
                }

                foreach (VariantId variant in options[index])
                {
                    current[index] = variant;
                    Enumerate(index + 1);
                }
            }
        }
    }
}
