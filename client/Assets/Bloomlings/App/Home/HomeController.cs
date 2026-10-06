using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Gameplay.Board;
using Bloomlings.Client.Gameplay.Themes;
using Bloomlings.Client.Meta.Clearing;
using Bloomlings.Client.Meta.Collection;
using Bloomlings.Client.Meta.DailyChallenge;
using Bloomlings.Client.Meta.DailyReward;
using Bloomlings.Client.Meta.Profile;
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
using Bloomlings.Client.UI.Design;
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
    /// and account linking), the Store (from L12), the Daily Reward popup (from L7), the optional free-booster ad, Home's
    /// promo scenes (spec 005 FR-032, FR-033: No Ads, opening the <see cref="RemoveAdsCard"/> at every level until Remove
    /// Ads is owned, and the Daily Reward's, opening its popup once unlocked), and
    /// the long-run features (US7): the leaderboard rank (L10), the Wardrobe (L40), the Daily Challenge (L50), the
    /// Collection, the milestone teaser and the band's background theme. Home and its four pages (the Store, the Wardrobe,
    /// the Leaderboard and the Collection; every place a page since the owner's request of 2026-10-04) each show the
    /// bottom menu (spec 005 FR-030), whose places this controller navigates (<c>Navigate</c>): all five always show, a
    /// tap shows that place's page (or Home) and hides the others, and a locked one opens its page locked, saying from
    /// which level of the progression's roadmap it is available. Opening Home syncs the cloud save and refreshes the rank
    /// in the background. Opened without Boot (in the Editor), it loads Boot first.
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
            ProfileService profile = services.Get<ProfileService>();
            CollectionService collection = services.Get<CollectionService>();
            DailyChallengeService dailyChallenge = services.Get<DailyChallengeService>();
            LeaderboardClient leaderboard = services.Get<LeaderboardClient>();
            CloudSaveSync sync = services.Get<CloudSaveSync>();
            IAuthService auth = services.Get<IAuthService>();
            CatalogService catalog = services.Get<CatalogService>();
            services.TryGet(out Boot? boot);
            services.TryGet(out GameAnalytics? analytics);
            services.TryGet(out ClearingService? clearing);

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
                    next?.Level,
                    next?.WinsToGo,
                    dailyChallenge.IsAvailable,
                    dailyChallenge.CompletedToday,
                    wardrobe.IsAvailable,
                    collection.Count > 0,
                    background,
                    catalog.HasLevel(progression.CurrentLevel),
                    accent,
                    Theme: band,
                    DailyChallengePetals: DailyChallengeService.RewardPetals,
                    OutfitOf: wardrobe.OutfitOf,
                    Profile: wardrobe.Profile,
                    AvatarOutfit: wardrobe.IsAvailable ? wardrobe.OutfitOf(profile.Avatar.Family) : null,
                    NoAdsPromo: !ledger.RemoveAds,
                    DailyRewardPromo: daily.IsUnlocked,
                    DailyRewardWaiting: daily.CanClaim,
                    Avatar: profile.Avatar));
                home.SetFreeBoosterOffer(ads.IsRewardedReady && freeBooster.IsAvailable && FreeBoosterKind(economy).HasValue);
                if (board != null && board.ShowsRanks)
                {
                    board.Show(leaderboard.LastPage, leaderboard.IsStale, wardrobe.Profile, profile.Avatar);
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

            void OpenStore() => store!.Show(StoreItems(economy, purchases, ledger, products, save, wardrobe, OpenStore, Refresh), economy.Petals, purchases.IsAvailable, wardrobe, clearing, progression.CurrentLevel, () =>
            {
                OpenStore();
                Refresh();
            });

            // The bottom menu (spec 005 FR-030): which of its five places are open, from the unlocked features as on Home
            // (HomeLook), and from which level of the progression's roadmap a locked one is available; and where a tap on
            // one goes. Defined before the screens that show the menu, which call them later.
            HomeLook NavLook() => new HomeLook(
                Store: progression.IsUnlocked(HomeLook.StoreUnlock),
                Teaser: false,
                Hero: wardrobe.IsAvailable,
                Wardrobe: wardrobe.IsAvailable,
                Collection: collection.Count > 0,
                Rank: leaderboard.IsUnlocked,
                DailyChallenge: false,
                FreeBoosterOffer: false);
            int UnlockLevel(NavPlace place) => BottomNav.UnlockLevel(place, progression.Roadmap.LevelOf);
            Action? showBoard = null;
            Action? showCollection = null;
            WardrobeScreen? wardrobeScreen = null;
            CollectionScreen? collectionScreen = null;
            ProfileScreen? profileScreen = null;

            // The Store page over the screen that opened it (Home or one of its pages: the menu's Shop or a Petals "+"),
            // where its back returns; `from` names that screen for store_open. A locked page is not a Store visit: no
            // store_open.
            void StoreFrom(string from)
            {
                if (BottomNav.IsOpen(NavPlace.Shop, NavLook()))
                {
                    analytics?.StoreOpen(from);
                    OpenStore();
                }
                else
                {
                    store!.ShowLocked(UnlockLevel(NavPlace.Shop), economy.Petals);
                }
            }

            // The page that shows the menu now (the Store's origin), else Home.
            string Origin() =>
                profileScreen != null && profileScreen.IsOpen ? "profile"
                : wardrobeScreen != null && wardrobeScreen.IsOpen ? "wardrobe"
                : board != null && board.IsOpen ? "leaderboard"
                : collectionScreen != null && collectionScreen.IsOpen ? "collection"
                : "home";

            void Navigate(NavPlace place)
            {
                if (place == NavPlace.Shop)
                {
                    StoreFrom(Origin());
                    return;
                }

                // Every other place shows its own page (or Home) and hides the others (the owner's request of 2026-10-04:
                // "All the menu's places must be a separate page. Not popups."); a locked page says its level.
                bool open = BottomNav.IsOpen(place, NavLook());
                store!.Hide();
                profileScreen?.Hide();
                wardrobeScreen!.Hide();
                board!.Hide();
                collectionScreen!.Hide();
                switch (place)
                {
                    case NavPlace.Wardrobe:
                        if (open)
                        {
                            wardrobeScreen.Show();
                        }
                        else
                        {
                            wardrobeScreen.ShowLocked(UnlockLevel(place));
                        }

                        break;
                    case NavPlace.Leaderboard:
                        if (open)
                        {
                            showBoard?.Invoke();
                        }
                        else
                        {
                            board.ShowLocked(UnlockLevel(place));
                        }

                        break;
                    case NavPlace.Collection:
                        if (open)
                        {
                            showCollection?.Invoke();
                        }
                        else
                        {
                            collectionScreen.ShowLocked(UnlockLevel(place));
                        }

                        break;
                    default:
                        Refresh();
                        break;
                }
            }

            // The pages' Petals pills: their "+" shows once the Store is open (a locked page can show from L1, FR-030).
            wardrobeScreen = WardrobeScreen.Create(root, wardrobe, () => economy.Petals, () => StoreFrom("wardrobe"), Navigate, NavLook, () => profile.Avatar);
            wardrobe.Changed += Refresh;
            collectionScreen = CollectionScreen.Create(root, () => economy.Petals, () => StoreFrom("collection"), Navigate, NavLook);
            DailyChallengeScreen dailyScreen = DailyChallengeScreen.Create(root, () =>
            {
                LevelAttempt? attempt = dailyChallenge.BeginAttempt();
                if (attempt != null)
                {
                    flow.PlayDaily(attempt);
                }
            });
            // The profile page (spec 005 FR-037), opened by Home's avatar; its edit card keeps or buys, then Home redraws.
            profileScreen = ProfileScreen.Create(
                root,
                profile,
                wardrobe,
                () => progression.CurrentLevel,
                () => new ProfileStats(Achievements.Count(save, Achievements.LevelsCounter), collection.Count, save.Milestones.Claimed.Count, Achievements.Count(save, Achievements.DailyCounter)),
                UnlockLevel(NavPlace.Wardrobe),
                wardrobe.OutfitOf,
                () => economy.Petals,
                () => StoreFrom("profile"),
                NavLook,
                Refresh);
            board = LeaderboardScreen.Create(root, () => RunInBackground(leaderboard.Refresh()), () => economy.Petals, () => StoreFrom("leaderboard"), Navigate, NavLook);
            showCollection = () =>
            {
                analytics?.CollectionOpen(collection.Count);
                collectionScreen.Show(collection.Entries, (entry, side) => RenderCollectionEntry(catalog, entry, side));
            };
            showBoard = () =>
            {
                analytics?.LeaderboardView(leaderboard.LastPage?.Player?.Rank ?? 0);
                board.Show(leaderboard.LastPage, leaderboard.IsStale, wardrobe.Profile, profile.Avatar);
                RunInBackground(leaderboard.Refresh());
            };
            // Restore Purchases (Settings and the Remove Ads card): Home refreshes after it, so a restored Remove Ads hides
            // the No Ads scene at once.
            void RestorePurchases(Action<bool> done) => purchases.Restore(ok =>
            {
                Debug.Log(ok ? "[Store] Purchases restored." : "[Store] Restore unavailable.");
                Refresh();
                done(ok);
            });

            // The Daily Reward popup (FR-055), built when first shown: by itself once a day while a claim is due, and from
            // the Daily scene at any time once unlocked (spec 005 FR-032; after the claim, Claim greyed). The ad bonus
            // claims the reward with its extra Petals, so it can be earned once a day.
            DailyRewardPopup? dailyPopup = null;
            void ShowDailyReward()
            {
                if (!daily.IsUnlocked)
                {
                    return;
                }

                dailyPopup ??= DailyRewardPopup.Create(root);
                bool claimable = daily.CanClaim;
                dailyPopup.Show(
                    claimable ? daily.NextPetals : daily.PetalsOn(daily.TodayStreak),
                    daily.TodayStreak,
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
                    }),
                    claimable: claimable);
            }

            // The Remove Ads card (spec 005 FR-033): the Store row's purchase and Settings' restore; Home refreshes after
            // either, so the No Ads scene hides as soon as Remove Ads is owned.
            RemoveAdsCard? removeAds = null;
            var features = new HomeFeatureActions(
                () => dailyScreen.Show(new DailyChallengeModel(dailyChallenge.Today, dailyChallenge.CompletedToday, DailyChallengeService.RewardPetals)),
                OnProfile: () => profileScreen?.Show(),
                OnNoAds: () => removeAds?.Show(purchases.PriceOf(ProductCatalog.RemoveAdsId), purchases.IsAvailable),
                OnDailyReward: ShowDailyReward);

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
                features,
                Navigate,
                () => save.Settings.HomePetals);
            var account = new AccountActions(
                () => save.LinkedIdentity != null
                    ? (save.LinkedIdentity == "apple" ? Loc.T("account.linked_apple") : Loc.T("account.linked_google"))
                    : (auth.IsSignedIn ? Loc.T("account.signed_in") : Loc.T("account.local")),
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
                RestorePurchases,
                account,
                services.TryGet(out IConsentService? consent) ? consent : null);
            store = StoreScreen.Create(root, Navigate, NavLook);
            removeAds = RemoveAdsCard.Create(
                root,
                done => purchases.Buy(ProductCatalog.RemoveAdsId, _ =>
                {
                    Refresh();
                    done();
                }),
                RestorePurchases,
                () => ledger.RemoveAds);
            Refresh();

            // Background refresh: a cloud merge, a new rank or a purchase the store delivers later (an order left pending,
            // the owned purchases fetched at start) updates Home when it arrives (never blocking it).
            Action onMerged = Refresh;
            Action onRank = Refresh;
            Action<ValidatedPurchase> onGranted = _ => Refresh();
            sync.Merged += onMerged;
            leaderboard.Updated += onRank;
            ledger.Granted += onGranted;
            _unsubscribe = () =>
            {
                sync.Merged -= onMerged;
                leaderboard.Updated -= onRank;
                ledger.Granted -= onGranted;
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

            // The Daily Reward pops up once a day while a claim is due (FR-055): it does not come back by itself after a
            // claim (the Daily scene still opens it).
            if (daily.CanClaim)
            {
                ShowDailyReward();
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
                    PetalPrice: economy.Price(kind),
                    BoosterId: boosterId,
                    Charges: economy.Charges(kind),
                    Name: BoosterName(kind)));
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
                        cosmetic.Price,
                        Cosmetic: cosmetic,
                        Name: WardrobeScreen.Name(cosmetic)));
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

        /// <summary>
        /// Redraws a Collection entry with the current content when its level still has the same picture and colors, to fit
        /// <paramref name="side"/> pixels (0: at full detail); otherwise the entry shows its level number only.
        /// </summary>
        private static Texture2D? RenderCollectionEntry(CatalogService catalog, CollectionEntry entry, int side)
        {
            ContentSet content = catalog.Content;
            if (!content.TryGetLevel(entry.LevelNumber, out LevelDefinition level)
                || level.Picture.Id != entry.PictureId
                || level.Picture.Version != entry.PictureVersion
                || CollectionService.MappingHash(level.Mapping) != entry.MappingHash)
            {
                return null;
            }

            BasePicture picture = content.GetPicture(level.Picture);
            int cell = side > 0 ? FinishedPictureRenderer.CellPixelsToFit(picture, side) : FinishedPictureRenderer.PicturePixelsPerCell;
            return FinishedPictureRenderer.Render(level, picture, null, cellPixels: cell);
        }
    }
}
