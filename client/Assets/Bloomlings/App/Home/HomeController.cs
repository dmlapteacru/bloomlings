using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Meta.DailyReward;
using Bloomlings.Client.Services.Ads;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Purchases;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Screens;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bloomlings.Client.App.Home
{
    /// <summary>
    /// The Home scene (FR-058): builds <see cref="HomeScreen"/>, <see cref="SettingsScreen"/> (with Restore Purchases),
    /// the Store (from L12), the Daily Reward popup (from L7) and the optional free-booster ad from the save and the
    /// services. Opened without Boot (in the Editor), it loads Boot first.
    /// </summary>
    public sealed class HomeController : MonoBehaviour
    {
        private void Start()
        {
            AppServices? services = AppServices.Current;
            if (services == null)
            {
                SceneManager.LoadScene(0);
                return;
            }

            PlayerSave save = services.Get<PlayerSave>();
            SaveService saves = services.Get<SaveService>();
            ProgressionService progression = services.Get<ProgressionService>();
            GameFlow flow = services.Get<GameFlow>();
            EconomyService economy = services.Get<EconomyService>();
            IAdsService ads = services.Get<IAdsService>();
            IPurchaseService purchases = services.Get<IPurchaseService>();
            PurchaseLedger ledger = services.Get<PurchaseLedger>();
            ProductCatalog products = services.Get<ProductCatalog>();
            DailyRewardService daily = services.Get<DailyRewardService>();
            IRemoteConfigService config = services.Get<IRemoteConfigService>();

            Canvas canvas = UiFactory.CreateCanvas("HomeCanvas", 0);
            canvas.transform.SetParent(transform, false);
            var root = (RectTransform)canvas.transform;
            SettingsScreen? settings = null;
            StoreScreen? store = null;
            HomeScreen? home = null;
            void Refresh()
            {
                (int? milestone, int? toGo) = NextMilestone(progression);
                home!.Show(new HomeModel(
                    progression.CurrentLevel,
                    economy.Petals,
                    progression.IsUnlocked("system.store"),
                    progression.IsUnlocked("system.leaderboard"),
                    null,
                    milestone,
                    toGo));
                home.SetFreeBoosterOffer(ads.IsRewardedReady && !_freeBoosterTaken && FreeBoosterKind(economy).HasValue);
            }

            void OpenStore() => store!.Show(StoreItems(economy, purchases, ledger, products, save, OpenStore, Refresh), economy.Petals, purchases.IsAvailable);

            home = HomeScreen.Create(
                UiFactory.Stretch(UiFactory.CreateRect("Home", root)),
                flow.Play,
                () => settings!.Show(),
                OpenStore,
                () => ads.ShowRewarded(AdPlacements.FreeBooster, earned =>
                {
                    BoosterKind? kind = FreeBoosterKind(economy);
                    if (earned && kind.HasValue)
                    {
                        _freeBoosterTaken = true;
                        economy.Grant(0, Grant(kind.Value));
                    }

                    Refresh();
                }));
            settings = SettingsScreen.Create(root, save.Settings, saves.Save, () => purchases.Restore(ledger.Grant, ok => Debug.Log(ok ? "[Store] Purchases restored." : "[Store] Restore unavailable.")));
            store = StoreScreen.Create(root);
            Refresh();

            // The Daily Reward pops up once a day while a claim is due (FR-055).
            if (daily.CanClaim)
            {
                DailyRewardPopup popup = DailyRewardPopup.Create(root);
                popup.Show(
                    daily.NextPetals,
                    daily.NextStreak,
                    ads.IsRewardedReady,
                    () =>
                    {
                        int paid = daily.Claim();
                        Refresh();
                        return paid;
                    },
                    done => ads.ShowRewarded(AdPlacements.DailyBonus, earned =>
                    {
                        int extra = earned ? config.Get(RemoteConfigKeys.DailyRewardPetals) : 0;
                        if (extra > 0)
                        {
                            economy.Grant(extra, null);
                        }

                        done(extra);
                        Refresh();
                    }));
            }
        }

        private static bool _freeBoosterTaken;

        /// <summary>The unlocked booster with the fewest charges, for the free-booster offer (one per session).</summary>
        private static BoosterKind? FreeBoosterKind(EconomyService economy)
        {
            BoosterKind? best = null;
            foreach ((string _, BoosterKind kind) in EconomyService.BoosterUnlocks)
            {
                if (economy.IsUnlocked(kind) && (!best.HasValue || economy.Charges(kind) < economy.Charges(best.Value)))
                {
                    best = kind;
                }
            }

            return best;
        }

        private static BoosterGrant Grant(BoosterKind kind) => new BoosterGrant(
            kind == BoosterKind.ExtraSlot ? 1 : 0,
            kind == BoosterKind.Shuffle ? 1 : 0,
            kind == BoosterKind.Return ? 1 : 0,
            kind == BoosterKind.BloomBurst ? 1 : 0);

        /// <summary>The Store rows (FR-051): Petal packs, boosters for Petals, the starter pack once, Remove Ads until owned.</summary>
        private static List<StoreItem> StoreItems(EconomyService economy, IPurchaseService purchases, PurchaseLedger ledger, ProductCatalog products, PlayerSave save, Action reopen, Action refreshHome)
        {
            var items = new List<StoreItem>();
            foreach (StoreProduct product in products.Products)
            {
                if ((product.OfferedOnce && save.Purchases.StarterPackOffered) || (product.RemoveAds && ledger.RemoveAds))
                {
                    continue;
                }

                string id = product.Id;
                items.Add(new StoreItem(
                    id,
                    Title(product),
                    purchases.PriceOf(id) ?? (purchases.IsAvailable ? "…" : "—"),
                    purchases.IsAvailable,
                    () => purchases.Buy(id, ledger.Grant, _ =>
                    {
                        reopen();
                        refreshHome();
                    })));
            }

            foreach ((string _, BoosterKind kind) in EconomyService.BoosterUnlocks)
            {
                if (!economy.IsUnlocked(kind))
                {
                    continue;
                }

                BoosterKind k = kind;
                items.Add(new StoreItem(
                    "booster_" + kind,
                    BoosterName(kind) + " (owned " + economy.Charges(kind).ToString(CultureInfo.InvariantCulture) + ")",
                    economy.Price(kind).ToString(CultureInfo.InvariantCulture) + " ✿",
                    economy.Petals >= economy.Price(kind),
                    () =>
                    {
                        economy.TryBuy(k);
                        reopen();
                        refreshHome();
                    }));
            }

            return items;
        }

        private static string Title(StoreProduct product)
        {
            if (product.RemoveAds)
            {
                return "Remove Ads";
            }

            if (product.OfferedOnce)
            {
                return "Starter pack";
            }

            return product.Petals > 0 && product.Boosters == null
                ? product.Petals.ToString(CultureInfo.InvariantCulture) + " Petals"
                : "Booster bundle";
        }

        private static string BoosterName(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => "Extra Slot",
            BoosterKind.Shuffle => "Shuffle",
            BoosterKind.Return => "Return",
            _ => "Bloom Burst",
        };

        /// <summary>Milestones come every 25 levels (FR-061).</summary>
        public const int MilestoneCadence = 25;

        /// <summary>The next milestone level and the number of wins still needed to reach it (FR-058 teaser).</summary>
        public static (int? Level, int? WinsToGo) NextMilestone(ProgressionService progression)
        {
            int highest = progression.HighestCompletedLevel;
            int next = ((highest / MilestoneCadence) + 1) * MilestoneCadence;
            return (next, next - highest);
        }
    }
}
