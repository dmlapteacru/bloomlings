using System;
using System.Collections;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Economy;
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
            services.Register<IRemoteConfigService>(new BundledRemoteConfigService());

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

            var flow = new GameFlow(progression, catalog);
            services.Register(flow);
            AppServices.MakeCurrent(services);
            flow.Begin(firstLaunch);

            // Offline-first (FR-074): the check runs after the game is already playable and never blocks it.
            StartCoroutine(updates.CheckForUpdate());
        }
    }
}
