namespace Bloomlings.Core.Simulation
{
    /// <summary>Level status after each fixpoint, checked in the order Won, Jammed, Stuck (FR-025, FR-026).</summary>
    public enum LevelStatus
    {
        Playing,
        Won,
        Jammed,
        Stuck,
    }

    /// <summary>The recovery boosters that a Jam or Stuck screen may offer (FR-027).</summary>
    public enum Recovery
    {
        ExtraSlot,
        Shuffle,
        Return,
        BloomBurst,
    }
}
