using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Save;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.Meta.Wardrobe
{
    /// <summary>
    /// The Wardrobe (FR-063, T143): unlocked at L40 (<c>system.wardrobe</c>) behind the remote flag
    /// <c>feature.wardrobe</c>. Each family (Sprig, Bloom, Drop, Twig) wears at most one owned hat, trail or expression,
    /// stored in the save's <c>cosmetics.equipped</c>. Cosmetics change presentation only: nothing here reaches the
    /// level rules, and <see cref="CosmeticCatalog.ReadabilityProblems"/> keeps them readable. The starter items are
    /// given once when the Wardrobe unlocks. Engine-free.
    /// </summary>
    public sealed class WardrobeService
    {
        public const string UnlockId = "system.wardrobe";

        private const string StarterGrantedFlag = "granted." + UnlockId;

        private readonly PlayerSave _save;
        private readonly CosmeticCatalog _catalog;
        private readonly IRemoteConfigService _config;
        private readonly Action _persist;

        public WardrobeService(PlayerSave save, CosmeticCatalog catalog, IRemoteConfigService config, Action persist)
        {
            _save = save;
            _catalog = catalog;
            _config = config;
            _persist = persist;
        }

        public event Action? Changed;

        /// <summary>A family put on an item (the <c>cosmetic_equip</c> event).</summary>
        public event Action<Family, string>? Equipped;

        public static IReadOnlyList<Family> Families { get; } = new[] { Family.Sprig, Family.Bloom, Family.Drop, Family.Twig };

        public CosmeticCatalog Catalog => _catalog;

        public bool IsAvailable => _save.Unlocks.IsSet(UnlockId) && _config.Get(RemoteConfigKeys.WardrobeEnabled);

        /// <summary>Owned catalog items, in catalog order (unknown ids from newer content are skipped).</summary>
        public IReadOnlyList<CosmeticItem> Owned
        {
            get
            {
                var owned = new List<CosmeticItem>();
                foreach (CosmeticItem item in _catalog.Items)
                {
                    if (_save.Cosmetics.Owned.Contains(item.Id))
                    {
                        owned.Add(item);
                    }
                }

                return owned;
            }
        }

        /// <summary>A roadmap unlock was reached: the Wardrobe's starter items are given once.</summary>
        public void OnUnlock(string unlockId)
        {
            if (unlockId != UnlockId || _save.Unlocks.IsSet(StarterGrantedFlag))
            {
                return;
            }

            foreach (CosmeticItem item in _catalog.Items)
            {
                if (item.Starter)
                {
                    _save.Cosmetics.Owned.Add(item.Id);
                }
            }

            _save.Unlocks.Flags[StarterGrantedFlag] = true;
            _persist();
            Changed?.Invoke();
        }

        /// <summary>Equips an owned worn item on one family; false when it is not owned or not wearable.</summary>
        public bool Equip(Family family, string itemId)
        {
            if (!_save.Cosmetics.Owned.Contains(itemId) || !_catalog.TryGet(itemId, out CosmeticItem? item) || !item!.IsWorn)
            {
                return false;
            }

            _save.Cosmetics.Equipped[FamilyKey(family)] = itemId;
            _persist();
            Changed?.Invoke();
            Equipped?.Invoke(family, itemId);
            return true;
        }

        public void Unequip(Family family)
        {
            if (_save.Cosmetics.Equipped.Remove(FamilyKey(family)))
            {
                _persist();
                Changed?.Invoke();
            }
        }

        /// <summary>What a family wears, or null. Nothing is worn while the Wardrobe is switched off remotely.</summary>
        public CosmeticItem? EquippedFor(Family family)
        {
            if (!IsAvailable
                || !_save.Cosmetics.Equipped.TryGetValue(FamilyKey(family), out string? id)
                || !_save.Cosmetics.Owned.Contains(id)
                || !_catalog.TryGet(id, out CosmeticItem? item))
            {
                return null;
            }

            return item;
        }

        /// <summary>The profile decoration shown on Home and the leaderboard: the last owned frame, badge or marker in catalog order.</summary>
        public CosmeticItem? ProfileDecoration()
        {
            CosmeticItem? best = null;
            foreach (CosmeticItem item in Owned)
            {
                if (!item.IsWorn)
                {
                    best = item;
                }
            }

            return best;
        }

        public static string FamilyKey(Family family) => family switch
        {
            Family.Sprig => "sprig",
            Family.Bloom => "bloom",
            Family.Drop => "drop",
            _ => "twig",
        };
    }
}
