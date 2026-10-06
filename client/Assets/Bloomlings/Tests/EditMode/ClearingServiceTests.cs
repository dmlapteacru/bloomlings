using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Clearing;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>The board's clearing styles as board cosmetics (spec 005 FR-038; spec 001 FR-063, FR-051 as amended).</summary>
    public class ClearingServiceTests
    {
        private static readonly DateTime Today = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void TheFreePair_PlaysByLevel_UntilABoughtStyleIsChosen()
        {
            ClearingService clearing = Service(out _, out _);
            Assert.That(clearing.Price, Is.EqualTo(5000), "the owner's price, for now");
            Assert.That(clearing.Chosen, Is.Null);
            Assert.That(clearing.StyleFor(5), Is.EqualTo(ClearStyle.Blossom));
            Assert.That(clearing.StyleFor(44), Is.EqualTo(ClearStyle.Munchers));
            Assert.That(clearing.IsChosen(ClearStyle.Blossom) && clearing.IsChosen(ClearStyle.Munchers), Is.True, "the free card is the chosen one");
            Assert.That(clearing.Owns(ClearStyle.Munchers), Is.True);
            Assert.That(clearing.Owns(ClearStyle.Bubbles), Is.False);
        }

        [Test]
        public void ABoughtStyle_IsLockedBeforeLevel40_AndCostsItsPrice()
        {
            ClearingService clearing = Service(out PlayerSave save, out EconomyService economy);
            economy.Grant(12000, null);
            Assert.That(clearing.Tap(ClearStyle.Fireflies, 39), Is.EqualTo(ClearingTap.Locked), "the preview shows, the buying waits");
            Assert.That(economy.Petals, Is.EqualTo(12000));

            Assert.That(clearing.Tap(ClearStyle.Fireflies, 40), Is.EqualTo(ClearingTap.Bought));
            Assert.That(economy.Petals, Is.EqualTo(7000));
            Assert.That(clearing.Chosen, Is.EqualTo(ClearStyle.Fireflies));
            Assert.That(clearing.StyleFor(12), Is.EqualTo(ClearStyle.Fireflies), "a chosen style plays on every level");
            Assert.That(clearing.StyleFor(13), Is.EqualTo(ClearStyle.Fireflies));
            Assert.That(save.Cosmetics.Owned, Does.Contain("clear.fireflies"));

            Assert.That(clearing.Tap(ClearStyle.Bubbles, 41), Is.EqualTo(ClearingTap.Bought));
            Assert.That(clearing.Tap(ClearStyle.Pushers, 41), Is.EqualTo(ClearingTap.Short), "2000 Petals left");
            Assert.That(clearing.Owns(ClearStyle.Pushers), Is.False);
            Assert.That(clearing.Chosen, Is.EqualTo(ClearStyle.Bubbles), "a failed buy keeps the chosen style");
        }

        [Test]
        public void ChoosingAnOwnedStyle_OrTheFreeCard_IsFree()
        {
            ClearingService clearing = Service(out PlayerSave save, out EconomyService economy);
            var chose = new List<string>();
            clearing.Chose += chose.Add;
            economy.Grant(10000, null);
            clearing.Tap(ClearStyle.Parade, 50);
            clearing.Tap(ClearStyle.Pushers, 50);
            Assert.That(clearing.Tap(ClearStyle.Parade, 50), Is.EqualTo(ClearingTap.Chosen));
            Assert.That(economy.Petals, Is.EqualTo(0));
            Assert.That(clearing.Chosen, Is.EqualTo(ClearStyle.Parade));

            Assert.That(clearing.Tap(ClearStyle.Blossom, 50), Is.EqualTo(ClearingTap.Chosen), "the free card brings back the pair");
            Assert.That(clearing.Chosen, Is.Null);
            Assert.That(save.Cosmetics.Equipped.ContainsKey("board.clearing"), Is.False);
            Assert.That(clearing.StyleFor(50), Is.EqualTo(ClearStyle.Munchers));
            Assert.That(chose, Is.EqualTo(new[] { "clear.parade", "clear.pushers", "clear.parade", "clear.free" }));
            Assert.That(clearing.Choose(ClearStyle.Fireworks), Is.False, "not owned");
        }

        [Test]
        public void TheStyles_RoundTripThroughTheSave_AndMerge()
        {
            ClearingService clearing = Service(out PlayerSave save, out EconomyService economy);
            economy.Grant(5000, null);
            clearing.Tap(ClearStyle.Fireworks, 60);

            PlayerSave read = SaveSerializer.Read(SaveSerializer.Write(save));
            Assert.That(read.Cosmetics.Equipped["board.clearing"], Is.EqualTo("clear.fireworks"));
            Assert.That(read.Cosmetics.Owned, Does.Contain("clear.fireworks"));
            Assert.That(SaveSerializer.ToJObject(read)["cosmetics"]!["equipped"]!["board"]!["clearing"]!.ToString(), Is.EqualTo("clear.fireworks"));

            PlayerSave other = PlayerSave.CreateNew("p2", Today);
            other.Cosmetics.Owned.Add("clear.bubbles");
            PlayerSave merged = SaveMerge.Merge(read, other);
            Assert.That(merged.Cosmetics.Owned, Is.SupersetOf(new[] { "clear.fireworks", "clear.bubbles" }));
            Assert.That(merged.Cosmetics.Equipped["board.clearing"], Is.EqualTo("clear.fireworks"));

            // An edited save that names a style it does not own plays the free pair.
            read.Cosmetics.Owned.Remove("clear.fireworks");
            var again = new ClearingService(read, new BundledRemoteConfigService(), () => { });
            Assert.That(again.Chosen, Is.Null);
            Assert.That(again.StyleFor(12), Is.EqualTo(ClearStyle.Munchers));
        }

        [Test]
        public void EachCard_ShowsWhatItsTapDoes_AndCheckingBuysNothing()
        {
            // The Animations tab's buttons (FR-038 as amended on 2026-10-06): Buy, Choose, Chosen, or the padlock before L40;
            // Check says what a tap would do so the hosts ask the purchase confirmation first (FR-040).
            ClearingService clearing = Service(out PlayerSave save, out EconomyService economy);
            Assert.That(clearing.ActionOf(ClearStyle.Blossom, 39), Is.EqualTo(ClearingAction.Chosen), "the free pair while nothing is chosen");
            Assert.That(clearing.ActionOf(ClearStyle.Bubbles, 39), Is.EqualTo(ClearingAction.Locked));
            Assert.That(clearing.ActionOf(ClearStyle.Bubbles, 40), Is.EqualTo(ClearingAction.Buy));
            Assert.That(clearing.Check(ClearStyle.Bubbles, 39), Is.EqualTo(ClearingTap.Locked));
            Assert.That(clearing.Check(ClearStyle.Bubbles, 40), Is.EqualTo(ClearingTap.Short), "no Petals yet");

            economy.Grant(6000, null);
            Assert.That(clearing.Check(ClearStyle.Bubbles, 40), Is.EqualTo(ClearingTap.Bought));
            Assert.That(economy.Petals, Is.EqualTo(6000), "checking spends nothing");
            Assert.That(clearing.Owns(ClearStyle.Bubbles) || save.Cosmetics.Owned.Contains("clear.bubbles"), Is.False);

            Assert.That(clearing.Tap(ClearStyle.Bubbles, 40), Is.EqualTo(ClearingTap.Bought));
            Assert.That(clearing.ActionOf(ClearStyle.Bubbles, 40), Is.EqualTo(ClearingAction.Chosen));
            Assert.That(clearing.ActionOf(ClearStyle.Blossom, 40), Is.EqualTo(ClearingAction.Choose), "the free pair can be chosen back");
            Assert.That(clearing.Check(ClearStyle.Blossom, 40), Is.EqualTo(ClearingTap.Chosen));
            Assert.That(clearing.ActionOf(ClearStyle.Fireflies, 40), Is.EqualTo(ClearingAction.Buy));
            Assert.That(clearing.Check(ClearStyle.Fireflies, 40), Is.EqualTo(ClearingTap.Short), "1000 Petals left");
        }

        private static ClearingService Service(out PlayerSave save, out EconomyService economy)
        {
            save = PlayerSave.CreateNew("0a1b2c3d4e5f60718293a4b5c6d7e8f9", Today);
            economy = new EconomyService(save, EconomyConfig.Bundled, () => { });
            return new ClearingService(save, new BundledRemoteConfigService(), () => { }, economy);
        }
    }
}
