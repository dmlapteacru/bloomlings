using System.Collections.Generic;
using Bloomlings.Core.Variants;

namespace Bloomlings.Generator.Profiles
{
    public sealed record IntRange(int Min, int Max)
    {
        public bool Contains(int value) => value >= Min && value <= Max;

        public override string ToString() => $"{Min}–{Max}";
    }

    public enum BufferPressure
    {
        /// <summary>Peak 1–2 occupied slots.</summary>
        Relaxed,

        /// <summary>Peak 2–3.</summary>
        Normal,

        /// <summary>Peak 3–4.</summary>
        Tense,

        /// <summary>Peak 4–5.</summary>
        Critical,
    }

    /// <summary>Structure targets for picture selection (R7): nesting depth and background share.</summary>
    public sealed record StructureTargets(IntRange NestingDepth, IntRange BackgroundSharePermille);

    /// <summary>How the source design changes for Hard and Super Hard levels (FR-059).</summary>
    public sealed record HardMode(int ExtraPods, int MaxInjections, BufferPressure HardPressure, BufferPressure SuperHardPressure);

    /// <summary>
    /// A generation profile for one progression band (data-model §1.5, FR-079, T083). Values come from the spec's Level
    /// Band Guidelines and the unlock roadmap.
    /// </summary>
    public sealed record GenerationProfile(
        string BandId,
        IntRange LevelRange,
        IntRange BoardWidth,
        IntRange BoardHeight,
        IReadOnlyList<string> PictureThemes,
        StructureTargets StructureTargets,
        IntRange VariantCount,
        IReadOnlyList<VariantId> AllowedVariants,
        bool AllowRoleMerge,
        IReadOnlyList<string> EntryLayouts,
        int MaxLayerDepth,
        IReadOnlyList<string> AllowedMechanics,
        IntRange Stacks,
        IntRange PodCount,
        IntRange PodSize,
        IntRange Work,
        BufferPressure BufferPressureTarget,
        IntRange DurationSeconds,
        HardMode HardMode,
        int SolverNodeBudget,
        int MaxCandidatesPerLevel)
    {
        public bool Allows(string mechanic) => ((ICollection<string>)AllowedMechanics).Contains(mechanic);
    }
}
