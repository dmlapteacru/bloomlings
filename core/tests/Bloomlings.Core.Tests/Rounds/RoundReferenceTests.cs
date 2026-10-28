using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Rounds
{
    /// <summary>
    /// The C# port against the reference round engine (spec 006 FR-030, FR-031, research R4): the expected values in
    /// <c>Fixtures/reference-rounds.json</c> were written by <c>Fixtures/export-reference.cjs</c> from the JS engine on
    /// the placed levels L1–L20, so no Node runs here. Every saved solution must give the same canonical events and
    /// extended signature per command window; the same route on a running clock with fractional-millisecond delays must
    /// end identically; every recorded random game must end with the same outcome, end time and final signature, and the
    /// win counts must equal the levels' records; the patient solver must find the saved route.
    /// </summary>
    public class RoundReferenceTests
    {
        private static IEnumerable<int> Levels() => RoundTestLevels.ReferenceLevels.Select(l => l.GetProperty("number").GetInt32()).ToArray();

        [Test]
        public void TheFixtureCoversTheTwentyPlacedLevels()
        {
            Assert.That(Levels(), Is.EquivalentTo(Enumerable.Range(1, 20)));
        }

        [Test]
        public void TheFixtureWasWrittenByTheCurrentReferenceEngine()
        {
            JsonElement engine = RoundTestLevels.Reference.GetProperty("engine");
            foreach (string part in new[] { "rules", "timeline", "palette", "clock" })
            {
                string path = Path.Combine(RoundTestLevels.RepoRoot, "tools", "rounds", "engine", part + ".cjs");
                string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
                Assert.That(hash, Is.EqualTo(engine.GetProperty(part).GetString()), part + ".cjs changed: regenerate the fixture (export-reference.cjs).");
            }
        }

        [TestCaseSource(nameof(Levels))]
        public void TheFixtureLevelIsTheContentLevel(int number)
        {
            RoundLevel fromFixture = RoundTestLevels.FromReference(RoundTestLevels.ReferenceLevel(number));
            RoundLevel fromContent = RoundTestLevels.Content(number);
            Assert.That(RoundRules.Create(fromFixture).Signature(), Is.EqualTo(RoundRules.Create(fromContent).Signature()));
            Assert.That(fromFixture.Tray.Select(c => string.Join(",", c)), Is.EqualTo(fromContent.Tray.Select(c => string.Join(",", c))));
        }

        [TestCaseSource(nameof(Levels))]
        public void TheSavedSolutionReplaysWithTheSameEventsAndStates(int number)
        {
            JsonElement fixture = RoundTestLevels.ReferenceLevel(number);
            RoundLevel level = RoundTestLevels.FromReference(fixture);
            JsonElement expected = fixture.GetProperty("solution");
            JsonElement[] windows = expected.GetProperty("windows").EnumerateArray().ToArray();

            var t = new RoundTimeline(level);
            var log = new RoundEventLog();
            var replay = new RoundReplay(t, log);
            var texts = new List<string>();
            var signatures = new List<string>();
            var commands = new List<RoundInput>();
            foreach (JsonElement a in fixture.GetProperty("route").EnumerateArray())
            {
                replay.Finish();
                if (commands.Count > 0)
                {
                    texts.Add(log.Take());
                }

                RoundInput command = RoundInput.Of(RoundTestLevels.Action(a), t.Now);
                RoundCommitResult result = replay.Execute(command);
                Assert.That(result.Accepted, Is.True, $"L{number} refused {command}: {result.Reason}");
                commands.Add(command);
                signatures.Add(RoundEvents.SignatureHash(t));
            }

            replay.Finish();
            texts.Add(log.Take());

            Assert.That(commands.Count, Is.EqualTo(windows.Length));
            for (int k = 0; k < windows.Length; k++)
            {
                JsonElement w = windows[k];
                Assert.That(commands[k].AtUs, Is.EqualTo(w.GetProperty("command").GetProperty("atUs").GetInt64()), $"L{number} window {k}: command time");
                string events = RoundEvents.Sha256(texts[k]);
                if (events != w.GetProperty("events").GetString())
                {
                    Assert.Fail($"L{number:0000} solution window {k} ({commands[k]}): events differ. C# canonical log of the window:\n{texts[k]}");
                }

                Assert.That(signatures[k], Is.EqualTo(w.GetProperty("signature").GetString()), $"L{number} window {k} ({commands[k]}): signature after the command");
            }

            Assert.That(RoundState.StatusText(t.State.Status), Is.EqualTo(expected.GetProperty("outcome").GetString()));
            Assert.That(t.Now, Is.EqualTo(expected.GetProperty("endUs").GetInt64()), "end time");
            Assert.That(RoundEvents.SignatureHash(t), Is.EqualTo(expected.GetProperty("finalSignature").GetString()), "final signature");
        }

        [TestCaseSource(nameof(Levels))]
        public void TheSavedRouteOnARunningClockEndsTheSame(int number)
        {
            JsonElement fixture = RoundTestLevels.ReferenceLevel(number);
            RoundLevel level = RoundTestLevels.FromReference(fixture);
            JsonElement expected = fixture.GetProperty("timed");
            long[] delays = expected.GetProperty("delaysUs").EnumerateArray().Select(d => d.GetInt64()).ToArray();
            string[] windows = expected.GetProperty("windows").EnumerateArray().Select(w => w.GetString()!).ToArray();

            var t = new RoundTimeline(level);
            var log = new RoundEventLog();
            var replay = new RoundReplay(t, log, idleClock: true);
            var texts = new List<string>();
            long at = 0;
            int refused = 0;
            int k = 0;
            foreach (JsonElement a in fixture.GetProperty("route").EnumerateArray())
            {
                at += delays[k++];
                RoundInput command = RoundInput.Of(RoundTestLevels.Action(a), at);
                if (command.AtUs > replay.ClockUs)
                {
                    replay.AdvanceTo(command.AtUs);
                }

                // The reference's wait for a refused recorded tap, then the window ends just before the command.
                RoundAction action = RoundTestLevels.Action(a);
                for (int guard = 0; guard < RoundReplay.WaitGuard && t.Check(action) != RoundRefusal.None && t.Pending; guard++)
                {
                    replay.AdvanceTo(Math.Min(replay.NextAtUs, replay.ClockUs + RoundReplay.WaitChunkUs));
                }

                texts.Add(log.Take());
                RoundCommitResult result = replay.Execute(command.At(replay.ClockUs));
                if (!result.Accepted)
                {
                    refused++;
                }
            }

            replay.Finish();
            texts.Add(log.Take());

            Assert.That(texts.Count, Is.EqualTo(windows.Length));
            for (int w = 0; w < windows.Length; w++)
            {
                if (RoundEvents.Sha256(texts[w]) != windows[w])
                {
                    Assert.Fail($"L{number:0000} timed window {w}: events differ. C# canonical log of the window:\n{texts[w]}");
                }
            }

            Assert.That(refused, Is.EqualTo(expected.GetProperty("refused").GetInt32()), "refused");
            Assert.That(RoundState.StatusText(t.State.Status), Is.EqualTo(expected.GetProperty("outcome").GetString()));
            Assert.That(replay.ClockUs, Is.EqualTo(expected.GetProperty("endUs").GetInt64()), "end time");
            Assert.That(replay.IdleUs, Is.EqualTo(expected.GetProperty("idleUs").GetInt64()), "idle time");
            Assert.That(RoundEvents.SignatureHash(t), Is.EqualTo(expected.GetProperty("finalSignature").GetString()), "final signature");
        }

        [TestCaseSource(nameof(Levels))]
        public void EveryRecordedRandomGameEndsTheSame(int number)
        {
            JsonElement fixture = RoundTestLevels.ReferenceLevel(number);
            RoundLevel level = RoundTestLevels.FromReference(fixture);
            JsonElement random = fixture.GetProperty("random");
            long seedBase = random.GetProperty("seedBase").GetInt64();
            int n = 0;
            int wins = 0;
            var differences = new StringBuilder();
            foreach (JsonElement game in random.GetProperty("games").EnumerateArray())
            {
                JsonElement[] g = game.EnumerateArray().ToArray();
                uint seed = RoundRandomPolicy.SeedOf(seedBase, n++);
                Assert.That(seed, Is.EqualTo(g[0].GetUInt32()), "seed");
                RoundRandomGame played = RoundRandomPolicy.Play(level, seed);
                if (played.Won)
                {
                    wins++;
                }

                string sig = RoundEvents.SignatureHash(played.Timeline).Substring(7, 16);
                string status = RoundState.StatusText(played.Status).Substring(0, 1);
                if ((played.Won ? 1 : 0) != g[1].GetInt32() || played.EndUs != g[2].GetInt64() || sig != g[3].GetString() || status != g[4].GetString())
                {
                    differences.AppendLine($"seed {seed}: expected won={g[1]} end={g[2]} sig={g[3]} status={g[4]}, got won={(played.Won ? 1 : 0)} end={played.EndUs} sig={sig} status={status}");
                }
            }

            Assert.That(differences.ToString(), Is.Empty, $"L{number}: random games differ");
            Assert.That(n, Is.EqualTo(random.GetProperty("runs").GetInt32()));
            Assert.That(wins, Is.EqualTo(random.GetProperty("wins").GetInt32()), "wins");
        }

        /// <summary>
        /// The owner's pilot attempts in the JS exporter's conformance fixtures (<c>content/game/rounds/export/*/
        /// conformance/</c>, written by <c>tools/rounds/export.mjs</c>): commands on the timeline's own clock, refused ones
        /// kept, then everything settles; the outcome, end time and final signature must match.
        /// </summary>
        [TestCaseSource(nameof(Levels))]
        public void TheOwnersRecordedAttemptsEndTheSame(int number)
        {
            string exports = Path.Combine(RoundTestLevels.RepoRoot, "content", "game", "rounds", "export");
            string? file = Directory.Exists(exports)
                ? Directory.GetFiles(exports, "L" + number.ToString("0000") + ".json", SearchOption.AllDirectories)
                    .Where(f => Path.GetFileName(Path.GetDirectoryName(f)) == "conformance").OrderBy(f => f, StringComparer.Ordinal).LastOrDefault()
                : null;
            if (file == null)
            {
                Assert.Ignore("No exported conformance fixture for L" + number);
            }

            RoundLevel level = RoundTestLevels.FromReference(RoundTestLevels.ReferenceLevel(number));
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(file!));
            JsonElement root = doc.RootElement;
            Assert.That(root.GetProperty("solution").GetProperty("finalSignature").GetString(),
                Is.EqualTo(RoundTestLevels.ReferenceLevel(number).GetProperty("solution").GetProperty("finalSignature").GetString()), "the export agrees with this fixture");
            if (!root.TryGetProperty("owner", out JsonElement owner) || owner.GetArrayLength() == 0)
            {
                Assert.Pass("No owner attempts for L" + number);
            }

            foreach (JsonElement attempt in owner.EnumerateArray())
            {
                var t = new RoundTimeline(level);
                var replay = new RoundReplay(t);
                foreach (JsonElement c in attempt.GetProperty("commands").EnumerateArray())
                {
                    long at = c.GetProperty("atUs").GetInt64();
                    RoundInput command = c.GetProperty("type").GetString() == "tap"
                        ? RoundInput.Tap(c.GetProperty("pod").GetInt32(), at)
                        : RoundInput.Bridge(c.GetProperty("choice").GetInt32(), at);
                    RoundCommitResult result = replay.Execute(command);
                    string? refused = c.TryGetProperty("refused", out JsonElement r) ? r.GetString() : null;
                    Assert.That(result.Reason, Is.EqualTo(refused), $"L{number} attempt {attempt.GetProperty("attempt")}: {command}");
                }

                replay.Finish();
                string id = attempt.GetProperty("attempt").ToString();
                Assert.That(RoundState.StatusText(t.State.Status).ToLowerInvariant(), Is.EqualTo(attempt.GetProperty("outcome").GetString()), $"L{number} attempt {id}: outcome");
                Assert.That(t.Now, Is.EqualTo(attempt.GetProperty("endUs").GetInt64()), $"L{number} attempt {id}: end time");
                Assert.That(RoundEvents.SignatureHash(t), Is.EqualTo(attempt.GetProperty("finalSignature").GetString()), $"L{number} attempt {id}: final signature");
            }
        }

        [TestCaseSource(nameof(Levels))]
        public void ThePatientSolverFindsTheSavedRoute(int number)
        {
            JsonElement fixture = RoundTestLevels.ReferenceLevel(number);
            RoundLevel level = RoundTestLevels.FromReference(fixture);
            JsonElement expected = fixture.GetProperty("solver");
            RoundSolveResult result = new RoundSolver(200000).Route(level);
            Assert.That(result.Win, Is.EqualTo(expected.GetProperty("win").GetBoolean()));
            Assert.That(result.Nodes, Is.EqualTo(expected.GetProperty("nodes").GetInt32()), "nodes");
            RoundAction[] saved = fixture.GetProperty("route").EnumerateArray().Select(RoundTestLevels.Action).ToArray();
            Assert.That(result.Route!.SequenceEqual(saved), Is.EqualTo(expected.GetProperty("sameAsSaved").GetBoolean()), "route");
        }

        [Test]
        public void TheRandomTestWidensAsTheReference()
        {
            // L1 wins all 100 games (no widening); L4's first 100 cross the 50 % border, so it plays 400.
            RoundRandomRun easy = RoundRandomPolicy.Test(RoundTestLevels.FromReference(RoundTestLevels.ReferenceLevel(1)));
            Assert.That((easy.Runs, easy.Wins, easy.ClassOfShare), Is.EqualTo((100, 100, (RoundClass?)RoundClass.Normal)));
            JsonElement l4 = RoundTestLevels.ReferenceLevel(4);
            RoundRandomRun wide = RoundRandomPolicy.Test(RoundTestLevels.FromReference(l4));
            Assert.That(wide.Runs, Is.EqualTo(l4.GetProperty("random").GetProperty("runs").GetInt32()));
            Assert.That(wide.Wins, Is.EqualTo(l4.GetProperty("random").GetProperty("wins").GetInt32()));
        }
    }
}
