using System;

namespace Bloomlings.Client.Services.Clock
{
    /// <summary>
    /// Wall-clock time in UTC. Daily features (reward, challenge) and ad caps read time only through this interface so
    /// tests can inject a fixed clock. Gameplay never reads the clock (FR-020).
    /// </summary>
    public interface IClock
    {
        /// <summary>Current time, <see cref="DateTimeKind.Utc"/>.</summary>
        DateTime UtcNow { get; }

        /// <summary>Current UTC calendar date (time 00:00, <see cref="DateTimeKind.Utc"/>).</summary>
        DateTime UtcToday { get; }
    }
}
