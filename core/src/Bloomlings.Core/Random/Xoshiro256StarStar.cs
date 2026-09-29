using System;

namespace Bloomlings.Core.Random
{
    /// <summary>
    /// xoshiro256** 1.0 (Blackman, Vigna). The only source of randomness in the rules; used by Shuffle and seeded
    /// from level data, so results are reproducible on every device (research R3, R10). Never use System.Random.
    /// </summary>
    public struct Xoshiro256StarStar
    {
        private ulong _s0;
        private ulong _s1;
        private ulong _s2;
        private ulong _s3;

        /// <summary>Seeds the state with four consecutive SplitMix64 outputs, as recommended by the authors.</summary>
        public Xoshiro256StarStar(ulong seed)
        {
            var sm = new SplitMix64(seed);
            _s0 = sm.Next();
            _s1 = sm.Next();
            _s2 = sm.Next();
            _s3 = sm.Next();
        }

        /// <summary>Creates a generator from an explicit state; the state must not be all zero.</summary>
        public Xoshiro256StarStar(ulong s0, ulong s1, ulong s2, ulong s3)
        {
            if ((s0 | s1 | s2 | s3) == 0)
            {
                throw new ArgumentException("The xoshiro256** state must not be all zero.");
            }

            _s0 = s0;
            _s1 = s1;
            _s2 = s2;
            _s3 = s3;
        }

        public ulong Next()
        {
            unchecked
            {
                ulong result = RotateLeft(_s1 * 5, 7) * 9;
                ulong t = _s1 << 17;

                _s2 ^= _s0;
                _s3 ^= _s1;
                _s1 ^= _s2;
                _s0 ^= _s3;
                _s2 ^= t;
                _s3 = RotateLeft(_s3, 45);

                return result;
            }
        }

        /// <summary>Uniform integer in [0, <paramref name="bound"/>) without modulo bias (rejection sampling).</summary>
        public ulong NextBelow(ulong bound)
        {
            if (bound == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bound), "bound must be positive.");
            }

            unchecked
            {
                // Reject the top (2^64 mod bound) values so that every residue is equally likely.
                ulong threshold = (0UL - bound) % bound;
                while (true)
                {
                    ulong r = Next();
                    if (r >= threshold)
                    {
                        return r % bound;
                    }
                }
            }
        }

        /// <summary>Uniform integer in [0, <paramref name="bound"/>).</summary>
        public int NextInt(int bound)
        {
            if (bound <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bound), "bound must be positive.");
            }

            return (int)NextBelow((ulong)bound);
        }

        private static ulong RotateLeft(ulong x, int k) => (x << k) | (x >> (64 - k));
    }
}
