using System;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.App.Progression
{
    /// <summary>What a milestone paid: Petals, booster charges and at most one cosmetic or profile item.</summary>
    public sealed record MilestoneGrant(int Level, MilestoneCadence Cadence, int Petals, BoosterGrant? Boosters, string? Item);

    /// <summary>
    /// Milestone rewards (FR-061, T142). Completing a milestone level grants the reward of the largest cadence it
    /// matches, exactly once: the claim is recorded in the save's <c>milestones.claimed</c>, so replays, cloud merges
    /// and the Editor fast-forward never pay twice. Items the player already owns are skipped, and once a cadence's
    /// list is all owned it grants a generated level badge (a level marker for prestige), so the rewards never run out.
    /// The service also feeds the Home teaser ("Level 100 reward in 12", FR-058). Engine-free.
    /// </summary>
    public sealed class MilestoneService
    {
        private readonly PlayerSave _save;
        private readonly MilestoneTable _table;
        private readonly EconomyService _economy;
        private readonly Action _persist;

        public MilestoneService(PlayerSave save, MilestoneTable table, EconomyService economy, Action persist)
        {
            _save = save;
            _table = table;
            _economy = economy;
            _persist = persist;
        }

        /// <summary>A milestone was granted (the Win screen celebrates it).</summary>
        public event Action<MilestoneGrant>? Granted;

        public MilestoneTable Table => _table;

        /// <summary>The most recent grant in this session, or null.</summary>
        public MilestoneGrant? LastGrant { get; private set; }

        /// <summary>Grants the milestone of a newly completed level; null when there is none or it was already claimed.</summary>
        public MilestoneGrant? OnLevelCompleted(int level)
        {
            MilestoneCadence? cadence = _table.CadenceFor(level);
            if (cadence == null || !_save.Milestones.TryClaim(level))
            {
                return null;
            }

            MilestoneGrant grant = Preview(level, cadence);
            if (grant.Item != null)
            {
                _save.Cosmetics.Owned.Add(grant.Item);
            }

            // Grant persists the save, claim and item included.
            _economy.Grant(grant.Petals, grant.Boosters);
            _persist();
            LastGrant = grant;
            Granted?.Invoke(grant);
            return grant;
        }

        /// <summary>The next milestone after <paramref name="highestCompleted"/> and the wins still needed, for Home.</summary>
        public (int Level, MilestoneCadence Cadence, int WinsToGo)? Next(int highestCompleted)
        {
            int? level = _table.NextMilestoneLevel(highestCompleted);
            if (!level.HasValue)
            {
                return null;
            }

            return (level.Value, _table.CadenceFor(level.Value)!, level.Value - highestCompleted);
        }

        private MilestoneGrant Preview(int level, MilestoneCadence cadence)
        {
            int extraSlot = cadence.EachBooster;
            int shuffle = cadence.EachBooster;
            int back = cadence.EachBooster;
            int burst = cadence.EachBooster;
            if (cadence.BoosterCharges > 0)
            {
                switch (FewestChargesBooster())
                {
                    case BoosterKind.ExtraSlot:
                        extraSlot += cadence.BoosterCharges;
                        break;
                    case BoosterKind.Shuffle:
                        shuffle += cadence.BoosterCharges;
                        break;
                    case BoosterKind.Return:
                        back += cadence.BoosterCharges;
                        break;
                    case BoosterKind.BloomBurst:
                        burst += cadence.BoosterCharges;
                        break;
                }
            }

            BoosterGrant? boosters = extraSlot + shuffle + back + burst > 0 ? new BoosterGrant(extraSlot, shuffle, back, burst) : null;
            string? item = null;
            foreach (string id in cadence.Items)
            {
                if (!_save.Cosmetics.Owned.Contains(id))
                {
                    item = id;
                    break;
                }
            }

            // Rewards never run out (FR-061): past its listed items, a cadence grants the level's own badge, or its
            // leaderboard marker for a prestige cadence.
            if (item == null && cadence.Items.Count > 0)
            {
                item = cadence.Tier == MilestoneTier.Prestige ? CosmeticCatalog.LevelMarkerId(level) : CosmeticCatalog.LevelBadgeId(level);
            }

            return new MilestoneGrant(level, cadence, cadence.Petals, boosters, item);
        }

        /// <summary>The unlocked booster with the fewest charges (roadmap order on ties); Extra Slot if none is unlocked.</summary>
        private BoosterKind FewestChargesBooster()
        {
            BoosterKind? best = null;
            foreach ((string _, BoosterKind kind) in EconomyService.BoosterUnlocks)
            {
                if (_economy.IsUnlocked(kind) && (!best.HasValue || _economy.Charges(kind) < _economy.Charges(best.Value)))
                {
                    best = kind;
                }
            }

            return best ?? BoosterKind.ExtraSlot;
        }
    }
}
