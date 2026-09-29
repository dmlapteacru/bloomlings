using System;
using System.Collections;
using System.Collections.Generic;
using Bloomlings.Client.Services.Ads;
using Bloomlings.Client.Services.Analytics;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Services.Consent;
using Bloomlings.Client.Services.Purchases;
using Bloomlings.Client.Services.Save;

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

        /// <summary>Analytics (Firebase Analytics); started only after consent.</summary>
        public static Func<IAnalyticsService>? Analytics { get; set; }

        /// <summary>Crash reporting (Firebase Crashlytics); started only after consent.</summary>
        public static Func<ICrashReporter>? Crashes { get; set; }

        /// <summary>Anonymous sign-in and identity linking (UGS Authentication).</summary>
        public static Func<IAuthService>? Auth { get; set; }

        /// <summary>The cloud copy of the save (UGS Cloud Save).</summary>
        public static Func<ICloudSaveService>? CloudSave { get; set; }

        /// <summary>The global leaderboard (UGS Leaderboards with the Cloud Code submission).</summary>
        public static Func<ILeaderboardService>? Leaderboard { get; set; }

        /// <summary>
        /// Platform sign-in tokens for linking (FR-087): the Sign in with Apple identity token and the Google Play Games
        /// server auth code. Set by the platform sign-in plugins when they are installed; the link buttons in Settings are
        /// shown only when a token source exists.
        /// </summary>
        public static Func<Action<string?>, IEnumerator>? AppleIdToken { get; set; }

        public static Func<Action<string?>, IEnumerator>? GooglePlayGamesAuthCode { get; set; }

        /// <summary>Fetches the remote values as raw strings by key (UGS Remote Config).</summary>
        public static Func<Action<IReadOnlyDictionary<string, string>>, IEnumerator>? RemoteConfigFetch { get; set; }
    }
}
