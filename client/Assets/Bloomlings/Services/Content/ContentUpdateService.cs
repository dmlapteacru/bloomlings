using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Bloomlings.Client.Services.Config;
using Bloomlings.Content.Packs;
using UnityEngine;
using UnityEngine.Networking;

namespace Bloomlings.Client.Services.Content
{
    /// <summary>
    /// <see cref="IContentUpdateService"/> over <see cref="UnityWebRequest"/> (T092). Every pack is checked against the
    /// manifest's length and SHA-256 before anything is installed; installation into <see cref="ContentCache"/> is atomic,
    /// and the new content is handed to <see cref="CatalogService.Activate"/>, which keeps attempts in progress on
    /// the content they started with. Failures leave the current content in use and raise <c>content_error</c>.
    /// </summary>
    public sealed class ContentUpdateService : IContentUpdateService
    {
        private readonly IRemoteConfigService _config;
        private readonly CatalogService _catalog;
        private readonly ContentCache _cache;
        private readonly string _appVersion;

        public ContentUpdateService(IRemoteConfigService config, CatalogService catalog, ContentCache cache, string appVersion)
        {
            _config = config;
            _catalog = catalog;
            _cache = cache;
            _appVersion = appVersion;
        }

        public event Action<ContentSet>? Activated;

        public event Action<string, IReadOnlyDictionary<string, string>>? AnalyticsEvent;

        public string Status { get; private set; } = "not checked";

        /// <summary>The default cache under <see cref="Application.persistentDataPath"/>.</summary>
        public static ContentCache DefaultCache() => new ContentCache(System.IO.Path.Combine(Application.persistentDataPath, "content"));

        public IEnumerator CheckForUpdate()
        {
            string url = _config.Get(RemoteConfigKeys.ContentManifestUrl);
            if (url.Length == 0)
            {
                Status = "no manifest URL";
                yield break;
            }

            var manifestUri = new Uri(url, UriKind.Absolute);
            byte[]? manifestBytes = null;
            string? error = null;
            yield return Download(manifestUri, bytes => manifestBytes = bytes, message => error = message);
            if (manifestBytes == null)
            {
                Fail("manifest", error ?? "download failed");
                yield break;
            }

            ContentManifest remote;
            try
            {
                remote = ContentManifest.Read(Encoding.UTF8.GetString(manifestBytes));
            }
            catch (Exception ex)
            {
                Fail("manifest", ex.Message);
                yield break;
            }

            int from = _catalog.ContentVersion;
            UpdateDecision decision = ContentUpdater.Decide(remote, from, _appVersion);
            if (decision != UpdateDecision.Download)
            {
                Status = decision == UpdateDecision.UpToDate
                    ? $"up to date (v{from})"
                    : $"v{remote.ContentVersion} needs app {remote.MinAppVersion}";
                yield break;
            }

            var packs = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (PackEntry entry in remote.Packs)
            {
                Uri packUri;
                try
                {
                    packUri = ContentUpdater.PackUri(manifestUri, entry);
                }
                catch (Exception ex)
                {
                    Fail(entry.Id, ex.Message);
                    yield break;
                }

                byte[]? bytes = null;
                yield return Download(packUri, b => bytes = b, message => error = message);
                if (bytes == null)
                {
                    Fail(entry.Id, error ?? "download failed");
                    yield break;
                }

                try
                {
                    PackIntegrity.Verify(entry, bytes);
                }
                catch (Exception ex)
                {
                    Fail(entry.Id, ex.Message);
                    yield break;
                }

                packs[entry.Id] = bytes;
            }

            // Parse and install off the main thread; activation happens back on it.
            Task<ContentSet> install = Task.Run(() =>
            {
                ContentSet parsed = ContentUpdater.Parse(remote, packs);
                _cache.Install(remote, packs);
                return parsed;
            });
            yield return new WaitUntil(() => install.IsCompleted);
            if (install.IsFaulted || install.IsCanceled)
            {
                Exception ex = install.Exception?.GetBaseException() ?? new OperationCanceledException("Content install was cancelled.");
                Fail(ex is ContentIntegrityException integrity ? integrity.PackId : "install", ex.Message);
                yield break;
            }

            _catalog.Activate(install.Result);
            Status = $"activated v{remote.ContentVersion} (was v{from})";
            AnalyticsEvent?.Invoke("content_update", new Dictionary<string, string>
            {
                ["from_version"] = from.ToString(CultureInfo.InvariantCulture),
                ["to_version"] = remote.ContentVersion.ToString(CultureInfo.InvariantCulture),
            });
            Activated?.Invoke(install.Result);
        }

        /// <summary>Reports a failure; <paramref name="levelNumber"/> is set for a level that failed to load.</summary>
        public void Fail(string packId, string message, int? levelNumber = null)
        {
            Status = $"error: {packId}: {message}";
            Debug.LogWarning($"[ContentUpdate] {Status}");
            AnalyticsEvent?.Invoke("content_error", new Dictionary<string, string>
            {
                ["pack_id"] = packId,
                ["level_number"] = levelNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                ["error"] = message.Length > 100 ? message.Substring(0, 100) : message,
            });
        }

        private static IEnumerator Download(Uri uri, Action<byte[]> onDone, Action<string> onError)
        {
            using UnityWebRequest request = UnityWebRequest.Get(uri.AbsoluteUri);
            request.timeout = 30;
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                onDone(request.downloadHandler.data);
            }
            else
            {
                onError(request.error ?? "request failed");
            }
        }
    }
}
