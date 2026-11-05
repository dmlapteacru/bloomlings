using System;
using System.Collections.Generic;

namespace Bloomlings.Core.Rounds
{
    /// <summary>
    /// Validates a round level definition (spec 006 FR-023, data model RoundLevel): the port of the reference's
    /// <c>rules.cjs validate</c>, narrowed to what round levels use, plus the placement rules of 17 that a definition
    /// alone decides (the difficulty calendar and the mechanic introductions of §5 and §9). Proof checks (the saved
    /// solution, the random test, the bridge decision …) belong to the pipeline.
    ///
    /// A level may carry one documented exception; it is accepted only for a rule in <see cref="ExceptableRules"/>, and
    /// only that rule's error is waived (see <see cref="IsExcepted"/>). The structural rules the engine relies on (board,
    /// tray, accounting, groups, hidden pods, bridge sites) are never exceptable.
    /// </summary>
    public static class RoundLevelValidator
    {
        public const string BoardSize = "board 7–48 wide, 8–48 high";
        public const string LevelNumber = "level number ≥ 1";
        public const string Rows = "row length and characters";
        public const string TrayPartition = "tray is a partition of the pods";
        public const string Columns = "2–6 columns";
        public const string CapacityFive = "capacity 5";
        public const string PodCount = "pod count 1–60";
        public const string PodMaterial = "pod material is one of the 24 codes";
        public const string Accounting = "exact accounting: cells of each code equal the sum of counts";
        public const string Groups = "groups are pairs in different columns at equal start depth";
        public const string Hidden = "hidden pods are at most 6, never in a group";
        public const string HiddenMode = "hidden pods reveal in depth mode";
        public const string Entries = "entries are cells of the board";
        public const string BridgeSites = "bridge sites are walls with an orthogonal neighbour as anchor";
        public const string Mechanics = "mechanics match pods and hero";
        public const string UnknownMechanic = "mechanics are hidden, links, bridge, cells, latch, canal or shutters";

        /// <summary>17 §8 «Скрытые клетки» (2026-11-01): hidden cells are sorted, unique cell indices (reference "hidden cells").</summary>
        public const string HiddenCells = "hidden cells are sorted unique cells of the board";

        /// <summary>Each hidden cell is a material cell (reference "hidden cell not a colour cell").</summary>
        public const string HiddenCellMaterial = "a hidden cell is a material cell";

        /// <summary>Hidden pods and hidden cells are interchangeable unknowns, never on one level (reference "hidden cells with hidden pods").</summary>
        public const string HiddenCellsWithPods = "hidden cells never with hidden pods";

        /// <summary>
        /// 17 §8 «Засов и жёлудь-ключ» (2026-11-01; reference "latch"): each latch's column exists, its depth is
        /// <c>0 ≤ depth &lt; column length</c> (a pod stands behind it), and no two latches of one column share a depth.
        /// </summary>
        public const string Latch = "a latch stands in a column with a pod behind it, one per depth";

        /// <summary>Each key is an empty '.' cell of the board, not an entry, and keys are distinct (reference "latch key").</summary>
        public const string LatchKey = "a latch key is its own empty non-entry cell";

        /// <summary>
        /// The cheap necessary check (reference <c>latchKeysBlocked</c>, "latch key unreachable"): no key needs a material
        /// wholly held behind closed latches, and latches never wait for each other in a cycle.
        /// </summary>
        public const string LatchKeyUnreachable = "every latch key can be reached";

        /// <summary>At the start at most <c>max(1, columns − 2)</c> columns are blocked (reference "latches block too many columns").</summary>
        public const string LatchesBlockTooMany = "latches block at most max(1, columns − 2) columns at the start";

        /// <summary>
        /// 17 §8 «Drop Водный канал» (2026-11-05; reference "channel"): the spring is an empty '.' cell, not an entry or a
        /// latch key, and the channel has exactly two exits.
        /// </summary>
        public const string Channel = "the channel's spring is an empty non-entry cell and it has two exits";

        /// <summary>Each exit is an empty non-entry cell of its own, not the spring or a latch key (reference "channel exit").</summary>
        public const string ChannelExit = "a channel exit is its own empty non-entry cell";

        /// <summary>
        /// Each exit's route runs from the spring to the exit by orthogonal steps without a repeat, inside the board (reference
        /// "channel route").
        /// </summary>
        public const string ChannelRoute = "a channel route runs from the spring to its exit by orthogonal steps";

        /// <summary>Each exit's ride is whole milliseconds, 1 ms to 10 s (reference "channel ride").</summary>
        public const string ChannelRide = "a channel ride is whole milliseconds, 1 ms to 10 s";

        /// <summary>
        /// The cheap necessary check (reference <c>objectsBlocked</c>, "channel spring unreachable"): the spring can be
        /// reached at all.
        /// </summary>
        public const string ChannelSpringUnreachable = "the channel's spring can be reached";

        /// <summary>
        /// 17 §8 «Twig Деревянные створки» (2026-11-05; reference "shutter"): each shutter covers a full rectangle of
        /// material cells (sorted unique indices; never an entry or a hidden cell, never under another shutter), counts one
        /// material and at least 1.
        /// </summary>
        public const string Shutter = "a shutter covers a full rectangle of material cells, counts one material and at least 1";

        /// <summary>
        /// The cheap necessary check (reference <c>objectsBlocked</c>, "shutter count unreachable"): every count can be
        /// reached without the cells under the shutters still closed (no shutter waits for itself or in a cycle).
        /// </summary>
        public const string ShutterCountUnreachable = "every shutter's count can be reached";

        /// <summary>17 §5: every 15th level Super Hard, other multiples of 5 Hard, the rest Normal.</summary>
        public const string Calendar = "class matches the calendar";

        /// <summary>17 §9: an introduction level shows only its mechanic; others only mechanics already introduced.</summary>
        public const string Introductions = "introductions follow 17 §9";

        /// <summary>17 §8: on the introduction of hidden pods, none is a top pod at the start.</summary>
        public const string HiddenFirstRow = "hidden pods never in the first row on an introduction";

        /// <summary>17 §8: from the second bridge introduction exactly one bridge decides (a proof check of the pipeline).</summary>
        public const string BridgeDecides = "one deciding bridge from the second introduction";

        /// <summary>
        /// 17 §5 as amended on 2026-10-31: the calm test (Hard at most 50 %, Super Hard at most 25 %, a regular Normal at
        /// least 50 %; a proof check of the round tools' audit). The owner kept L30 above it.
        /// </summary>
        public const string CalmTest = "calm test (17 §5, amendment of 2026-10-31)";

        public const string UnknownException = "the exception names a rule that may be excepted";

        /// <summary>The rules an owner's exception may waive.</summary>
        public static IReadOnlyList<string> ExceptableRules { get; } = new[] { Calendar, Introductions, HiddenFirstRow, BridgeDecides, CalmTest };

        /// <summary>The mechanic introductions of 17 §9 the round engine plays: mechanic, first and second meeting.</summary>
        public static IReadOnlyList<(string Mechanic, int First, int Second)> IntroductionTable { get; } = new[]
        {
            ("hidden", 6, 8),
            ("links", 11, 13),
            ("bridge", 16, 18),
            ("cells", 51, 53),
            ("latch", 61, 63),
            ("canal", 101, 103),
            ("shutters", 121, 123),
        };

        /// <summary>
        /// The calendar class of a level number (17 §5): up to L100 every 15th level Super Hard, other multiples of 5 Hard;
        /// from L101 (the owner, 2026-11-05) every 10th level Super Hard, the other multiples of 5 Hard.
        /// </summary>
        public static RoundClass ClassOf(int number) =>
            (number > 100 ? number % 10 == 0 : number % 15 == 0) ? RoundClass.SuperHard : number % 5 == 0 ? RoundClass.Hard : RoundClass.Normal;

        /// <summary>Whether the level's exception waives <paramref name="rule"/>.</summary>
        public static bool IsExcepted(RoundLevel level, string rule) =>
            level.Exception != null && string.Equals(level.Exception.Rule, rule, StringComparison.Ordinal) && Contains(ExceptableRules, rule);

        /// <summary>All errors: the definition's (<see cref="ValidateDefinition"/>) and the placement rules of 17.</summary>
        public static List<string> Validate(RoundLevel level)
        {
            List<string> errors = ValidateDefinition(level);
            if (level.Exception != null && !Contains(ExceptableRules, level.Exception.Rule))
            {
                Add(errors, UnknownException);
            }

            void Rule(string rule, bool ok)
            {
                if (!ok && !IsExcepted(level, rule))
                {
                    Add(errors, rule);
                }
            }

            Rule(Calendar, level.Class == ClassOf(level.Number));

            int n = level.Number;
            string? intro = null;
            bool second = false;
            var opened = new List<string>();
            foreach ((string mechanic, int first, int secondAt) in IntroductionTable)
            {
                if (n == first || n == secondAt)
                {
                    intro = mechanic;
                    second = n == secondAt;
                }
                else if (n > secondAt)
                {
                    opened.Add(mechanic);
                }
            }

            bool introOk;
            if (intro != null)
            {
                introOk = level.Introduction != null && level.Introduction.Mechanic == intro && level.Introduction.Second == second
                    && Contains(level.Mechanics, intro);
                foreach (string m in level.Mechanics)
                {
                    introOk &= m == intro;
                }
            }
            else
            {
                introOk = level.Introduction == null;
                foreach (string m in level.Mechanics)
                {
                    introOk &= n > 5 && opened.Contains(m);
                }
            }

            Rule(Introductions, introOk);

            if (intro == "hidden")
            {
                bool clear = true;
                foreach (IReadOnlyList<int> column in level.Tray)
                {
                    if (column.Count > 0 && column[0] >= 0 && column[0] < level.Pods.Count && level.Pods[column[0]].Mystery != RoundMystery.None)
                    {
                        clear = false;
                    }
                }

                Rule(HiddenFirstRow, clear);
            }

            return errors;
        }

        /// <summary>
        /// The definition's errors, the reference's <c>validate</c> for round levels: board size, rows, tray partition,
        /// columns, capacity, pod counts, exact accounting, link pairs, hidden pods, hidden cells, entries, latches and their
        /// keys, the water channel, the wooden shutters, bridge sites and mechanics.
        /// </summary>
        public static List<string> ValidateDefinition(RoundLevel level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            var errors = new List<string>();
            int w = level.Width;
            int h = level.Height;
            int cellCount = w * h;
            if (level.Number < 1)
            {
                Add(errors, LevelNumber);
            }

            if (w < 7 || h < 8 || w > RoundLevel.MaxSide || h > RoundLevel.MaxSide)
            {
                Add(errors, BoardSize);
            }

            var cells = new int[RoundMaterial.Codes.Length];
            bool rowsOk = level.RowsBottomFirst.Count == h;
            var grid = new List<char>(Math.Max(0, cellCount));
            foreach (string row in level.RowsBottomFirst)
            {
                if (row == null || row.Length != w)
                {
                    rowsOk = false;
                    continue;
                }

                foreach (char c in row)
                {
                    int material = RoundMaterial.IndexOf(c);
                    if (material >= 0)
                    {
                        cells[material]++;
                    }
                    else if (c != '.' && c != '#')
                    {
                        rowsOk = false;
                    }

                    grid.Add(c);
                }
            }

            if (!rowsOk)
            {
                Add(errors, Rows);
            }

            int pods = level.Pods.Count;
            if (level.Tray.Count < 2 || level.Tray.Count > 6)
            {
                Add(errors, Columns);
            }

            if (level.Capacity != RoundRules.Capacity)
            {
                Add(errors, CapacityFive);
            }

            var seen = new int[pods];
            int listed = 0;
            bool partition = true;
            foreach (IReadOnlyList<int> column in level.Tray)
            {
                foreach (int p in column)
                {
                    listed++;
                    if (p < 0 || p >= pods || seen[p]++ > 0)
                    {
                        partition = false;
                    }
                }
            }

            if (!partition || listed != pods)
            {
                Add(errors, TrayPartition);
            }

            var counts = new int[RoundMaterial.Codes.Length];
            foreach (RoundPod pod in level.Pods)
            {
                int material = RoundMaterial.IndexOf(pod.Code);
                if (material < 0)
                {
                    Add(errors, PodMaterial);
                    continue;
                }

                if (pod.Count < 1 || pod.Count > 60)
                {
                    Add(errors, PodCount);
                }

                counts[material] += pod.Count;
            }

            for (int c = 0; c < cells.Length; c++)
            {
                if (cells[c] != counts[c])
                {
                    Add(errors, Accounting);
                    break;
                }
            }

            // Link groups, in the order their names first appear.
            var names = new List<string>();
            foreach (RoundPod pod in level.Pods)
            {
                if (pod.Group != null && !names.Contains(pod.Group))
                {
                    names.Add(pod.Group);
                }
            }

            foreach (string name in names)
            {
                var members = new List<int>();
                for (int p = 0; p < pods; p++)
                {
                    if (level.Pods[p].Group == name)
                    {
                        members.Add(p);
                    }
                }

                bool ok = members.Count == 2;
                if (ok && partition)
                {
                    (int column, int depth) a = Place(level, members[0]);
                    (int column, int depth) b = Place(level, members[1]);
                    ok = a.column != b.column && a.depth == b.depth;
                }

                if (!ok)
                {
                    Add(errors, Groups);
                }
            }

            int hidden = 0;
            foreach (RoundPod pod in level.Pods)
            {
                if (pod.Mystery == RoundMystery.None)
                {
                    continue;
                }

                hidden++;
                if (pod.Group != null)
                {
                    Add(errors, Hidden);
                }

                if (pod.Mystery != RoundMystery.Depth)
                {
                    Add(errors, HiddenMode);
                }
            }

            if (hidden > 6)
            {
                Add(errors, Hidden);
            }

            // Hidden cells (17 §8, 2026-11-01): sorted unique cell indices, each a material cell, never with hidden pods.
            IReadOnlyList<int> hiddenCells = level.HiddenCells;
            if (hiddenCells.Count > 0)
            {
                bool listOk = true;
                for (int k = 0; k < hiddenCells.Count; k++)
                {
                    int c = hiddenCells[k];
                    if (c < 0 || c >= cellCount || (k > 0 && c <= hiddenCells[k - 1]))
                    {
                        listOk = false;
                    }
                }

                if (!listOk)
                {
                    Add(errors, HiddenCells);
                }
                else if (rowsOk)
                {
                    foreach (int c in hiddenCells)
                    {
                        if (RoundMaterial.IndexOf(grid[c]) < 0)
                        {
                            Add(errors, HiddenCellMaterial);
                            break;
                        }
                    }
                }

                if (hidden > 0)
                {
                    Add(errors, HiddenCellsWithPods);
                }
            }

            foreach (int entry in level.Entries)
            {
                if (entry < 0 || entry >= cellCount)
                {
                    Add(errors, Entries);
                }
            }

            // Latch + acorn key (17 §8, 2026-11-01; reference validate): an existing column with a pod behind the latch,
            // one latch per depth of a column, each key its own empty non-entry cell; then the cheap reachability checks.
            IReadOnlyList<RoundLatch> latches = level.Latches;
            if (latches.Count > 0)
            {
                bool ok = true;
                for (int i = 0; i < latches.Count; i++)
                {
                    RoundLatch l = latches[i];
                    if (l.Column < 0 || l.Column >= level.Tray.Count || l.Depth < 0 || l.Depth >= level.Tray[l.Column].Count)
                    {
                        Add(errors, Latch);
                        ok = false;
                        continue;
                    }

                    bool sameDepth = false;
                    bool sameKey = false;
                    for (int j = 0; j < latches.Count; j++)
                    {
                        if (j != i)
                        {
                            sameDepth |= latches[j].Column == l.Column && latches[j].Depth == l.Depth;
                            sameKey |= latches[j].Key == l.Key;
                        }
                    }

                    if (sameDepth)
                    {
                        Add(errors, Latch);
                        ok = false;
                    }

                    if (l.Key < 0 || l.Key >= cellCount || !rowsOk || grid[l.Key] != '.' || Contains(level.Entries, l.Key) || sameKey)
                    {
                        Add(errors, LatchKey);
                        ok = false;
                    }
                }

                if (ok && partition && listed == pods && rowsOk)
                {
                    if (LatchKeysBlocked(level).Count > 0)
                    {
                        Add(errors, LatchKeyUnreachable);
                    }
                    else if (StartBlocked(level) > Math.Max(1, level.Tray.Count - 2))
                    {
                        Add(errors, LatchesBlockTooMany);
                    }
                }
            }

            // Water channel and wooden shutters (17 §8, 2026-11-05; reference validate), then the cheap reachability check.
            bool objectsOk = partition && listed == pods && rowsOk && !errors.Contains(Latch) && !errors.Contains(LatchKey);
            bool EmptyCell(int c)
            {
                if (c < 0 || c >= cellCount || !rowsOk || grid[c] != '.' || Contains(level.Entries, c))
                {
                    return false;
                }

                foreach (RoundLatch l in latches)
                {
                    if (l.Key == c)
                    {
                        return false;
                    }
                }

                return true;
            }

            RoundChannel? channel = level.Channel;
            if (channel != null)
            {
                if (!EmptyCell(channel.Spring) || channel.Exits.Count != 2)
                {
                    Add(errors, Channel);
                    objectsOk = false;
                }
                else
                {
                    var used = new List<int> { channel.Spring };
                    foreach (RoundChannelExit exit in channel.Exits)
                    {
                        if (!EmptyCell(exit.Cell) || used.Contains(exit.Cell))
                        {
                            Add(errors, ChannelExit);
                            objectsOk = false;
                            continue;
                        }

                        used.Add(exit.Cell);
                        IReadOnlyList<int> route = exit.Route;
                        bool routeOk = route.Count >= 2 && route[0] == channel.Spring && route[route.Count - 1] == exit.Cell;
                        var visited = new HashSet<int>();
                        for (int k = 0; routeOk && k < route.Count; k++)
                        {
                            routeOk = route[k] >= 0 && route[k] < cellCount && visited.Add(route[k]) && (k == 0 || Orthogonal(route[k], route[k - 1], w));
                        }

                        if (!routeOk)
                        {
                            Add(errors, ChannelRoute);
                            objectsOk = false;
                        }

                        if (exit.RideUs < 1000 || exit.RideUs > 10_000_000 || exit.RideUs % 1000 != 0)
                        {
                            Add(errors, ChannelRide);
                            objectsOk = false;
                        }
                    }
                }
            }

            IReadOnlyList<RoundShutter> shutters = level.Shutters;
            if (shutters.Count > 0)
            {
                var under = new bool[Math.Max(0, cellCount)];
                foreach (RoundShutter shutter in shutters)
                {
                    IReadOnlyList<int> cs = shutter.Cells;
                    bool ok = cs.Count > 0;
                    for (int k = 0; ok && k < cs.Count; k++)
                    {
                        ok = cs[k] >= 0 && cs[k] < cellCount && (k == 0 || cs[k] > cs[k - 1]);
                    }

                    if (!ok)
                    {
                        Add(errors, Shutter);
                        objectsOk = false;
                        continue;
                    }

                    int x0 = int.MaxValue, x1 = int.MinValue, y0 = int.MaxValue, y1 = int.MinValue;
                    foreach (int c in cs)
                    {
                        x0 = Math.Min(x0, c % w);
                        x1 = Math.Max(x1, c % w);
                        y0 = Math.Min(y0, c / w);
                        y1 = Math.Max(y1, c / w);
                    }

                    bool shape = (x1 - x0 + 1) * (y1 - y0 + 1) == cs.Count;
                    foreach (int c in cs)
                    {
                        shape &= rowsOk && RoundMaterial.IndexOf(grid[c]) >= 0 && !Contains(level.Entries, c) && !Contains(hiddenCells, c) && !under[c];
                    }

                    foreach (int c in cs)
                    {
                        under[c] = true;
                    }

                    if (!shape || RoundMaterial.IndexOf(shutter.Code) < 0 || shutter.Count < 1)
                    {
                        Add(errors, Shutter);
                        objectsOk = false;
                    }
                }
            }

            if (objectsOk && (channel != null || shutters.Count > 0))
            {
                Blocked blocked = ObjectsBlocked(level);
                if (channel != null && !blocked.Spring)
                {
                    Add(errors, ChannelSpringUnreachable);
                }

                if (blocked.Shutters.Count > 0)
                {
                    Add(errors, ShutterCountUnreachable);
                }
            }

            if (level.Bridge != null)
            {
                foreach (RoundBridgeChoice site in level.Bridge)
                {
                    bool inside = site.Cell >= 0 && site.Cell < cellCount && site.Anchor >= 0 && site.Anchor < cellCount;
                    if (!inside || !rowsOk || grid[site.Cell] != '#' || !Orthogonal(site.Cell, site.Anchor, w))
                    {
                        Add(errors, BridgeSites);
                    }
                }
            }

            foreach (string m in level.Mechanics)
            {
                if (m != "hidden" && m != "links" && m != "bridge" && m != "cells" && m != "latch" && m != "canal" && m != "shutters")
                {
                    Add(errors, UnknownMechanic);
                }
            }

            bool mechanicsOk = Contains(level.Mechanics, "hidden") == (hidden > 0)
                && Contains(level.Mechanics, "links") == (names.Count > 0)
                && Contains(level.Mechanics, "bridge") == (level.Bridge != null && level.Bridge.Count > 0)
                && Contains(level.Mechanics, "cells") == (hiddenCells.Count > 0)
                && Contains(level.Mechanics, "latch") == (latches.Count > 0)
                && Contains(level.Mechanics, "canal") == (channel != null)
                && Contains(level.Mechanics, "shutters") == (shutters.Count > 0);
            if (!mechanicsOk)
            {
                Add(errors, Mechanics);
            }

            return errors;
        }

        /// <summary>
        /// Whether <paramref name="anchor"/> is an orthogonal neighbour of <paramref name="cell"/> on a board
        /// <paramref name="w"/> wide (17 §8 as amended on 2026-10-28; reference <c>rules.cjs validate</c>): the row below
        /// or above (<c>|cell − anchor| == w</c>), or the cell beside it in the same row (<c>|cell − anchor| == 1</c>).
        /// </summary>
        public static bool Orthogonal(int cell, int anchor, int w)
        {
            int d = Math.Abs(cell - anchor);
            return d == w || (d == 1 && cell / w == anchor / w);
        }

        /// <summary>
        /// The latches whose key can never be reached (reference <c>latchKeysBlocked</c>, a necessary condition of 17 §8):
        /// starting with every latch closed, a material all of whose pods stand at or behind their column's first closed latch
        /// is blocked and its cells are walls; every other material cell counts as clearable, open cells and bridge sites as
        /// passable; each closed latch whose key that flood from the entries reaches opens, until nothing changes. Empty: the
        /// keys may all be reached (the solver decides the rest). The level's rows and tray must be sound.
        /// </summary>
        public static List<int> LatchKeysBlocked(RoundLevel level) => ObjectsBlocked(level).Latches;

        /// <summary>What the cheap fixed point of <see cref="ObjectsBlocked"/> never opens.</summary>
        public sealed class Blocked
        {
            internal Blocked(List<int> latches, List<int> shutters, bool spring)
            {
                Latches = latches;
                Shutters = shutters;
                Spring = spring;
            }

            /// <summary>The latches whose key is never reached.</summary>
            public List<int> Latches { get; }

            /// <summary>The shutters whose count is never reached.</summary>
            public List<int> Shutters { get; }

            /// <summary>Whether the channel's spring is reached (false without a channel).</summary>
            public bool Spring { get; }
        }

        /// <summary>
        /// The same cheap fixed point for every board object (reference <c>objectsBlocked</c>, 17 §8, 2026-11-05): a cell
        /// under a closed shutter is a wall; once the spring is flooded both exits flood too (either may be chosen); a closed
        /// shutter opens when the flooded cells of its material are at least its count. Without shutters and a channel it is
        /// exactly <see cref="LatchKeysBlocked"/>. The level's rows and tray must be sound.
        /// </summary>
        public static Blocked ObjectsBlocked(RoundLevel level)
        {
            IReadOnlyList<RoundLatch> latches = level.Latches;
            IReadOnlyList<RoundShutter> shutters = level.Shutters;
            RoundChannel? channel = level.Channel;
            int w = level.Width;
            int n = level.CellCount;
            var grid = new char[n];
            int k = 0;
            foreach (string row in level.RowsBottomFirst)
            {
                foreach (char c in row)
                {
                    if (k < n)
                    {
                        grid[k++] = c;
                    }
                }
            }

            var sites = new bool[n];
            if (level.Bridge != null)
            {
                foreach (RoundBridgeChoice site in level.Bridge)
                {
                    if (site.Cell >= 0 && site.Cell < n)
                    {
                        sites[site.Cell] = true;
                    }
                }
            }

            var cover = new int[n];
            Array.Fill(cover, -1);
            for (int s = 0; s < shutters.Count; s++)
            {
                foreach (int c in shutters[s].Cells)
                {
                    if (c >= 0 && c < n)
                    {
                        cover[c] = s;
                    }
                }
            }

            var open = new bool[latches.Count];
            var opened = new bool[shutters.Count];
            bool spring = false;
            var free = new bool[RoundMaterial.Codes.Length];
            var seen = new bool[n];
            var stack = new List<int>();
            while (true)
            {
                Array.Clear(free, 0, free.Length);
                for (int c = 0; c < level.Tray.Count; c++)
                {
                    int first = int.MaxValue;
                    for (int i = 0; i < latches.Count; i++)
                    {
                        if (!open[i] && latches[i].Column == c)
                        {
                            first = Math.Min(first, latches[i].Depth);
                        }
                    }

                    IReadOnlyList<int> column = level.Tray[c];
                    for (int d = 0; d < column.Count && d < first; d++)
                    {
                        int material = RoundMaterial.IndexOf(level.Pods[column[d]].Code);
                        if (material >= 0)
                        {
                            free[material] = true;
                        }
                    }
                }

                bool Pass(int i)
                {
                    if (cover[i] >= 0 && !opened[cover[i]])
                    {
                        return false;
                    }

                    char c = grid[i];
                    if (c == '.' || sites[i])
                    {
                        return true;
                    }

                    int material = RoundMaterial.IndexOf(c);
                    return material >= 0 && free[material];
                }

                Array.Clear(seen, 0, n);
                stack.Clear();
                var sources = new List<int>(level.Entries);
                if (channel != null && spring)
                {
                    foreach (RoundChannelExit exit in channel.Exits)
                    {
                        sources.Add(exit.Cell);
                    }
                }

                foreach (int e in sources)
                {
                    if (e >= 0 && e < n && !seen[e] && Pass(e))
                    {
                        seen[e] = true;
                        stack.Add(e);
                    }
                }

                while (stack.Count > 0)
                {
                    int i = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                    int x = i % w;
                    int[] next = { i - w, i + w, x > 0 ? i - 1 : -1, x < w - 1 ? i + 1 : -1 };
                    foreach (int j in next)
                    {
                        if (j >= 0 && j < n && !seen[j] && Pass(j))
                        {
                            seen[j] = true;
                            stack.Add(j);
                        }
                    }
                }

                bool changed = false;
                for (int i = 0; i < latches.Count; i++)
                {
                    if (!open[i] && latches[i].Key >= 0 && latches[i].Key < n && seen[latches[i].Key])
                    {
                        open[i] = true;
                        changed = true;
                    }
                }

                if (channel != null && !spring && channel.Spring >= 0 && channel.Spring < n && seen[channel.Spring])
                {
                    spring = true;
                    changed = true;
                }

                // A flooded cell is never under a closed shutter, so it counts as clearable work of its material.
                for (int s = 0; s < shutters.Count; s++)
                {
                    if (opened[s])
                    {
                        continue;
                    }

                    int got = 0;
                    for (int i = 0; i < n; i++)
                    {
                        if (seen[i] && grid[i] == shutters[s].Code)
                        {
                            got++;
                        }
                    }

                    if (got >= shutters[s].Count)
                    {
                        opened[s] = true;
                        changed = true;
                    }
                }

                if (!changed)
                {
                    break;
                }
            }

            var closedLatches = new List<int>();
            for (int i = 0; i < latches.Count; i++)
            {
                if (!open[i])
                {
                    closedLatches.Add(i);
                }
            }

            var closedShutters = new List<int>();
            for (int s = 0; s < shutters.Count; s++)
            {
                if (!opened[s])
                {
                    closedShutters.Add(s);
                }
            }

            return new Blocked(closedLatches, closedShutters, spring);
        }

        /// <summary>The columns blocked at the start (reference <c>startBlocked</c>): a latch at depth 0 whose key the start board does not reach.</summary>
        public static int StartBlocked(RoundLevel level)
        {
            var state = new RoundState(level, RoundRules.Capacity);
            IReadOnlyList<int> distance = state.Reach().Distance;
            var columns = new List<int>();
            foreach (RoundLatch l in level.Latches)
            {
                if (l.Depth == 0 && !(distance[l.Key] > 0) && !columns.Contains(l.Column))
                {
                    columns.Add(l.Column);
                }
            }

            return columns.Count;
        }

        private static bool Contains(IReadOnlyList<int> list, int value)
        {
            foreach (int item in list)
            {
                if (item == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static (int Column, int Depth) Place(RoundLevel level, int pod)
        {
            for (int c = 0; c < level.Tray.Count; c++)
            {
                IReadOnlyList<int> column = level.Tray[c];
                for (int k = 0; k < column.Count; k++)
                {
                    if (column[k] == pod)
                    {
                        return (c, k);
                    }
                }
            }

            return (-1, -1);
        }

        private static bool Contains(IReadOnlyList<string> list, string value)
        {
            foreach (string item in list)
            {
                if (string.Equals(item, value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Add(List<string> errors, string error)
        {
            if (!errors.Contains(error))
            {
                errors.Add(error);
            }
        }
    }
}
