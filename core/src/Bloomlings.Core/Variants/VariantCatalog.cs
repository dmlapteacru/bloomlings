using System;
using System.Collections.Generic;

namespace Bloomlings.Core.Variants
{
    /// <summary>
    /// One entry of the variant catalog (data-model §1.1).
    /// <see cref="ColorHex"/> and <see cref="IconId"/> are placeholders owned by art; hue alone never carries meaning (FR-005).
    /// </summary>
    public sealed record VariantInfo(
        VariantId Id,
        Family Family,
        ColorGroup ColorGroup,
        string ColorHex,
        string IconId,
        VariantStatus Status,
        int? IntroducedAtLevel);

    /// <summary>
    /// The global variant pool. "A variant catalog of at least 12 entries must be supported without rule changes":
    /// entries are data, and the rules only ever compare <see cref="VariantId"/> values.
    /// Launch variants default to <see cref="VariantInfo.IntroducedAtLevel"/> = 1; the unlock roadmap decides which of them
    /// a level may use. Expansion variants have no scheduled level yet (research, deferred decisions).
    /// </summary>
    public sealed class VariantCatalog
    {
        private readonly List<VariantInfo> _all;
        private readonly Dictionary<VariantId, int> _indexById;

        public VariantCatalog(IEnumerable<VariantInfo> variants)
        {
            if (variants == null)
            {
                throw new ArgumentNullException(nameof(variants));
            }

            _all = new List<VariantInfo>(variants);
            _indexById = new Dictionary<VariantId, int>();
            var icons = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _all.Count; i++)
            {
                VariantInfo info = _all[i];
                if (info.Id.IsNone)
                {
                    throw new ArgumentException("A catalog entry has no variant id.", nameof(variants));
                }

                if (_indexById.ContainsKey(info.Id))
                {
                    throw new ArgumentException($"Duplicate variant id '{info.Id}'.", nameof(variants));
                }

                if (!icons.Add(info.IconId))
                {
                    throw new ArgumentException($"Duplicate icon id '{info.IconId}': every variant needs its own icon (FR-072).", nameof(variants));
                }

                _indexById.Add(info.Id, i);
            }
        }

        /// <summary>The 8 launch variants (2 per family) plus the 4 planned expansion variants (FR-002).</summary>
        public static VariantCatalog Default { get; } = new VariantCatalog(new[]
        {
            new VariantInfo(VariantId.Leaf, Family.Sprig, ColorGroup.Green, "#5DAE4B", "leaf", VariantStatus.Launch, 1),
            new VariantInfo(VariantId.Moss, Family.Sprig, ColorGroup.Green, "#2E9C8F", "moss", VariantStatus.Launch, 1),
            new VariantInfo(VariantId.Flower, Family.Bloom, ColorGroup.PinkPurple, "#F07AA8", "flower", VariantStatus.Launch, 1),
            new VariantInfo(VariantId.VioletBud, Family.Bloom, ColorGroup.PinkPurple, "#8E5CC4", "bud", VariantStatus.Launch, 1),
            new VariantInfo(VariantId.Water, Family.Drop, ColorGroup.BlueCyan, "#3B7DD8", "drop", VariantStatus.Launch, 1),
            new VariantInfo(VariantId.Dew, Family.Drop, ColorGroup.BlueCyan, "#4CC9E0", "dew", VariantStatus.Launch, 1),
            new VariantInfo(VariantId.Wood, Family.Twig, ColorGroup.BrownOrange, "#8A5A3C", "log", VariantStatus.Launch, 1),
            new VariantInfo(VariantId.Acorn, Family.Twig, ColorGroup.BrownOrange, "#F0913A", "acorn", VariantStatus.Launch, 1),
            new VariantInfo(VariantId.Vine, Family.Sprig, ColorGroup.Lime, "#A6D63A", "vine", VariantStatus.Expansion, null),
            new VariantInfo(VariantId.Berry, Family.Bloom, ColorGroup.Red, "#D83A4A", "berry", VariantStatus.Expansion, null),
            new VariantInfo(VariantId.Mist, Family.Drop, ColorGroup.Indigo, "#4B4FB0", "mist", VariantStatus.Expansion, null),
            new VariantInfo(VariantId.Bark, Family.Twig, ColorGroup.Gold, "#D9A53A", "bark", VariantStatus.Expansion, null),
        });

        /// <summary>Entries in catalog order; the position is the stable index used for hashing.</summary>
        public IReadOnlyList<VariantInfo> All => _all;

        public int Count => _all.Count;

        public bool Contains(VariantId id) => _indexById.ContainsKey(id);

        public bool TryGet(VariantId id, out VariantInfo info)
        {
            if (_indexById.TryGetValue(id, out int index))
            {
                info = _all[index];
                return true;
            }

            info = null!;
            return false;
        }

        public VariantInfo Get(VariantId id)
        {
            if (!TryGet(id, out VariantInfo info))
            {
                throw new KeyNotFoundException($"Unknown variant '{id}'.");
            }

            return info;
        }

        /// <summary>Stable catalog index of a variant, used by Zobrist hashing.</summary>
        public int IndexOf(VariantId id)
        {
            if (!_indexById.TryGetValue(id, out int index))
            {
                throw new KeyNotFoundException($"Unknown variant '{id}'.");
            }

            return index;
        }
    }
}
