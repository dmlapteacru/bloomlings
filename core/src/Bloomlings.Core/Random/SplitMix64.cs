namespace Bloomlings.Core.Random
{
    /// <summary>
    /// SplitMix64 (Steele, Lea, Flood 2014; reference by Vigna). Used to seed <see cref="Xoshiro256StarStar"/> and
    /// to derive Zobrist keys. Integer-only and platform-independent (research R3).
    /// </summary>
    public struct SplitMix64
    {
        private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

        private ulong _state;

        public SplitMix64(ulong seed)
        {
            _state = seed;
        }

        public ulong Next()
        {
            unchecked
            {
                _state += GoldenGamma;
                return Mix(_state);
            }
        }

        /// <summary>The SplitMix64 output function, usable as a stand-alone 64-bit mixer.</summary>
        public static ulong Mix(ulong z)
        {
            unchecked
            {
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
