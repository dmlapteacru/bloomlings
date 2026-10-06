using System.Collections.Generic;

namespace Bloomlings.Client.Services.Analytics
{
    /// <summary>
    /// An analytics backend (contracts/analytics-events.md, FR-086, research R14). The initial provider is Firebase
    /// Analytics (<c>Integrations/Firebase</c>). The game logs through <see cref="GameAnalytics"/>, which adds the common
    /// parameters and holds events until consent allows analytics (FR-090).
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Starts collection, only after consent allows analytics. Called again when the player changes consent in the
        /// privacy options, with the new personalization, and after <see cref="Stop"/> when consent is given again.
        /// </summary>
        /// <param name="personalized">False: no advertising id or personal data (non-personalized consent).</param>
        void Initialize(bool personalized);

        /// <summary>Consent was withdrawn: collection stops and every consent type is denied.</summary>
        void Stop();

        /// <summary>Logs one event. Values are strings, longs or doubles.</summary>
        void Log(string eventName, IReadOnlyDictionary<string, object> parameters);
    }

    /// <summary>No analytics: without the SDK, or when consent was refused.</summary>
    public sealed class NullAnalyticsService : IAnalyticsService
    {
        public void Initialize(bool personalized)
        {
        }

        public void Stop()
        {
        }

        public void Log(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
        }
    }
}
