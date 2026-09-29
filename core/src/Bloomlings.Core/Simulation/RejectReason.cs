namespace Bloomlings.Core.Simulation
{
    /// <summary>Why a command was refused. A refused command never changes the state (contracts/simulation-api.md).</summary>
    public enum RejectReason
    {
        /// <summary>The pod is buried, already committed or unknown.</summary>
        NotExposed,

        /// <summary>The pod's key has not been collected (FR-034).</summary>
        Locked,

        /// <summary>No free usable slot (FR-014).</summary>
        NoFreeSlot,

        /// <summary>A connected group needs more free slots than there are (FR-035).</summary>
        NotEnoughSlotsForGroup,

        /// <summary>The level is Won, Jammed or Stuck, and the command is not a recovery or Restart.</summary>
        LevelNotPlaying,

        /// <summary>The booster cannot be used in this state (FR-051).</summary>
        BoosterNotApplicable,
    }
}
