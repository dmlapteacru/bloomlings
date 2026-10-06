namespace Bloomlings.Core.Definitions
{
    /// <summary>
    /// How a level's board is drawn (FR-036 as amended on 2026-10-06, the owner). It is stored in the level data
    /// (<c>boardLook</c>), never chosen per device, and follows the board's cell count (<see cref="BoardLooks.For"/>).
    /// </summary>
    public enum BoardLook
    {
        /// <summary>Candy tiles with their icons and the small next-layer chip, the layer peek (boards of up to 288 cells).</summary>
        Peek,

        /// <summary>
        /// Icons only, the next layer hidden (boards over 288 cells, the rare big levels): a layered tile does not show
        /// what lies under it, and clearing its top layer reveals it as a surprise.
        /// </summary>
        Icons,
    }

    /// <summary>The board look rule (FR-036 as amended on 2026-10-06).</summary>
    public static class BoardLooks
    {
        /// <summary>The most cells a <see cref="BoardLook.Peek"/> board has: 288, the largest regular board (16 × 18).</summary>
        public const int MaxPeekCells = 288;

        /// <summary>The look a board of <paramref name="width"/> × <paramref name="height"/> cells must have.</summary>
        public static BoardLook For(int width, int height) => width * height > MaxPeekCells ? BoardLook.Icons : BoardLook.Peek;

        /// <summary>The look the level stores, or <see cref="BoardLook.Peek"/> when it stores none (content written before 2026-10-06).</summary>
        public static BoardLook Of(LevelDefinition definition) => definition.BoardLook ?? BoardLook.Peek;
    }
}
