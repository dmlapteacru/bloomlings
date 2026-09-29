using System;
using System.Collections;
using System.Collections.Generic;
using Bloomlings.Client.Services.Ads;
using Bloomlings.Client.Services.Consent;
using Bloomlings.Client.Services.Purchases;

namespace Bloomlings.Client.Services
{
    /// <summary>
    /// Factories for the SDK-backed services. Each SDK integration is its own assembly under <c>Integrations/</c>,
    /// compiled only when its package is installed (asmdef define constraints), and registers itself here before the
    /// first scene loads. The game assembly never references an SDK, and without one Boot uses the offline fallbacks
    /// the backend contract prescribes (contracts/backend-services.md).
    /// </summary>
    public static class ServiceProviders
    {
        public static Func<IAdsService>? Ads { get; set; }

        public static Func<IConsentProvider>? Consent { get; set; }

        public static Func<IPurchaseService>? Purchases { get; set; }

        /// <summary>Fetches the remote values as raw strings by key (UGS Remote Config).</summary>
        public static Func<Action<IReadOnlyDictionary<string, string>>, IEnumerator>? RemoteConfigFetch { get; set; }
    }
}
