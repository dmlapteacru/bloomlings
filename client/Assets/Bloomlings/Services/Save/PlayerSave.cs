using System;
using System.Collections.Generic;

namespace Bloomlings.Client.Services.Save
{
    public enum BoosterKind
    {
        ExtraSlot,
        Shuffle,
        Return,
        BloomBurst,
    }

    /// <summary>
    /// The local player save (contracts/player-save.schema.json; data-model §3.1). Its rules are enforced by the
    /// mutators: "Petals and charges are never negative", and "`claimed` and `ledger.transactionId` are unique".
    /// </summary>
    public sealed class PlayerSave
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; internal set; } = CurrentSchemaVersion;

        public string LocalPlayerId { get; internal set; } = string.Empty;

        /// <summary><c>apple</c>, <c>google_play_games</c> or null.</summary>
        public string? LinkedIdentity { get; set; }

        public string? DeviceId { get; set; }

        /// <summary>ISO 8601 UTC time of the last write, for example <c>2026-09-29T12:00:00Z</c>.</summary>
        public string UpdatedAt { get; internal set; } = string.Empty;

        public ProgressionData Progression { get; } = new ProgressionData();

        public WalletData Wallet { get; } = new WalletData();

        public PurchasesData Purchases { get; } = new PurchasesData();

        public BoosterCharges Boosters { get; } = new BoosterCharges();

        public UnlocksData Unlocks { get; } = new UnlocksData();

        public MilestonesData Milestones { get; } = new MilestonesData();

        public CosmeticsData Cosmetics { get; } = new CosmeticsData();

        public DailyData Daily { get; } = new DailyData();

        public List<CollectionEntry> Collection { get; } = new List<CollectionEntry>();

        public SettingsData Settings { get; } = new SettingsData();

        /// <summary>Counters such as <c>levelsWon</c>, and counter groups such as <c>boostersUsed</c>.</summary>
        public StatsData Stats { get; } = new StatsData();

        /// <summary>The profile page's name and joining day (spec 005 FR-037); the chosen avatar is a profile slot of <see cref="Cosmetics"/>.</summary>
        public ProfileData Profile { get; } = new ProfileData();

        /// <summary>A fresh profile, created automatically on first launch with no sign-in (FR-087).</summary>
        public static PlayerSave CreateNew(string localPlayerId, DateTime utcNow)
        {
            var save = new PlayerSave { LocalPlayerId = localPlayerId };
            save.Touch(utcNow);
            return save;
        }

        public static string FormatTime(DateTime utc) => utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);

        internal void Touch(DateTime utcNow) => UpdatedAt = FormatTime(utcNow);

        /// <summary>
        /// Copies every field of <paramref name="source"/> into this save, keeping this instance: the services hold it,
        /// so a cloud merge (R15) replaces its contents rather than the object.
        /// </summary>
        internal void Assign(PlayerSave source)
        {
            SchemaVersion = source.SchemaVersion;
            LocalPlayerId = source.LocalPlayerId;
            LinkedIdentity = source.LinkedIdentity;
            DeviceId = source.DeviceId;
            UpdatedAt = source.UpdatedAt;
            Progression.HighestCompletedLevel = source.Progression.HighestCompletedLevel;
            Progression.ContentVersionSeen = source.Progression.ContentVersionSeen;
            Wallet.SetPetals(source.Wallet.Petals);
            foreach (BoosterKind kind in new[] { BoosterKind.ExtraSlot, BoosterKind.Shuffle, BoosterKind.Return, BoosterKind.BloomBurst })
            {
                Boosters.Set(kind, source.Boosters.Get(kind));
            }

            Purchases.Assign(source.Purchases);
            Unlocks.Assign(source.Unlocks);
            Milestones.Assign(source.Milestones);
            Cosmetics.Owned.Clear();
            Cosmetics.Owned.UnionWith(source.Cosmetics.Owned);
            Cosmetics.Equipped.Clear();
            foreach (KeyValuePair<string, string> pair in source.Cosmetics.Equipped)
            {
                Cosmetics.Equipped[pair.Key] = pair.Value;
            }

            Daily.RewardLastClaimUtcDate = source.Daily.RewardLastClaimUtcDate;
            Daily.RewardStreak = source.Daily.RewardStreak;
            Daily.ChallengeLastCompletedUtcDate = source.Daily.ChallengeLastCompletedUtcDate;
            Daily.FreeBoosterAdUtcDate = source.Daily.FreeBoosterAdUtcDate;
            Collection.Clear();
            Collection.AddRange(source.Collection);
            Settings.Music = source.Settings.Music;
            Settings.Sfx = source.Settings.Sfx;
            Settings.Haptics = source.Settings.Haptics;
            Settings.Speed2x = source.Settings.Speed2x;
            Settings.HomePetals = source.Settings.HomePetals;
            Settings.Language = source.Settings.Language;
            Profile.Name = source.Profile.Name;
            Profile.JoinedAt = source.Profile.JoinedAt;
            Stats.Counters.Clear();
            foreach (KeyValuePair<string, long> pair in source.Stats.Counters)
            {
                Stats.Counters[pair.Key] = pair.Value;
            }

            Stats.Groups.Clear();
            foreach (KeyValuePair<string, SortedDictionary<string, long>> group in source.Stats.Groups)
            {
                Stats.Groups[group.Key] = new SortedDictionary<string, long>(group.Value, StringComparer.Ordinal);
            }
        }
    }

    public sealed class ProgressionData
    {
        private int _highest;
        private int _contentVersionSeen = 1;

        public int HighestCompletedLevel
        {
            get => _highest;
            set => _highest = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Levels are never negative.");
        }

        /// <summary>The level Home shows and Play starts: highest completed + 1.</summary>
        public int CurrentLevel => _highest + 1;

        public int ContentVersionSeen
        {
            get => _contentVersionSeen;
            set => _contentVersionSeen = Math.Max(1, value);
        }
    }

    public sealed class WalletData
    {
        public int Petals { get; private set; }

        public void AddPetals(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Use TrySpendPetals to remove Petals.");
            }

            Petals = checked(Petals + amount);
        }

        /// <summary>Spends only when the balance covers the cost, so Petals never go negative.</summary>
        public bool TrySpendPetals(int amount)
        {
            if (amount < 0 || amount > Petals)
            {
                return false;
            }

            Petals -= amount;
            return true;
        }

        internal void SetPetals(int petals) => Petals = petals >= 0 ? petals : throw new ArgumentOutOfRangeException(nameof(petals));
    }

    public sealed class BoosterCharges
    {
        private readonly int[] _charges = new int[4];

        public int Get(BoosterKind kind) => _charges[(int)kind];

        public void Add(BoosterKind kind, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Use TryUse to consume charges.");
            }

            _charges[(int)kind] = checked(_charges[(int)kind] + amount);
        }

        /// <summary>Consumes one charge if there is one; charges never go negative.</summary>
        public bool TryUse(BoosterKind kind)
        {
            if (_charges[(int)kind] == 0)
            {
                return false;
            }

            _charges[(int)kind]--;
            return true;
        }

        internal void Set(BoosterKind kind, int value) => _charges[(int)kind] = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    public sealed record BoosterGrant(int ExtraSlot, int Shuffle, int Return, int BloomBurst);

    public sealed record LedgerEntry(string TransactionId, string ProductId, string GrantedAt, int GrantedPetals, BoosterGrant? GrantedBoosters);

    public sealed class PurchasesData
    {
        private readonly List<LedgerEntry> _ledger = new List<LedgerEntry>();

        public IReadOnlyList<LedgerEntry> Ledger => _ledger;

        public bool RemoveAds { get; set; }

        public bool StarterPackOffered { get; set; }

        public bool Contains(string transactionId) => _ledger.Exists(e => e.TransactionId == transactionId);

        internal void Assign(PurchasesData source)
        {
            _ledger.Clear();
            _ledger.AddRange(source._ledger);
            RemoveAds = source.RemoveAds;
            StarterPackOffered = source.StarterPackOffered;
        }

        /// <summary>Adds a ledger entry once per transaction id (idempotent grants, R13).</summary>
        public bool TryAdd(LedgerEntry entry)
        {
            if (Contains(entry.TransactionId))
            {
                return false;
            }

            _ledger.Add(entry);
            return true;
        }
    }

    public sealed class UnlocksData
    {
        private readonly List<string> _demosSeen = new List<string>();

        /// <summary>Unlock flags keyed by roadmap unlock id, sorted for a stable file.</summary>
        public SortedDictionary<string, bool> Flags { get; } = new SortedDictionary<string, bool>(StringComparer.Ordinal);

        public IReadOnlyList<string> DemosSeen => _demosSeen;

        public bool IsSet(string unlockId) => Flags.TryGetValue(unlockId, out bool value) && value;

        public bool HasSeenDemo(string demoId) => _demosSeen.Contains(demoId);

        internal void Assign(UnlocksData source)
        {
            Flags.Clear();
            foreach (KeyValuePair<string, bool> pair in source.Flags)
            {
                Flags[pair.Key] = pair.Value;
            }

            _demosSeen.Clear();
            _demosSeen.AddRange(source._demosSeen);
        }

        public bool MarkDemoSeen(string demoId)
        {
            if (_demosSeen.Contains(demoId))
            {
                return false;
            }

            _demosSeen.Add(demoId);
            return true;
        }
    }

    public sealed class MilestonesData
    {
        private readonly SortedSet<int> _claimed = new SortedSet<int>();

        public IReadOnlyCollection<int> Claimed => _claimed;

        /// <summary>Each milestone is granted exactly once (FR-061).</summary>
        public bool TryClaim(int level) => level >= 1 && _claimed.Add(level);

        public bool IsClaimed(int level) => _claimed.Contains(level);

        internal void Assign(MilestonesData source)
        {
            _claimed.Clear();
            _claimed.UnionWith(source._claimed);
        }
    }

    public sealed class CosmeticsData
    {
        /// <summary>The owner of the profile slots (frame, badge, marker, avatar) in <see cref="Equipped"/>.</summary>
        public const string ProfileOwner = "profile";

        /// <summary>The profile slot of the chosen avatar picture (spec 005 FR-037): <c>profile.avatar</c>.</summary>
        public const string AvatarKind = "avatar";

        /// <summary>The kinds of slot each owner has (the family slots are worn, the profile slots are shown).</summary>
        public static string[] KindsOf(string owner) => owner == ProfileOwner
            ? new[] { "frame", "badge", "marker", AvatarKind }
            : new[] { "skin", "hat", "trail", "expression" };

        public SortedSet<string> Owned { get; } = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Slot → item id. A slot is <c>owner.kind</c>: a family wears one skin, hat, trail and expression
        /// (<c>drop.hat</c>), and the profile shows one frame, badge, marker and avatar (<c>profile.frame</c>,
        /// <c>profile.avatar</c>). The bought avatars are in <see cref="Owned"/> by their ids (<c>avatar.drop_sailor_sticker</c>).
        /// </summary>
        public SortedDictionary<string, string> Equipped { get; } = new SortedDictionary<string, string>(StringComparer.Ordinal);

        public static string Slot(string owner, string kind) => owner + "." + kind;
    }

    /// <summary>
    /// The profile page's own fields (spec 005 FR-037), optional in the save as <c>profile</c>: the player's chosen name
    /// (null while they keep the default one) and the UTC day they joined (<c>yyyy-MM-dd</c>, set once).
    /// </summary>
    public sealed class ProfileData
    {
        public string? Name { get; set; }

        public string? JoinedAt { get; set; }
    }

    public sealed class DailyData
    {
        /// <summary>UTC date <c>yyyy-MM-dd</c> of the last Daily Reward claim, or null.</summary>
        public string? RewardLastClaimUtcDate { get; set; }

        public int RewardStreak { get; set; }

        public string? ChallengeLastCompletedUtcDate { get; set; }

        /// <summary>UTC date <c>yyyy-MM-dd</c> of the last free-booster rewarded ad on Home, or null.</summary>
        public string? FreeBoosterAdUtcDate { get; set; }
    }

    public sealed record CollectionEntry(string PictureId, int PictureVersion, string MappingHash, int LevelNumber);

    public sealed class SettingsData
    {
        public bool Music { get; set; } = true;

        public bool Sfx { get; set; } = true;

        public bool Haptics { get; set; } = true;

        /// <summary>Default animation speed for new levels (FR-069).</summary>
        public bool Speed2x { get; set; }

        /// <summary>
        /// Whether Home shows its falling petals (spec 005 FR-028; the owner's Settings switch of 2026-10-04). Optional in
        /// the save as <c>homePetalsOn</c>, off by default since the owner's tuning of 2026-10-05 (spec 005 FR-036); the
        /// earlier <c>homePetals</c>, which every save wrote as on, is read and ignored, so the petals start off.
        /// </summary>
        public bool HomePetals { get; set; }

        public string Language { get; set; } = "en";
    }

    public sealed class StatsData
    {
        public SortedDictionary<string, long> Counters { get; } = new SortedDictionary<string, long>(StringComparer.Ordinal);

        public SortedDictionary<string, SortedDictionary<string, long>> Groups { get; } =
            new SortedDictionary<string, SortedDictionary<string, long>>(StringComparer.Ordinal);

        public void Increment(string counter, long by = 1) =>
            Counters[counter] = (Counters.TryGetValue(counter, out long value) ? value : 0) + by;

        public void Increment(string group, string key, long by = 1)
        {
            if (!Groups.TryGetValue(group, out SortedDictionary<string, long>? counters))
            {
                counters = new SortedDictionary<string, long>(StringComparer.Ordinal);
                Groups[group] = counters;
            }

            counters[key] = (counters.TryGetValue(key, out long value) ? value : 0) + by;
        }
    }
}
