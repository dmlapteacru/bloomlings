#if BLOOMLINGS_IAP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bloomlings.Client.Services;
using Bloomlings.Client.Services.Purchases;
using Bloomlings.Client.Services.Save;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Bloomlings.Integrations.Iap
{
    /// <summary>
    /// <see cref="IPurchaseService"/> over Unity IAP v5 (research R13; T131). Every pending order goes to the Cloud Code
    /// function <c>ValidatePurchase</c> (FR-089); only a valid answer reaches the ledger, and the order is confirmed to
    /// the store after the grant is saved, so a crash in between re-delivers it and the ledger keeps it idempotent.
    /// <list type="bullet">
    /// <item>The ledger is known from <see cref="Initialize"/>, before connecting: an order left pending by an earlier
    /// session is delivered right after it and must be granted, not just confirmed.</item>
    /// <item>Owned purchases are fetched at start and on Restore; a confirmed non-consumable (Remove Ads) is validated
    /// and granted again, so it comes back on a new device. The ledger ignores what it already has.</item>
    /// <item>An order the backend refuses is not confirmed: Google Play refunds an unacknowledged purchase.</item>
    /// </list>
    /// </summary>
    internal sealed class UnityIapPurchaseService : IPurchaseService
    {
        private StoreController? _store;
        private ProductCatalog _catalog = new ProductCatalog(Array.Empty<StoreProduct>());
        private Func<ValidatedPurchase, bool>? _grant;
        private Action<PurchaseResult>? _onPurchaseDone;
        private Action<bool>? _onRestoreDone;

        public bool IsAvailable { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => ServiceProviders.Purchases = () => new UnityIapPurchaseService();

        public string? PriceOf(string productId) => _store?.GetProducts().FirstOrDefault(p => p.definition.id == productId)?.metadata.localizedPriceString;

        public async void Initialize(ProductCatalog catalog, Func<ValidatedPurchase, bool> grant, Action<bool> onReady)
        {
            _catalog = catalog;
            _grant = grant;
            try
            {
                _store = UnityIAPServices.StoreController();
                _store.OnPurchasePending += OnPurchasePending;
                _store.OnPurchaseFailed += failed => Finish(PurchaseResult.Failed);
                _store.OnPurchasesFetched += OnPurchasesFetched;
                _store.OnPurchasesFetchFailed += _ => FinishRestore(false);
                _store.OnProductsFetched += _ =>
                {
                    IsAvailable = true;
                    onReady(true);
                    _store.FetchPurchases();
                };
                _store.OnProductsFetchFailed += _ => onReady(false);
                await _store.Connect();
                _store.FetchProducts(catalog.Products
                    .Select(p => new ProductDefinition(p.Id, p.Kind == ProductKind.NonConsumable ? ProductType.NonConsumable : ProductType.Consumable))
                    .ToList());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[IAP] Store unavailable: " + ex.Message);
                onReady(false);
            }
        }

        public void Buy(string productId, Action<PurchaseResult> onDone)
        {
            if (!IsAvailable || _store == null)
            {
                onDone(PurchaseResult.Unavailable);
                return;
            }

            _onPurchaseDone = onDone;
            _store.PurchaseProduct(productId);
        }

        public void Restore(Action<bool> onDone)
        {
            if (!IsAvailable || _store == null)
            {
                onDone(false);
                return;
            }

            _onRestoreDone = onDone;
#if UNITY_IOS
            // Apple needs an explicit restore; the restored purchases are then fetched like on Android.
            _store.RestoreTransactions((success, _) =>
            {
                if (success)
                {
                    _store.FetchPurchases();
                }
                else
                {
                    FinishRestore(false);
                }
            });
#else
            _store.FetchPurchases();
#endif
        }

        public async void CheckStarterPackOffer(Action<bool?> onAnswer)
        {
            try
            {
                await SignInAsync();
                OfferAnswer answer = await CloudCodeService.Instance.CallEndpointAsync<OfferAnswer>("GetStarterPackOffer", new Dictionary<string, object>());
                onAnswer(answer.eligible);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[IAP] Starter pack offer unknown: " + ex.Message);
                onAnswer(null);
            }
        }

        private async void OnPurchasePending(PendingOrder order)
        {
            string productId = order.CartOrdered.Items().First().Product.definition.id;
            try
            {
                ValidatedPurchase? validated = await ValidateAsync(productId, order.Info.Receipt);
                if (validated == null)
                {
                    Finish(PurchaseResult.Failed);
                    return;
                }

                if (_grant == null)
                {
                    // Never confirm what was not granted: the store delivers the order again on the next start.
                    Finish(PurchaseResult.Failed);
                    return;
                }

                _grant(validated);
                _store!.ConfirmPurchase(order);
                Finish(PurchaseResult.Granted);
            }
            catch (Exception ex)
            {
                // Not confirmed: the store delivers the order again later, and the ledger keeps the grant single.
                Debug.LogWarning("[IAP] Validation failed: " + ex.Message);
                Finish(PurchaseResult.Failed);
            }
        }

        /// <summary>Owned purchases: confirmed non-consumables are granted again if missing (Remove Ads on a new device).</summary>
        private async void OnPurchasesFetched(Orders orders)
        {
            bool ok = true;
            foreach (ConfirmedOrder order in orders.ConfirmedOrders)
            {
                string productId = order.CartOrdered.Items().First().Product.definition.id;
                if (!_catalog.TryGet(productId, out StoreProduct? product) || product == null || product.Kind != ProductKind.NonConsumable)
                {
                    continue;
                }

                try
                {
                    ValidatedPurchase? validated = await ValidateAsync(productId, order.Info.Receipt);
                    if (validated != null)
                    {
                        _grant?.Invoke(validated);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[IAP] Restore validation failed: " + ex.Message);
                    ok = false;
                }
            }

            FinishRestore(ok);
        }

        private static async Task SignInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        private static async Task<ValidatedPurchase?> ValidateAsync(string productId, string receipt)
        {
            await SignInAsync();
            var args = new Dictionary<string, object>
            {
                ["platform"] = Application.platform == RuntimePlatform.IPhonePlayer ? "apple" : "google",
                ["receipt"] = receipt,
                ["productId"] = productId,
            };
            ValidationAnswer answer = await CloudCodeService.Instance.CallEndpointAsync<ValidationAnswer>("ValidatePurchase", args);
            if (!answer.valid || string.IsNullOrEmpty(answer.transactionId))
            {
                Debug.LogWarning($"[IAP] {productId} refused: {answer.reason ?? "invalid"}");
                return null;
            }

            GrantsAnswer? grants = answer.grants;
            return new ValidatedPurchase(
                answer.transactionId!,
                productId,
                grants?.petals,
                grants?.boosters == null ? null : new BoosterGrant(grants.boosters.extraSlot, grants.boosters.shuffle, grants.boosters.@return, grants.boosters.bloomBurst));
        }

        private void Finish(PurchaseResult result)
        {
            Action<PurchaseResult>? done = _onPurchaseDone;
            _onPurchaseDone = null;
            done?.Invoke(result);
        }

        private void FinishRestore(bool ok)
        {
            Action<bool>? done = _onRestoreDone;
            _onRestoreDone = null;
            done?.Invoke(ok);
        }

        [Serializable]
        private sealed class ValidationAnswer
        {
            public bool valid;
            public string? transactionId;
            public string? reason;
            public GrantsAnswer? grants;
        }

        [Serializable]
        private sealed class GrantsAnswer
        {
            public int? petals;
            public BoostersAnswer? boosters;
        }

        [Serializable]
        private sealed class BoostersAnswer
        {
            public int extraSlot;
            public int shuffle;
            public int @return;
            public int bloomBurst;
        }

        [Serializable]
        private sealed class OfferAnswer
        {
            public bool? eligible;
        }
    }
}
#endif
