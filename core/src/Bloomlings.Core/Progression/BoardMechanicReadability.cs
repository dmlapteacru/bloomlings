namespace Bloomlings.Core.Progression
{
    /// <summary>Content limits for a phone-sized board. Legibility is distinct from logical solvability.</summary>
    public static class BoardMechanicReadability
    {
        public static int ObjectSide(int width, int height) => width > 24 || height > 26 ? 3 : 2;
        public static bool Allows(string mechanic, int width, int height) => mechanic switch
        {
            "stone" or "connected_pair" or "connected_triple" => true,
            "key" or "locked_pod" or "locked_slot" => width <= 20 && height <= 24,
            "counted_stack" => width <= 24 && height <= 26,
            // New content uses at least a 2×2 footprint for these objects; the validator checks that separately.
            "gate" or "fountain" or "chest" or "environment_2" => true,
            "layered_tile" => width <= 28 && height <= 28,
            "mystery_tile" or "mystery_pod" => width * height <= 288,
            _ => false,
        };
    }
}
