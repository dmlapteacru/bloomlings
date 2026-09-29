using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Services.Purchases
{
    public enum ProductKind
    {
        Consumable,
        NonConsumable,
    }

    /// <summary>One store product and what it grants (research R13).</summary>
    public sealed record StoreProduct(string Id, ProductKind Kind, int Petals, BoosterGrant? Boosters, bool RemoveAds, bool OfferedOnce);

    /// <summary>
    /// The product catalog (<c>Services/Purchases/ProductCatalog.json</c>, T131): Petal packs and booster bundles are
    /// consumable, the starter pack is consumable and offered once, Remove Ads is non-consumable. Store prices come from
    /// the platform store; the catalog holds only ids and grants.
    /// </summary>
    public sealed class ProductCatalog
    {
        public const string RemoveAdsId = "remove_ads";
        public const string StarterPackId = "starter_pack";

        private readonly Dictionary<string, StoreProduct> _products = new Dictionary<string, StoreProduct>(StringComparer.Ordinal);

        public ProductCatalog(IEnumerable<StoreProduct> products)
        {
            foreach (StoreProduct product in products)
            {
                _products.Add(product.Id, product);
            }
        }

        public IReadOnlyCollection<StoreProduct> Products => _products.Values;

        public bool TryGet(string productId, out StoreProduct? product) => _products.TryGetValue(productId, out product);

        public static ProductCatalog Parse(string json)
        {
            var products = new List<StoreProduct>();
            foreach (JToken token in (JArray)JObject.Parse(json)["products"]!)
            {
                var item = (JObject)token;
                JObject? boosters = item["boosters"] as JObject;
                products.Add(new StoreProduct(
                    (string)item["id"]!,
                    (string)item["type"]! == "non_consumable" ? ProductKind.NonConsumable : ProductKind.Consumable,
                    (int?)item["petals"] ?? 0,
                    boosters == null ? null : new BoosterGrant((int?)boosters["extraSlot"] ?? 0, (int?)boosters["shuffle"] ?? 0, (int?)boosters["return"] ?? 0, (int?)boosters["bloomBurst"] ?? 0),
                    (bool?)item["removeAds"] ?? false,
                    (bool?)item["offeredOnce"] ?? false));
            }

            return new ProductCatalog(products);
        }
    }
}
