using System.Collections.Generic;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Content.Golden
{
    /// <summary>What a replay produced: the event log text, its digest, the final state hash and status.</summary>
    public sealed record GoldenOutcome(IReadOnlyList<string> Log, string EventsDigest, string StateHash, LevelStatus Status);

    /// <summary>
    /// Replays a <see cref="GoldenCase"/>. The log has one line per command (<c>&gt; tap:p1</c>, with
    /// <c>rejected:&lt;reason&gt;</c> for a refused one) followed by the canonical lines of its events
    /// (<see cref="EventText"/>). The digest covers the whole log.
    /// </summary>
    public static class GoldenRunner
    {
        public static GoldenOutcome Run(GoldenCase golden)
        {
            var options = new SessionOptions(golden.ContentVersion, golden.ShuffleNodeBudget);
            LevelSession session = LevelSession.Load(golden.Definition, golden.Picture, options);
            var log = new List<string>();
            foreach (Command command in golden.Commands)
            {
                CommandResult result = session.Apply(command);
                string header = "> " + CommandText.Format(command);
                if (!result.Accepted)
                {
                    log.Add(header + " rejected:" + result.Reason);
                    continue;
                }

                log.Add(header);
                foreach (GameEvent e in result.Events)
                {
                    log.Add(EventText.Format(e));
                }
            }

            return new GoldenOutcome(log, EventText.Digest(log), EventText.Hex(session.StateHash), session.Status);
        }

        /// <summary>The case with its expected values replaced by what the replay produced (reviewed regeneration).</summary>
        public static GoldenCase WithOutcome(GoldenCase golden, GoldenOutcome outcome) => golden with
        {
            ExpectedEventsDigest = outcome.EventsDigest,
            ExpectedStateHash = outcome.StateHash,
            ExpectedStatus = outcome.Status,
        };
    }
}
