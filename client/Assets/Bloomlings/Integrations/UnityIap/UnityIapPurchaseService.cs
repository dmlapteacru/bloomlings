#if BLOOMLINGS_IAP
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bloomlings.Client.Services;
using Bloomlings.Client.Services.Purchases;
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
    /// </summary>
    internal sealed class UnityIapPurchaseService : IPurchaseService
    {
        private StoreController? _store;
        private Func<ValidatedPurchase, bool>? _grant;
        private Action<PurchaseResult>? _onPurchaseDone;

        public bool IsAvailable { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => ServiceProviders.Purchases = () => new UnityIapPurchaseService();

        public string? PriceOf(string productId) => _store?.GetProducts().FirstOrDefault(p => p.definition.id == productId)?.metadata.localizedPriceString;

        public async void Initialize(ProductCatalog catalog, Action<bool> onReady)
        {
            try
            {
                _store = UnityIAPServices.StoreController();
                _store.OnPurchasePending += OnPurchasePending;
                _store.OnPurchaseFailed += failed => Finish(PurchaseResult.Failed);
                _store.OnProductsFetched += _ =>
                {
                    IsAvailable = true;
                    onReady(true);
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

        public void Buy(string productId, Func<ValidatedPurchase, bool> grant, Action<PurchaseResult> onDone)
        {
            if (!IsAvailable || _store == null)
            {
                onDone(PurchaseResult.Unavailable);
                return;
            }

            _grant = grant;
            _onPurchaseDone = onDone;
            _store.PurchaseProduct(productId);
        }

        public void Restore(Func<ValidatedPurchase, bool> grant, Action<bool> onDone)
        {
            if (!IsAvailable || _store == null)
            {
                onDone(false);
                return;
            }

            // Restored non-consumables arrive as pending orders and go through validation and the ledger.
            _grant = grant;
            _store.RestoreTransactions((success, _) => onDone(success));
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

                _grant?.Invoke(validated);
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

        private static async Task<ValidatedPurchase?> ValidateAsync(string productId, string receipt)
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            var args = new Dictionary<string, object>
            {
                ["platform"] = Application.platform == RuntimePlatform.IPhonePlayer ? "apple" : "google",
                ["receipt"] = receipt,
                ["productId"] = productId,
            };
            ValidationAnswer answer = await CloudCodeService.Instance.CallEndpointAsync<ValidationAnswer>("ValidatePurchase", args);
            return answer.valid && !string.IsNullOrEmpty(answer.transactionId) ? new ValidatedPurchase(answer.transactionId!, productId) : null;
        }

        private void Finish(PurchaseResult result)
        {
            Action<PurchaseResult>? done = _onPurchaseDone;
            _onPurchaseDone = null;
            done?.Invoke(result);
        }

        [Serializable]
        private sealed class ValidationAnswer
        {
            public bool valid;
            public string? transactionId;
        }
    }
}
#endif
