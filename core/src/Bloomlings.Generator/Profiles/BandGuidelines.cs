using System;
using System.Globalization;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Generator.Profiles
{
    /// <summary>
    /// The boards a level may use (FR-008 as amended on 2026-10-06): widths, heights and a range of cells
    /// (width × height), all three.
    /// </summary>
    public sealed record BoardRule(IntRange Width, IntRange Height, IntRange Cells)
    {
        public bool Allows(int width, int height) => Width.Contains(width) && Height.Contains(height) && Cells.Contains(width * height);

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0}–{1} × {2}–{3} with {4}–{5} cells", Width.Min, Width.Max, Height.Min, Height.Max, Cells.Min, Cells.Max);
    }

    /// <summary>One row of the spec's Level Band Guidelines.</summary>
    /// <param name="Work">Tile-layers of a Normal level.</param>
    /// <param name="HardWork">Tile-layers of a Hard or Super Hard level, when the band sets its own range.</param>
    public sealed record GuidelineBand(
        string Name,
        IntRange Levels,
        BoardRule Board,
        IntRange Pods,
        IntRange Work,
        IntRange? HardWork,
        IntRange DurationSeconds);

    /// <summary>
    /// The spec's Level Band Guidelines and the unlock roadmap rows that change them within a band, in one place for
    /// the generator and the validator (FR-004, FR-060, FR-079). The generation profiles stay the outer bounds; a level
    /// must satisfy both.
    /// <list type="bullet">
    /// <item>Variants: L1 has 2; L2–10 2–3; L11–25 3–4; L26–31 4 and L32–50 4–5 (5-variant levels routine from L32);
    /// L51–100 5, with 6 for Hard and Super Hard from L70; L101–299 5, with 6 for Hard and Super Hard; L300–500 5–6
    /// (6 normal in advanced profiles from L300); L501+ 4–6, with 7 a rare exception (a warning, never generated).</item>
    /// <item>Hidden layers under a tile: none before L28 (Layered Tile), at most 1 before L125, at most 2 after.</item>
    /// <item>Pods: at least <see cref="MinPodSize"/> tiles, the "small" class of the pod size table.</item>
    /// <item>From L20 all four families are regular: any <see cref="FamilyWindow"/> consecutive levels use all four.</item>
    /// <item>Boards (FR-008 as amended on 2026-10-06, the owner): 11–12 × 12 for the curated Levels 1–10
    /// (<see cref="MinBoardWidth"/>, <see cref="MinBoardHeight"/>); from L11 regular boards of
    /// <see cref="RegularMinCells"/>–<see cref="BoardLooks.MaxPeekCells"/> cells (14 × 16 up to 16 × 18); and from
    /// L<see cref="BigLevelsFrom"/> every milestone level (every <see cref="BigLevelEvery"/>th, a Normal level) is a big
    /// level (<see cref="IsBigLevel"/>) with <see cref="BigMinCells"/>–<see cref="BigMaxCells"/> cells, at most 22 × 28.
    /// Work follows the boards: 75–95% occupancy (FR-008) plus the hidden layers from L28.</item>
    /// </list>
    /// </summary>
    public static class BandGuidelines
    {
        public const int MinPodSize = 5;

        /// <summary>The smallest shipped board, the curated Levels 1–10 (FR-008, the owner's amendment of 2026-10-05; it was 7×8).</summary>
        public const int MinBoardWidth = 11;

        public const int MinBoardHeight = 12;

        /// <summary>The fewest cells of a board from L11 (14 × 16; FR-008 as amended on 2026-10-06).</summary>
        public const int RegularMinCells = 224;

        /// <summary>The fewest cells of a big level's board: one more than the largest regular (peek) board.</summary>
        public const int BigMinCells = BoardLooks.MaxPeekCells + 1;

        /// <summary>The most cells of any board: 22 × 28, for big levels only.</summary>
        public const int BigMaxCells = 616;

        /// <summary>The first big level (the owner's "rare" big levels in late bands, 2026-10-06).</summary>
        public const int BigLevelsFrom = 525;

        /// <summary>Big levels are the milestone levels from <see cref="BigLevelsFrom"/>: every 25th level.</summary>
        public const int BigLevelEvery = 25;

        /// <summary>From this level, every window of <see cref="FamilyWindow"/> consecutive levels uses all four families.</summary>
        public const int AllFamiliesFrom = 20;

        public const int FamilyWindow = 5;

        /// <summary>The boards of the curated Levels 1–10 (FR-008 as amended on 2026-10-05).</summary>
        public static readonly BoardRule OnboardingBoard = new BoardRule(new IntRange(MinBoardWidth, 12), new IntRange(MinBoardHeight, 12), new IntRange(MinBoardWidth * MinBoardHeight, 144));

        /// <summary>The regular boards from L11: 224–288 cells, 14 × 16 up to 16 × 18; they show the layer peek.</summary>
        public static readonly BoardRule RegularBoard = new BoardRule(new IntRange(14, 16), new IntRange(16, 18), new IntRange(RegularMinCells, BoardLooks.MaxPeekCells));

        /// <summary>The big levels' boards: 289–616 cells, at most 22 × 28; they show icons only.</summary>
        public static readonly BoardRule BigBoard = new BoardRule(new IntRange(14, 22), new IntRange(16, 28), new IntRange(BigMinCells, BigMaxCells));

        /// <summary>
        /// The big levels' row (<see cref="IsBigLevel"/>): pods, work and durations for their boards. Work is 75–95% of
        /// 289–616 cells less the stones and specials, plus the hidden layers (fewer on an icons board, at most
        /// <see cref="MaxHiddenLayersOnIcons"/>).
        /// </summary>
        public static readonly GuidelineBand BigLevel = new GuidelineBand("big", new IntRange(BigLevelsFrom, int.MaxValue), BigBoard, new IntRange(24, 56), new IntRange(200, 650), null, new IntRange(240, 600));

        /// <summary>
        /// The most hidden layers of an <see cref="BoardLook.Icons"/> board, where none shows: the generator keeps under
        /// it, and the hidden-layer fairness check (<c>FairnessChecker</c>, research R8b) refuses more.
        /// </summary>
        public const int MaxHiddenLayersOnIcons = 72;

        private static readonly GuidelineBand[] Bands =
        {
            // Levels 1–10 are curated on 11–12 × 12 (the owner, 2026-10-05). From L11 every regular board has 224–288
            // cells (the owner, 2026-10-06; they were 12×12 to 14×16), and pods, work and durations follow the boards.
            new GuidelineBand("onboarding", new IntRange(1, 10), OnboardingBoard, new IntRange(3, 8), new IntRange(95, 140), null, new IntRange(45, 90)),
            new GuidelineBand("early", new IntRange(11, 25), RegularBoard, new IntRange(10, 22), new IntRange(150, 275), null, new IntRange(90, 240)),
            new GuidelineBand("early-mid", new IntRange(26, 50), RegularBoard, new IntRange(14, 30), new IntRange(150, 330), null, new IntRange(90, 240)),
            new GuidelineBand("core-completion", new IntRange(51, 100), RegularBoard, new IntRange(15, 36), new IntRange(150, 330), null, new IntRange(120, 300)),
            new GuidelineBand("combination", new IntRange(101, 500), RegularBoard, new IntRange(16, 40), new IntRange(150, 360), null, new IntRange(120, 360)),
            new GuidelineBand("long-run", new IntRange(501, int.MaxValue), RegularBoard, new IntRange(12, 40), new IntRange(150, 360), null, new IntRange(120, 360)),
        };

        /// <summary>The band row of the level number (big levels included; see <see cref="For"/>).</summary>
        public static GuidelineBand BandOf(int level)
        {
            foreach (GuidelineBand band in Bands)
            {
                if (band.Levels.Contains(level))
                {
                    return band;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(level), level, "Levels start at 1.");
        }

        /// <summary>
        /// Whether <paramref name="level"/> is a big level: from L<see cref="BigLevelsFrom"/>, every milestone level
        /// (level % <see cref="BigLevelEvery"/> == 0), which the difficulty schedule never makes Super Hard (FR-059) nor,
        /// being big, Hard (<see cref="DifficultySchedule"/>). Its board has 289–616 cells and the icons look.
        /// </summary>
        public static bool IsBigLevel(int level) => level >= BigLevelsFrom && level % BigLevelEvery == 0;

        /// <summary>The guidelines that apply to the level: the big levels' row for a big level, else its band's row.</summary>
        public static GuidelineBand For(int level) => IsBigLevel(level) ? BigLevel : BandOf(level);

        /// <summary>The boards the level may use (FR-008 as amended on 2026-10-06).</summary>
        public static BoardRule Board(int level) => For(level).Board;

        /// <summary>The allowed number of active variants (see the class summary).</summary>
        public static IntRange Variants(int level, DifficultyClass difficulty)
        {
            bool hard = difficulty != DifficultyClass.Normal;
            if (level <= 1)
            {
                return new IntRange(2, 2);
            }

            if (level <= 10)
            {
                return new IntRange(2, 3);
            }

            if (level <= 25)
            {
                return new IntRange(3, 4);
            }

            if (level < 32)
            {
                return new IntRange(4, 4);
            }

            if (level <= 50)
            {
                return new IntRange(4, 5);
            }

            if (level <= 100)
            {
                return new IntRange(5, hard && level >= 70 ? 6 : 5);
            }

            if (level < 300)
            {
                return new IntRange(5, hard ? 6 : 5);
            }

            if (level <= 500)
            {
                return new IntRange(5, 6);
            }

            return new IntRange(4, 6);
        }

        /// <summary>Tile-layers by class: Hard and Super Hard use the band's Hard range where it has one.</summary>
        public static IntRange Work(int level, DifficultyClass difficulty)
        {
            GuidelineBand band = For(level);
            return difficulty != DifficultyClass.Normal && band.HardWork != null ? band.HardWork : band.Work;
        }

        /// <summary>The most hidden layers under a tile's top layer (FR-036, roadmap L28 and L125).</summary>
        public static int MaxLayersBelow(int level) => level < 28 ? 0 : level < 125 ? 1 : 2;

        /// <summary>The overlap of two ranges, or null when they do not meet.</summary>
        public static IntRange? Intersect(IntRange a, IntRange b)
        {
            int min = Math.Max(a.Min, b.Min);
            int max = Math.Min(a.Max, b.Max);
            return min <= max ? new IntRange(min, max) : null;
        }

        /// <summary>Peak occupied slots of a buffer-pressure target (spec table).</summary>
        public static IntRange PeakSlots(BufferPressure pressure) => pressure switch
        {
            BufferPressure.Relaxed => new IntRange(1, 2),
            BufferPressure.Normal => new IntRange(2, 3),
            BufferPressure.Tense => new IntRange(3, 4),
            _ => new IntRange(4, 5),
        };

        /// <summary>
        /// A big level's buffer-pressure target, lower than its band's (generator slack for the hidden layers of its
        /// icons board, research R8b): a Normal big level keeps 1–3 slots busy at its peak, and one forced to Hard
        /// (<c>generate --class hard</c>; the schedule keeps big levels Normal) at most tense (3–4), never critical.
        /// </summary>
        public static IntRange BigLevelPeakSlots(DifficultyClass difficulty) =>
            difficulty == DifficultyClass.Normal ? new IntRange(1, 3) : PeakSlots(BufferPressure.Tense);
    }
}
