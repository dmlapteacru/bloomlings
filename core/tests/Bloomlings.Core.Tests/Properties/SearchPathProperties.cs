using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Search;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Tests.Fixtures;
using Bloomlings.Core.Tray;
using FsCheck.NUnit;

namespace Bloomlings.Core.Tests.Properties
{
    /// <summary>
    /// The search's fast path changes no outcome (T179): a search step (<see cref="LevelSession.SearchChild"/>) reaches
    /// exactly the state of a clone with the command applied, the board's kept reachability and layer count equal a
    /// fresh computation, and the symmetry pruning keeps the moves the signature texts keep.
    /// </summary>
    public class SearchPathProperties
    {
        private const int Runs = 300;

        [Property(MaxTest = Runs)]
        public bool SearchChild_EqualsCloneAndApply(ulong seed) =>
            Replay(seed, session =>
            {
                ulong parent = session.StateHash;
                foreach (string id in session.View.PodIds)
                {
                    var tap = new TapPod(id);
                    LevelSession? fast = session.SearchChild(tap);
                    LevelSession slow = session.Clone();
                    bool accepted = slow.Apply(tap).Accepted;
                    if ((fast != null) != accepted || session.StateHash != parent)
                    {
                        return false;
                    }

                    if (fast == null)
                    {
                        continue;
                    }

                    if (fast.StateHash != slow.StateHash || fast.Status != slow.Status || fast.ComputeFullStateHash() != fast.StateHash)
                    {
                        return false;
                    }

                    // The search's copy plays on exactly like the ordinary one.
                    foreach (Command next in RandomLevels.Commands(seed ^ (ulong)id.Length, slow.Clone(), 8))
                    {
                        CommandResult a = fast.Apply(next);
                        CommandResult b = slow.Apply(next);
                        if (a.Accepted != b.Accepted || Digest(a) != Digest(b) || fast.StateHash != slow.StateHash || fast.Status != slow.Status)
                        {
                            return false;
                        }
                    }
                }

                return true;
            });

        [Property(MaxTest = Runs)]
        public bool KeptReachability_EqualsFreshCompute(ulong seed) =>
            Replay(seed, session =>
            {
                Boards.Board board = session.State.Board;
                ReachabilityResult kept = board.Reach;
                ReachabilityResult fresh = Reachability.Compute(board);
                if (!kept.Targets.Select(t => (t.Index, t.Cell, t.Distance)).SequenceEqual(fresh.Targets.Select(t => (t.Index, t.Cell, t.Distance))))
                {
                    return false;
                }

                for (int i = 0; i < board.CellCount; i++)
                {
                    if (kept.OpenDistance(i) != fresh.OpenDistance(i) || kept.IsReachable(i) != fresh.IsReachable(i)
                        || (fresh.IsReachable(i) && !kept.RouteTo(i).SequenceEqual(fresh.RouteTo(i))))
                    {
                        return false;
                    }
                }

                return true;
            });

        [Property(MaxTest = Runs)]
        public bool Reachability_EqualsTheFirstImplementation(ulong seed) =>
            Replay(seed, session =>
            {
                Boards.Board board = session.State.Board;
                (List<ReachableTarget> targets, int[] distance, Func<int, List<CellPos>> route) = ReferenceReachability(board);
                ReachabilityResult full = Reachability.Compute(board);
                ReachabilityResult search = Reachability.Compute(board, routes: false);
                var expected = targets.Select(t => (t.Index, t.Cell, t.Distance)).ToList();
                if (!full.Targets.Select(t => (t.Index, t.Cell, t.Distance)).SequenceEqual(expected)
                    || !search.Targets.Select(t => (t.Index, t.Cell, t.Distance)).SequenceEqual(expected))
                {
                    return false;
                }

                for (int i = 0; i < board.CellCount; i++)
                {
                    if (full.OpenDistance(i) != distance[i] || search.OpenDistance(i) != distance[i])
                    {
                        return false;
                    }
                }

                return targets.All(t => full.RouteTo(t.Index).SequenceEqual(route(t.Index)));
            });

        [Property(MaxTest = Runs)]
        public bool LayerCount_EqualsRecount(ulong seed) =>
            Replay(seed, session =>
            {
                Boards.Board board = session.State.Board;
                int sum = 0;
                for (int i = 0; i < board.CellCount; i++)
                {
                    sum += board.RemainingLayers(i);
                }

                return board.CountAllLayers() == sum;
            });

        [Property(MaxTest = Runs)]
        public bool Moves_EqualTheSignatureTextPruning(ulong seed) =>
            Replay(seed, session =>
                StateSearch.Moves(session, MoveOrder.ProgressFirst).SequenceEqual(ReferenceMoves(session, MoveOrder.ProgressFirst))
                && StateSearch.Moves(session, MoveOrder.IdleFirst).SequenceEqual(ReferenceMoves(session, MoveOrder.IdleFirst)));

        /// <summary>
        /// The pruning as it was first written: a tap is checked with <see cref="LevelSession.Check"/>, and an exposed pod
        /// whose stack prints like an earlier one's is dropped.
        /// </summary>
        private static List<Command> ReferenceMoves(LevelSession session, MoveOrder order)
        {
            LevelState state = session.State;
            var result = new List<Command>();
            if (state.Status != LevelStatus.Playing)
            {
                return result;
            }

            var progressable = new bool[state.Catalog.Count];
            foreach (ReachableTarget target in Reachability.Compute(state.Board).Targets)
            {
                if (!state.Board.IsMysteryHidden(target.Index))
                {
                    progressable[state.Catalog.IndexOf(state.Board.TopLayer(target.Index))] = true;
                }
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var second = new List<Command>();
            foreach (int pod in state.Tray.Exposed())
            {
                if (!session.Check(new TapPod(state.PodId(pod))).IsAllowed || !seen.Add(Text(state, pod)))
                {
                    continue;
                }

                bool progresses = progressable[state.PodVariantIndex[pod]];
                bool early = order == MoveOrder.ProgressFirst ? progresses : !progresses;
                (early ? result : second).Add(new TapPod(state.PodId(pod)));
            }

            result.AddRange(second);
            return result;
        }

        private static string Text(LevelState state, int exposedPod)
        {
            var sb = new StringBuilder();
            foreach (int pod in state.Tray.StackTopFirst(state.Tray.StackOf(exposedPod)))
            {
                PodDef def = state.PodDefs[pod];
                PodRuntime runtime = state.Pods[pod];
                sb.Append(def.Variant.Key).Append(':').Append(runtime.Remaining)
                    .Append(def.Mystery && !runtime.VariantRevealed ? "?" : string.Empty)
                    .Append(def.LockKeyId != null ? "L" + def.LockKeyId : string.Empty)
                    .Append(def.ConnectedGroupId != null ? "C" + def.ConnectedGroupId : string.Empty)
                    .Append('|');
            }

            return sb.ToString();
        }

        /// <summary>Reachability as it was first written: CellPos neighbours, a comparison sort, routes from parent links.</summary>
        private static (List<ReachableTarget> Targets, int[] Distance, Func<int, List<CellPos>> Route) ReferenceReachability(Boards.Board board)
        {
            int n = board.CellCount;
            var distance = Enumerable.Repeat(-1, n).ToArray();
            var parent = Enumerable.Repeat(-1, n).ToArray();
            var queue = new Queue<int>();
            foreach (EntryDef entry in board.Entries)
            {
                int index = board.IndexOf(entry.Cell);
                if (board.IsWalkable(index) && distance[index] < 0)
                {
                    distance[index] = 1;
                    queue.Enqueue(index);
                }
            }

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                CellPos pos = board.PosOf(current);
                for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
                {
                    if (pos.TryGetNeighbour(dir, board.Width, board.Height, out CellPos next))
                    {
                        int j = board.IndexOf(next);
                        if (distance[j] < 0 && board.IsWalkable(j))
                        {
                            distance[j] = distance[current] + 1;
                            parent[j] = current;
                            queue.Enqueue(j);
                        }
                    }
                }
            }

            var targets = new List<ReachableTarget>();
            var via = Enumerable.Repeat(-1, n).ToArray();
            for (int i = 0; i < n; i++)
            {
                if (!board.IsTarget(i))
                {
                    continue;
                }

                int best = board.IsEntryCell(i) ? 1 : int.MaxValue;
                CellPos pos = board.PosOf(i);
                for (int dir = 0; dir < CellPos.NeighbourCount; dir++)
                {
                    if (pos.TryGetNeighbour(dir, board.Width, board.Height, out CellPos next))
                    {
                        int d = distance[board.IndexOf(next)];
                        if (d > 0 && d + 1 < best)
                        {
                            best = d + 1;
                            via[i] = board.IndexOf(next);
                        }
                    }
                }

                if (best != int.MaxValue)
                {
                    targets.Add(new ReachableTarget(i, pos, best));
                }
            }

            targets.Sort((a, b) => CellPos.CompareCandidates(a.Distance, a.Cell, b.Distance, b.Cell));
            List<CellPos> Route(int target)
            {
                var route = new List<CellPos>();
                for (int step = via[target]; step >= 0; step = parent[step])
                {
                    route.Insert(0, board.PosOf(step));
                }

                route.Add(board.PosOf(target));
                return route;
            }

            return (targets, distance, Route);
        }

        /// <summary>Checks <paramref name="invariant"/> on the start state and after every command of a random sequence.</summary>
        private static bool Replay(ulong seed, Func<LevelSession, bool> invariant)
        {
            LevelSession session = RandomLevels.Create(seed);
            if (!invariant(session))
            {
                return false;
            }

            foreach (Command command in RandomLevels.Commands(seed, session))
            {
                session.Apply(command);
                if (!invariant(session))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Digest(CommandResult result) => EventText.Digest(result.Events.Select(EventText.Format));
    }
}
