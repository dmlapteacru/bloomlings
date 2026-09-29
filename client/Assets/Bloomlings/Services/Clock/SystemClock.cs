using System;

namespace Bloomlings.Client.Services.Clock
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;

        public DateTime UtcToday => DateTime.UtcNow.Date;
    }

    /// <summary>A settable clock for tests and editor tools.</summary>
    public sealed class ManualClock : IClock
    {
        public ManualClock(DateTime utcNow)
        {
            UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        }

        public DateTime UtcNow { get; private set; }

        public DateTime UtcToday => UtcNow.Date;

        public void Set(DateTime utcNow) => UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

        public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
    }
}
