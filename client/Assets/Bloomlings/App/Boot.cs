using System;
using System.Collections;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Content;
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

            Debug.Log($"[Boot] Content v{content.ContentVersion}: {content.LevelCount} levels, {content.PictureCount} pictures ({loader.Source}); save from {saves.Source}.");
            services.Register(content);
            var catalog = new CatalogService(content);
            services.Register(catalog);

            save.Progression.ContentVersionSeen = Math.Max(save.Progression.ContentVersionSeen, content.ContentVersion);
            var progression = new ProgressionService(save, UnlockRoadmap.Default, saves.Save);
            progression.UnlockReached += entry => Debug.Log($"[Progression] Unlocked {entry.UnlockId} at L{entry.Level}.");
            services.Register(progression);
            progression.Initialize();

            var flow = new GameFlow(progression, catalog);
            services.Register(flow);
            AppServices.MakeCurrent(services);
            flow.Begin(firstLaunch);
        }
    }
}
