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
    /// <summary>
    /// Player-information fairness of the hidden layers of an <see cref="BoardLook.Icons"/> board (FR-036 as amended on
    /// 2026-10-06, FR-039, FR-080; research R8b). On such a board a layered tile looks like any other, so the player sees
    /// the top layers, the tray and every event, and knows from the pods' counts how many hidden layers of each variant
    /// there are, but not where they lie. A world is one placement of those hidden layers (at most the level's layers per
    /// cell) that the player cannot rule out.
    /// <para>
    /// The check is sampled, not a proof. It draws <see cref="SampledWorlds"/> worlds from the level's seed: the same
    /// hidden layers (so the exact per-variant totals the pods show hold, and every hidden variant is an active one)
    /// spread over random target cells. A world that even a player who saw everything could not win is one the player
    /// rules out (every shipped level is winnable, FR-046) and is dropped, but when the draws (at most
    /// <see cref="WorldDraws"/>) find fewer than half of the wanted worlds that can be won, the level is unfair: whether it
    /// can be won then hangs on where the hidden layers lie. Then the
    /// <see cref="VisiblePlayer"/>, a solver that uses only what the player sees, must win the real level and every kept
    /// world. It plans on a model world of its own, drawn from what it has seen, and draws a new one whenever a revealed
    /// layer surprises it. Winning everywhere shows a strategy that needs no hidden knowledge on those worlds; a world
    /// that is never drawn may still defeat it.
    /// </para>
    /// Every budget counts nodes, never time, so the result is reproducible. All of them are fixed here, the whole
    /// check's (<see cref="CheckBudget"/>) included, so the generator and the validator, whatever their solve budgets,
    /// always reach the same result.
    /// </summary>
    public static class HiddenLayerFairness
    {
        /// <summary>The nodes the whole check may use: every applied command of its searches, replays and plays counts.</summary>
        public const int CheckBudget = 300_000;

        /// <summary>The worlds besides the real one that the visible player must win.</summary>
        public const int SampledWorlds = 12;

        /// <summary>The worlds drawn to find <see cref="SampledWorlds"/> that can be won.</summary>
        public const int WorldDraws = 2 * SampledWorlds;

        /// <summary>The node budget of the full-information search that decides whether a drawn world can be won.</summary>
        public const int WorldSolveBudget = 4_000;

        /// <summary>The node budget of each plan the visible player makes on its model world.</summary>
        public const int PlanBudget = 2_000;

        /// <summary>How many model worlds the visible player draws for one plan before it falls back to the first good tap.</summary>
        public const int PlanAttempts = 3;

        /// <param name="maxLayersBelow">The most hidden layers a cell may hold at this level (the player knows the rule, FR-036).</param>
        /// <param name="maxHiddenLayers">The most hidden layers the check takes on (more is <see cref="FairnessStatus.OverCap"/>).</param>
        public static FairnessResult Check(LevelDefinition definition, BasePicture picture, SessionOptions options, int maxLayersBelow, int maxHiddenLayers)
        {
            LevelSession real = LevelSession.Load(definition, picture, options);
            var hidden = HiddenLayers(real);
            if (hidden.Count == 0)
            {
                return new FairnessResult(FairnessStatus.Fair, 1, 0);
            }

            if (hidden.Count > maxHiddenLayers)
            {
                return new FairnessResult(FairnessStatus.OverCap, 0, 0) { PlayerInfoFair = false, Detail = $"{hidden.Count} hidden layers; the check takes at most {maxHiddenLayers}" };
            }

            int below = Math.Max(1, Math.Min(BoardBuilder.MaxLayersBelow, maxLayersBelow));
            var budget = new Budget(CheckBudget);
            int[] hosts = TargetCells(real);

            // The worlds the player cannot rule out: the real one and the drawn worlds that can be won.
            var worlds = new List<LevelSession> { real };
            int winnable = 0;
            var rng = new Xoshiro256StarStar(SplitMix64.Mix(definition.Seed ^ 0x41DD3E7A7E5UL));
            for (int draw = 0; draw < WorldDraws && winnable < SampledWorlds; draw++)
            {
                LevelDefinition placed = Place(definition, picture.Width, hosts, Capacities(hosts, below, null, null), Shuffled(hidden, ref rng), null, ref rng);
                LevelSession world = LevelSession.Load(placed, picture, options);
                SearchResult solved = StateSearch.Find(world, StateSearch.IsWon, MoveOrder.ProgressFirst, WorldSolveBudget);
                if (!budget.Spend(solved.NodesUsed))
                {
                    return new FairnessResult(FairnessStatus.Unknown, worlds.Count, budget.Used);
                }

                if (solved.Outcome == SearchOutcome.Found)
                {
                    winnable++;
                    worlds.Add(world);
                }
            }

            if (winnable * 2 < SampledWorlds)
            {
                return new FairnessResult(FairnessStatus.Unfair, worlds.Count, budget.Used)
                {
                    PlayerInfoFair = false,
                    Detail = $"only {winnable} of {WorldDraws} drawn placements of the {hidden.Count} hidden layers can be won at all",
                };
            }

            for (int w = 0; w < worlds.Count; w++)
            {
                var player = new VisiblePlayer(definition, picture, options, hidden, hosts, below, budget);
                bool won = player.Play(worlds[w]);
                if (budget.Exceeded)
                {
                    return new FairnessResult(FairnessStatus.Unknown, worlds.Count, budget.Used);
                }

                if (!won)
                {
                    return new FairnessResult(FairnessStatus.Unfair, worlds.Count, budget.Used)
                    {
                        PlayerInfoFair = false,
                        Detail = w == 0
                            ? "a player who sees only the board and the pods loses the real level"
                            : $"a player who sees only the board and the pods loses world {w} of {worlds.Count - 1}",
                    };
                }
            }

            return new FairnessResult(FairnessStatus.Fair, worlds.Count, budget.Used) { PlayerInfoFair = true };
        }

        /// <summary>
        /// The hidden layers as the player counts them at the start: for each variant, the pods' total minus the tiles
        /// that show it, in catalog order.
        /// </summary>
        internal static List<VariantId> HiddenLayers(LevelSession start)
        {
            VariantCatalog catalog = start.Options.Catalog;
            var count = new int[catalog.Count];
            foreach (string id in start.View.PodIds)
            {
                PodInfo pod = start.View.Pod(id);
                VariantId variant = pod.Variant ?? throw new InvalidOperationException("A mystery pod on an icons board is not covered.");
                count[catalog.IndexOf(variant)] += pod.Remaining;
            }

            for (int i = 0; i < start.View.Width * start.View.Height; i++)
            {
                CellInfo cell = start.View.Cell(i);
                if (cell.Kind == CellKind.Target)
                {
                    VariantId top = cell.Visible ?? throw new InvalidOperationException("A mystery tile on an icons board is not covered.");
                    count[catalog.IndexOf(top)]--;
                }
            }

            var hidden = new List<VariantId>();
            for (int v = 0; v < count.Length; v++)
            {
                for (int k = 0; k < count[v]; k++)
                {
                    hidden.Add(catalog.All[v].Id);
                }
            }

            return hidden;
        }

        /// <summary>The target cells at the start, by index: where hidden layers may lie.</summary>
        private static int[] TargetCells(LevelSession start)
        {
            var cells = new List<int>();
            for (int i = 0; i < start.View.Width * start.View.Height; i++)
            {
                if (start.View.Cell(i).Kind == CellKind.Target)
                {
                    cells.Add(i);
                }
            }

            return cells.ToArray();
        }

        /// <summary>How many more hidden layers each host may hold: none once open, else the rule less what it revealed.</summary>
        private static int[] Capacities(int[] hosts, int below, bool[]? opened, List<VariantId>[]? revealed)
        {
            var capacity = new int[hosts.Length];
            for (int h = 0; h < hosts.Length; h++)
            {
                int cell = hosts[h];
                capacity[h] = opened != null && opened[cell] ? 0 : below - (revealed?[cell]?.Count ?? 0);
            }

            return capacity;
        }

        private static List<VariantId> Shuffled(List<VariantId> layers, ref Xoshiro256StarStar rng)
        {
            var list = new List<VariantId>(layers);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }

        /// <summary>
        /// The level with its hidden layers replaced: under each host, first what it has revealed (in order), then the
        /// <paramref name="layers"/> placed on random free places. Keys, stones and holes stay as they are.
        /// </summary>
        private static LevelDefinition Place(LevelDefinition definition, int width, int[] hosts, int[] capacity, List<VariantId> layers, List<VariantId>[]? revealed, ref Xoshiro256StarStar rng)
        {
            var places = new List<int>();
            for (int h = 0; h < hosts.Length; h++)
            {
                for (int k = 0; k < capacity[h]; k++)
                {
                    places.Add(h);
                }
            }

            if (places.Count < layers.Count)
            {
                throw new InvalidOperationException("The hidden layers do not fit under the tiles that may hold them.");
            }

            var below = new Dictionary<int, List<VariantId>>();
            for (int i = 0; i < layers.Count; i++)
            {
                int pick = i + rng.NextInt(places.Count - i);
                (places[i], places[pick]) = (places[pick], places[i]);
                int cell = hosts[places[i]];
                if (!below.TryGetValue(cell, out List<VariantId>? stack))
                {
                    stack = new List<VariantId>();
                    below.Add(cell, stack);
                }

                stack.Add(layers[i]);
            }

            var byCell = new SortedDictionary<int, CellOverlay>();
            foreach (CellOverlay overlay in definition.Overlays)
            {
                byCell[overlay.Cell.ToIndex(width)] = overlay with { LayersBelow = Array.Empty<VariantId>() };
            }

            foreach (int cell in hosts)
            {
                var stack = new List<VariantId>();
                if (revealed?[cell] != null)
                {
                    stack.AddRange(revealed[cell]);
                }

                if (below.TryGetValue(cell, out List<VariantId>? placed))
                {
                    stack.AddRange(placed);
                }

                if (stack.Count == 0)
                {
                    continue;
                }

                CellPos pos = CellPos.FromIndex(cell, width);
                byCell[cell] = byCell.TryGetValue(cell, out CellOverlay? existing)
                    ? existing with { LayersBelow = stack }
                    : new CellOverlay(pos, stack, false, false, false, null);
            }

            var overlays = byCell.Values.Where(o => o.LayersBelow.Count > 0 || o.Mystery || o.Stone || o.Hole || o.KeyId != null).ToList();
            return definition with { Overlays = overlays };
        }

        private static string Observe(CommandResult result) =>
            result.Accepted ? string.Join("\n", result.Events.Select(EventText.Format)) : "refused:" + result.Reason;

        /// <summary>The nodes the whole check may use (every applied command counts).</summary>
        private sealed class Budget
        {
            private readonly int _limit;

            public Budget(int limit)
            {
                _limit = limit;
            }

            public int Used { get; private set; }

            public bool Exceeded => Used > _limit;

            public bool Spend(int nodes)
            {
                Used += nodes;
                return !Exceeded;
            }
        }

        /// <summary>
        /// A solver that uses only what the player sees. It keeps the taps it made and what each showed, and plans on a
        /// model world: the level with the layers it has seen revealed where they were, and the hidden layers it has not
        /// seen drawn at random under the tiles that may still hold them, from a seed made of what it has seen (so equal
        /// observations give equal choices in every world). It follows the model's winning line while the model foretells
        /// every event; when a revealed layer surprises it, it draws a new model and plans again. Without a winning line
        /// it taps the first pod of the search's move order, which reads only visible state.
        /// </summary>
        private sealed class VisiblePlayer
        {
            private readonly LevelDefinition _definition;
            private readonly BasePicture _picture;
            private readonly SessionOptions _options;
            private readonly List<VariantId> _hidden;
            private readonly int[] _hosts;
            private readonly int _below;
            private readonly Budget _budget;
            private readonly List<Command> _taps = new List<Command>();
            private readonly List<string> _seen = new List<string>();
            private List<VariantId>[] _revealed = Array.Empty<List<VariantId>>();
            private bool[] _opened = Array.Empty<bool>();

            public VisiblePlayer(LevelDefinition definition, BasePicture picture, SessionOptions options, List<VariantId> hidden, int[] hosts, int below, Budget budget)
            {
                _definition = definition;
                _picture = picture;
                _options = options;
                _hidden = hidden;
                _hosts = hosts;
                _below = below;
                _budget = budget;
            }

            /// <summary>Plays <paramref name="world"/> (a fresh session) to its end; true when it is won.</summary>
            public bool Play(LevelSession world)
            {
                int cells = world.View.Width * world.View.Height;
                _revealed = new List<VariantId>[cells];
                _opened = new bool[cells];
                LevelSession? model = null;
                IReadOnlyList<Command> plan = Array.Empty<Command>();
                int next = 0;
                while (world.Status == LevelStatus.Playing)
                {
                    if (model == null || next >= plan.Count)
                    {
                        (model, plan) = Plan(world);
                        next = 0;
                        if (_budget.Exceeded || model == null || plan.Count == 0)
                        {
                            return false;
                        }
                    }

                    Command tap = plan[next++];
                    CommandResult shown = world.Apply(tap);
                    if (!_budget.Spend(1))
                    {
                        return false;
                    }

                    string observation = Observe(shown);
                    if (!shown.Accepted)
                    {
                        // The model and the world look the same, so a tap the model allows the world allows too.
                        return false;
                    }

                    _taps.Add(tap);
                    _seen.Add(observation);
                    Learn(shown.Events);

                    CommandResult foretold = model.Apply(tap);
                    if (!_budget.Spend(1))
                    {
                        return false;
                    }

                    if (!string.Equals(Observe(foretold), observation, StringComparison.Ordinal))
                    {
                        model = null; // A surprise: the model is wrong about a hidden layer.
                    }
                }

                return world.Status == LevelStatus.Won;
            }

            private void Learn(IReadOnlyList<GameEvent> events)
            {
                foreach (GameEvent e in events)
                {
                    switch (e)
                    {
                        case LayerRevealed layer:
                            int cell = layer.Cell.ToIndex(Width);
                            (_revealed[cell] ??= new List<VariantId>()).Add(layer.NewTopVariant);
                            break;
                        case CellOpened opened:
                            _opened[opened.Cell.ToIndex(Width)] = true;
                            break;
                    }
                }
            }

            private int Width => _picture.Width;

            /// <summary>A model consistent with everything seen, brought to the current state, and its winning line from there.</summary>
            private (LevelSession? Model, IReadOnlyList<Command> Plan) Plan(LevelSession world)
            {
                // The hidden layers not yet seen: the start's, less those revealed since.
                var unseen = new List<VariantId>(_hidden);
                foreach (List<VariantId>? layers in _revealed)
                {
                    if (layers == null)
                    {
                        continue;
                    }

                    foreach (VariantId layer in layers)
                    {
                        unseen.Remove(layer);
                    }
                }

                int[] capacity = Capacities(_hosts, _below, _opened, _revealed);
                ulong seed = SplitMix64.Mix(_definition.Seed ^ Fnv(_seen));
                LevelSession? consistent = null;
                for (int attempt = 0; attempt < PlanAttempts; attempt++)
                {
                    var rng = new Xoshiro256StarStar(SplitMix64.Mix(seed + (ulong)attempt));
                    LevelDefinition placed = Place(_definition, Width, _hosts, capacity, Shuffled(unseen, ref rng), _revealed, ref rng);
                    LevelSession model = LevelSession.Load(placed, _picture, _options);
                    bool same = true;
                    for (int i = 0; i < _taps.Count && same; i++)
                    {
                        same = string.Equals(Observe(model.Apply(_taps[i])), _seen[i], StringComparison.Ordinal);
                    }

                    if (!_budget.Spend(_taps.Count) || !same)
                    {
                        // A model that does not replay what was seen is a modelling error; the budget ends the check.
                        continue;
                    }

                    consistent = model;
                    SearchResult line = StateSearch.Find(model.Clone(), StateSearch.IsWon, MoveOrder.ProgressFirst, PlanBudget);
                    if (!_budget.Spend(line.NodesUsed))
                    {
                        return (null, Array.Empty<Command>());
                    }

                    if (line.Outcome == SearchOutcome.Found && line.Path.Count > 0)
                    {
                        return (model, line.Path);
                    }
                }

                if (consistent == null)
                {
                    return (null, Array.Empty<Command>());
                }

                // No winning line on the models: the first tap of the move order (visible state only).
                IReadOnlyList<Command> moves = StateSearch.Moves(consistent, MoveOrder.ProgressFirst);
                return moves.Count == 0 ? (null, Array.Empty<Command>()) : (consistent, new[] { moves[0] });
            }

            private static ulong Fnv(List<string> lines)
            {
                ulong hash = 14695981039346656037UL;
                foreach (string line in lines)
                {
                    foreach (char c in line)
                    {
                        hash = unchecked((hash ^ c) * 1099511628211UL);
                    }

                    hash = unchecked((hash ^ '\n') * 1099511628211UL);
                }

                return hash;
            }
        }
    }
}
