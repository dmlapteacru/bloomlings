using System;
using System.IO;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Purchases;
using Bloomlings.Client.Services.Save;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>Idempotent purchase grants and restoring Remove Ads (FR-054, FR-089; T125).</summary>
    public class PurchaseLedgerTests
    {
        private sealed class FixedClock : IClock
        {
            public DateTime UtcNow { get; set; } = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

            public DateTime UtcToday => UtcNow.Date;
        }

        private PlayerSave _save = null!;
        private EconomyService _economy = null!;
        private PurchaseLedger _ledger = null!;

        private static ProductCatalog Catalog => ProductCatalog.Parse(File.ReadAllText(Path.Combine(
            Services.Content.DevContent.RepositoryContentFolder, "..", "client", "Assets", "Bloomlings", "Services", "Purchases", "Resources", "ProductCatalog.json")));

        [SetUp]
        public void SetUp()
        {
            _save = PlayerSave.CreateNew("p1", new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc));
            _economy = new EconomyService(_save, EconomyConfig.Bundled, () => { });
            _ledger = new PurchaseLedger(_save, Catalog, _economy, new FixedClock(), () => { });
        }

        [Test]
        public void Grants_AreIdempotentByTransactionId()
        {
            Assert.That(_ledger.Grant(new ValidatedPurchase("t-1", "petals_m")), Is.True);
            Assert.That(_ledger.Grant(new ValidatedPurchase("t-1", "petals_m")), Is.False, "the same transaction again");
            Assert.That(_economy.Petals, Is.EqualTo(400));

            Assert.That(_ledger.Grant(new ValidatedPurchase("t-2", "petals_m")), Is.True, "a new purchase of the same product");
            Assert.That(_economy.Petals, Is.EqualTo(800));
            Assert.That(_save.Purchases.Ledger.Count, Is.EqualTo(2));
        }

        [Test]
        public void BoosterBundles_AndTheStarterPack_Grant()
        {
            _ledger.Grant(new ValidatedPurchase("t-3", "starter_pack"));

            Assert.That(_economy.Petals, Is.EqualTo(300));
            Assert.That(_economy.Charges(BoosterKind.BloomBurst), Is.EqualTo(2));
            Assert.That(_save.Purchases.StarterPackOffered, Is.True, "offered once");
        }

        [Test]
        public void RemoveAds_IsRestoredOnANewDevice()
        {
            _ledger.Grant(new ValidatedPurchase("t-ads", "remove_ads"));
            Assert.That(_ledger.RemoveAds, Is.True);

            // A new device: an empty save, and the store lists what the account owns.
            var fresh = PlayerSave.CreateNew("p2", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
            var ledger = new PurchaseLedger(fresh, Catalog, new EconomyService(fresh, EconomyConfig.Bundled, () => { }), new FixedClock(), () => { });
            int restored = ledger.Restore(new[] { new ValidatedPurchase("t-ads", "remove_ads") });

            Assert.That(restored, Is.EqualTo(1));
            Assert.That(ledger.RemoveAds, Is.True);
            Assert.That(ledger.Restore(new[] { new ValidatedPurchase("t-ads", "remove_ads") }), Is.EqualTo(0), "restoring twice grants nothing more");
        }

        [Test]
        public void UnknownProducts_GrantNothing()
        {
            Assert.That(_ledger.Grant(new ValidatedPurchase("t-x", "gold_bars")), Is.False);
            Assert.That(_save.Purchases.Ledger, Is.Empty);
        }
    }
}
