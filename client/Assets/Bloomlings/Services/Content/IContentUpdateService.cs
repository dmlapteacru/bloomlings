using System;
using System.Collections;
using System.Collections.Generic;
using Bloomlings.Content.Packs;

namespace Bloomlings.Client.Services.Content
{
    /// <summary>
    /// Downloads and activates newer content over HTTPS (FR-078, research R6, T092). The remote manifest URL comes from
    /// Remote Config (<c>content.manifestUrl</c>); an empty URL skips the check. Activation never changes the
    /// definition of an attempt in progress: <see cref="CatalogService"/> pins each attempt, and the new content applies
    /// from the next one.
    /// </summary>
    public interface IContentUpdateService
    {
        /// <summary>Raised on the main thread after a newer content version was verified, installed and activated.</summary>
        event Action<ContentSet>? Activated;

        /// <summary>
        /// Analytics hooks (contracts/analytics-events.md): <c>content_update</c> with <c>from_version</c> and
        /// <c>to_version</c>, and <c>content_error</c> with <c>pack_id</c>, <c>level_number</c> and <c>error</c>. The
        /// analytics service (T147) subscribes to them.
        /// </summary>
        event Action<string, IReadOnlyDictionary<string, string>>? AnalyticsEvent;

        /// <summary>A short description of the last check, for logs and the debug overlay.</summary>
        string Status { get; }

        /// <summary>Coroutine: checks the remote manifest once and activates a newer version if there is one.</summary>
        IEnumerator CheckForUpdate();
    }
}
