using System;
using System.Collections.Generic;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>Petals, charges, unlock grants, drops and remote clamping (T114).</summary>
    public class EconomyServiceTests
    {
        private PlayerSave _save = null!;
        private EconomyService _economy = null!;
        private int _saves;

        [SetUp]
        public void SetUp()
        {
            _save = PlayerSave.CreateNew("p1", new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc));
            _saves = 0;
            _economy = new EconomyService(_save, EconomyConfig.Bundled, () => _saves++);
        }

        private void Unlock(string id)
        {
            _save.Unlocks.Flags[id] = true;
            _economy.OnUnlock(id);
        }

        [Test]
        public void Win_PaysBasePlusCleanBonusPlusDifficultyBonus()
        {
            EconomyConfig c = EconomyConfig.Bundled;

            Assert.That(_economy.WinPetals(DifficultyClass.Normal, boostersUsed: 0), Is.EqualTo(c.PetalsBase + c.CleanBonus));
            Assert.That(_economy.WinPetals(DifficultyClass.Normal, boostersUsed: 1), Is.EqualTo(c.PetalsBase));
            Assert.That(_economy.WinPetals(DifficultyClass.Hard, boostersUsed: 0), Is.EqualTo(c.PetalsBase + c.CleanBonus + c.HardBonus));
            Assert.That(_economy.WinPetals(DifficultyClass.SuperHard, boostersUsed: 2), Is.EqualTo(c.PetalsBase + c.SuperHardBonus));

            LevelReward reward = _economy.GrantLevelReward(7, DifficultyClass.Normal, 0);
            Assert.That(reward.Petals, Is.EqualTo(20));
            Assert.That(_economy.Petals, Is.EqualTo(20));
            Assert.That(_saves, Is.EqualTo(1));
        }

        [Test]
        public void UnlockGrantsExactlyOneCharge_Once()
        {
            Unlock("booster.extra_slot");
            _economy.OnUnlock("booster.extra_slot");
            _economy.OnUnlock("system.store");

            Assert.That(_economy.Charges(BoosterKind.ExtraSlot), Is.EqualTo(1));
            Assert.That(_economy.Charges(BoosterKind.Shuffle), Is.EqualTo(0));
        }

        [Test]
        public void BuyingABooster_SpendsPetals_AndGrantsTheWholePack()
        {
            Assert.That(_economy.TryBuy(BoosterKind.Shuffle), Is.False, "not unlocked yet");
            Unlock("booster.shuffle");
            Assert.That(_economy.TryBuy(BoosterKind.Shuffle), Is.False, "no Petals");
            Assert.That(_economy.Petals, Is.EqualTo(0));

            _economy.Grant(EconomyConfig.Bundled.PriceShuffle + 100, null);
            Assert.That(_economy.TryBuy(BoosterKind.Shuffle), Is.True);
            Assert.That(_economy.Petals, Is.EqualTo(100));
            Assert.That(_economy.Charges(BoosterKind.Shuffle), Is.EqualTo(4), "one unlock charge plus a three-charge purchase");

            Unlock("booster.bloom_burst");
            Assert.That(_economy.Charges(BoosterKind.BloomBurst), Is.Zero, "the strong booster is not granted at unlock");
            _economy.Grant(EconomyConfig.Bundled.PriceBloomBurst, null);
            Assert.That(_economy.TryBuy(BoosterKind.BloomBurst), Is.True);
            Assert.That(_economy.Charges(BoosterKind.BloomBurst), Is.EqualTo(1), "Bloom Burst is sold singly");
        }

        [Test]
        public void UsingABooster_ConsumesACharge_OrBuysOne()
        {
            Unlock("booster.return");
            Assert.That(_economy.TryTakeCharge(BoosterKind.Return), Is.True);
            Assert.That(_economy.Charges(BoosterKind.Return), Is.EqualTo(0), "the free unlock charge was used");
            Assert.That(_economy.TryTakeCharge(BoosterKind.Return), Is.False, "no charge and no Petals");
            Assert.That(_economy.Charges(BoosterKind.Return), Is.EqualTo(0), "charges never go negative");

            _economy.Grant(EconomyConfig.Bundled.PriceReturn, null);
            Assert.That(_economy.TryTakeCharge(BoosterKind.Return), Is.True);
            Assert.That(_economy.Petals, Is.EqualTo(0));
            Assert.That(_economy.Charges(BoosterKind.Return), Is.EqualTo(2), "one charge used from the newly bought pack");
        }

        [Test]
        public void Drops_StartAtL20_AndNeverContainBloomBurst()
        {
            Unlock("booster.extra_slot");
            Unlock("booster.shuffle");
            Unlock("booster.return");
            Unlock("booster.bloom_burst");
            var drops = new List<BoosterKind?>();
            for (int level = 1; level <= 60; level++)
            {
                drops.Add(_economy.DropFor(level));
            }

            Assert.That(drops[9], Is.Null, "nothing on L10");
            Assert.That(drops[19], Is.EqualTo(BoosterKind.ExtraSlot));
            Assert.That(drops[29], Is.EqualTo(BoosterKind.Shuffle));
            Assert.That(drops[39], Is.EqualTo(BoosterKind.Return));
            Assert.That(drops[49], Is.Null, "L50 gives its milestone reward instead");
            Assert.That(drops[59], Is.EqualTo(BoosterKind.ExtraSlot), "the rotation skips the milestone level");
            Assert.That(drops.FindAll(d => d.HasValue).Count, Is.EqualTo(4));
            Assert.That(_economy.DropFor(100), Is.Null, "L100 gives its major milestone instead");
            Assert.That(_economy.DropFor(110), Is.EqualTo(BoosterKind.Shuffle));
            Assert.That(drops, Has.None.EqualTo(BoosterKind.BloomBurst));
        }

        [Test]
        public void RemoteValues_AreClampedWithoutForcingTheSingleBurstAboveThreePacks()
        {
            var remote = new Dictionary<string, int>
            {
                ["economy.petals.base"] = 1000,
                ["economy.price.extraSlot"] = 1,
                ["economy.price.bloomBurst"] = 20,
                ["economy.price.return"] = 80,
                ["economy.drop.everyLevels"] = 0,
            };

            EconomyConfig config = EconomyConfig.Read(key => remote.TryGetValue(key.Name, out int v) ? v : key.Default);

            Assert.That(config.PetalsBase, Is.EqualTo(50));
            Assert.That(config.PriceExtraSlot, Is.EqualTo(10));
            Assert.That(config.DropEveryLevels, Is.EqualTo(2));
            Assert.That(config.PriceBloomBurst, Is.EqualTo(20));

            // A single Bloom Burst can cost less than a three-charge pack.
            var capped = new Dictionary<string, int>
            {
                ["economy.price.extraSlot"] = 9999,
                ["economy.price.shuffle"] = 9999,
                ["economy.price.return"] = 9999,
                ["economy.price.bloomBurst"] = 1,
            };
            EconomyConfig top = EconomyConfig.Read(key => capped.TryGetValue(key.Name, out int v) ? v : key.Default);
            Assert.That(top.PriceReturn, Is.EqualTo(5000));
            Assert.That(top.PriceBloomBurst, Is.EqualTo(10));
        }

        [Test]
        public void ProgressionUnlocks_FeedTheEconomy()
        {
            var progression = new ProgressionService(_save, UnlockRoadmap.Default, () => { });
            progression.UnlockReached += entry => _economy.OnUnlock(entry.UnlockId);
            progression.Initialize();
            for (int level = 1; level <= 9; level++)
            {
                progression.CompleteLevel(level);
            }

            Assert.That(_economy.Charges(BoosterKind.ExtraSlot), Is.EqualTo(1));
            Assert.That(_economy.Charges(BoosterKind.Shuffle), Is.EqualTo(1));
            Assert.That(_economy.Charges(BoosterKind.Return), Is.EqualTo(1));
            Assert.That(_economy.Charges(BoosterKind.BloomBurst), Is.Zero, "the guided demo at L9 is free; future free charges come only from milestones");
        }
    }
}
