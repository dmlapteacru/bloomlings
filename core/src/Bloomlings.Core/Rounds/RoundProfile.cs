namespace Bloomlings.Core.Rounds
{
    /// <summary>
    /// The movement profile of the round timeline in integer microseconds (spec 006 research R2; the reference's
    /// <c>timeline.cjs PROFILE</c> and <c>TAIL</c> in milliseconds × 1000). A courier walks out at
    /// <see cref="OutPerCell"/> a cell (at least <see cref="MinOut"/>), acts for <see cref="Act"/>, and walks back at
    /// <see cref="ReturnPerCell"/> a cell (at least <see cref="MinReturn"/>); a pod's couriers leave
    /// <see cref="DepartureGap"/> apart; a link whose unsent work is all sent releases its places
    /// <see cref="ReleaseLag"/> later. <see cref="Default"/> is the reference; other values exist only for tests
    /// (for example the conformance break test of SC-005).
    /// </summary>
    public sealed class RoundProfile
    {
        public RoundProfile(
            long outPerCell = 100_000,
            long returnPerCell = 75_000,
            long act = 100_000,
            long minOut = 650_000,
            long minReturn = 400_000,
            long departureGap = 250_000,
            long releaseLag = 300_000,
            long tailDepartureGap = 250_000)
        {
            OutPerCell = outPerCell;
            ReturnPerCell = returnPerCell;
            Act = act;
            MinOut = minOut;
            MinReturn = minReturn;
            DepartureGap = departureGap;
            ReleaseLag = releaseLag;
            TailDepartureGap = tailDepartureGap;
        }

        /// <summary>The reference profile.</summary>
        public static RoundProfile Default { get; } = new RoundProfile();

        public long OutPerCell { get; }

        public long ReturnPerCell { get; }

        public long Act { get; }

        public long MinOut { get; }

        public long MinReturn { get; }

        public long DepartureGap { get; }

        public long ReleaseLag { get; }

        /// <summary>The departure gap in the tail (the reference keeps it equal to <see cref="DepartureGap"/>).</summary>
        public long TailDepartureGap { get; }

        /// <summary>
        /// The display speed of Auto in the tail (reference <c>TAIL.speed</c> is 2.5; spec 006 FR-011 shows the tail at
        /// 2×). Presentation only: model time is the same at every speed.
        /// </summary>
        public const int TailDisplaySpeed = 2;
    }
}
