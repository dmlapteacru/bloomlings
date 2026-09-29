using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Client.Services.Economy
{
    /// <summary>What a won level paid (FR-041, FR-047), for the Win screen.</summary>
    public sealed record LevelReward(int Petals, BoosterKind? DroppedBooster);

    /// <summary>
    /// Petals and booster charges (T119). Every change is saved at once through the persist callback, and the save's
    /// mutators keep "Petals and charges are never negative". Engine-free, so it runs in the .NET check:
    /// <list type="bullet">
    /// <item>a won level pays base, plus the clean-clear bonus without boosters, plus the Hard/Super Hard bonus (FR-041);</item>
    /// <item>each booster unlock (L3, L4, L6, L9) grants <c>economy.unlockGrant</c> charges, once (FR-042);</item>
    /// <item>every <c>economy.drop.everyLevels</c>-th completed level grants one charge, rotating through the unlocked
    /// boosters in a fixed order, with no randomness (FR-047);</item>
    /// <item>boosters can be bought with Petals once unlocked, and each use consumes a charge (FR-048);</item>
    /// <item>milestones and rewarded ads grant through <see cref="Grant"/>.</item>
    /// </list>
    /// </summary>
    public sealed class EconomyService
    {
        /// <summary>The booster unlocks of the roadmap, in the fixed drop rotation order.</summary>
        public static readonly IReadOnlyList<(string UnlockId, BoosterKind Kind)> BoosterUnlocks = new[]
        {
            ("booster.extra_slot", BoosterKind.ExtraSlot),
            ("booster.shuffle", BoosterKind.Shuffle),
            ("booster.return", BoosterKind.Return),
            ("booster.bloom_burst", BoosterKind.BloomBurst),
        };

        private const string GrantedPrefix = "granted.";

        private readonly PlayerSave _save;
        private readonly Action _persist;

        public EconomyService(PlayerSave save, EconomyConfig config, Action persist)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _persist = persist ?? throw new ArgumentNullException(nameof(persist));
        }

        /// <summary>Raised after Petals or charges change (the booster bar and the Home header listen).</summary>
        public event Action? Changed;

        public EconomyConfig Config { get; set; }

        public int Petals => _save.Wallet.Petals;

        public int Charges(BoosterKind kind) => _save.Boosters.Get(kind);

        public int Price(BoosterKind kind) => Config.Price(kind);

        public bool IsUnlocked(BoosterKind kind)
        {
            foreach ((string id, BoosterKind k) in BoosterUnlocks)
            {
                if (k == kind)
                {
                    return _save.Unlocks.IsSet(id);
                }
            }

            return false;
        }

        /// <summary>Owned, or affordable with Petals: the booster can be used now (the Jam screen lists only these).</summary>
        public bool CanAfford(BoosterKind kind) => IsUnlocked(kind) && (Charges(kind) > 0 || Petals >= Price(kind));

        /// <summary>The Petals of a win (FR-041).</summary>
        public int WinPetals(DifficultyClass difficulty, int boostersUsed)
        {
            int petals = Config.PetalsBase;
            if (boostersUsed == 0)
            {
                petals += Config.CleanBonus;
            }

            petals += difficulty switch
            {
                DifficultyClass.Hard => Config.HardBonus,
                DifficultyClass.SuperHard => Config.SuperHardBonus,
                _ => 0,
            };
            return petals;
        }

        /// <summary>Pays a newly completed level: Petals and, on every Nth level, one booster charge.</summary>
        public LevelReward GrantLevelReward(int level, DifficultyClass difficulty, int boostersUsed)
        {
            int petals = WinPetals(difficulty, boostersUsed);
            _save.Wallet.AddPetals(petals);
            BoosterKind? drop = DropFor(level);
            if (drop.HasValue)
            {
                _save.Boosters.Add(drop.Value, 1);
            }

            Save();
            return new LevelReward(petals, drop);
        }

        /// <summary>The booster the Nth-level drop gives at this level, or null (FR-047).</summary>
        public BoosterKind? DropFor(int level)
        {
            if (level <= 0 || level % Config.DropEveryLevels != 0)
            {
                return null;
            }

            var unlocked = new List<BoosterKind>();
            foreach ((string _, BoosterKind kind) in BoosterUnlocks)
            {
                if (IsUnlocked(kind))
                {
                    unlocked.Add(kind);
                }
            }

            return unlocked.Count == 0 ? (BoosterKind?)null : unlocked[((level / Config.DropEveryLevels) - 1) % unlocked.Count];
        }

        /// <summary>A roadmap unlock was reached: booster unlocks grant their free charges, exactly once (FR-042).</summary>
        public void OnUnlock(string unlockId)
        {
            foreach ((string id, BoosterKind kind) in BoosterUnlocks)
            {
                if (id == unlockId && !_save.Unlocks.IsSet(GrantedPrefix + id))
                {
                    _save.Boosters.Add(kind, Config.UnlockGrant);
                    _save.Unlocks.Flags[GrantedPrefix + id] = true;
                    Save();
                }
            }
        }

        /// <summary>Buys one charge with Petals; false when locked or unaffordable (the balance never goes negative).</summary>
        public bool TryBuy(BoosterKind kind)
        {
            if (!IsUnlocked(kind) || !_save.Wallet.TrySpendPetals(Price(kind)))
            {
                return false;
            }

            _save.Boosters.Add(kind, 1);
            Save();
            return true;
        }

        /// <summary>Takes a charge for one use, buying it first if needed; false when neither is possible.</summary>
        public bool TryTakeCharge(BoosterKind kind)
        {
            if (!IsUnlocked(kind))
            {
                return false;
            }

            if (Charges(kind) == 0 && !TryBuy(kind))
            {
                return false;
            }

            bool used = _save.Boosters.TryUse(kind);
            Save();
            return used;
        }

        /// <summary>Gives back a charge whose use the level refused (the charge was taken optimistically).</summary>
        public void Refund(BoosterKind kind)
        {
            _save.Boosters.Add(kind, 1);
            Save();
        }

        /// <summary>Milestones, rewarded ads and purchases (FR-047).</summary>
        public void Grant(int petals, BoosterGrant? boosters)
        {
            if (petals > 0)
            {
                _save.Wallet.AddPetals(petals);
            }

            if (boosters != null)
            {
                _save.Boosters.Add(BoosterKind.ExtraSlot, boosters.ExtraSlot);
                _save.Boosters.Add(BoosterKind.Shuffle, boosters.Shuffle);
                _save.Boosters.Add(BoosterKind.Return, boosters.Return);
                _save.Boosters.Add(BoosterKind.BloomBurst, boosters.BloomBurst);
            }

            Save();
        }

        private void Save()
        {
            _persist();
            Changed?.Invoke();
        }
    }
}
