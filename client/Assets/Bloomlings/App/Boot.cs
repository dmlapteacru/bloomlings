using System;
using System.Collections;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Content;
using Bloomlings.Content.Packs;
using UnityEngine;

namespace Bloomlings.Client.App
{
    /// <summary>
    /// Composition root in the Boot scene (build index 0): creates the services, loads the bundled content, then
    /// hands over to <see cref="GameFlow"/>. Everything works offline (FR-074); online services are added by later
    /// stories behind their interfaces.
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
            services.Register<IClock>(new SystemClock());
            services.Register<IRemoteConfigService>(new BundledRemoteConfigService());

            ContentSet? content = null;
            Exception? error = null;
            var loader = new BundledContentLoader();
            yield return loader.Load(loaded => content = loaded, failed => error = failed);
            if (content == null)
            {
                Debug.LogException(error ?? new InvalidOperationException("Content did not load."));
                yield break;
            }

            Debug.Log($"[Boot] Content v{content.ContentVersion}: {content.LevelCount} levels, {content.PictureCount} pictures ({loader.Source}).");
            services.Register(content);

            var flow = new GameFlow(services);
            services.Register(flow);
            AppServices.MakeCurrent(services);
            flow.Begin();
        }
    }
}
