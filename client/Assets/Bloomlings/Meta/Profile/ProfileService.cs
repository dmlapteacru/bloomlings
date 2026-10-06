using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.Meta.Profile
{
    /// <summary>
    /// The profile page's own data (spec 005 FR-037): the avatar picture (the four free defaults, the others bought with
    /// Petals once), the player's name (kept on the device: no server sees it yet, so no name check runs), the short
    /// player ID and the day they joined. Open from Level 1, unlike the Wardrobe's frames and badges. Presentation only:
    /// nothing here reaches the level rules. Engine-free.
    /// </summary>
    public sealed class ProfileService
    {
        /// <summary>The longest name, in characters.</summary>
        public const int MaxNameLength = 16;

        private static readonly string AvatarSlot = CosmeticsData.Slot(CosmeticsData.ProfileOwner, CosmeticsData.AvatarKind);

        private readonly PlayerSave _save;
        private readonly IRemoteConfigService _config;
        private readonly Action _persist;
        private readonly EconomyService? _economy;

        /// <param name="economy">Pays for avatars; without it none can be bought.</param>
        public ProfileService(PlayerSave save, IRemoteConfigService config, IClock clock, Action persist, EconomyService? economy = null)
        {
            _save = save;
            _config = config;
            _persist = persist;
            _economy = economy;
            if (_save.Profile.JoinedAt == null)
            {
                _save.Profile.JoinedAt = clock.UtcToday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                _persist();
            }
        }

        public event Action? Changed;

        /// <summary>An avatar was bought with Petals.</summary>
        public event Action<AvatarItem>? Bought;

        public IReadOnlyList<AvatarItem> Avatars => AvatarCatalog.All;

        /// <summary>What an avatar costs in Petals: 0 for a free one.</summary>
        public int Price(AvatarItem avatar)
        {
            IntKey? key = AvatarCatalog.PriceKey(avatar.Tier);
            return key == null ? 0 : _config.Get(key);
        }

        public bool Owns(AvatarItem avatar) => avatar.IsFree || _save.Cosmetics.Owned.Contains(avatar.Id);

        /// <summary>The avatar shown: the chosen one while it is owned and known, else the default.</summary>
        public AvatarItem Avatar
        {
            get
            {
                AvatarItem? chosen = _save.Cosmetics.Equipped.TryGetValue(AvatarSlot, out string? id) ? AvatarCatalog.Get(id) : null;
                return chosen != null && Owns(chosen) ? chosen : AvatarCatalog.Default;
            }
        }

        /// <summary>Shows an owned avatar; false for an unknown or unowned one.</summary>
        public bool Choose(string avatarId)
        {
            AvatarItem? avatar = AvatarCatalog.Get(avatarId);
            if (avatar == null || !Owns(avatar))
            {
                return false;
            }

            if (Avatar.Id == avatar.Id && _save.Cosmetics.Equipped.ContainsKey(AvatarSlot))
            {
                return true;
            }

            _save.Cosmetics.Equipped[AvatarSlot] = avatar.Id;
            _persist();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Whether an avatar can be bought now: it has a price, it is not owned, and Petals can pay.</summary>
        public bool IsBuyable(AvatarItem avatar) => !Owns(avatar) && _economy != null && Price(avatar) > 0;

        /// <summary>Buys an avatar with Petals and shows it; false when it cannot be bought or the Petals are short.</summary>
        public bool TryBuy(string avatarId)
        {
            AvatarItem? avatar = AvatarCatalog.Get(avatarId);
            if (avatar == null || !IsBuyable(avatar) || _economy!.Petals < Price(avatar))
            {
                return false;
            }

            // The avatar is added and chosen first, so the one save that the spend makes holds all three.
            _save.Cosmetics.Owned.Add(avatar.Id);
            string? before = _save.Cosmetics.Equipped.TryGetValue(AvatarSlot, out string? id) ? id : null;
            _save.Cosmetics.Equipped[AvatarSlot] = avatar.Id;
            if (!_economy.TrySpend(Price(avatar)))
            {
                _save.Cosmetics.Owned.Remove(avatar.Id);
                if (before == null)
                {
                    _save.Cosmetics.Equipped.Remove(AvatarSlot);
                }
                else
                {
                    _save.Cosmetics.Equipped[AvatarSlot] = before;
                }

                return false;
            }

            Changed?.Invoke();
            Bought?.Invoke(avatar);
            return true;
        }

        /// <summary>
        /// The chosen name, or null while the player keeps the default one (<see cref="DefaultNumber"/>); cleaned as typed
        /// names are (<see cref="CleanName"/>), so an edited save file cannot show more.
        /// </summary>
        public string? Name => CleanName(_save.Profile.Name);

        /// <summary>
        /// The number in the default name (<c>profile.default_name</c>, "Gardener 4821"): four digits that stay the same
        /// for the player's ID.
        /// </summary>
        public int DefaultNumber => 1000 + (int)(Fnv(_save.LocalPlayerId) % 9000u);

        /// <summary>Sets the name (<see cref="CleanName"/>); false when nothing is left of it.</summary>
        public bool Rename(string text)
        {
            string? name = CleanName(text);
            if (name == null)
            {
                return false;
            }

            if (name == _save.Profile.Name)
            {
                return true;
            }

            _save.Profile.Name = name;
            _persist();
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// A typed name made fit to show: the letters and digits the bundled font draws (Latin and Cyrillic), spaces and
        /// <c>_ - .</c>; other characters dropped, runs of spaces made one, trimmed, and cut to
        /// <see cref="MaxNameLength"/>. Null when nothing is left.
        /// </summary>
        public static string? CleanName(string? text)
        {
            if (text == null)
            {
                return null;
            }

            var name = new StringBuilder();
            bool space = false;
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    space = name.Length > 0;
                    continue;
                }

                if (!Allowed(c))
                {
                    continue;
                }

                if (space)
                {
                    name.Append(' ');
                    space = false;
                }

                name.Append(c);
            }

            string clean = name.Length > MaxNameLength ? name.ToString(0, MaxNameLength).TrimEnd() : name.ToString();
            return clean.Length == 0 ? null : clean;
        }

        /// <summary>The short player ID the page shows: the first 8 characters of the local player ID, upper case.</summary>
        public string ShortId
        {
            get
            {
                string id = _save.LocalPlayerId.Replace("-", string.Empty);
                return (id.Length > 8 ? id.Substring(0, 8) : id).ToUpperInvariant();
            }
        }

        /// <summary>The UTC day the player joined (set on the first launch with this page, then kept).</summary>
        public DateTime JoinedAt =>
            DateTime.TryParseExact(_save.Profile.JoinedAt, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime day)
                ? DateTime.SpecifyKind(day, DateTimeKind.Utc)
                : DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

        /// <summary>The joining month as the page shows it: <c>10/2026</c>.</summary>
        public string JoinedMonth => JoinedAt.ToString("MM'/'yyyy", CultureInfo.InvariantCulture);

        private static bool Allowed(char c) =>
            (c >= '0' && c <= '9')
            || (c >= 'A' && c <= 'Z')
            || (c >= 'a' && c <= 'z')
            || (c >= 'À' && c <= 'ſ' && c != '×' && c != '÷')
            || (c >= 'Ѐ' && c <= 'ӿ' && char.IsLetter(c))
            || c == '_' || c == '-' || c == '.';

        private static uint Fnv(string text)
        {
            uint hash = 2166136261u;
            foreach (char c in text)
            {
                hash = unchecked((hash ^ c) * 16777619u);
            }

            return hash;
        }
    }
}
