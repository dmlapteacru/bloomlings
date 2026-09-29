using System;
using System.Collections;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Meta.Collection;
using Bloomlings.Client.Meta.DailyChallenge;
using Bloomlings.Client.Meta.DailyReward;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services;
using Bloomlings.Client.Services.Ads;
using Bloomlings.Client.Services.Analytics;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Consent;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Economy;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.Services.Purchases;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Progression;
using UnityEngine;

namespace Bloomlings.Client.App
{
    /// <summary>
    /// Composition root in the Boot scene (build index 0): loads the save, the bundled content and the roadmap,
    /// creates the services, then hands over to <see cref="GameFlow"/>. Everything works offline (FR-074); online
    /// services sit behind interfaces with offline fallbacks. Boot stays alive across scenes and runs the background
    /// coroutines (<see cref="Run"/>).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class Boot : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Frame rate cap for the whole app.")]
        private int _targetFrameRate = 60;

        /// <summary>Runs a background coroutine on the persistent Boot object (sync, leaderboard refresh).</summary>
        public void Run(IEnumerator routine) => StartCoroutine(routine);

        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = _targetFrameRate;

            var services = new AppServices();
            var clock = new SystemClock();
            services.Register<IClock>(clock);
            // Remote Config: bundled defaults until UGS answers (FR-085); values are always clamped.
            var remote = new UgsRemoteConfigService();
            services.Register<IRemoteConfigService>(remote);

            SaveService saves = SaveService.CreateDefault(clock);
            PlayerSave save = saves.Load();
            bool firstLaunch = saves.IsFirstLaunch;
            services.Register(saves);
            services.Register(save);

            // Sound, music and haptics follow the Settings toggles from the first frame (FR-073).
            services.Register(GameFeedback.Create(save.Settings));

            ContentSet? content = null;
            Exception? error = null;
            var loader = new BundledContentLoader();
            yield return loader.Load(loaded => content = loaded, failed => error = failed);
            if (content == null)
            {
                Debug.LogException(error ?? new InvalidOperationException("Content did not load."));
                yield break;
            }

            // A downloaded content version newer than the bundled one wins (R6); a damaged cache falls back to the bundle.
            ContentCache cache = ContentUpdateService.DefaultCache();
            string source = loader.Source;
            ContentSet bundled = content;
            content = ContentUpdater.ChooseStartContent(bundled, cache, Application.version, ex => Debug.LogWarning($"[Boot] Cached content ignored: {ex.Message}"));
            if (!ReferenceEquals(content, bundled))
            {
                source = $"downloaded v{content.ContentVersion}";
            }

            Debug.Log($"[Boot] Content v{content.ContentVersion}: {content.LevelCount} levels, {content.PictureCount} pictures ({source}); save from {saves.Source}.");
            services.Register(content);
            // Past the end of the catalog only development builds repeat it; a release build says more levels are coming.
            var catalog = new CatalogService(content, repeatPastEnd: Debug.isDebugBuild);
            services.Register(catalog);

            // Analytics and crash keys (T147): events wait on the device until consent allows them (FR-090).
            var analytics = new GameAnalytics(Application.version, () => catalog.ContentVersion, () => save.Progression.CurrentLevel);
            services.Register(analytics);
            var updates = new ContentUpdateService(services.Get<IRemoteConfigService>(), catalog, cache, Application.version);
            updates.Activated += activated =>
            {
                save.Progression.ContentVersionSeen = Math.Max(save.Progression.ContentVersionSeen, activated.ContentVersion);
                saves.Save();
                Debug.Log($"[ContentUpdate] {updates.Status}");
            };
            services.Register<IContentUpdateService>(updates);
            updates.AnalyticsEvent += (name, values) =>
            {
                int Int(string key) => int.TryParse(values.TryGetValue(key, out string? v) ? v : null, out int n) ? n : 0;
                if (name == AnalyticsEvents.ContentUpdate)
                {
                    analytics.ContentUpdate(Int("from_version"), Int("to_version"));
                }
                else if (name == AnalyticsEvents.ContentError)
                {
                    analytics.ContentError(values["pack_id"], Int("level_number"), values["error"]);
                }
            };

            save.Progression.ContentVersionSeen = Math.Max(save.Progression.ContentVersionSeen, content.ContentVersion);
            // Petals and booster charges (US5); the unlock grants listen before progression replays unlocks.
            var economy = new EconomyService(save, EconomyConfig.From(services.Get<IRemoteConfigService>()), saves.Save);
            services.Register(economy);

            // Long-run motivation (US7): milestones, Wardrobe, Collection; all local and offline-first.
            TextAsset? cosmeticsJson = Resources.Load<TextAsset>("CosmeticCatalog");
            CosmeticCatalog cosmetics = cosmeticsJson != null ? CosmeticCatalog.Parse(cosmeticsJson.text) : new CosmeticCatalog(Array.Empty<CosmeticItem>());
            var wardrobe = new WardrobeService(save, cosmetics, remote, saves.Save, economy);
            var milestones = new MilestoneService(save, MilestoneTable.Default, economy, saves.Save);
            var collection = new CollectionService(save, saves.Save);
            services.Register(wardrobe);
            services.Register(milestones);
            services.Register(collection);

            var progression = new ProgressionService(save, UnlockRoadmap.Default, saves.Save);
            progression.UnlockReached += entry => Debug.Log($"[Progression] Unlocked {entry.UnlockId} at L{entry.Level}.");
            progression.UnlockReached += entry => economy.OnUnlock(entry.UnlockId);
            progression.UnlockReached += entry => wardrobe.OnUnlock(entry.UnlockId);
            progression.UnlockReached += entry => analytics.Unlock(entry.UnlockId, entry.Kind.ToString().ToLowerInvariant());
            progression.LevelCompleted += level => milestones.OnLevelCompleted(level);
            milestones.Granted += grant => analytics.MilestoneClaim(grant.Level, grant.Cadence.Tier.ToString().ToLowerInvariant() + "_" + grant.Cadence.Every.ToString(System.Globalization.CultureInfo.InvariantCulture));
            wardrobe.Equipped += (family, itemId) => analytics.CosmeticEquip(WardrobeService.FamilyKey(family), itemId);
            services.Register(progression);
            progression.Initialize();

            // Store, ads, daily reward (US6). Every online part has an offline fallback (contracts/backend-services.md).
            var consent = new ConsentService(ServiceProviders.Consent?.Invoke());
            IAdsService ads = ServiceProviders.Ads?.Invoke() ?? new UnavailableAdsService();
            IPurchaseService purchases = ServiceProviders.Purchases?.Invoke() ?? new UnavailablePurchaseService();
            TextAsset? catalogJson = Resources.Load<TextAsset>("ProductCatalog");
            ProductCatalog products = catalogJson != null ? ProductCatalog.Parse(catalogJson.text) : new ProductCatalog(Array.Empty<StoreProduct>());
            var ledger = new PurchaseLedger(save, products, economy, clock, saves.Save);
            ledger.Granted += purchase =>
            {
                (long Micros, string Currency)? price = purchases.PriceDetailsOf(purchase.ProductId);
                analytics.Purchase(purchase.ProductId, price?.Micros ?? 0, price?.Currency ?? string.Empty, purchase.TransactionId);
            };
            var adPolicy = new AdPolicy(remote, clock.UtcNow);
            services.Register<IConsentService>(consent);
            services.Register(ads);
            services.Register(purchases);
            services.Register(products);
            services.Register(ledger);
            services.Register(adPolicy);
            services.Register(new DailyRewardService(save, clock, remote, economy, saves.Save));
            services.Register(new FreeBoosterAd(save, clock, saves.Save));
            services.Register(new DailyChallengeService(save, clock, remote, catalog, economy, saves.Save));

            // Identity, cloud save and leaderboard (US7): anonymous, in the background, never blocking play (FR-087).
            IAuthService auth = ServiceProviders.Auth?.Invoke() ?? new LocalOnlyAuthService();
            ICloudSaveService cloud = ServiceProviders.CloudSave?.Invoke() ?? new OfflineCloudSaveService();
            ILeaderboardService board = ServiceProviders.Leaderboard?.Invoke() ?? new OfflineLeaderboardService();
            var sync = new CloudSaveSync(save, saves.Save, cloud);
            saves.Saved += sync.MarkDirty;
            sync.Merged += () =>
            {
                // A merge can move progression forward: replay the unlocks it brought and refresh the economy view.
                progression.Initialize();
                Debug.Log($"[CloudSave] Merged: Level {progression.CurrentLevel}, {economy.Petals} Petals.");
            };
            var leaderboard = new LeaderboardClient(save, board, remote, clock, saves.Save);
            services.Register(auth);
            services.Register(sync);
            services.Register(leaderboard);
            services.Register(this);

            var flow = new GameFlow(progression, catalog);
            flow.LevelWon += _ => adPolicy.OnLevelWon();
            flow.LevelWon += level =>
            {
                LevelAttempt? attempt = flow.CurrentAttempt;
                if (attempt != null && attempt.LevelNumber == level)
                {
                    collection.Add(attempt.Definition, level);
                    leaderboard.OnLevelWon(attempt.Options.ContentVersion, flow.LastCommandLogHash);
                }

                // Between levels only, never mid-level (R15).
                StartCoroutine(sync.Sync());
                StartCoroutine(leaderboard.SubmitPending());
            };
            flow.PostWinTransition = (won, next) =>
            {
                if (adPolicy.MayShowInterstitial(AdMoment.PostWin, won, ledger.RemoveAds, clock.UtcNow) && ads.IsInterstitialReady)
                {
                    ads.ShowInterstitial(() =>
                    {
                        analytics.AdInterstitial(adPolicy.LevelsSinceLast, adPolicy.SecondsSinceLast(clock.UtcNow));
                        adPolicy.OnInterstitialShown(clock.UtcNow);
                        next();
                    });
                }
                else
                {
                    next();
                }
            };
            services.Register(flow);
            AppServices.MakeCurrent(services);
            flow.Begin(firstLaunch);

            // Offline-first (FR-074): everything below runs after the game is playable and never blocks it.
            StartCoroutine(OnlineServices(remote, economy, updates, consent, ads, purchases, products, ledger, auth, sync, leaderboard, analytics));
        }

        private static IEnumerator OnlineServices(
            UgsRemoteConfigService remote,
            EconomyService economy,
            ContentUpdateService updates,
            ConsentService consent,
            IAdsService ads,
            IPurchaseService purchases,
            ProductCatalog products,
            PurchaseLedger ledger,
            IAuthService auth,
            CloudSaveSync sync,
            LeaderboardClient leaderboard,
            GameAnalytics analytics)
        {
            // Anonymous sign-in first: Remote Config, Cloud Save and the leaderboard use the same player.
            bool signedIn = false;
            yield return auth.SignIn(ok => signedIn = ok);
            if (signedIn)
            {
                yield return sync.Sync(result => Debug.Log($"[CloudSave] {result}"));
                yield return leaderboard.Refresh();
            }

            yield return remote.Refresh();
            economy.Config = EconomyConfig.From(remote);
            yield return updates.CheckForUpdate();

            // Consent before any ad or analytics initialization (FR-090); the default is the most restrictive.
            yield return consent.Gather();
            analytics.Consent(consent.State.ToString().ToLowerInvariant(), AttText(consent.State));
            if (consent.AnalyticsAllowed)
            {
                analytics.Attach(
                    ServiceProviders.Analytics?.Invoke() ?? new NullAnalyticsService(),
                    ServiceProviders.Crashes?.Invoke() ?? new NullCrashReporter(),
                    consent.State == ConsentState.Personalized);
            }
            else
            {
                analytics.Disable();
            }

            ads.Initialize(consent.State);

            // A change in the privacy options applies at once: analytics stop or start, ads reload or stop.
            consent.Changed += state =>
            {
                analytics.ConsentChanged(
                    consent.AnalyticsAllowed,
                    state == ConsentState.Personalized,
                    () => ServiceProviders.Analytics?.Invoke() ?? new NullAnalyticsService(),
                    () => ServiceProviders.Crashes?.Invoke() ?? new NullCrashReporter());
                analytics.Consent(state.ToString().ToLowerInvariant(), AttText(state));
                ads.Initialize(state);
            };
            purchases.Initialize(products, ledger.Grant, ready =>
            {
                Debug.Log(ready ? "[Store] Connected." : "[Store] Unavailable.");
                if (ready)
                {
                    // The starter pack is once per player, not per install: the backend remembers a purchase.
                    purchases.CheckStarterPackOffer(eligible => ledger.OnStarterPackOffer(eligible));
                }
            });
        }

        private static string AttText(ConsentState state) =>
            Application.platform == RuntimePlatform.IPhonePlayer ? (state == ConsentState.Personalized ? "authorized" : "not_authorized") : "not_applicable";
    }
}
