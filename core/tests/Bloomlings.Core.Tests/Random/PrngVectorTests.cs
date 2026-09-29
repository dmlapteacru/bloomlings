using System.Collections.Generic;
using Bloomlings.Core.Hashing;
using Bloomlings.Core.Random;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Random
{
    /// <summary>Known-answer tests: the PRNGs must match the published reference algorithms bit for bit (research R3).</summary>
    public class PrngVectorTests
    {
        [Test]
        public void SplitMix64_MatchesReferenceOutputForSeed1234567()
        {
            // Reference values (Rosetta Code, "Pseudo-random numbers/Splitmix64").
            var rng = new SplitMix64(1234567UL);
            ulong[] expected =
            {
                6457827717110365317UL,
                3203168211198807973UL,
                9817491932198370423UL,
                4593380528125082431UL,
                16408922859458223821UL,
            };

            foreach (ulong value in expected)
            {
                Assert.That(rng.Next(), Is.EqualTo(value));
            }
        }

        [Test]
        public void Xoshiro256StarStar_MatchesReferenceOutputForState1234()
        {
            // Output of the reference C implementation (xoshiro256starstar.c) for s = {1, 2, 3, 4}.
            var rng = new Xoshiro256StarStar(1UL, 2UL, 3UL, 4UL);
            ulong[] expected =
            {
                11520UL,
                0UL,
                1509978240UL,
                1215971899390074240UL,
                1216172134540287360UL,
                607988272756665600UL,
            };

            foreach (ulong value in expected)
            {
                Assert.That(rng.Next(), Is.EqualTo(value));
            }
        }

        [Test]
        public void Xoshiro256StarStar_SeededThroughSplitMix64_IsStable()
        {
            var rng = new Xoshiro256StarStar(42UL);
            Assert.That(rng.Next(), Is.EqualTo(1546998764402558742UL));
            Assert.That(rng.Next(), Is.EqualTo(6990951692964543102UL));
            Assert.That(rng.Next(), Is.EqualTo(12544586762248559009UL));
        }

        [Test]
        public void NextInt_StaysInRangeAndIsReproducible()
        {
            var a = new Xoshiro256StarStar(7UL);
            var b = new Xoshiro256StarStar(7UL);
            for (int i = 0; i < 1000; i++)
            {
                int x = a.NextInt(6);
                Assert.That(x, Is.InRange(0, 5));
                Assert.That(b.NextInt(6), Is.EqualTo(x));
            }
        }

        [Test]
        public void ZobristKeys_AreDeterministicAndDistinct()
        {
            var seen = new HashSet<ulong>();
            for (int cell = 0; cell < 14 * 16; cell++)
            {
                for (int depth = 0; depth < 4; depth++)
                {
                    for (int variant = 0; variant < 12; variant++)
                    {
                        ulong key = ZobristKeys.Key(ZobristFeature.CellLayer, cell, depth, variant);
                        Assert.That(ZobristKeys.Key(ZobristFeature.CellLayer, cell, depth, variant), Is.EqualTo(key));
                        Assert.That(seen.Add(key), Is.True, "Zobrist keys must not collide on a full board.");
                    }
                }
            }

            Assert.That(ZobristKeys.Key(ZobristFeature.CellOpen, 5), Is.Not.EqualTo(ZobristKeys.Key(ZobristFeature.CellMysteryHidden, 5)));
        }

        [Test]
        public void StateHasher_ToggleTwiceRestoresValue()
        {
            var hasher = new StateHasher();
            hasher.Toggle(ZobristFeature.PodRemaining, 3, 12);
            ulong afterOne = hasher.Value;
            hasher.Toggle(ZobristFeature.PodRemaining, 3, 12);
            Assert.That(afterOne, Is.Not.EqualTo(0UL));
            Assert.That(hasher.Value, Is.EqualTo(0UL));
        }
    }
}
