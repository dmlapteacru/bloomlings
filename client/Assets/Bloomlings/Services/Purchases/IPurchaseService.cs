using System;
using System.Collections.Generic;

namespace Bloomlings.Client.Services.Purchases
{
    public enum PurchaseResult
    {
        /// <summary>Paid, validated by the backend and granted through the ledger.</summary>
        Granted,

        Cancelled,

        Failed,

        /// <summary>The store is offline or missing; the Store shows "unavailable" (FR-074).</summary>
        Unavailable,
    }

    /// <summary>
    /// In-app purchases (FR-051, FR-054, FR-089, research R13; T131): Unity IAP v5, with every receipt validated by the
    /// Cloud Code function <c>ValidatePurchase</c> before the ledger grants it. Consumables are confirmed to the store only
    /// after the grant is saved.
    /// </summary>
    public interface IPurchaseService
    {
        bool IsAvailable { get; }

        /// <summary>The store's localized price text, or null while unknown.</summary>
        string? PriceOf(string productId);

        void Initialize(ProductCatalog catalog, Action<bool> onReady);

        /// <summary>Buys a product; validated purchases are handed to <paramref name="grant"/> (the ledger).</summary>
        void Buy(string productId, Func<ValidatedPurchase, bool> grant, Action<PurchaseResult> onDone);

        /// <summary>Restore Purchases (FR-073): every owned purchase, validated, goes to <paramref name="grant"/>.</summary>
        void Restore(Func<ValidatedPurchase, bool> grant, Action<bool> onDone);
    }

    /// <summary>No store: offline, in the Editor without IAP, or when the store failed to connect.</summary>
    public sealed class UnavailablePurchaseService : IPurchaseService
    {
        public bool IsAvailable => false;

        public string? PriceOf(string productId) => null;

        public void Initialize(ProductCatalog catalog, Action<bool> onReady) => onReady(false);

        public void Buy(string productId, Func<ValidatedPurchase, bool> grant, Action<PurchaseResult> onDone) => onDone(PurchaseResult.Unavailable);

        public void Restore(Func<ValidatedPurchase, bool> grant, Action<bool> onDone) => onDone(false);
    }
}
