using System.Collections.Generic;
using Bloomlings.Core.Boards;

namespace Bloomlings.Core.Simulation
{
    /// <summary>
    /// Extension point of the settle loop for mechanics that react to rounds: mystery tiles, keys and locks, and
    /// special objects (US4). Hooks run in a fixed order, so they stay deterministic.
    /// </summary>
    internal interface IRoundHook
    {
        /// <summary>After reachability is computed and before allocation (mystery tiles reveal here, FR-039).</summary>
        void BeforeAllocation(RoundContext context);

        /// <summary>After one claimed layer is cleared (keys and special counters, FR-033, FR-037, FR-038).</summary>
        void OnLayerCleared(RoundContext context, int cell, LayerClearResult result, int pod);

        /// <summary>After all claims of the round are applied, before finished pods leave (special triggers).</summary>
        void AfterClears(RoundContext context);

        /// <summary>True while a mandatory special is unresolved, which blocks the win (FR-025).</summary>
        bool BlocksWin(LevelState state);
    }

    /// <summary>What a hook sees during one round.</summary>
    internal sealed class RoundContext
    {
        public RoundContext(LevelState state, int round, ReachabilityResult reach, List<GameEvent> events)
        {
            State = state;
            Round = round;
            Reach = reach;
            Events = events;
        }

        public LevelState State { get; }

        public int Round { get; }

        public ReachabilityResult Reach { get; }

        public List<GameEvent> Events { get; }

        /// <summary>Set by a hook that changed the state, so the loop runs another round even without claims.</summary>
        public bool Changed { get; set; }
    }
}
