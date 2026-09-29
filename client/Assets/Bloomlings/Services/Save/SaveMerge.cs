using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bloomlings.Client.Services.Save
{
    /// <summary>
    /// Merges the local save with the cloud save (research R15, FR-087, T139). The result preserves the furthest valid
    /// progression and never loses a purchase:
    /// <list type="number">
    /// <item>The base is the save with the higher <c>highestCompletedLevel</c>, then the later <c>updatedAt</c> (the
    /// local save on a full tie).</item>
    /// <item>Entitlements (Remove Ads, the starter pack offer), unlock flags, demos seen, owned cosmetics, claimed
    /// milestones and Collection entries are the union of both saves.</item>
    /// <item>Petals and booster charges come from the base, plus every purchase-ledger entry the base is missing,
    /// re-applied once by its transaction id, so merging again changes nothing.</item>
    /// </list>
    /// The identity fields (<c>localPlayerId</c>, <c>deviceId</c>) stay the local device's. Daily claims keep the later
    /// date on each side, so a reward claimed on another device today is not claimable again; statistics keep the larger
    /// value of each counter. Neither input is modified.
    /// </summary>
    public static class SaveMerge
    {
        public static PlayerSave Merge(PlayerSave local, PlayerSave remote)
        {
            bool localIsBase = CompareProgress(local, remote) >= 0;
            PlayerSave merged = Clone(localIsBase ? local : remote);
            PlayerSave other = localIsBase ? remote : local;

            merged.LocalPlayerId = local.LocalPlayerId;
            merged.DeviceId = local.DeviceId;
            merged.LinkedIdentity = local.LinkedIdentity ?? remote.LinkedIdentity;
            merged.UpdatedAt = ParseTime(local.UpdatedAt) >= ParseTime(remote.UpdatedAt) ? local.UpdatedAt : remote.UpdatedAt;
            merged.Progression.ContentVersionSeen = Math.Max(local.Progression.ContentVersionSeen, remote.Progression.ContentVersionSeen);

            // Purchases: entitlements are a union; grants missing from the base are applied once each.
            merged.Purchases.RemoveAds |= other.Purchases.RemoveAds;
            merged.Purchases.StarterPackOffered |= other.Purchases.StarterPackOffered;
            foreach (LedgerEntry entry in other.Purchases.Ledger)
            {
                if (!merged.Purchases.TryAdd(entry))
                {
                    continue;
                }

                if (entry.GrantedPetals > 0)
                {
                    merged.Wallet.AddPetals(entry.GrantedPetals);
                }

                if (entry.GrantedBoosters != null)
                {
                    merged.Boosters.Add(BoosterKind.ExtraSlot, entry.GrantedBoosters.ExtraSlot);
                    merged.Boosters.Add(BoosterKind.Shuffle, entry.GrantedBoosters.Shuffle);
                    merged.Boosters.Add(BoosterKind.Return, entry.GrantedBoosters.Return);
                    merged.Boosters.Add(BoosterKind.BloomBurst, entry.GrantedBoosters.BloomBurst);
                }
            }

            foreach (KeyValuePair<string, bool> flag in other.Unlocks.Flags)
            {
                if (flag.Value)
                {
                    merged.Unlocks.Flags[flag.Key] = true;
                }
            }

            foreach (string demo in other.Unlocks.DemosSeen)
            {
                merged.Unlocks.MarkDemoSeen(demo);
            }

            foreach (int level in other.Milestones.Claimed)
            {
                merged.Milestones.TryClaim(level);
            }

            merged.Cosmetics.Owned.UnionWith(other.Cosmetics.Owned);
            foreach (KeyValuePair<string, string> slot in other.Cosmetics.Equipped)
            {
                if (!merged.Cosmetics.Equipped.ContainsKey(slot.Key))
                {
                    merged.Cosmetics.Equipped[slot.Key] = slot.Value;
                }
            }

            MergeDaily(merged.Daily, other.Daily);
            MergeCollection(merged.Collection, other.Collection);
            MergeStats(merged.Stats, other.Stats);
            return merged;
        }

        /// <summary>Positive when <paramref name="a"/> is further: higher level, then later <c>updatedAt</c>.</summary>
        public static int CompareProgress(PlayerSave a, PlayerSave b)
        {
            int byLevel = a.Progression.HighestCompletedLevel.CompareTo(b.Progression.HighestCompletedLevel);
            return byLevel != 0 ? byLevel : ParseTime(a.UpdatedAt).CompareTo(ParseTime(b.UpdatedAt));
        }

        /// <summary>A deep copy through the save document.</summary>
        public static PlayerSave Clone(PlayerSave save) => SaveSerializer.Read(SaveSerializer.ToJObject(save));

        private static void MergeDaily(DailyData merged, DailyData other)
        {
            int byDate = string.CompareOrdinal(other.RewardLastClaimUtcDate ?? string.Empty, merged.RewardLastClaimUtcDate ?? string.Empty);
            if (byDate > 0 || (byDate == 0 && other.RewardStreak > merged.RewardStreak))
            {
                merged.RewardLastClaimUtcDate = other.RewardLastClaimUtcDate;
                merged.RewardStreak = other.RewardStreak;
            }

            if (string.CompareOrdinal(other.ChallengeLastCompletedUtcDate ?? string.Empty, merged.ChallengeLastCompletedUtcDate ?? string.Empty) > 0)
            {
                merged.ChallengeLastCompletedUtcDate = other.ChallengeLastCompletedUtcDate;
            }
        }

        private static void MergeCollection(List<CollectionEntry> merged, IReadOnlyList<CollectionEntry> other)
        {
            var seen = new HashSet<CollectionEntry>(merged);
            foreach (CollectionEntry entry in other)
            {
                if (seen.Add(entry))
                {
                    merged.Add(entry);
                }
            }

            merged.Sort((a, b) => a.LevelNumber != b.LevelNumber ? a.LevelNumber.CompareTo(b.LevelNumber) : string.CompareOrdinal(a.PictureId, b.PictureId));
        }

        private static void MergeStats(StatsData merged, StatsData other)
        {
            foreach (KeyValuePair<string, long> counter in other.Counters)
            {
                merged.Counters[counter.Key] = Math.Max(merged.Counters.TryGetValue(counter.Key, out long value) ? value : 0, counter.Value);
            }

            foreach (KeyValuePair<string, SortedDictionary<string, long>> group in other.Groups)
            {
                foreach (KeyValuePair<string, long> counter in group.Value)
                {
                    long current = merged.Groups.TryGetValue(group.Key, out SortedDictionary<string, long>? counters) && counters.TryGetValue(counter.Key, out long value) ? value : 0;
                    if (counter.Value > current)
                    {
                        merged.GroupOf(group.Key)[counter.Key] = counter.Value;
                    }
                }
            }
        }

        private static SortedDictionary<string, long> GroupOf(this StatsData stats, string group)
        {
            if (!stats.Groups.TryGetValue(group, out SortedDictionary<string, long>? counters))
            {
                counters = new SortedDictionary<string, long>(StringComparer.Ordinal);
                stats.Groups[group] = counters;
            }

            return counters;
        }

        private static DateTime ParseTime(string text) =>
            DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime time)
                ? time
                : DateTime.MinValue;
    }
}
