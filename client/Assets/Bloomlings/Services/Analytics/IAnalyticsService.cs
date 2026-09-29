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
        /// <summary>Starts collection; called once, only after consent allows analytics.</summary>
        /// <param name="personalized">False: no advertising id or personal data (non-personalized consent).</param>
        void Initialize(bool personalized);

        /// <summary>Logs one event. Values are strings, longs or doubles.</summary>
        void Log(string eventName, IReadOnlyDictionary<string, object> parameters);
    }

    /// <summary>No analytics: without the SDK, or when consent was refused.</summary>
    public sealed class NullAnalyticsService : IAnalyticsService
    {
        public void Initialize(bool personalized)
        {
        }

        public void Log(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
        }
    }
}
