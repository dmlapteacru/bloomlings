using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Bloomlings.Pipeline.Rounds;
using NUnit.Framework;

namespace Bloomlings.Pipeline.Tests.Rounds
{
    /// <summary>
    /// The game's round levels (<c>content/game/rounds/</c>) replay exactly as the reference engine recorded them in their
    /// conformance fixtures (T032, SC-001, SC-002), and a one-millisecond change of the release lag is caught with the
    /// level and the first differing window named (T033, SC-005). They sit here rather than in the content tests because
    /// the replay lives in the pipeline (<see cref="RoundReplayConformance"/>).
    /// </summary>
    [TestFixture]
    public class RoundConformanceTests
    {
        private static string RepositoryRoot
        {
            get
            {
                DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !File.Exists(Path.Combine(dir.FullName, "core", "Bloomlings.sln")))
                {
                    dir = dir.Parent;
                }

                return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
            }
        }

        private static readonly Lazy<RoundContent> GameRounds = new Lazy<RoundContent>(() =>
            RoundContent.Load(Path.Combine(RepositoryRoot, "content", "game", "rounds")));

        private static IEnumerable<int> Levels()
        {
            string folder = Path.Combine(RepositoryRoot, "content", "game", "rounds", RoundContent.LevelsFolder);
            return Directory.Exists(folder)
                ? Directory.GetFiles(folder, "L*.json").Select(f => int.Parse(Path.GetFileNameWithoutExtension(f).Substring(1))).OrderBy(n => n)
                : Enumerable.Empty<int>();
        }

        private static (RoundLevel Level, RoundProof Proof, RoundConformanceFixture Fixture) Set(int number)
        {
            RoundContent content = GameRounds.Value;
            Assert.That(content.Issues.Where(i => i.IsError), Is.Empty, "the game's round content reads cleanly");
            return (content.Levels[number].Value!, content.Proofs[number].Value!, content.Fixtures[number].Value!);
        }

        [Test]
        public void TheGameHasRoundLevels()
        {
            Assert.That(Levels(), Is.Not.Empty, "content/game/rounds/levels holds the transferred round levels");
        }

        [TestCaseSource(nameof(Levels))]
        public void TheLevel_ReplaysAsTheReference(int number)
        {
            (RoundLevel level, RoundProof proof, RoundConformanceFixture fixture) = Set(number);

            IReadOnlyList<string> errors = new RoundReplayConformance().Check(level, proof, fixture);

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(fixture.Games.Count, Is.EqualTo(proof.RandomTest.Runs), "every random game of the proof is in the fixture");
            Assert.That(fixture.RandomWins, Is.EqualTo(proof.RandomTest.Wins), "the fixture's wins are the proof's");
        }

        [Test]
        public void ABridgeLevelsRandomGames_BuildOnlyItsTestBridge_AndDifferWithoutTheRestriction()
        {
            // rules 17 §5 as amended on 2026-10-30: a bridge level's class test plays its best bridge only
            int[] restricted = Levels().Where(n => Set(n).Proof.RandomTest.Bridge.HasValue).ToArray();
            Assume.That(restricted, Is.Not.Empty, "no transferred level restricts its random test to one bridge");
            bool anyDiffers = false;
            foreach (int number in restricted)
            {
                (RoundLevel level, RoundProof proof, RoundConformanceFixture fixture) = Set(number);
                int only = proof.RandomTest.Bridge!.Value;
                Assert.That(fixture.RandomBridge, Is.EqualTo(only), $"L{number:0000}: the fixture names the proof's bridge");
                for (int n = 0; n < fixture.Games.Count; n++)
                {
                    uint seed = RoundRandomPolicy.SeedOf(fixture.RandomSeedBase, n);
                    RoundRandomGame kept = RoundRandomPolicy.Play(level, seed, RoundRandomPolicy.KeepBridge(only));
                    Assert.That(kept.Timeline.State.Bridge, Is.EqualTo(-1).Or.EqualTo(only), $"L{number:0000} game {n}: only bridge {only} may be built");
                    if (!anyDiffers)
                    {
                        RoundRandomGame free = RoundRandomPolicy.Play(level, seed);
                        anyDiffers = free.EndUs != kept.EndUs || RoundEvents.SignatureHash(free.Timeline) != RoundEvents.SignatureHash(kept.Timeline);
                    }
                }
            }

            Assert.That(anyDiffers, Is.True, "leaving the restriction out changes some random game");
        }

        [Test]
        public void AChangedReleaseLag_FailsAndNamesTheLevelAndTheWindow()
        {
            var broken = new RoundReplayConformance(new RoundProfile(releaseLag: 299_000));
            foreach (int number in Levels())
            {
                (RoundLevel level, RoundProof proof, RoundConformanceFixture fixture) = Set(number);
                IReadOnlyList<string> errors = broken.Check(level, proof, fixture);
                if (errors.Count > 0)
                {
                    Assert.That(errors[0], Does.StartWith("L" + number.ToString("0000") + " solution window "));
                    Assert.That(errors[0], Does.Contain("differ"));
                    return;
                }
            }

            Assert.Fail("A release lag of 299 ms replayed every level unchanged.");
        }
    }
}
