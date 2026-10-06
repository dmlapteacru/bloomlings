using System;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Client.Meta.Clearing
{
    /// <summary>What a tap on a clearing style's card did (<see cref="ClearingService.Tap"/>).</summary>
    public enum ClearingTap
    {
        /// <summary>The style is now chosen (an owned one, or the free pair).</summary>
        Chosen,

        /// <summary>The style was bought for Petals and chosen.</summary>
        Bought,

        /// <summary>Not before <see cref="ClearStyles.BuyFromLevel"/>: the card says from which level.</summary>
        Locked,

        /// <summary>Too few Petals.</summary>
        Short,

        /// <summary>Nothing can pay here (no economy).</summary>
        Unavailable,
    }

    /// <summary>
    /// A clearing style's card button on the Store's Animations tab (spec 005 FR-038 as amended on 2026-10-06,
    /// <see cref="ClearingService.ActionOf"/>): what a tap on the card does, shown so the player never has to guess.
    /// </summary>
    public enum ClearingAction
    {
        /// <summary>Not owned before <see cref="ClearStyles.BuyFromLevel"/>: the cost pill and the padlock; a tap says from which level.</summary>
        Locked,

        /// <summary>Not owned from L40: the green "Buy" with the price; a tap opens the purchase confirmation (FR-040).</summary>
        Buy,

        /// <summary>Owned (or the free pair) but not chosen: the cream "Choose".</summary>
        Choose,

        /// <summary>The chosen one (the free pair while no bought style is chosen): "Chosen" with a check; a tap changes nothing.</summary>
        Chosen,
    }

    /// <summary>
    /// The board's clearing styles as board cosmetics (spec 005 FR-038; spec 001 FR-063 and FR-051 as amended): the free
    /// pair, Blossom and Munchers, plays by level; the five bought styles are bought once with Petals from
    /// <see cref="ClearStyles.BuyFromLevel"/> (Remote Config <c>economy.price.clearing</c>, 5000 for now) and, once chosen,
    /// play on every level. The save keeps the bought ones in <c>cosmetics.owned</c> (<c>clear.&lt;name&gt;</c>) and the
    /// chosen one in <c>cosmetics.equipped.board.clearing</c>; no chosen style means the free pair. Presentation only:
    /// nothing here reaches the level rules, and every style takes the same time. Engine-free.
    /// </summary>
    public sealed class ClearingService
    {
        private static readonly string Slot = CosmeticsData.Slot(CosmeticsData.BoardOwner, CosmeticsData.ClearingKind);

        private readonly PlayerSave _save;
        private readonly IRemoteConfigService _config;
        private readonly Action _persist;
        private readonly EconomyService? _economy;

        /// <param name="economy">Pays for styles; without it none can be bought.</param>
        public ClearingService(PlayerSave save, IRemoteConfigService config, Action persist, EconomyService? economy = null)
        {
            _save = save;
            _config = config;
            _persist = persist;
            _economy = economy;
        }

        public event Action? Changed;

        /// <summary>A style was chosen (the <c>cosmetic_equip</c> event, family <c>board</c>): its id.</summary>
        public event Action<string>? Chose;

        /// <summary>What a bought style costs in Petals.</summary>
        public int Price => _config.Get(RemoteConfigKeys.PriceClearing);

        public bool Owns(ClearStyle style) => ClearStyles.IsFree(style) || _save.Cosmetics.Owned.Contains(ClearStyles.Id(style));

        /// <summary>The chosen bought style while it is owned, or null for the free pair.</summary>
        public ClearStyle? Chosen
        {
            get
            {
                ClearStyle? chosen = _save.Cosmetics.Equipped.TryGetValue(Slot, out string? id) ? ClearStyles.Parse(id) : null;
                return chosen.HasValue && !ClearStyles.IsFree(chosen.Value) && Owns(chosen.Value) ? chosen : null;
            }
        }

        /// <summary>Whether the card shows as chosen: the chosen bought style, or the free pair's card when there is none.</summary>
        public bool IsChosen(ClearStyle style) => Chosen.HasValue ? Chosen.Value == style : ClearStyles.IsFree(style);

        /// <summary>The style a level plays (<see cref="ClearStyles.ForLevel"/>).</summary>
        public ClearStyle StyleFor(int level) => ClearStyles.ForLevel(level, Chosen);

        /// <summary>Whether a bought style can be bought at <paramref name="level"/>: from L40, with the other cosmetics.</summary>
        public static bool IsOpenAt(int level) => level >= ClearStyles.BuyFromLevel;

        /// <summary>The card button of <paramref name="style"/> at <paramref name="level"/> (<see cref="ClearingAction"/>).</summary>
        public ClearingAction ActionOf(ClearStyle style, int level) =>
            IsChosen(style) ? ClearingAction.Chosen
            : Owns(style) ? ClearingAction.Choose
            : IsOpenAt(level) ? ClearingAction.Buy
            : ClearingAction.Locked;

        /// <summary>
        /// What a tap on <paramref name="style"/>'s card would do at <paramref name="level"/>, without doing it: the hosts
        /// ask the purchase confirmation first (spec 005 FR-040) when it would buy (<see cref="ClearingTap.Bought"/>) and
        /// tap at once otherwise (choosing spends nothing; a refused tap says why).
        /// </summary>
        public ClearingTap Check(ClearStyle style, int level)
        {
            if (Owns(style))
            {
                return ClearingTap.Chosen;
            }

            if (!IsOpenAt(level))
            {
                return ClearingTap.Locked;
            }

            if (_economy == null || Price <= 0)
            {
                return ClearingTap.Unavailable;
            }

            return _economy.Petals < Price ? ClearingTap.Short : ClearingTap.Bought;
        }

        /// <summary>
        /// A tap on a style's card at <paramref name="level"/>: an owned style (or the free pair) is chosen; a bought one
        /// not owned yet is bought for Petals and chosen, from L40. The hosts call it for a purchase only once the player
        /// has confirmed it (spec 005 FR-040, <see cref="Check"/>).
        /// </summary>
        public ClearingTap Tap(ClearStyle style, int level)
        {
            if (Owns(style))
            {
                Choose(style);
                return ClearingTap.Chosen;
            }

            if (!IsOpenAt(level))
            {
                return ClearingTap.Locked;
            }

            if (_economy == null || Price <= 0)
            {
                return ClearingTap.Unavailable;
            }

            if (_economy.Petals < Price)
            {
                return ClearingTap.Short;
            }

            // The style is added and chosen first, so the one save that the spend makes holds all three.
            string id = ClearStyles.Id(style);
            string? before = _save.Cosmetics.Equipped.TryGetValue(Slot, out string? was) ? was : null;
            _save.Cosmetics.Owned.Add(id);
            _save.Cosmetics.Equipped[Slot] = id;
            if (!_economy.TrySpend(Price))
            {
                _save.Cosmetics.Owned.Remove(id);
                if (before == null)
                {
                    _save.Cosmetics.Equipped.Remove(Slot);
                }
                else
                {
                    _save.Cosmetics.Equipped[Slot] = before;
                }

                return ClearingTap.Short;
            }

            Changed?.Invoke();
            Chose?.Invoke(id);
            return ClearingTap.Bought;
        }

        /// <summary>Chooses an owned bought style, or the free pair for a free one; false for a style not owned.</summary>
        public bool Choose(ClearStyle style)
        {
            if (!Owns(style))
            {
                return false;
            }

            if (ClearStyles.IsFree(style))
            {
                if (!_save.Cosmetics.Equipped.Remove(Slot))
                {
                    return true;
                }
            }
            else
            {
                string id = ClearStyles.Id(style);
                if (_save.Cosmetics.Equipped.TryGetValue(Slot, out string? now) && now == id)
                {
                    return true;
                }

                _save.Cosmetics.Equipped[Slot] = id;
            }

            _persist();
            Changed?.Invoke();
            Chose?.Invoke(ClearStyles.IsFree(style) ? "clear.free" : ClearStyles.Id(style));
            return true;
        }
    }
}
