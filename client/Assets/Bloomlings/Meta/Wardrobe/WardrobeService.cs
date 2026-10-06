using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.Meta.Wardrobe
{
    /// <summary>What one family wears: at most one item of each worn kind.</summary>
    public sealed record Outfit(CosmeticItem? Skin, CosmeticItem? Hat, CosmeticItem? Trail, CosmeticItem? Expression)
    {
        public static Outfit None { get; } = new Outfit(null, null, null, null);

        public bool IsEmpty => Skin == null && Hat == null && Trail == null && Expression == null;

        public CosmeticItem? Get(CosmeticKind kind) => kind switch
        {
            CosmeticKind.Skin => Skin,
            CosmeticKind.Hat => Hat,
            CosmeticKind.Trail => Trail,
            CosmeticKind.Expression => Expression,
            _ => null,
        };
    }

    /// <summary>What the profile shows on Home and on the player's own leaderboard row.</summary>
    public sealed record ProfileLook(CosmeticItem? Frame, CosmeticItem? Badge, CosmeticItem? Marker)
    {
        public static ProfileLook None { get; } = new ProfileLook(null, null, null);

        public CosmeticItem? Get(CosmeticKind kind) => kind switch
        {
            CosmeticKind.Frame => Frame,
            CosmeticKind.Badge => Badge,
            CosmeticKind.Marker => Marker,
            _ => null,
        };
    }

    /// <summary>
    /// The Wardrobe (FR-063, T143): unlocked at L40 (<c>system.wardrobe</c>) behind the remote flag
    /// <c>feature.wardrobe</c>. Each family (Sprig, Bloom, Drop, Twig) wears at most one owned skin, hat, trail and
    /// expression, and the profile shows one owned frame, badge and marker (by default the newest of each), stored in
    /// the save's <c>cosmetics.equipped</c> slots. Items with a price can be bought with Petals once the Wardrobe is open
    /// (the Store's cosmetics, FR-051). Cosmetics change presentation only: nothing here reaches the level rules, and
    /// <see cref="CosmeticCatalog.ReadabilityProblems"/> keeps them readable. The starter items are given once when the
    /// Wardrobe unlocks. Free items (<see cref="CosmeticItem.Free"/>: the five drawn frames, spec 005 FR-037 as amended
    /// 2026-10-06) are owned by every player without a save entry: the profile shows a chosen free frame from Level 1,
    /// before the Wardrobe opens, and a free frame is never the default one. Engine-free.
    /// </summary>
    public sealed class WardrobeService
    {
        public const string UnlockId = "system.wardrobe";

        private const string StarterGrantedFlag = "granted." + UnlockId;

        private readonly PlayerSave _save;
        private readonly CosmeticCatalog _catalog;
        private readonly IRemoteConfigService _config;
        private readonly Action _persist;
        private readonly EconomyService? _economy;

        /// <param name="economy">Pays for Store cosmetics; without it nothing can be bought.</param>
        public WardrobeService(PlayerSave save, CosmeticCatalog catalog, IRemoteConfigService config, Action persist, EconomyService? economy = null)
        {
            _save = save;
            _catalog = catalog;
            _config = config;
            _persist = persist;
            _economy = economy;
        }

        public event Action? Changed;

        /// <summary>A family put on an item (the <c>cosmetic_equip</c> event).</summary>
        public event Action<Family, string>? Equipped;

        /// <summary>An item was bought with Petals.</summary>
        public event Action<CosmeticItem>? Bought;

        public static IReadOnlyList<Family> Families { get; } = new[] { Family.Sprig, Family.Bloom, Family.Drop, Family.Twig };

        public CosmeticCatalog Catalog => _catalog;

        public bool IsAvailable => _save.Unlocks.IsSet(UnlockId) && _config.Get(RemoteConfigKeys.WardrobeEnabled);

        /// <summary>Whether the player owns an item: a free one always, any other once the save holds it.</summary>
        public bool Owns(CosmeticItem item) => item.Free || _save.Cosmetics.Owned.Contains(item.Id);

        /// <summary>
        /// Owned items: the listed ones in catalog order (the free ones among them), then the generated level badges and
        /// markers by level (unknown ids from newer content are skipped).
        /// </summary>
        public IReadOnlyList<CosmeticItem> Owned
        {
            get
            {
                var owned = new List<CosmeticItem>();
                foreach (CosmeticItem item in _catalog.Items)
                {
                    if (Owns(item))
                    {
                        owned.Add(item);
                    }
                }

                var generated = new List<CosmeticItem>();
                foreach (string id in _save.Cosmetics.Owned)
                {
                    CosmeticItem? item = CosmeticCatalog.Generated(id);
                    if (item != null)
                    {
                        generated.Add(item);
                    }
                }

                generated.Sort((a, b) => a.MilestoneLevel!.Value != b.MilestoneLevel!.Value
                    ? a.MilestoneLevel.Value.CompareTo(b.MilestoneLevel.Value)
                    : a.Kind.CompareTo(b.Kind));
                owned.AddRange(generated);
                return owned;
            }
        }

        /// <summary>Owned items of one kind, in <see cref="Owned"/> order.</summary>
        public IReadOnlyList<CosmeticItem> OwnedOf(CosmeticKind kind)
        {
            var items = new List<CosmeticItem>();
            foreach (CosmeticItem item in Owned)
            {
                if (item.Kind == kind)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        /// <summary>Listed items with a price that the player does not own yet, in catalog order.</summary>
        public IReadOnlyList<CosmeticItem> ForSale
        {
            get
            {
                var items = new List<CosmeticItem>();
                foreach (CosmeticItem item in _catalog.Items)
                {
                    if (item.ForSale && !_save.Cosmetics.Owned.Contains(item.Id))
                    {
                        items.Add(item);
                    }
                }

                return items;
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

        /// <summary>Whether the Store can sell this item now: the Wardrobe is open, it has a price, and it is not owned.</summary>
        public bool IsBuyable(CosmeticItem item) => IsAvailable && item.ForSale && !Owns(item) && _economy != null;

        /// <summary>Buys a Store cosmetic with Petals; false when it cannot be bought or the Petals are short.</summary>
        public bool TryBuy(string itemId)
        {
            if (!_catalog.TryGet(itemId, out CosmeticItem? item) || !IsBuyable(item!) || _economy!.Petals < item!.Price)
            {
                return false;
            }

            // The item is added first, so the one save that the spend makes holds both.
            _save.Cosmetics.Owned.Add(item.Id);
            if (!_economy.TrySpend(item.Price))
            {
                _save.Cosmetics.Owned.Remove(item.Id);
                return false;
            }

            Changed?.Invoke();
            Bought?.Invoke(item);
            return true;
        }

        /// <summary>Equips an owned worn item on one family, in its kind's slot; false when it is not owned or not wearable.</summary>
        public bool Equip(Family family, string itemId)
        {
            if (!_save.Cosmetics.Owned.Contains(itemId) || !_catalog.TryGet(itemId, out CosmeticItem? item) || !item!.IsWorn)
            {
                return false;
            }

            _save.Cosmetics.Equipped[Slot(family, item.Kind)] = itemId;
            _persist();
            Changed?.Invoke();
            Equipped?.Invoke(family, itemId);
            return true;
        }

        public void Unequip(Family family, CosmeticKind kind)
        {
            if (_save.Cosmetics.Equipped.Remove(Slot(family, kind)))
            {
                _persist();
                Changed?.Invoke();
            }
        }

        /// <summary>What a family wears of one kind, or null. Nothing is worn while the Wardrobe is switched off remotely.</summary>
        public CosmeticItem? EquippedFor(Family family, CosmeticKind kind)
        {
            if (!IsAvailable
                || !_save.Cosmetics.Equipped.TryGetValue(Slot(family, kind), out string? id)
                || !_save.Cosmetics.Owned.Contains(id)
                || !_catalog.TryGet(id, out CosmeticItem? item)
                || item!.Kind != kind)
            {
                return null;
            }

            return item;
        }

        /// <summary>Everything a family wears (presentation only; the workers and the Wardrobe draw it).</summary>
        public Outfit OutfitOf(Family family) => IsAvailable
            ? new Outfit(
                EquippedFor(family, CosmeticKind.Skin),
                EquippedFor(family, CosmeticKind.Hat),
                EquippedFor(family, CosmeticKind.Trail),
                EquippedFor(family, CosmeticKind.Expression))
            : Outfit.None;

        /// <summary>
        /// Shows an owned frame, badge or marker on the profile (a free one also while the Wardrobe is closed); false when it
        /// is not owned or not a profile item.
        /// </summary>
        public bool Show(string itemId)
        {
            if (!_catalog.TryGet(itemId, out CosmeticItem? item) || item!.IsWorn || !Owns(item))
            {
                return false;
            }

            _save.Cosmetics.Equipped[CosmeticsData.Slot(CosmeticsData.ProfileOwner, CosmeticCatalog.KindName(item.Kind))] = itemId;
            _persist();
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// The profile item shown of one kind: the chosen one while owned, else the newest owned that is not free; null for
        /// none. While the Wardrobe is closed only a chosen free item shows (the profile's free frames, from Level 1).
        /// </summary>
        public CosmeticItem? Shown(CosmeticKind kind)
        {
            string slot = CosmeticsData.Slot(CosmeticsData.ProfileOwner, CosmeticCatalog.KindName(kind));
            CosmeticItem? chosen = _save.Cosmetics.Equipped.TryGetValue(slot, out string? id)
                && _catalog.TryGet(id, out CosmeticItem? item)
                && item!.Kind == kind
                && Owns(item)
                ? item
                : null;
            if (!IsAvailable)
            {
                return chosen != null && chosen.Free ? chosen : null;
            }

            if (chosen != null)
            {
                return chosen;
            }

            // Every player owns the free items, so the default stays the newest earned or bought one (none at first).
            IReadOnlyList<CosmeticItem> owned = OwnedOf(kind);
            for (int i = owned.Count - 1; i >= 0; i--)
            {
                if (!owned[i].Free)
                {
                    return owned[i];
                }
            }

            return null;
        }

        /// <summary>The profile decorations shown on Home and on the player's leaderboard row.</summary>
        public ProfileLook Profile => new ProfileLook(Shown(CosmeticKind.Frame), Shown(CosmeticKind.Badge), Shown(CosmeticKind.Marker));

        public static string FamilyKey(Family family) => family switch
        {
            Family.Sprig => "sprig",
            Family.Bloom => "bloom",
            Family.Drop => "drop",
            _ => "twig",
        };

        private static string Slot(Family family, CosmeticKind kind) => CosmeticsData.Slot(FamilyKey(family), CosmeticCatalog.KindName(kind));
    }
}
