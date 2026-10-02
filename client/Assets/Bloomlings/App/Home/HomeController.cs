using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Gameplay.Board;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Meta.Collection;
using Bloomlings.Client.Meta.DailyChallenge;
using Bloomlings.Client.Meta.DailyReward;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Consent;
using Bloomlings.Client.Services.Ads;
using Bloomlings.Client.Services.Analytics;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Purchases;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Screens;
using Bloomlings.Client.UI.Tutorial;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.App.Home
{
    /// <summary>
    /// The Home scene (FR-058): builds <see cref="HomeScreen"/>, <see cref="SettingsScreen"/> (with Restore Purchases
    /// and account linking), the Store (from L12), the Daily Reward popup (from L7), the optional free-booster ad, and
    /// the long-run features (US7): the leaderboard rank (L10), the Wardrobe (L40), the Daily Challenge (L50), the
    /// Collection, the milestone teaser and the band's background theme. Opening Home syncs the cloud save and refreshes
    /// the rank in the background. Opened without Boot (in the Editor), it loads Boot first.
    /// </summary>
    public sealed class HomeController : MonoBehaviour
    {
        private Action? _unsubscribe;

        private void OnDestroy() => _unsubscribe?.Invoke();

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
            FreeBoosterAd freeBooster = services.Get<FreeBoosterAd>();
            IRemoteConfigService config = services.Get<IRemoteConfigService>();
            MilestoneService milestones = services.Get<MilestoneService>();
            WardrobeService wardrobe = services.Get<WardrobeService>();
            CollectionService collection = services.Get<CollectionService>();
            DailyChallengeService dailyChallenge = services.Get<DailyChallengeService>();
            LeaderboardClient leaderboard = services.Get<LeaderboardClient>();
            CloudSaveSync sync = services.Get<CloudSaveSync>();
            IAuthService auth = services.Get<IAuthService>();
            CatalogService catalog = services.Get<CatalogService>();
            services.TryGet(out Boot? boot);
            services.TryGet(out GameAnalytics? analytics);

            Canvas canvas = UiFactory.CreateCanvas("HomeCanvas", 0);
            canvas.transform.SetParent(transform, false);
            var root = (RectTransform)canvas.transform;
            SettingsScreen? settings = null;
            StoreScreen? store = null;
            HomeScreen? home = null;
            LeaderboardScreen? board = null;
            void Refresh()
            {
                if (!this || home == null)
                {
                    return; // A background callback after Home was left.
                }

                (int Level, MilestoneCadence Cadence, int WinsToGo)? next = milestones.Next(progression.HighestCompletedLevel);
                BackgroundTheme band = ThemeRotation.Default.ThemeFor(progression.CurrentLevel);
                Color? background = ColorUtility.TryParseHtmlString(band.Background, out Color theme) ? theme : (Color?)null;
                Color? accent = ColorUtility.TryParseHtmlString(band.Accent, out Color accentColor) ? accentColor : (Color?)null;
                home.Show(new HomeModel(
                    progression.CurrentLevel,
                    economy.Petals,
                    progression.IsUnlocked("system.store"),
                    leaderboard.IsUnlocked,
                    RankText(leaderboard),
                    next?.Level,
                    next?.WinsToGo,
                    dailyChallenge.IsAvailable,
                    dailyChallenge.CompletedToday,
                    wardrobe.IsAvailable,
                    collection.Count > 0,
                    background,
                    catalog.HasLevel(progression.CurrentLevel),
                    wardrobe.Profile,
                    wardrobe.OutfitOf(Core.Variants.Family.Bloom),
                    accent,
                    Theme: band,
                    DailyChallengePetals: DailyChallengeService.RewardPetals));
                home.SetFreeBoosterOffer(ads.IsRewardedReady && freeBooster.IsAvailable && FreeBoosterKind(economy).HasValue);
                if (board != null && board.IsOpen)
                {
                    board.Show(leaderboard.LastPage, leaderboard.IsStale, wardrobe.Profile);
                }
            }

            void RunInBackground(System.Collections.IEnumerator routine)
            {
                if (boot != null)
                {
                    boot.Run(routine);
                }
                else
                {
                    StartCoroutine(routine);
                }
            }

            void OpenStore() => store!.Show(StoreItems(economy, purchases, ledger, products, save, wardrobe, OpenStore, Refresh), economy.Petals, purchases.IsAvailable);

            WardrobeScreen wardrobeScreen = WardrobeScreen.Create(root, wardrobe, () => economy.Petals, () =>
            {
                analytics?.StoreOpen("wardrobe");
                OpenStore();
            });
            wardrobe.Changed += Refresh;
            CollectionScreen collectionScreen = CollectionScreen.Create(root);
            DailyChallengeScreen dailyScreen = DailyChallengeScreen.Create(root, () =>
            {
                LevelAttempt? attempt = dailyChallenge.BeginAttempt();
                if (attempt != null)
                {
                    flow.PlayDaily(attempt);
                }
            });
            board = LeaderboardScreen.Create(root, () => RunInBackground(leaderboard.Refresh()));
            var features = new HomeFeatureActions(
                () => dailyScreen.Show(new DailyChallengeModel(dailyChallenge.Today, dailyChallenge.CompletedToday, DailyChallengeService.RewardPetals)),
                wardrobeScreen.Show,
                () =>
                {
                    analytics?.CollectionOpen(collection.Count);
                    collectionScreen.Show(collection.Entries, entry => RenderCollectionEntry(catalog, entry));
                },
                () =>
                {
                    analytics?.LeaderboardView(leaderboard.LastPage?.Player?.Rank ?? 0);
                    board.Show(leaderboard.LastPage, leaderboard.IsStale, wardrobe.Profile);
                    RunInBackground(leaderboard.Refresh());
                },
                wardrobeScreen.ShowProfile);

            home = HomeScreen.Create(
                UiFactory.Stretch(UiFactory.CreateRect("Home", root)),
                flow.Play,
                () => settings!.Show(),
                () =>
                {
                    analytics?.StoreOpen("home");
                    OpenStore();
                },
                () => ads.ShowRewarded(AdPlacements.FreeBooster, earned =>
                {
                    BoosterKind? kind = FreeBoosterKind(economy);
                    if (earned && kind.HasValue && freeBooster.IsAvailable)
                    {
                        analytics?.AdRewarded("free_booster");
                        freeBooster.MarkTaken();
                        economy.Grant(0, Grant(kind.Value));
                    }

                    Refresh();
                }),
                features);
            var account = new AccountActions(
                () => save.LinkedIdentity != null
                    ? Loc.T(save.LinkedIdentity == "apple" ? "account.linked_apple" : "account.linked_google")
                    : Loc.T(auth.IsSignedIn ? "account.signed_in" : "account.local"),
                auth.CanLink(LinkProvider.Apple),
                auth.CanLink(LinkProvider.GooglePlayGames),
                (provider, done) => RunInBackground(auth.Link(provider, ok =>
                {
                    if (ok)
                    {
                        save.LinkedIdentity = LinkedIdentities.Name(provider);
                        saves.Save();
                        RunInBackground(sync.Sync(_ => Refresh()));
                    }

                    done(ok);
                })));
            settings = SettingsScreen.Create(
                root,
                save.Settings,
                saves.Save,
                done => purchases.Restore(ok =>
                {
                    Debug.Log(ok ? "[Store] Purchases restored." : "[Store] Restore unavailable.");
                    done(ok);
                }),
                account,
                services.TryGet(out IConsentService? consent) ? consent : null);
            store = StoreScreen.Create(root);
            Refresh();

            // Background refresh: a cloud merge or a new rank updates Home when it arrives (never blocking it).
            Action onMerged = Refresh;
            Action onRank = Refresh;
            sync.Merged += onMerged;
            leaderboard.Updated += onRank;
            _unsubscribe = () =>
            {
                sync.Merged -= onMerged;
                leaderboard.Updated -= onRank;
                wardrobe.Changed -= Refresh;
            };
            RunInBackground(sync.Sync());
            RunInBackground(leaderboard.Refresh());

            // A system reached since the last visit is demonstrated once, on its button (FR-031: "demonstrated at that
            // level or within the next 1–2 levels"; Home is where these systems live). Not over the Daily Reward popup.
            if (!daily.CanClaim)
            {
                foreach (string unlockId in DemoScripts.HomeSystems)
                {
                    if (!progression.IsUnlocked(unlockId) || progression.HasSeenDemo(unlockId) || !home.CanDemo(unlockId))
                    {
                        continue;
                    }

                    string id = unlockId;
                    DemoScript? demo = DemoScripts.HomeSystem(id, () => home.DemoTarget(id));
                    if (demo != null)
                    {
                        analytics?.TutorialStep(null, id, 1, false);
                        DemoOverlay.Create(root).Show(demo, _ =>
                        {
                            analytics?.TutorialStep(null, id, 1, true);
                            progression.MarkDemoSeen(id);
                        });
                        break;
                    }
                }
            }

            // The Daily Reward pops up once a day while a claim is due (FR-055). The ad bonus claims the reward with its
            // extra Petals, so it can be earned once a day: the popup does not come back after a claim.
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
                        if (paid > 0)
                        {
                            analytics?.DailyRewardClaim(save.Daily.RewardStreak);
                        }

                        Refresh();
                        return paid;
                    },
                    done => ads.ShowRewarded(AdPlacements.DailyBonus, earned =>
                    {
                        int extra = 0;
                        if (earned && daily.CanClaim)
                        {
                            int paid = daily.Claim();
                            analytics?.DailyRewardClaim(save.Daily.RewardStreak);
                            extra = config.Get(RemoteConfigKeys.DailyRewardPetals);
                            analytics?.AdRewarded("daily");
                            economy.Grant(extra, null);
                            extra += paid;
                        }

                        done(extra);
                        Refresh();
                    }));
            }
        }

        /// <summary>The unlocked booster with the fewest charges, for the free-booster offer (once a day).</summary>
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

        /// <summary>
        /// The Store rows (FR-051): Petal packs, boosters for Petals, the starter pack once, Remove Ads until owned; and,
        /// once the Wardrobe is open, the cosmetics for Petals on their own tab.
        /// </summary>
        private static List<StoreItem> StoreItems(EconomyService economy, IPurchaseService purchases, PurchaseLedger ledger, ProductCatalog products, PlayerSave save, WardrobeService wardrobe, Action reopen, Action refreshHome)
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
                    () => purchases.Buy(id, _ =>
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
                string boosterId = kind switch
                {
                    BoosterKind.ExtraSlot => "extra_slot",
                    BoosterKind.Shuffle => "shuffle",
                    BoosterKind.Return => "return",
                    _ => "bloom_burst",
                };
                items.Add(new StoreItem(
                    "booster_" + kind,
                    Loc.F("store.booster_owned", BoosterName(kind), economy.Charges(kind)),
                    economy.Price(kind).ToString(CultureInfo.InvariantCulture) + " ✿",
                    economy.Petals >= economy.Price(kind),
                    () =>
                    {
                        economy.TryBuy(k);
                        reopen();
                        refreshHome();
                    },
                    Icon: Art.ProceduralSprites.Shape("booster." + boosterId),
                    IconTint: UiTheme.Of(UI.Design.DesignTokens.BoosterColor(boosterId)),
                    PetalPrice: economy.Price(kind)));
            }

            if (wardrobe.IsAvailable)
            {
                foreach (CosmeticItem cosmetic in wardrobe.ForSale)
                {
                    string id = cosmetic.Id;
                    items.Add(new StoreItem(
                        id,
                        Loc.F("store.cosmetic", WardrobeScreen.Name(cosmetic), WardrobeScreen.KindLabel(cosmetic.Kind)),
                        cosmetic.Price.ToString(CultureInfo.InvariantCulture) + " ✿",
                        wardrobe.IsBuyable(cosmetic) && economy.Petals >= cosmetic.Price,
                        () =>
                        {
                            wardrobe.TryBuy(id);
                            reopen();
                            refreshHome();
                        },
                        StoreTab.Cosmetics,
                        WardrobeScreen.Icon(cosmetic),
                        Gameplay.Workers.BloomlingFigure.Tint(cosmetic),
                        cosmetic.Price));
                }
            }

            return items;
        }

        private static string Title(StoreProduct product)
        {
            if (product.RemoveAds)
            {
                return Loc.T("store.remove_ads");
            }

            if (product.OfferedOnce)
            {
                return Loc.T("store.starter_pack");
            }

            return product.Petals > 0 && product.Boosters == null
                ? Loc.F("common.petals", product.Petals)
                : Loc.T("store.booster_bundle");
        }

        private static string BoosterName(BoosterKind kind) => kind switch
        {
            BoosterKind.ExtraSlot => Loc.T("booster.extra_slot"),
            BoosterKind.Shuffle => Loc.T("booster.shuffle"),
            BoosterKind.Return => Loc.T("booster.return"),
            _ => Loc.T("booster.bloom_burst"),
        };

        /// <summary>The Home rank slot (FR-058): null before the unlock; a stale rank says so.</summary>
        private static string? RankText(LeaderboardClient leaderboard)
        {
            if (!leaderboard.IsUnlocked)
            {
                return null;
            }

            int? rank = leaderboard.LastPage?.Player?.Rank;
            if (!rank.HasValue)
            {
                return Loc.T(leaderboard.IsStale ? "home.rank_unknown_offline" : "home.rank_unknown");
            }

            string number = rank.Value.ToString("N0", CultureInfo.InvariantCulture);
            return leaderboard.IsStale ? Loc.F("home.rank_offline", number) : Loc.F("home.rank", number);
        }

        /// <summary>
        /// Redraws a Collection entry with the current content when its level still has the same picture and colors;
        /// otherwise the entry shows its level number only.
        /// </summary>
        private static Texture2D? RenderCollectionEntry(CatalogService catalog, CollectionEntry entry)
        {
            ContentSet content = catalog.Content;
            if (!content.TryGetLevel(entry.LevelNumber, out LevelDefinition level)
                || level.Picture.Id != entry.PictureId
                || level.Picture.Version != entry.PictureVersion
                || CollectionService.MappingHash(level.Mapping) != entry.MappingHash)
            {
                return null;
            }

            return FinishedPictureRenderer.Render(level, content.GetPicture(level.Picture), null);
        }
    }
}
