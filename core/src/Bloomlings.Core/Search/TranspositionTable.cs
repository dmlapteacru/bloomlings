using System.Collections.Generic;

namespace Bloomlings.Core.Search
{
    /// <summary>
    /// Settled states already decided during a search, keyed by <see cref="Simulation.LevelSession.StateHash"/>
    /// (research R8). Every tap commits a pod, so the state graph has no cycles and a state explored once never needs
    /// exploring again. A 64-bit Zobrist key makes collisions negligible for the state counts involved.
    /// </summary>
    public sealed class TranspositionTable
    {
        private readonly Dictionary<ulong, bool> _outcomes = new Dictionary<ulong, bool>();

        public int Count => _outcomes.Count;

        /// <summary>Whether the goal is reachable from the state, if already decided.</summary>
        public bool TryGet(ulong stateHash, out bool goalReachable) => _outcomes.TryGetValue(stateHash, out goalReachable);

        public void Set(ulong stateHash, bool goalReachable) => _outcomes[stateHash] = goalReachable;

        public void Clear() => _outcomes.Clear();
    }
}
