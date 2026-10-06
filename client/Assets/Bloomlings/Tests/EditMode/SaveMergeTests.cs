using System;
using System.Linq;
using Bloomlings.Client.Services.Save;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>Cloud save merge (research R15, FR-087; T135).</summary>
    public class SaveMergeTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

        private static PlayerSave Save(string id, int highest, DateTime updated, int petals = 0)
        {
            PlayerSave save = PlayerSave.CreateNew(id, updated);
            save.Progression.HighestCompletedLevel = highest;
            save.Wallet.AddPetals(petals);
            return save;
        }

        private static LedgerEntry Purchase(string tx, int petals, BoosterGrant? boosters = null) =>
            new LedgerEntry(tx, "petals_s", "2026-09-29T12:00:00Z", petals, boosters);

        [Test]
        public void Base_IsTheHigherLevel_ThenTheLaterUpdate()
        {
            PlayerSave local = Save("local", 40, T0.AddHours(2), petals: 10);
            PlayerSave remote = Save("remote", 55, T0, petals: 70);

            PlayerSave merged = SaveMerge.Merge(local, remote);
            Assert.That(merged.Progression.HighestCompletedLevel, Is.EqualTo(55), "the furthest progression wins");
            Assert.That(merged.Wallet.Petals, Is.EqualTo(70), "Petals come from the base");
            Assert.That(merged.LocalPlayerId, Is.EqualTo("local"), "the identity stays the local device's");

            PlayerSave tieLocal = Save("local", 40, T0, petals: 10);
            PlayerSave tieRemote = Save("remote", 40, T0.AddMinutes(5), petals: 90);
            Assert.That(SaveMerge.Merge(tieLocal, tieRemote).Wallet.Petals, Is.EqualTo(90), "on a level tie the later update wins");
            Assert.That(SaveMerge.Merge(tieRemote, tieLocal).Wallet.Petals, Is.EqualTo(90), "the order of the arguments does not matter");
        }

        [Test]
        public void Entitlements_Cosmetics_AndMilestones_AreUnioned()
        {
            PlayerSave local = Save("local", 60, T0.AddHours(1));
            local.Cosmetics.Owned.Add("hat.leaf_cap");
            local.Milestones.TryClaim(25);
            local.Milestones.TryClaim(50);
            local.Unlocks.Flags["system.wardrobe"] = true;
            local.Collection.Add(new CollectionEntry("fish_01", 1, "aaaa", 11));

            PlayerSave remote = Save("remote", 30, T0);
            remote.Purchases.RemoveAds = true;
            remote.Cosmetics.Owned.Add("frame.daisy");
            remote.Cosmetics.Owned.Add("hat.acorn_cap");
            remote.Cosmetics.Equipped["drop.hat"] = "hat.acorn_cap";
            remote.Milestones.TryClaim(25);
            remote.Unlocks.MarkDemoSeen("mechanic.key");
            remote.Collection.Add(new CollectionEntry("fish_01", 1, "aaaa", 11));
            remote.Collection.Add(new CollectionEntry("bee_01", 1, "bbbb", 12));

            PlayerSave merged = SaveMerge.Merge(local, remote);
            Assert.That(merged.Purchases.RemoveAds, Is.True);
            Assert.That(merged.Cosmetics.Owned, Is.EquivalentTo(new[] { "hat.leaf_cap", "frame.daisy", "hat.acorn_cap" }));
            Assert.That(merged.Cosmetics.Equipped["drop.hat"], Is.EqualTo("hat.acorn_cap"), "slots the base leaves empty are filled");
            Assert.That(merged.Milestones.Claimed, Is.EqualTo(new[] { 25, 50 }));
            Assert.That(merged.Unlocks.IsSet("system.wardrobe"), Is.True);
            Assert.That(merged.Unlocks.HasSeenDemo("mechanic.key"), Is.True);
            Assert.That(merged.Collection.Select(c => c.LevelNumber), Is.EqualTo(new[] { 11, 12 }), "no duplicates, ordered by level");
        }

        [Test]
        public void MissingLedgerEntries_AreReappliedOnce()
        {
            PlayerSave local = Save("local", 20, T0, petals: 50);
            local.Purchases.TryAdd(Purchase("tx-local", 0));

            PlayerSave remote = Save("remote", 10, T0, petals: 999);
            remote.Purchases.TryAdd(Purchase("tx-local", 0));
            remote.Purchases.TryAdd(Purchase("tx-remote", 120, new BoosterGrant(1, 0, 2, 0)));

            PlayerSave merged = SaveMerge.Merge(local, remote);
            Assert.That(merged.Wallet.Petals, Is.EqualTo(170), "base 50 + the missing purchase's 120; the remote balance is ignored");
            Assert.That(merged.Boosters.Get(BoosterKind.ExtraSlot), Is.EqualTo(1));
            Assert.That(merged.Boosters.Get(BoosterKind.Return), Is.EqualTo(2));
            Assert.That(merged.Purchases.Ledger.Select(e => e.TransactionId), Is.EquivalentTo(new[] { "tx-local", "tx-remote" }));

            PlayerSave again = SaveMerge.Merge(merged, remote);
            Assert.That(again.Wallet.Petals, Is.EqualTo(170), "merging again is idempotent");
            Assert.That(again.Boosters.Get(BoosterKind.Return), Is.EqualTo(2));
            Assert.That(SaveSerializer.Write(SaveMerge.Merge(again, merged)), Is.EqualTo(SaveSerializer.Write(again)));
        }

        [Test]
        public void DailyClaims_KeepTheLaterDate_AndInputsAreNotModified()
        {
            PlayerSave local = Save("local", 20, T0);
            local.Daily.RewardLastClaimUtcDate = "2026-09-28";
            local.Daily.RewardStreak = 4;
            PlayerSave remote = Save("remote", 10, T0);
            remote.Daily.RewardLastClaimUtcDate = "2026-09-29";
            remote.Daily.RewardStreak = 1;
            remote.Daily.ChallengeLastCompletedUtcDate = "2026-09-29";
            remote.Stats.Increment("levelsWon", 30);
            string before = SaveSerializer.Write(local);

            PlayerSave merged = SaveMerge.Merge(local, remote);
            Assert.That(merged.Daily.RewardLastClaimUtcDate, Is.EqualTo("2026-09-29"), "a reward claimed elsewhere today is not claimable again");
            Assert.That(merged.Daily.RewardStreak, Is.EqualTo(1));
            Assert.That(merged.Daily.ChallengeLastCompletedUtcDate, Is.EqualTo("2026-09-29"));
            Assert.That(merged.Stats.Counters["levelsWon"], Is.EqualTo(30));
            Assert.That(SaveSerializer.Write(local), Is.EqualTo(before));
        }
    }
}
