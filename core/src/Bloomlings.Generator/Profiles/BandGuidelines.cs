using System;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Generator.Profiles
{
    /// <summary>One row of the spec's Level Band Guidelines.</summary>
    /// <param name="Work">Tile-layers of a Normal level.</param>
    /// <param name="HardWork">Tile-layers of a Hard or Super Hard level, when the band sets its own range.</param>
    public sealed record GuidelineBand(
        string Name,
        IntRange Levels,
        IntRange BoardWidth,
        IntRange BoardHeight,
        IntRange Pods,
        IntRange Work,
        IntRange? HardWork,
        IntRange DurationSeconds);

    /// <summary>
    /// The spec's Level Band Guidelines and the unlock roadmap rows that change them within a band, in one place for
    /// the generator and the validator (FR-004, FR-060, FR-079). The generation profiles stay the outer bounds; a level
    /// must satisfy both. "300+" style open maxima are unbounded.
    /// <list type="bullet">
    /// <item>Variants: L1 has 2; L2–10 2–3; L11–25 3–4; L26–31 4 and L32–50 4–5 (5-variant levels routine from L32);
    /// L51–100 5, with 6 for Hard and Super Hard from L70; L101–299 5, with 6 for Hard and Super Hard; L300–500 5–6
    /// (6 normal in advanced profiles from L300); L501+ 4–6, with 7 a rare exception (a warning, never generated).</item>
    /// <item>Hidden layers under a tile: none before L28 (Layered Tile), at most 1 before L125, at most 2 after.</item>
    /// <item>Pods: at least <see cref="MinPodSize"/> tiles, the "small" class of the pod size table.</item>
    /// <item>From L20 all four families are regular: any <see cref="FamilyWindow"/> consecutive levels use all four.</item>
    /// </list>
    /// </summary>
    public static class BandGuidelines
    {
        public const int MinPodSize = 5;

        /// <summary>From this level, every window of <see cref="FamilyWindow"/> consecutive levels uses all four families.</summary>
        public const int AllFamiliesFrom = 20;

        public const int FamilyWindow = 5;

        private static readonly IntRange Open = new IntRange(0, int.MaxValue);

        private static readonly GuidelineBand[] Bands =
        {
            new GuidelineBand("onboarding", new IntRange(1, 10), new IntRange(7, 8), new IntRange(8, 8), new IntRange(3, 7), new IntRange(30, 60), null, new IntRange(20, 45)),
            new GuidelineBand("early", new IntRange(11, 25), new IntRange(9, 10), new IntRange(10, 10), new IntRange(6, 12), new IntRange(50, 100), null, new IntRange(45, 120)),
            new GuidelineBand("early-mid", new IntRange(26, 50), new IntRange(10, 12), new IntRange(10, 12), new IntRange(10, 20), new IntRange(90, 180), null, new IntRange(45, 120)),
            new GuidelineBand("core-completion", new IntRange(51, 100), new IntRange(10, 14), new IntRange(12, 14), new IntRange(10, 24), new IntRange(90, 180), new IntRange(150, 300), new IntRange(45, 240)),
            new GuidelineBand("combination", new IntRange(101, 500), new IntRange(7, 14), new IntRange(8, 16), new IntRange(15, 30), new IntRange(150, Open.Max), null, new IntRange(60, 240)),
            new GuidelineBand("long-run", new IntRange(501, int.MaxValue), new IntRange(7, 14), new IntRange(8, 16), new IntRange(10, Open.Max), new IntRange(90, Open.Max), null, new IntRange(45, 240)),
        };

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
            GuidelineBand band = BandOf(level);
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
    }
}
