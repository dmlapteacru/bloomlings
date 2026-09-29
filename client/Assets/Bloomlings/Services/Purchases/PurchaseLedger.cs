using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Save;

namespace Bloomlings.Client.Services.Purchases
{
    /// <summary>A purchase the backend validated (Cloud Code <c>ValidatePurchase</c>, FR-089).</summary>
    public sealed record ValidatedPurchase(string TransactionId, string ProductId);

    /// <summary>
    /// Grants purchases exactly once (research R13, T131). Every grant is recorded in the save's ledger under its
    /// transaction id before anything is added, so a repeated callback, a restore or a cloud merge never grants twice,
    /// and a lost grant is re-applied on the next restore. Remove Ads is an entitlement that restores (FR-054).
    /// </summary>
    public sealed class PurchaseLedger
    {
        private readonly PlayerSave _save;
        private readonly ProductCatalog _catalog;
        private readonly EconomyService _economy;
        private readonly IClock _clock;
        private readonly Action _persist;

        public PurchaseLedger(PlayerSave save, ProductCatalog catalog, EconomyService economy, IClock clock, Action persist)
        {
            _save = save;
            _catalog = catalog;
            _economy = economy;
            _clock = clock;
            _persist = persist;
        }

        public bool RemoveAds => _save.Purchases.RemoveAds;

        /// <summary>Grants a validated purchase; false when this transaction was already granted or the product is unknown.</summary>
        public bool Grant(ValidatedPurchase purchase)
        {
            if (!_catalog.TryGet(purchase.ProductId, out StoreProduct? product) || product == null)
            {
                return false;
            }

            var entry = new LedgerEntry(purchase.TransactionId, product.Id, PlayerSave.FormatTime(_clock.UtcNow), product.Petals, product.Boosters);
            if (!_save.Purchases.TryAdd(entry))
            {
                // Already granted; a non-consumable still makes sure its entitlement is on.
                if (product.RemoveAds && !_save.Purchases.RemoveAds)
                {
                    _save.Purchases.RemoveAds = true;
                    _persist();
                }

                return false;
            }

            if (product.RemoveAds)
            {
                _save.Purchases.RemoveAds = true;
            }

            if (product.OfferedOnce)
            {
                _save.Purchases.StarterPackOffered = true;
            }

            _economy.Grant(product.Petals, product.Boosters);
            _persist();
            return true;
        }

        /// <summary>
        /// Restore Purchases (FR-073, FR-054): the store lists what the account owns; each is granted through the ledger,
        /// so only missing grants apply and Remove Ads comes back on a new device.
        /// </summary>
        public int Restore(IEnumerable<ValidatedPurchase> owned)
        {
            int granted = 0;
            foreach (ValidatedPurchase purchase in owned)
            {
                if (Grant(purchase))
                {
                    granted++;
                }
            }

            return granted;
        }
    }
}
