using System;
using System.Collections.Generic;

namespace Bloomlings.Core.Simulation
{
    /// <summary>
    /// Outcome of <see cref="LevelSession.Apply"/>: accepted with the ordered event log of the command and its settle,
    /// or rejected with a reason and no events. Game rules never throw.
    /// </summary>
    public sealed record CommandResult(bool Accepted, RejectReason? Reason, IReadOnlyList<GameEvent> Events)
    {
        public static CommandResult Accept(IReadOnlyList<GameEvent> events) => new CommandResult(true, null, events);

        public static CommandResult Reject(RejectReason reason) =>
            new CommandResult(false, reason, Array.Empty<GameEvent>());
    }

    /// <summary>Outcome of <see cref="LevelSession.Check"/>: the same validation as Apply, without mutation.</summary>
    public readonly struct CommandCheck
    {
        private CommandCheck(RejectReason? reason)
        {
            Reason = reason;
        }

        public static CommandCheck Allowed { get; } = new CommandCheck(null);

        public bool IsAllowed => Reason == null;

        public RejectReason? Reason { get; }

        public static CommandCheck Refused(RejectReason reason) => new CommandCheck(reason);

        public override string ToString() => Reason?.ToString() ?? "Allowed";
    }
}
