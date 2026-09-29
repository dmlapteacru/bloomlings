using System.Collections;
using System.Collections.Generic;
using Bloomlings.Client.Services.Config;

namespace Bloomlings.Client.Services.Backend
{
    /// <summary>
    /// Remote Config (FR-085, research R11; T126). Values are fetched from UGS Remote Config by the UGS integration
    /// (<see cref="ServiceProviders.RemoteConfigFetch"/>) and always read through <see cref="BundledRemoteConfigService"/>:
    /// a missing, malformed or out-of-range value falls back to the bundled default or is clamped. Offline, or without
    /// the UGS package, the bundled defaults apply. Core rules and level definitions are never remote (FR-085).
    /// </summary>
    public sealed class UgsRemoteConfigService : IRemoteConfigService
    {
        private BundledRemoteConfigService _current = new BundledRemoteConfigService();

        /// <summary>True after a fetch delivered values in this session.</summary>
        public bool IsRemote { get; private set; }

        /// <summary>Coroutine: fetches once; on failure the current values stay.</summary>
        public IEnumerator Refresh()
        {
            if (ServiceProviders.RemoteConfigFetch == null)
            {
                yield break;
            }

            yield return ServiceProviders.RemoteConfigFetch(values =>
            {
                _current = new BundledRemoteConfigService(new Dictionary<string, string>(values));
                IsRemote = true;
            });
        }

        public int Get(IntKey key) => _current.Get(key);

        public bool Get(BoolKey key) => _current.Get(key);

        public string Get(StringKey key) => _current.Get(key);
    }
}
