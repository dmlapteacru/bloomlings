using System;
using System.Collections.Generic;
using System.IO;
using Bloomlings.Client.App;
using Bloomlings.Client.App.Progression;
using Bloomlings.Client.Services.Content;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Packs;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Progression;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Client.Tests
{
    /// <summary>
    /// The Unity client on the round levels (spec 006 US5, T057, T060), engine-free: it reads the published round content as
    /// the client does (<see cref="RoundContentLoader"/>), plays every round level's saved solution through the client's
    /// flow (<see cref="GameFlow"/>, <see cref="RoundCatalogService"/>, <see cref="ProgressionService"/>) to a win, and after
    /// the last level shows "new levels soon" instead of a level.
    /// </summary>
    public class RoundContentClientTests
    {
        private static string ProofsFolder => Path.Combine(DevContent.RepositoryContentFolder, "game", "rounds", "proofs");

        private static RoundContentSet Published() => RoundContentLoader.ReadFolder(RoundContentLoader.RepositoryPublishedFolder);

        private static ContentSet NoLegacyLevels() =>
            new ContentSet(0, LooseContentFolder.DevShuffleNodeBudget, Array.Empty<LevelDefinition>(), Array.Empty<BasePicture>());

        private sealed class Game
        {
            public Game(IRoundLevelSource rounds)
            {
                Save = PlayerSave.CreateNew("p1", new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc));
                Progression = new ProgressionService(Save, UnlockRoadmap.Default, () => { });
                Progression.Initialize();
                Rounds = new RoundCatalogService(rounds);
                Flow = new GameFlow(Progression, new CatalogService(NoLegacyLevels()), Rounds, Scenes.Add);
            }

            public PlayerSave Save { get; }

            public ProgressionService Progression { get; }

            public RoundCatalogService Rounds { get; }

            public GameFlow Flow { get; }

            public List<string> Scenes { get; } = new List<string>();
        }

        private static RoundInput Input(RoundCommand command) => command.Type switch
        {
            RoundCommandType.Tap => RoundInput.Tap(command.Index, command.AtUs),
            RoundCommandType.Bridge => RoundInput.Bridge(command.Index, command.AtUs),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command.Type, "Unknown round command type."),
        };

        /// <summary>Plays the saved solution of the attempt's level patiently, as the reference replays it; returns the timeline.</summary>
        private static RoundTimeline PlaySavedSolution(RoundAttempt attempt, RoundProof proof)
        {
            var timeline = new RoundTimeline(attempt.Level);
            var replay = new RoundReplay(timeline);
            foreach (RoundCommand command in proof.Solution.Commands)
            {
                RoundCommitResult result = replay.Execute(Input(command), patient: true);
                Assert.That(result.Accepted, Is.True, $"L{attempt.LevelNumber}: {command} refused ({result.Reason})");
            }

            replay.Finish();
            return timeline;
        }

        private static RoundProof ProofOf(int level) =>
            RoundJson.ReadProof(File.ReadAllText(Path.Combine(ProofsFolder, "L" + level.ToString("0000") + ".json")));

        [Test]
        public void ThePublishedRoundContent_ReadsAsTheClientReadsIt()
        {
            RoundContentSet set = Published();

            Assert.That(set.ContentVersion, Is.GreaterThanOrEqualTo(1));
            Assert.That(set.LevelCount, Is.GreaterThanOrEqualTo(20), "the first transfer gives L1–L20 (FR-060)");
            Assert.That(set.ReadPacks, Is.Empty, "levels and pictures are parsed on first use");
            for (int n = 1; n <= set.LevelCount; n++)
            {
                RoundLevel level = set.GetLevel(n);
                RoundManifestLevel listed = set.Listed(n);
                Assert.That(level.Number, Is.EqualTo(n));
                Assert.That(level.DefinitionVersion, Is.EqualTo(listed.DefinitionVersion));
                Assert.That(set.GetPicture(level).Id, Is.EqualTo(listed.PictureId));
            }

            Assert.That(set.Has(set.LevelCount + 1), Is.False);
        }

        [Test]
        public void EveryRoundLevel_WinsWithItsSavedSolution_ThroughTheClientsFlow()
        {
            var game = new Game(Published());
            int count = game.Rounds.LevelCount;
            Assert.That(game.Progression.CurrentLevel, Is.EqualTo(1));

            game.Flow.Begin(firstLaunch: true);
            for (int n = 1; n <= count; n++)
            {
                RoundAttempt attempt = game.Flow.CurrentRoundAttempt ?? throw new AssertionException($"L{n}: no round attempt started");
                Assert.That(attempt.LevelNumber, Is.EqualTo(n));
                Assert.That(attempt.ContentVersion, Is.EqualTo(game.Rounds.ContentVersion));
                Assert.That(game.Flow.CurrentAttempt, Is.Null, $"L{n}: no legacy level starts with round content");

                RoundProof proof = ProofOf(n);
                Assert.That(proof.Number, Is.EqualTo(n));
                Assert.That(proof.DefinitionVersion, Is.EqualTo(attempt.Level.DefinitionVersion), $"L{n}: the proof is for the published definition");

                RoundTimeline played = PlaySavedSolution(attempt, proof);
                Assert.That(RoundState.StatusText(played.State.Status), Is.EqualTo(proof.Solution.Outcome), $"L{n}: the saved solution wins");
                Assert.That(played.Now, Is.EqualTo(proof.Solution.EndUs), $"L{n}: the win ends when the proof says");

                Assert.That(game.Flow.OnLevelWon(n), Is.True, $"L{n}: a new win");
                Assert.That(game.Progression.CurrentLevel, Is.EqualTo(n + 1));
                Assert.That(game.Flow.NewLevelsSoonPending, Is.False, $"L{n}: not yet past the last level");
                game.Flow.Next();
            }

            // After the last level: "new levels soon" on Home, and no level follows.
            Assert.That(game.Flow.CurrentRoundAttempt, Is.Null);
            Assert.That(game.Flow.CurrentAttempt, Is.Null);
            Assert.That(game.Flow.NewLevelsSoonPending, Is.True);
            Assert.That(game.Scenes[game.Scenes.Count - 1], Is.EqualTo(GameFlow.HomeScene));
            Assert.That(game.Save.Progression.HighestCompletedLevel, Is.EqualTo(count));
        }

        [Test]
        public void PastTheLastRoundLevel_PlayShowsNewLevelsSoon_OnHome()
        {
            var game = new Game(Published());
            int count = game.Rounds.LevelCount;

            Assert.That(game.Rounds.HasLevel(count), Is.True);
            Assert.That(game.Rounds.IsPastLastLevel(count), Is.False);
            Assert.That(game.Rounds.IsPastLastLevel(count + 1), Is.True);
            Assert.That(game.Rounds.BeginAttempt(count + 1), Is.Null);
            Assert.That(game.Flow.CanPlay(count + 1), Is.True, "Home's Play stays active and answers with the card");

            game.Progression.FastForward(count);
            Assert.That(game.Flow.IsPastLastRoundLevel(game.Progression.CurrentLevel), Is.True);

            // Not on Home (no listener): the flow goes Home, which takes the card once.
            game.Flow.Play();
            Assert.That(game.Flow.CurrentRoundAttempt, Is.Null);
            Assert.That(game.Scenes, Is.EqualTo(new[] { GameFlow.HomeScene }));
            Assert.That(game.Flow.TakeNewLevelsSoon(), Is.True);
            Assert.That(game.Flow.TakeNewLevelsSoon(), Is.False, "the card shows once");

            // On Home (listening): the card shows at once, with no scene change.
            int shown = 0;
            game.Flow.NewLevelsSoon += () =>
            {
                if (game.Flow.TakeNewLevelsSoon())
                {
                    shown++;
                }
            };
            game.Flow.Play();
            Assert.That(shown, Is.EqualTo(1));
            Assert.That(game.Scenes.Count, Is.EqualTo(1));
        }

        [Test]
        public void TheLastLevelsNext_SkipsTheIris_AndShowsNewLevelsSoon()
        {
            var game = new Game(Published());
            int transitions = 0;
            game.Flow.PostWinTransition = (_, next) =>
            {
                transitions++;
                next();
            };

            game.Progression.FastForward(game.Rounds.LevelCount - 1);
            game.Flow.Play();
            Assert.That(game.Flow.CurrentRoundAttempt!.LevelNumber, Is.EqualTo(game.Rounds.LevelCount));
            Assert.That(game.Flow.OnLevelWon(game.Rounds.LevelCount), Is.True);
            game.Flow.Next();

            Assert.That(transitions, Is.Zero, "no iris with a level number that does not exist");
            Assert.That(game.Flow.NewLevelsSoonPending, Is.True);
        }

        [Test]
        public void TheBuildsCopy_IsVerified_AndDamageIsRefused()
        {
            string root = Path.Combine(Path.GetTempPath(), "bloomlings-rounds-" + Guid.NewGuid().ToString("N"));
            try
            {
                string target = Path.Combine(root, "StreamingAssets", "rounds");
                Directory.CreateDirectory(target);
                File.WriteAllText(Path.Combine(target, "stale.txt"), "left over");

                RoundContentManifest manifest = Editor.ImportContentMenu.ImportRoundsFrom(RoundContentLoader.RepositoryPublishedFolder, target);
                Assert.That(File.Exists(Path.Combine(target, "stale.txt")), Is.False, "the copy holds the published set only");
                RoundContentSet copy = RoundContentLoader.ReadFolder(target);
                Assert.That(copy.ContentVersion, Is.EqualTo(manifest.ContentVersion));
                Assert.That(copy.LevelCount, Is.EqualTo(manifest.Levels.Count));

                // A damaged pack fails the load, and the import refuses it before replacing anything.
                string damaged = Path.Combine(root, "damaged");
                Editor.ImportContentMenu.ImportRoundsFrom(RoundContentLoader.RepositoryPublishedFolder, damaged);
                string pack = Path.Combine(damaged, manifest.Packs[0].Path);
                byte[] bytes = File.ReadAllBytes(pack);
                bytes[bytes.Length / 2] ^= 0x5a;
                File.WriteAllBytes(pack, bytes);
                Assert.Throws<ContentIntegrityException>(() => RoundContentLoader.ReadFolder(damaged));
                Assert.Throws<ContentIntegrityException>(() => Editor.ImportContentMenu.ImportRoundsFrom(damaged, target));
                Assert.That(RoundContentLoader.ReadFolder(target).ContentVersion, Is.EqualTo(manifest.ContentVersion), "the good copy stays");
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }
    }
}
