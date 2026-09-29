using System;
using System.Collections.Generic;
using Bloomlings.Content.Packs;

namespace Bloomlings.Client.Services.Content
{
    /// <summary>What to do with a remote manifest.</summary>
    public enum UpdateDecision
    {
        /// <summary>Download and activate it.</summary>
        Download,

        /// <summary>Its content version is not newer than the content in use.</summary>
        UpToDate,

        /// <summary>It needs a newer app (<c>minAppVersion</c>).</summary>
        AppTooOld,
    }

    /// <summary>
    /// The engine-free decisions of a content update (T092, research R6): whether a remote manifest applies, where each
    /// pack comes from, and which cached version to start with. <see cref="ContentUpdateService"/> does the I/O.
    /// </summary>
    public static class ContentUpdater
    {
        public static UpdateDecision Decide(ContentManifest remote, int contentVersionInUse, string appVersion)
        {
            if (remote.ContentVersion <= contentVersionInUse)
            {
                return UpdateDecision.UpToDate;
            }

            return SemVer.Compare(appVersion, remote.MinAppVersion) < 0 ? UpdateDecision.AppTooOld : UpdateDecision.Download;
        }

        /// <summary>
        /// The download URL of a pack: its absolute <c>url</c>, or its <c>path</c> resolved against the manifest URL.
        /// Only HTTPS is accepted (FR-078).
        /// </summary>
        public static Uri PackUri(Uri manifestUri, PackEntry entry)
        {
            Uri uri = entry.Url != null ? new Uri(entry.Url, UriKind.Absolute) : new Uri(manifestUri, entry.Path!.Replace('\\', '/'));
            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException($"Pack '{entry.Id}' is not served over HTTPS: {uri}");
            }

            return uri;
        }

        /// <summary>
        /// Chooses the content to start with: the installed version when it is newer than the bundled one and this app
        /// can read it; otherwise the bundled content. A damaged cache falls back to the bundled content and is reported
        /// through <paramref name="onError"/>.
        /// </summary>
        public static ContentSet ChooseStartContent(ContentSet bundled, ContentCache cache, string appVersion, Action<Exception> onError)
        {
            try
            {
                ContentManifest? cached = cache.ActiveManifest();
                if (cached == null || Decide(cached, bundled.ContentVersion, appVersion) != UpdateDecision.Download)
                {
                    return bundled;
                }

                return cache.LoadActive();
            }
            catch (Exception ex)
            {
                onError(ex);
                return bundled;
            }
        }

        /// <summary>Parses the downloaded packs into a content set before they are installed (the pack readers verify each hash).</summary>
        public static ContentSet Parse(ContentManifest manifest, IReadOnlyDictionary<string, byte[]> packs) =>
            ContentLoader.FromPacks(manifest, entry => packs[entry.Id]);
    }
}
