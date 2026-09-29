namespace Bloomlings.Core.Definitions
{
    public enum DifficultyClass
    {
        Normal,
        Hard,
        SuperHard,
    }

    /// <summary>Difficulty class and score (FR-082). The score is an integer fixed-point value (× 1000).</summary>
    public sealed record DifficultyDef(DifficultyClass Class, int Score, bool Overridden);
}
