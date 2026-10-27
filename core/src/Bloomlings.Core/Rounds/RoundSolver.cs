using System;
using System.Collections.Generic;

namespace Bloomlings.Core.Rounds
{
    /// <summary>The result of a solver search: a win (with its route), a proven loss, or unknown (budget ran out).</summary>
    public sealed class RoundSolveResult
    {
        internal RoundSolveResult(bool? win, IReadOnlyList<RoundAction>? route, int nodes)
        {
            Win = win;
            Route = route;
            Nodes = nodes;
        }

        /// <summary>True: a winning route; false: proven unwinnable; null: the node budget ran out.</summary>
        public bool? Win { get; }

        public IReadOnlyList<RoundAction>? Route { get; }

        /// <summary>The positions expanded.</summary>
        public int Nodes { get; }
    }

    /// <summary>
    /// The patient full-information search of the reference (<c>idle-rules.cjs</c> step, <c>level-stats/solve.mjs</c>
    /// route and winnable): after each action the player waits until all work has settled, so a position is a settled
    /// state and is memoized by its signature. It finds sound winning witnesses, not every timed strategy; a search that
    /// runs out of its node budget answers "unknown", never "lost". Shuffle uses it to keep a winning continuation.
    /// </summary>
    public sealed class RoundSolver
    {
        private readonly Dictionary<string, bool> _memo = new Dictionary<string, bool>(StringComparer.Ordinal);

        public RoundSolver(int budget = 60000)
        {
            if (budget < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(budget));
            }

            Budget = budget;
        }

        public int Budget { get; }

        /// <summary>
        /// The patient step (reference <c>idle-rules.cjs step</c>): a fresh timeline on <paramref name="state"/>, the
        /// action committed, then every step run until nothing is pending. Null when the action is refused.
        /// </summary>
        public static RoundState? Step(RoundState state, RoundAction action, RoundProfile? profile = null)
        {
            var t = new RoundTimeline(state, profile) { CheckInvariants = false };
            if (!t.Commit(action).Accepted)
            {
                return null;
            }

            t.AdvanceUntilIdle();
            return t.State;
        }

        /// <summary>
        /// One winning route from the start, trying legal actions in their order, depth first with a memo of failed
        /// positions (reference <c>solver.route</c>).
        /// </summary>
        public RoundSolveResult Route(RoundLevel level, int capacity = 0)
        {
            var memo = new Dictionary<string, List<RoundAction>?>(StringComparer.Ordinal);
            int nodes = 0;
            List<RoundAction>? Go(RoundState s)
            {
                if (s.Status == RoundStatus.Won)
                {
                    return new List<RoundAction>();
                }

                if (s.Status != RoundStatus.Playing)
                {
                    return null;
                }

                string key = s.Signature();
                if (memo.TryGetValue(key, out List<RoundAction>? known))
                {
                    return known;
                }

                if (++nodes > Budget)
                {
                    throw new BudgetExceeded();
                }

                foreach (RoundAction a in RoundRules.Legal(s))
                {
                    List<RoundAction>? rest = Go(Step(s, a)!);
                    if (rest != null)
                    {
                        var route = new List<RoundAction>(rest.Count + 1) { a };
                        route.AddRange(rest);
                        memo[key] = route;
                        return route;
                    }
                }

                memo[key] = null;
                return null;
            }

            try
            {
                List<RoundAction>? found = Go(RoundRules.Create(level, capacity));
                return new RoundSolveResult(found != null, found, nodes);
            }
            catch (BudgetExceeded)
            {
                return new RoundSolveResult(null, null, nodes);
            }
        }

        /// <summary>
        /// Whether a settled position can still be won (reference <c>solver.winnable</c>): true, false, or null when this
        /// call ran out of budget. Settled positions are remembered across calls of this solver.
        /// </summary>
        public bool? Winnable(RoundState start)
        {
            int nodes = 0;
            bool Go(RoundState s)
            {
                if (s.Status == RoundStatus.Won)
                {
                    return true;
                }

                if (s.Status != RoundStatus.Playing)
                {
                    return false;
                }

                string key = s.Signature();
                if (_memo.TryGetValue(key, out bool known))
                {
                    return known;
                }

                if (++nodes > Budget)
                {
                    throw new BudgetExceeded();
                }

                foreach (RoundAction a in RoundRules.Legal(s))
                {
                    if (Go(Step(s, a)!))
                    {
                        _memo[key] = true;
                        return true;
                    }
                }

                _memo[key] = false;
                return false;
            }

            try
            {
                return Go(start);
            }
            catch (BudgetExceeded)
            {
                return null;
            }
        }

        /// <summary>
        /// Whether a live position can still be won: a copy of the timeline runs until nothing is pending (the patient
        /// continuation), then <see cref="Winnable(RoundState)"/>.
        /// </summary>
        public bool? Winnable(RoundTimeline timeline)
        {
            RoundTimeline copy = timeline.Copy();
            copy.CheckInvariants = false;
            copy.AdvanceUntilIdle();
            return Winnable(copy.State);
        }

        private sealed class BudgetExceeded : Exception
        {
        }
    }
}
