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
    /// The Home scene (FR-058): builds <see cref="HomeScreen"/>, <see cref="SettingsScreen"/> (saved toggles and level controls
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
                    AvatarOutfit: wardrobe.OutfitOf(profile.Avatar.Family),
                    NoAdsPromo: !ledger.RemoveAds,
                    DailyRewardPromo: daily.IsUnlocked,
                    DailyRewardWaiting: daily.CanClaim,
                    Avatar: profile.Avatar,
                    DailyRewardBadge: daily.ShowsBadge,
                    DailyRewardCompleted: daily.IsUnlocked && daily.ClaimedToday == DailyRewardService.StepCount,
                    ProfileName: ProfileScreen.NameOf(profile),
                    MilestoneProgressLeaves: next.HasValue ? PrimaryActionDesign.FilledLeaves(milestones.Table,next.Value.Level,next.Value.WinsToGo) : 0));
                home.SetFreeBoosterOffer(ads.IsRewardedReady && freeBooster.IsAvailable && FreeBoosterKind(economy).HasValue);
                if (board != null && board.ShowsRanks)
                {
                    board.Show(leaderboard.LastPage, leaderboard.IsStale, wardrobe.Profile, profile.Avatar, progression.HighestCompletedLevel);
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

            void OpenStore() => store!.Show(StoreItems(economy, ledger, wardrobe, OpenStore, Refresh), economy.Petals, false, wardrobe, clearing, progression.CurrentLevel, () =>
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
            Action submitAppearance = () => RunInBackground(leaderboard.SubmitPending());
            wardrobe.Changed += submitAppearance;
            profile.Changed += submitAppearance;
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
                save,
                () => progression.CurrentLevel,
                UnlockLevel(NavPlace.Wardrobe),
                wardrobe.OutfitOf,
                () => economy.Petals,
                () => StoreFrom("profile"),
                NavLook,
                Refresh);
            // The profile page and its edit card's grid of avatars: a drag on them never taps (spec 005 FR-041).
            UiKit.Scrolls(profileScreen.gameObject);
            board = LeaderboardScreen.Create(root, () => RunInBackground(leaderboard.Refresh()), () => economy.Petals, () => StoreFrom("leaderboard"), Navigate, NavLook, wardrobe.Catalog);
            showCollection = () =>
            {
                analytics?.CollectionOpen(collection.Count);
                collectionScreen.Show(collection.Entries, (entry, side) => RenderCollectionEntry(catalog, entry, side));
            };
            showBoard = () =>
            {
                analytics?.LeaderboardView(leaderboard.LastPage?.Player?.Rank ?? 0);
                board.Show(leaderboard.LastPage, leaderboard.IsStale, wardrobe.Profile, profile.Avatar, progression.HighestCompletedLevel);
                RunInBackground(leaderboard.Refresh());
            };
            // The Daily Reward popup (FR-055 as amended on 2026-10-09), built when first shown: from the Daily scene at any
            // time once unlocked. It no longer opens by itself (spec 005 FR-050): the scene's "!" calls for it until it is
            // opened that day. The day's five steps are claimed in order on the card, which stays open; an ad step plays a
            // rewarded ad when one is ready, else its stand-in pays it (DailyRewardService.AdStub) until the ads pay.
            DailyRewardPopup? dailyPopup = null;
            void ShowDailyReward()
            {
                if (!daily.IsUnlocked)
                {
                    return;
                }

                daily.MarkSeen();
                dailyPopup ??= DailyRewardPopup.Create(root);
                dailyPopup.Show(daily, () => ads.IsRewardedReady, (step, done) =>
                {
                    void Pay(bool watched, bool realAd)
                    {
                        int paid = daily.Claim(step.Number, adWatched: watched);
                        if (paid > 0)
                        {
                            analytics?.DailyRewardClaim(step.Number, step.Ad, daily.Streak);
                            if (realAd)
                            {
                                analytics?.AdRewarded("daily");
                            }
                        }

                        done(paid > 0);
                        Refresh();
                    }

                    if (!step.Ad)
                    {
                        Pay(false, false);
                    }
                    else if (ads.IsRewardedReady)
                    {
                        ads.ShowRewarded(AdPlacements.DailyBonus, earned => Pay(earned, earned));
                    }
                    else if (DailyRewardService.AdStub)
                    {
                        Debug.Log("[Ads] Daily Reward step " + step.Number + ": no rewarded ad yet, its stand-in pays it.");
                        Pay(true, false);
                    }
                    else
                    {
                        done(false);
                    }
                });
                Refresh();
            }

            // Real-money offers are previews until the platform products are created and connected.
            RemoveAdsCard? removeAds = null;
            var features = new HomeFeatureActions(
                () => dailyScreen.Show(new DailyChallengeModel(dailyChallenge.Today, dailyChallenge.CompletedToday, DailyChallengeService.RewardPetals)),
                OnProfile: () => profileScreen?.Show(),
                OnNoAds: () => removeAds?.Show(purchases.PriceOf("remove_ads"), purchases.IsAvailable),
                OnDailyReward: ShowDailyReward);

            home = HomeScreen.Create(
                UiFactory.Stretch(UiFactory.CreateRect("Home", root)),
                flow.Play,
                () => settings!.Show(),
                () =>
                {
                    analytics?.StoreOpen("home");
                    OpenStore();
                    store!.SelectShop();
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
                Navigate);
            string AccountStatus()
            {
                if (save.CloudOwnerId != null && auth.IsSignedIn && save.CloudOwnerId != auth.PlayerId)
                    return Loc.T("account.other_active");
                if (auth is IAccountLinkStatus link && link.LastLinkResult == AccountLinkResult.ExistingAccount)
                    return Loc.T("account.existing");
                if (auth is IAccountLinkStatus failure && failure.LastLinkResult == AccountLinkResult.Failed)
                    return Loc.T("account.link_failed");
                string identity = save.LinkedIdentity != null
                    ? (save.LinkedIdentity == "apple" ? Loc.T("account.linked_apple") : Loc.T("account.linked_google"))
                    : (auth.IsSignedIn ? Loc.T("account.signed_in") : Loc.T("account.local"));
                string state = sync.LastResult switch
                {
                    CloudSyncResult.Conflict => "account.sync_conflict",
                    CloudSyncResult.AccountChanged => "account.other_active",
                    CloudSyncResult.NeedsInitialization => "account.cloud_not_ready",
                    CloudSyncResult.UnsupportedProvider => "account.cloud_not_ready",
                    CloudSyncResult.Failed => "account.sync_retry",
                    _ => sync.IsDirty ? "account.saved_local" : "account.saved_cloud",
                };
                return identity + "\n" + Loc.T(state);
            }
            var account = new AccountActions(
                AccountStatus,
                auth.CanLink(LinkProvider.Apple),
                auth.CanLink(LinkProvider.GooglePlayGames),
                (provider, done) =>
                {
                    if (save.CloudOwnerId != null && auth.PlayerId != save.CloudOwnerId)
                    { done(false); return; }
                    RunInBackground(auth.Link(provider, ok =>
                {
                    if (ok)
                    {
                        save.LinkedIdentity = LinkedIdentities.Name(provider);
                        saves.Save();
                        RunInBackground(sync.Sync(_ => Refresh()));
                    }

                    done(ok);
                }));
                });
            settings = SettingsScreen.Create(
                root,
                save.Settings,
                saves.Save,
                account,
                services.TryGet(out IConsentService? consent) ? consent : null,
                new LevelActions(()=>progression.CurrentLevel,delta=>
                {
                    if(delta>0) progression.FastForward(progression.HighestCompletedLevel+delta);
                    else {save.Progression.HighestCompletedLevel=delta==0 ? 0 : Math.Max(0,progression.HighestCompletedLevel-1);saves.Save();}
                    Refresh();
                }),new PetalActions(()=>economy.Petals,delta=>economy.AdjustPetalsForTesting(delta==0?-economy.Petals:delta)));
            store = StoreScreen.Create(root, Navigate, NavLook);
            removeAds = RemoveAdsCard.Create(root, done => purchases.Buy("remove_ads", result => {Refresh();done(result);}), () => ledger.RemoveAds);
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
                wardrobe.Changed -= submitAppearance;
                profile.Changed -= submitAppearance;
            };
            RunInBackground(sync.Sync());
            RunInBackground(leaderboard.Refresh());

            // A system reached since the last visit is demonstrated once, on its button (FR-031: "demonstrated at that
            // level or within the next 1–2 levels"; Home is where these systems live). The Daily Reward no longer opens by
            // itself (spec 005 FR-050), so nothing covers them.
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

        /// <summary>The unlocked booster with the fewest charges, for the free-booster offer (once a day).</summary>
        private static BoosterKind? FreeBoosterKind(EconomyService economy)
        {
            BoosterKind? best = null;
            foreach ((string _, BoosterKind kind) in EconomyService.BoosterUnlocks)
            {
                if (kind != BoosterKind.BloomBurst && economy.IsUnlocked(kind) && (!best.HasValue || economy.Charges(kind) < economy.Charges(best.Value)))
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
        /// The Store rows (FR-051): previews of the approved real-money offers, boosters for Petals; and,
        /// once the Wardrobe is open, the cosmetics for Petals on their own tab.
        /// </summary>
        private static List<StoreItem> StoreItems(EconomyService economy, PurchaseLedger ledger, WardrobeService wardrobe, Action reopen, Action refreshHome)
        {
            var items = new List<StoreItem>();
            var drafts = new (string Id, string Title, string Details, string Price, string? Badge)[]
            {
                ("remove_ads", "store.remove_ads", "store.preview_remove_ads", "$5.99", null),
                ("remove_ads_bundle", "store.remove_ads_bundle", "store.preview_remove_ads_bundle", "$9.99", null),
                ("starter_garden", "store.starter_pack", "store.preview_starter", "$4.99", null),
                ("garden_small", "store.garden_bundle", "store.preview_garden_small", "$9.99", "store.best_sale"),
                ("garden_medium", "store.garden_bundle", "store.preview_garden_medium", "$19.99", null),
                ("garden_large", "store.garden_bundle", "store.preview_garden_large", "$39.99", null),
                ("petals_1200", "store.preview_petals_1200", "store.preview_base", "$1.99", null),
                ("petals_5500", "store.preview_petals_5500", "store.preview_value_14", "$7.99", null),
                ("petals_12000", "store.preview_petals_12000", "store.preview_value_16", "$14.99", null),
                ("petals_27500", "store.preview_petals_27500", "store.preview_value_14", "$29.99", null),
                ("petals_58000", "store.preview_petals_58000", "store.preview_value_15", "$54.99", null),
                ("petals_120000", "store.preview_petals_120000", "store.preview_value_13", "$99.99", null),
            };
            foreach ((string id, string title, string details, string price, string? badge) in drafts)
            {
                if ((id == "remove_ads" || id == "remove_ads_bundle") && ledger.RemoveAds)
                {
                    continue;
                }

                items.Add(new StoreItem(
                    id,
                    Loc.T(title),
                    price,
                    false,
                    () => { },
                    Badge: badge == null ? null : Loc.T(badge),
                    Description: Loc.T(details),
                    PreviewOnly: true));
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
                    Loc.F("store.booster_quantity", BoosterName(kind), EconomyService.PurchaseQuantity(kind)),
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
                    Name: Loc.F("store.booster_quantity", BoosterName(kind), EconomyService.PurchaseQuantity(kind))));
            }

            return items;
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
            return FinishedPictureRenderer.Render(level, picture, null, cellPixels: cell,shadow:false);
        }
    }
}
