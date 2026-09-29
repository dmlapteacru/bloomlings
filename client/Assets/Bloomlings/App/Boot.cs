using System;
using System.Collections;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Meta.DailyReward;
using Bloomlings.Client.Services;
using Bloomlings.Client.Services.Ads;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Consent;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Economy;
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
    /// services are added by later stories behind their interfaces.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class Boot : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Frame rate cap for the whole app.")]
        private int _targetFrameRate = 60;

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
            var catalog = new CatalogService(content);
            services.Register(catalog);
            var updates = new ContentUpdateService(services.Get<IRemoteConfigService>(), catalog, cache, Application.version);
            updates.Activated += activated =>
            {
                save.Progression.ContentVersionSeen = Math.Max(save.Progression.ContentVersionSeen, activated.ContentVersion);
                saves.Save();
                Debug.Log($"[ContentUpdate] {updates.Status}");
            };
            services.Register<IContentUpdateService>(updates);

            save.Progression.ContentVersionSeen = Math.Max(save.Progression.ContentVersionSeen, content.ContentVersion);
            // Petals and booster charges (US5); the unlock grants listen before progression replays unlocks.
            var economy = new EconomyService(save, EconomyConfig.From(services.Get<IRemoteConfigService>()), saves.Save);
            services.Register(economy);

            var progression = new ProgressionService(save, UnlockRoadmap.Default, saves.Save);
            progression.UnlockReached += entry => Debug.Log($"[Progression] Unlocked {entry.UnlockId} at L{entry.Level}.");
            progression.UnlockReached += entry => economy.OnUnlock(entry.UnlockId);
            services.Register(progression);
            progression.Initialize();

            // Store, ads, daily reward (US6). Every online part has an offline fallback (contracts/backend-services.md).
            var consent = new ConsentService(ServiceProviders.Consent?.Invoke());
            IAdsService ads = ServiceProviders.Ads?.Invoke() ?? new UnavailableAdsService();
            IPurchaseService purchases = ServiceProviders.Purchases?.Invoke() ?? new UnavailablePurchaseService();
            TextAsset? catalogJson = Resources.Load<TextAsset>("ProductCatalog");
            ProductCatalog products = catalogJson != null ? ProductCatalog.Parse(catalogJson.text) : new ProductCatalog(Array.Empty<StoreProduct>());
            var ledger = new PurchaseLedger(save, products, economy, clock, saves.Save);
            var adPolicy = new AdPolicy(remote, clock.UtcNow);
            services.Register<IConsentService>(consent);
            services.Register(ads);
            services.Register(purchases);
            services.Register(products);
            services.Register(ledger);
            services.Register(adPolicy);
            services.Register(new DailyRewardService(save, clock, remote, economy, saves.Save));

            var flow = new GameFlow(progression, catalog);
            flow.LevelWon += _ => adPolicy.OnLevelWon();
            flow.PostWinTransition = (won, next) =>
            {
                if (adPolicy.MayShowInterstitial(AdMoment.PostWin, won, ledger.RemoveAds, clock.UtcNow) && ads.IsInterstitialReady)
                {
                    ads.ShowInterstitial(() =>
                    {
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
            StartCoroutine(OnlineServices(remote, economy, updates, consent, ads, purchases, products));
        }

        private static IEnumerator OnlineServices(
            UgsRemoteConfigService remote,
            EconomyService economy,
            ContentUpdateService updates,
            ConsentService consent,
            IAdsService ads,
            IPurchaseService purchases,
            ProductCatalog products)
        {
            yield return remote.Refresh();
            economy.Config = EconomyConfig.From(remote);
            yield return updates.CheckForUpdate();

            // Consent before any ad or analytics initialization (FR-090); the default is the most restrictive.
            yield return consent.Gather();
            ads.Initialize(consent.State);
            purchases.Initialize(products, ready => Debug.Log(ready ? "[Store] Connected." : "[Store] Unavailable."));
        }
    }
}
