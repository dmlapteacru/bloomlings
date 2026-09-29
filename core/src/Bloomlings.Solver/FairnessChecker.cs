using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Random;
using Bloomlings.Core.Search;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;

namespace Bloomlings.Solver
{
    public enum FairnessStatus
    {
        /// <summary>No mystery, or a strategy wins in every world the player cannot rule out.</summary>
        Fair,

        /// <summary>Some observation leaves the player without a safe continuation: winning needs hidden knowledge.</summary>
        Unfair,

        /// <summary>More mystery than the cap (2 mystery pods plus 3 mystery tiles).</summary>
        OverCap,

        /// <summary>The node budget ran out.</summary>
        Unknown,
    }

    public sealed record FairnessResult(FairnessStatus Status, int Worlds, int NodesUsed)
    {
        /// <summary>The value of <c>playerInfoFair</c> in the validation record: null when the level has no mystery.</summary>
        public bool? PlayerInfoFair { get; init; }
    }

    /// <summary>
    /// Player-information fairness for mystery pods and tiles (FR-039, FR-080, research R8; T104). The player sees the
    /// board, the tray and every event, but not the hidden variants. A world is one assignment of variants to the
    /// hidden parts that keeps the exact per-variant accounting, so it is a level the player cannot rule out. The check
    /// is an AND-OR search: the player picks a tap (OR); worlds whose events then differ become distinguishable, and
    /// every such observation class must still be winnable (AND). The level is fair when some strategy wins in every
    /// world consistent with the start the player sees. Hidden layers below a mystery tile's top are not enumerated;
    /// they show through the layer peek once the tile reveals.
    /// </summary>
    public static class FairnessChecker
    {
        public const int MaxMysteryPods = 2;
        public const int MaxMysteryTiles = 3;

        public static FairnessResult Check(LevelDefinition definition, BasePicture picture, SessionOptions options, int nodeBudget)
        {
            var tiles = definition.Overlays.Where(o => o.Mystery).Select(o => o.Cell).ToArray();
            var pods = definition.Pods.Where(p => p.Mystery).Select(p => p.Id).ToArray();
            if (tiles.Length == 0 && pods.Length == 0)
            {
                return new FairnessResult(FairnessStatus.Fair, 1, 0);
            }

            if (pods.Length > MaxMysteryPods || tiles.Length > MaxMysteryTiles)
            {
                return new FairnessResult(FairnessStatus.OverCap, 0, 0) { PlayerInfoFair = false };
            }

            LevelSession actual = LevelSession.Load(definition, picture, options);
            string start = StartSignature(actual, tiles);
            List<World> worlds = Worlds(definition, picture, options, tiles, pods)
                .Where(w => StartSignature(w, tiles) == start)
                .Select((w, i) => new World(i, w))
                .ToList();

            var search = new AndOr(nodeBudget);
            bool fair = search.Winnable(worlds);
            if (search.BudgetExceeded)
            {
                return new FairnessResult(FairnessStatus.Unknown, worlds.Count, search.Nodes);
            }

            return new FairnessResult(fair ? FairnessStatus.Fair : FairnessStatus.Unfair, worlds.Count, search.Nodes) { PlayerInfoFair = fair };
        }

        /// <summary>Every assignment of the level's active variants to the hidden parts that loads (exact accounting holds).</summary>
        private static IEnumerable<LevelSession> Worlds(LevelDefinition definition, BasePicture picture, SessionOptions options, CellPos[] tiles, string[] pods)
        {
            VariantId[] active = definition.Pods.Select(p => p.Variant).Distinct().OrderBy(v => v).ToArray();
            int unknowns = tiles.Length + pods.Length;
            var choice = new int[unknowns];
            while (true)
            {
                var tileMap = new Dictionary<CellPos, VariantId>();
                var podMap = new Dictionary<string, VariantId>(StringComparer.Ordinal);
                for (int i = 0; i < tiles.Length; i++)
                {
                    tileMap[tiles[i]] = active[choice[i]];
                }

                for (int i = 0; i < pods.Length; i++)
                {
                    podMap[pods[i]] = active[choice[tiles.Length + i]];
                }

                LevelSession? world = null;
                try
                {
                    world = LevelSession.LoadHypothesis(definition, picture, options, new MysteryAssignment(tileMap, podMap));
                }
                catch (InvalidLevelException)
                {
                    // Breaks exact accounting: the player can rule this world out.
                }

                if (world != null)
                {
                    yield return world;
                }

                int k = 0;
                while (k < unknowns && ++choice[k] == active.Length)
                {
                    choice[k] = 0;
                    k++;
                }

                if (k == unknowns)
                {
                    yield break;
                }
            }
        }

        /// <summary>What the player sees of the mystery tiles at the start (those reachable at once are already revealed).</summary>
        private static string StartSignature(LevelSession session, CellPos[] tiles) =>
            string.Join(",", tiles.Select(c => session.View.Cell(c).Visible?.Key ?? "?"));

        /// <summary>
        /// One hypothetical level and its current session. The id matters: a pod's variant is not part of the state
        /// hash, so two worlds can share a hash and still differ.
        /// </summary>
        private readonly struct World
        {
            public World(int id, LevelSession session)
            {
                Id = id;
                Session = session;
            }

            public int Id { get; }

            public LevelSession Session { get; }
        }

        private sealed class AndOr
        {
            private readonly Dictionary<ulong, bool> _memo = new Dictionary<ulong, bool>();
            private readonly int _budget;

            public AndOr(int budget)
            {
                _budget = budget;
            }

            public int Nodes { get; private set; }

            public bool BudgetExceeded { get; private set; }

            /// <summary>True when one strategy wins in every world of the class (the worlds look identical to the player).</summary>
            public bool Winnable(List<World> worlds)
            {
                LevelStatus status = worlds[0].Session.Status;
                if (status == LevelStatus.Won)
                {
                    return true;
                }

                if (status != LevelStatus.Playing)
                {
                    return false;
                }

                ulong key = Key(worlds);
                if (_memo.TryGetValue(key, out bool known))
                {
                    return known;
                }

                bool result = false;
                foreach (Command move in StateSearch.Moves(worlds[0].Session, MoveOrder.ProgressFirst))
                {
                    if (Nodes >= _budget)
                    {
                        BudgetExceeded = true;
                        return false;
                    }

                    // Apply the tap in every world and split the worlds by what the player then sees.
                    var classes = new Dictionary<string, List<World>>(StringComparer.Ordinal);
                    var order = new List<string>();
                    foreach (World world in worlds)
                    {
                        LevelSession child = world.Session.Clone();
                        Nodes++;
                        CommandResult applied = child.Apply(move);
                        string observation = applied.Accepted
                            ? string.Join("\n", applied.Events.Select(EventText.Format))
                            : "refused:" + applied.Reason;
                        if (!classes.TryGetValue(observation, out List<World>? list))
                        {
                            list = new List<World>();
                            classes.Add(observation, list);
                            order.Add(observation);
                        }

                        list.Add(new World(world.Id, child));
                    }

                    bool allWin = true;
                    foreach (string observation in order)
                    {
                        if (observation.StartsWith("refused:", StringComparison.Ordinal) || !Winnable(classes[observation]))
                        {
                            allWin = false;
                            break;
                        }
                    }

                    if (BudgetExceeded)
                    {
                        return false;
                    }

                    if (allWin)
                    {
                        result = true;
                        break;
                    }
                }

                _memo[key] = result;
                return result;
            }

            /// <summary>An order-independent key over (world, state) pairs.</summary>
            private static ulong Key(List<World> worlds)
            {
                ulong key = (ulong)worlds.Count;
                foreach (World world in worlds)
                {
                    key += SplitMix64.Mix(world.Session.StateHash ^ ((ulong)(uint)world.Id * 0x9E3779B97F4A7C15UL));
                }

                return key;
            }
        }
    }
}
