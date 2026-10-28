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
        public const string PodMaterial = "pod material is one of the 16 codes";
        public const string Accounting = "exact accounting: cells of each code equal the sum of counts";
        public const string Groups = "groups are pairs in different columns at equal start depth";
        public const string Hidden = "hidden pods are at most 6, never in a group";
        public const string HiddenMode = "hidden pods reveal in depth mode";
        public const string Entries = "entries are cells of the board";
        public const string BridgeSites = "bridge sites are walls with the anchor one row below";
        public const string Mechanics = "mechanics match pods and hero";
        public const string UnknownMechanic = "mechanics are hidden, links or bridge";

        /// <summary>17 §5: every 15th level Super Hard, other multiples of 5 Hard, the rest Normal.</summary>
        public const string Calendar = "class matches the calendar";

        /// <summary>17 §9: an introduction level shows only its mechanic; others only mechanics already introduced.</summary>
        public const string Introductions = "introductions follow 17 §9";

        /// <summary>17 §8: on the introduction of hidden pods, none is a top pod at the start.</summary>
        public const string HiddenFirstRow = "hidden pods never in the first row on an introduction";

        /// <summary>17 §8: from the second bridge introduction exactly one bridge decides (a proof check of the pipeline).</summary>
        public const string BridgeDecides = "one deciding bridge from the second introduction";

        public const string UnknownException = "the exception names a rule that may be excepted";

        /// <summary>The rules an owner's exception may waive.</summary>
        public static IReadOnlyList<string> ExceptableRules { get; } = new[] { Calendar, Introductions, HiddenFirstRow, BridgeDecides };

        /// <summary>The mechanic introductions of 17 §9 the round engine plays: mechanic, first and second meeting.</summary>
        public static IReadOnlyList<(string Mechanic, int First, int Second)> IntroductionTable { get; } = new[]
        {
            ("hidden", 6, 8),
            ("links", 11, 13),
            ("bridge", 16, 18),
        };

        /// <summary>The calendar class of a level number (17 §5).</summary>
        public static RoundClass ClassOf(int number) =>
            number % 15 == 0 ? RoundClass.SuperHard : number % 5 == 0 ? RoundClass.Hard : RoundClass.Normal;

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
        /// columns, capacity, pod counts, exact accounting, link pairs, hidden pods, entries, bridge sites and mechanics.
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

            foreach (int entry in level.Entries)
            {
                if (entry < 0 || entry >= cellCount)
                {
                    Add(errors, Entries);
                }
            }

            if (level.Bridge != null)
            {
                foreach (RoundBridgeChoice site in level.Bridge)
                {
                    bool inside = site.Cell >= 0 && site.Cell < cellCount && site.Anchor >= 0 && site.Anchor < cellCount;
                    if (!inside || !rowsOk || grid[site.Cell] != '#' || site.Anchor != site.Cell - w)
                    {
                        Add(errors, BridgeSites);
                    }
                }
            }

            foreach (string m in level.Mechanics)
            {
                if (m != "hidden" && m != "links" && m != "bridge")
                {
                    Add(errors, UnknownMechanic);
                }
            }

            bool mechanicsOk = Contains(level.Mechanics, "hidden") == (hidden > 0)
                && Contains(level.Mechanics, "links") == (names.Count > 0)
                && Contains(level.Mechanics, "bridge") == (level.Bridge != null && level.Bridge.Count > 0);
            if (!mechanicsOk)
            {
                Add(errors, Mechanics);
            }

            return errors;
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
