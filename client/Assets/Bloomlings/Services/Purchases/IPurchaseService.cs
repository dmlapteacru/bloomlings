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
    /// after the grant is saved. The grant callback is known from <see cref="Initialize"/> on, so an order the store
    /// delivers at startup (a purchase interrupted in an earlier session) is granted too, never confirmed unpaid-out.
    /// </summary>
    public interface IPurchaseService
    {
        bool IsAvailable { get; }

        /// <summary>The store's localized price text, or null while unknown.</summary>
        string? PriceOf(string productId);

        /// <summary>The store's price in micros (price × 1,000,000) and its ISO 4217 currency, or null while unknown.</summary>
        (long Micros, string Currency)? PriceDetailsOf(string productId);

        /// <summary>Connects; every validated purchase, whenever it arrives, is handed to <paramref name="grant"/> (the ledger).</summary>
        void Initialize(ProductCatalog catalog, Func<ValidatedPurchase, bool> grant, Action<bool> onReady);

        void Buy(string productId, Action<PurchaseResult> onDone);

        /// <summary>Restore Purchases (FR-073): every owned purchase is validated again and granted if missing.</summary>
        void Restore(Action<bool> onDone);

        /// <summary>
        /// Asks the backend whether the once-only starter pack can still be bought by this player (Cloud Code
        /// <c>GetStarterPackOffer</c>); null when it cannot tell (offline).
        /// </summary>
        void CheckStarterPackOffer(Action<bool?> onAnswer);
    }

    /// <summary>No store: offline, in the Editor without IAP, or when the store failed to connect.</summary>
    public sealed class UnavailablePurchaseService : IPurchaseService
    {
        public bool IsAvailable => false;

        public string? PriceOf(string productId) => null;

        public (long Micros, string Currency)? PriceDetailsOf(string productId) => null;

        public void Initialize(ProductCatalog catalog, Func<ValidatedPurchase, bool> grant, Action<bool> onReady) => onReady(false);

        public void Buy(string productId, Action<PurchaseResult> onDone) => onDone(PurchaseResult.Unavailable);

        public void Restore(Action<bool> onDone) => onDone(false);

        public void CheckStarterPackOffer(Action<bool?> onAnswer) => onAnswer(null);
    }
}
