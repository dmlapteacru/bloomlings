using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bloomlings.Core.Rounds;
using NUnit.Framework;

namespace Bloomlings.Core.Tests.Rounds
{
    /// <summary>
    /// Latch + acorn key (rules 17 §8 «Засов и жёлудь-ключ», the owner, 2026-11-01; <c>tools/rounds/README.md</c> "Latch
    /// and key"): the C# port against the reference engine on the fixture's <c>latches</c> cases (written by
    /// <c>Fixtures/export-reference.cjs</c>: the small level of <c>tools/rounds/test/latch.test.mjs</c> in four forms and
    /// placed levels L14, L16, L17 and L28 given latches), plus what the fixture cannot show: blocking, the event order and
    /// times, a restored snapshot, identity for a latch open from the start, the validator's refusals, the solver's and
    /// the random policy's legal actions and the boosters.
    /// </summary>
    public class RoundLatchTests
    {
        private static IEnumerable<string> Cases() => RoundTestLevels.ReferenceLatchCases.Select(c => c.GetProperty("name").GetString()!).ToArray();

        private static RoundLevel LevelOf(JsonElement c) => RoundTestLevels.FromDefinition(1, c.GetProperty("definition"));

        private static RoundLevel Small(string name = "small") => LevelOf(RoundTestLevels.ReferenceLatchCase(name));

        private static RoundLevel WithLatches(RoundLevel ok, IReadOnlyList<RoundLatch> latches, IReadOnlyList<RoundPod>? pods = null,
            IReadOnlyList<IReadOnlyList<int>>? tray = null, IReadOnlyList<string>? rows = null, IReadOnlyList<string>? mechanics = null) =>
            new RoundLevel(ok.Number, ok.Width, ok.Height, rows ?? ok.RowsBottomFirst, pods ?? ok.Pods, tray ?? ok.Tray, ok.Entries, ok.Bridge,
                mechanics: mechanics ?? (latches.Count > 0 ? ok.Mechanics.Where(m => m != "latch").Append("latch").ToArray() : ok.Mechanics.Where(m => m != "latch").ToArray()),
                hiddenCells: ok.HiddenCells, latches: latches);

        [Test]
        public void TheFixtureHoldsTheSmallLevelsAndFourPlacedLevels()
        {
            Assert.That(Cases(), Is.EqualTo(new[] { "small", "small-commit", "small-start", "small-pair", "L0014+latch", "L0016+latch", "L0017+latch", "L0028+latch" }));
        }

        [TestCaseSource(nameof(Cases))]
        public void TheDefinitionIsValidAndTheStartStateMatches(string name)
        {
            JsonElement c = RoundTestLevels.ReferenceLatchCase(name);
            RoundLevel level = LevelOf(c);
            Assert.That(RoundLevelValidator.ValidateDefinition(level), Is.Empty);
            Assert.That(level.Mechanics, Does.Contain("latch"));
            RoundState s = RoundRules.Create(level);
            int[] closed = Enumerable.Range(0, level.Latches.Count).Where(i => s.Latched![i]).ToArray();
            Assert.That(closed, Is.EqualTo(c.GetProperty("latchedAtStart").EnumerateArray().Select(x => x.GetInt32()).ToArray()));
            Assert.That(s.LatchedLeft, Is.EqualTo(closed.Length));
            AssertLatchInvariant(s);
        }

        [TestCaseSource(nameof(Cases))]
        public void TheSolutionOpensInTheReferenceOrder(string name)
        {
            JsonElement c = RoundTestLevels.ReferenceLatchCase(name);
            List<string> lines = RoundReferenceTests.CheckSolution(c, LevelOf(c), name);
            var opens = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains(" latchOpen ", StringComparison.Ordinal))
                {
                    opens.Add(i > 0 ? lines[i - 1] : string.Empty);
                    opens.Add(lines[i]);
                }
            }

            Assert.That(opens, Is.EqualTo(c.GetProperty("opens").EnumerateArray().Select(x => x.GetString()!)));
            if (c.TryGetProperty("lines", out JsonElement all))
            {
                Assert.That(lines, Is.EqualTo(all.EnumerateArray().Select(x => x.GetString()!)), "the whole canonical log");
            }
        }

        [TestCaseSource(nameof(Cases))]
        public void TheRouteOnARunningClockEndsTheSame(string name)
        {
            JsonElement c = RoundTestLevels.ReferenceLatchCase(name);
            RoundReferenceTests.CheckTimed(c, LevelOf(c), name);
        }

        [TestCaseSource(nameof(Cases))]
        public void RandomGamesEndTheSame(string name)
        {
            JsonElement c = RoundTestLevels.ReferenceLatchCase(name);
            RoundReferenceTests.CheckRandom(c, LevelOf(c), name);
        }

        [TestCaseSource(nameof(Cases))]
        public void ThePatientSolverAgrees(string name)
        {
            JsonElement c = RoundTestLevels.ReferenceLatchCase(name);
            RoundReferenceTests.CheckSolver(c, LevelOf(c));
        }

        [Test]
        public void PodsBehindAClosedLatchAreNoTopsAndCannotBeTapped()
        {
            RoundLevel level = Small();
            RoundState s = RoundRules.Create(level);
            Assert.That(s.Latched, Is.EqualTo(new[] { true }));
            Assert.That(RoundRules.TopPods(s), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(s.BlockAt(2), Is.EqualTo(1));
            Assert.That(s.BlockAt(0), Is.EqualTo(int.MaxValue));
            Assert.That(s.BlockedColumns(), Is.Empty);
            Assert.That(s.Revealed[3], Is.False, "the hidden pod behind the latch stays hidden");

            // tapping the G pod brings the latch on top of column 2: the column has no top pod until it opens
            RoundState after = RoundSolver.Step(s, RoundAction.Tap(2))!;
            Assert.That(after.BlockedColumns(), Is.EqualTo(new[] { 2 }));
            Assert.That(after.TopLatchOf(2), Is.EqualTo(0));
            Assert.That(after.TopOf(2), Is.EqualTo(-1));
            Assert.That(RoundRules.CanTap(after, 3), Is.EqualTo(RoundRefusal.NotExposed));
            Assert.That(RoundRules.Legal(after).Contains(RoundAction.Tap(3)), Is.False, "the solver and the random test never see a held pod");
            Assert.That(after.Revealed[3], Is.False);
            Assert.That(after.QueueEmpty, Is.False, "a held pod still counts in its column");

            // a pair whose partner stands behind the latch waits for it
            RoundLevel pair = Small("small-pair");
            RoundState p = RoundSolver.Step(RoundRules.Create(pair), RoundAction.Tap(0))!;
            Assert.That(RoundRules.CanTap(p, 1), Is.EqualTo(RoundRefusal.PartnerNotExposed));
            Assert.That(RoundRules.Create(RoundTestLevels.Content(5)).Latched, Is.Null);
        }

        [Test]
        public void AClearOpensTheLatchAtItsTimeAndTheExposedPodRevealsRightAfter()
        {
            RoundLevel level = Small();
            var t = new RoundTimeline(level);
            foreach (int pod in new[] { 0, 1, 2 })
            {
                Assert.That(t.Commit(RoundAction.Tap(pod)).Accepted, Is.True);
            }

            int seen = 0;
            while (t.Pending)
            {
                t.Advance(50_000);
                IReadOnlyList<RoundEvent> ev = t.LastEvents;
                AssertLatchInvariant(t.State);
                for (int i = 0; i < ev.Count; i++)
                {
                    if (ev[i].Type != RoundEventType.LatchOpen)
                    {
                        continue;
                    }

                    seen++;
                    int k = i - 1;
                    while (ev[k].Type == RoundEventType.CellReveal)
                    {
                        k--;
                    }

                    Assert.That(ev[k].Type, Is.EqualTo(RoundEventType.Clear));
                    Assert.That(ev[i].At, Is.EqualTo(ev[k].At));
                    Assert.That((ev[i].Latch, ev[i].Column, ev[i].Cell), Is.EqualTo((0, 2, level.Latches[0].Key)));
                    Assert.That(ev[i + 1].Type, Is.EqualTo(RoundEventType.Reveal));
                    Assert.That((ev[i + 1].At, ev[i + 1].Pod, ev[i + 1].Mode), Is.EqualTo((ev[i].At, 3, RoundMystery.Depth)));
                    Assert.That(RoundEvents.Line(ev[i]), Is.EqualTo(ev[i].At + " latchOpen latch=0"));
                }

                if (t.Pending)
                {
                    // a restored snapshot recomputes the latches from heads and board
                    RoundTimeline copy = t.Copy();
                    Assert.That(copy.State.Latched, Is.EqualTo(t.State.Latched));
                    Assert.That(copy.State.LatchedLeft, Is.EqualTo(t.State.LatchedLeft));
                    Assert.That(copy.State.Signature(), Is.EqualTo(t.State.Signature()));
                }
            }

            Assert.That(seen, Is.EqualTo(1));
            Assert.That(RoundRules.TopPods(t.State), Is.EqualTo(new[] { 3 }));
        }

        [Test]
        public void ACommitOpensALatchWhoseKeyIsAlreadyReachedAtTheCommitTime()
        {
            RoundLevel level = Small("small-commit");
            var t = new RoundTimeline(level);
            foreach (int pod in new[] { 0, 2 })
            {
                t.Commit(RoundAction.Tap(pod));
                t.AdvanceUntilIdle();
            }

            Assert.That(t.State.Latched![0], Is.True);
            Assert.That(t.State.Reach().Distance[level.Latches[0].Key], Is.GreaterThan(0), "the key is reached");
            RoundCommitResult r = t.Commit(RoundAction.Tap(1));
            Assert.That(r.Events[0].Type, Is.EqualTo(RoundEventType.Commit));
            Assert.That(r.Events[1].Type, Is.EqualTo(RoundEventType.LatchOpen));
            Assert.That(r.Events[1].At, Is.EqualTo(t.Now));
            Assert.That(t.State.LatchedLeft, Is.EqualTo(0));

            // a latch on top at the start with its key reached opens silently at create
            RoundState start = RoundRules.Create(Small("small-start"));
            Assert.That(start.Latched, Is.EqualTo(new[] { false }));
            Assert.That(start.LatchedLeft, Is.EqualTo(0));
        }

        [TestCase(5)]
        [TestCase(16)]
        [TestCase(21)]
        [TestCase(30)]
        public void ALatchOpenFromTheStartChangesNoEventStateOrSignature(int number)
        {
            RoundLevel without = RoundTestLevels.Content(number);
            Assert.That(without.Latches, Is.Empty);
            Assert.That(RoundRules.Create(without).Latched, Is.Null);

            // in front of column 0, its key a ring cell reached from the start: it opens silently at create
            RoundLevel with = WithLatches(without, new[] { new RoundLatch(0, 0, without.Width + 1) });
            Assert.That(RoundLevelValidator.ValidateDefinition(with), Is.Empty);
            Assert.That(RoundRules.Create(with).Latched, Is.EqualTo(new[] { false }));

            using JsonDocument doc = JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(RoundTestLevels.RepoRoot, "content", "rounds", "levels", "L" + number.ToString("0000") + ".json")));
            RoundAction[] route = doc.RootElement.GetProperty("solution").GetProperty("route").EnumerateArray().Select(RoundTestLevels.Action).ToArray();
            List<string> Play(RoundLevel level)
            {
                var t = new RoundTimeline(level);
                var log = new List<string>();
                foreach (RoundAction a in route)
                {
                    t.AdvanceUntilIdle();
                    log.AddRange(t.LastEvents.Select(RoundEvents.Line));
                    log.AddRange(t.Commit(a).Events.Select(RoundEvents.Line));
                    log.Add(RoundEvents.Signature(t));
                }

                t.AdvanceUntilIdle();
                log.AddRange(t.LastEvents.Select(RoundEvents.Line));
                log.Add(RoundEvents.Signature(t));
                return log;
            }

            Assert.That(Play(with), Is.EqualTo(Play(without)));
            for (int k = 0; k < 4; k++)
            {
                uint seed = (uint)(k * 7919 + 1);
                RoundRandomGame a = RoundRandomPolicy.Play(without, seed);
                RoundRandomGame b = RoundRandomPolicy.Play(with, seed);
                Assert.That((b.Won, b.EndUs, RoundEvents.SignatureHash(b.Timeline)), Is.EqualTo((a.Won, a.EndUs, RoundEvents.SignatureHash(a.Timeline))));
            }
        }

        [Test]
        public void TheValidatorRefusesWhatTheReferenceRefuses()
        {
            RoundLevel ok = Small("small-commit");
            int hole = ok.Latches[0].Key;
            List<string> V(params RoundLatch[] latches) => RoundLevelValidator.ValidateDefinition(WithLatches(ok, latches));

            Assert.That(V(new RoundLatch(2, 1, hole)), Is.Empty);
            Assert.That(V(new RoundLatch(3, 0, hole)), Does.Contain(RoundLevelValidator.Latch), "no such column");
            Assert.That(V(new RoundLatch(2, 2, hole)), Does.Contain(RoundLevelValidator.Latch), "nothing behind it");
            Assert.That(V(new RoundLatch(2, -1, hole)), Does.Contain(RoundLevelValidator.Latch), "a negative depth");
            Assert.That(V(new RoundLatch(2, 1, hole), new RoundLatch(2, 1, ok.Width + 1)), Does.Contain(RoundLevelValidator.Latch), "one depth twice in a column");
            Assert.That(V(new RoundLatch(2, 1, hole - 1)), Does.Contain(RoundLevelValidator.LatchKey), "a colour cell");
            Assert.That(V(new RoundLatch(2, 1, ok.Entries[0])), Does.Contain(RoundLevelValidator.LatchKey), "an entry");
            Assert.That(V(new RoundLatch(2, 1, 0)), Does.Contain(RoundLevelValidator.LatchKey), "a wall");
            Assert.That(V(new RoundLatch(2, 1, ok.CellCount)), Does.Contain(RoundLevelValidator.LatchKey), "off the board");
            Assert.That(V(new RoundLatch(1, 0, hole), new RoundLatch(2, 1, hole)), Does.Contain(RoundLevelValidator.LatchKey), "one key twice");

            // the key enclosed by G and every G pod behind its own latch: never reachable (small-commit's tray [[0],[2],[1,3]]:
            // pods 1 and 3 are A and G, so use the plain small level whose column 2 holds both G pods)
            RoundLevel small = Small();
            List<string> unreachable = RoundLevelValidator.ValidateDefinition(WithLatches(small, new[] { new RoundLatch(2, 0, small.Latches[0].Key) }));
            Assert.That(unreachable, Does.Contain(RoundLevelValidator.LatchKeyUnreachable));
            Assert.That(RoundLevelValidator.LatchKeysBlocked(WithLatches(small, new[] { new RoundLatch(2, 0, small.Latches[0].Key) })), Is.EqualTo(new[] { 0 }));

            // two columns of three blocked at the start (two holes, two latches at depth 0)
            string[] picture = { "GGGGGGG", "G.GGG.G", "GGGGGGG", "AAAAAAA", "AAAAAAA" };
            var rows = ok.RowsBottomFirst.ToArray();
            int H = ok.Height;
            for (int y = 0; y < picture.Length; y++)
            {
                int row = H - 1 - (y + 2);
                rows[row] = rows[row].Substring(0, 2) + picture[y] + rows[row].Substring(2 + picture[y].Length);
            }

            int At(int x, int y) => (H - 1 - (y + 2)) * ok.Width + x + 2;
            var pods = new[] { new RoundPod('A', 7), new RoundPod('A', 7), new RoundPod('G', 10), new RoundPod('G', 9) };
            var tray = new IReadOnlyList<int>[] { new[] { 0, 2 }, new[] { 1 }, new[] { 3 } };
            RoundLevel two = WithLatches(ok, new[] { new RoundLatch(1, 0, At(1, 1)), new RoundLatch(2, 0, At(5, 1)) }, pods, tray, rows);
            Assert.That(RoundLevelValidator.ValidateDefinition(two), Is.EqualTo(new[] { RoundLevelValidator.LatchesBlockTooMany }));
            Assert.That(RoundLevelValidator.ValidateDefinition(WithLatches(two, new[] { two.Latches[0] })), Is.Empty);

            // "latch" is listed exactly when the level has latches
            Assert.That(RoundLevelValidator.ValidateDefinition(WithLatches(ok, ok.Latches, mechanics: Array.Empty<string>())), Does.Contain(RoundLevelValidator.Mechanics));
            Assert.That(RoundLevelValidator.ValidateDefinition(WithLatches(ok, Array.Empty<RoundLatch>(), mechanics: new[] { "latch" })), Does.Contain(RoundLevelValidator.Mechanics));
        }

        [Test]
        public void TheLatchIsIntroducedOnLevels61And63()
        {
            Assert.That(RoundLevelValidator.IntroductionTable, Does.Contain(("latch", 61, 63)));
            RoundLevel small = Small();
            RoundLevel At(int number, RoundIntroduction? intro, IReadOnlyList<string> mechanics) => new RoundLevel(number, small.Width, small.Height, small.RowsBottomFirst,
                small.Pods, small.Tray, small.Entries, roundClass: RoundLevelValidator.ClassOf(number), introduction: intro, mechanics: mechanics, latches: small.Latches,
                exception: null);
            Assert.That(RoundLevelValidator.Validate(At(61, new RoundIntroduction("latch", false), new[] { "hidden", "latch" })), Does.Contain(RoundLevelValidator.Introductions), "only its own mechanic");
            Assert.That(RoundLevelValidator.Validate(At(62, null, new[] { "latch" })), Does.Contain(RoundLevelValidator.Introductions), "not between its meetings");
            Assert.That(RoundLevelValidator.Validate(At(64, null, new[] { "latch" })), Does.Not.Contain(RoundLevelValidator.Introductions));
        }

        [Test]
        public void ShuffleNeverMovesAPodALatchHolds()
        {
            RoundLevel level = LevelOf(RoundTestLevels.ReferenceLatchCase("L0014+latch"));
            RoundLatch latch = level.Latches[0];
            for (ulong seed = 1; seed <= 6; seed++)
            {
                var t = new RoundTimeline(level);
                int[] held = t.State.Columns[latch.Column].Skip(latch.Depth).ToArray();
                RoundCommitResult r = RoundBoosters.Shuffle(t, seed);
                Assert.That(r.Accepted, Is.True, r.Reason);
                Assert.That(t.State.Columns[latch.Column].Skip(latch.Depth).ToArray(), Is.EqualTo(held), $"seed {seed}");
            }
        }

        [Test]
        public void ReturnPutsAPodBackBeforeAClosedLatchAndAnOpenLatchStaysOpen()
        {
            // the G pod of column 2 has no reachable cell yet, so it waits in its place with no courier
            RoundLevel level = Small();
            var t = new RoundTimeline(level);
            t.Commit(RoundAction.Tap(2));
            Assert.That(t.State.BlockedColumns(), Is.EqualTo(new[] { 2 }));
            Assert.That(RoundBoosters.Return(t, 2).Accepted, Is.True);
            Assert.That(RoundRules.TopPods(t.State), Does.Contain(2));
            Assert.That(t.State.BlockAt(2), Is.EqualTo(1));
            Assert.That(t.State.Latched, Is.EqualTo(new[] { true }));
            Assert.That(t.Copy().State.Latched, Is.EqualTo(new[] { true }));

            // a latch whose key is reached from the start opens as the tap brings it on top; Return keeps it open
            RoundLevel ring = WithLatches(level, new[] { new RoundLatch(2, 1, level.Width + 1) });
            var u = new RoundTimeline(ring);
            RoundCommitResult tap = u.Commit(RoundAction.Tap(2));
            Assert.That(tap.Events.Count(e => e.Type == RoundEventType.LatchOpen), Is.EqualTo(1));
            Assert.That(RoundBoosters.Return(u, 2).Accepted, Is.True);
            Assert.That(u.State.Latched, Is.EqualTo(new[] { false }));
            Assert.That(u.Copy().State.Latched, Is.EqualTo(new[] { false }), "a restored snapshot keeps it open");
            Assert.That(RoundRules.TopPods(u.State), Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void BloomBurstMayOpenTheWayToAKey()
        {
            RoundLevel level = Small();
            var t = new RoundTimeline(level);
            foreach (int pod in new[] { 0, 1 })
            {
                t.Commit(RoundAction.Tap(pod));
            }

            t.AdvanceUntilIdle();
            t.Commit(RoundAction.Tap(2));
            Assert.That(t.State.BlockedColumns(), Is.EqualTo(new[] { 2 }));
            RoundCommitResult burst = RoundBoosters.BloomBurst(t, 'G');
            Assert.That(burst.Accepted, Is.True, burst.Reason);
            int open = burst.Events.ToList().FindIndex(e => e.Type == RoundEventType.LatchOpen);
            Assert.That(open, Is.GreaterThan(0), "the burst opened the way to the key");
            Assert.That(burst.Events[open + 1].Type, Is.EqualTo(RoundEventType.Reveal));
            Assert.That(burst.Events[open + 1].Pod, Is.EqualTo(3));
            Assert.That(t.State.LatchedLeft, Is.EqualTo(0));
            t.AdvanceUntilIdle();
            AssertLatchInvariant(t.State);
        }

        // Without boosters a latch is open exactly when its column's head passed it or stands at it with the key reached,
        // and no top pod stands behind a closed latch.
        private static void AssertLatchInvariant(RoundState s)
        {
            if (s.Latched == null)
            {
                return;
            }

            IReadOnlyList<int> distance = s.Reach().Distance;
            for (int i = 0; i < s.Level.Latches.Count; i++)
            {
                RoundLatch l = s.Level.Latches[i];
                bool open = s.Heads[l.Column] > l.Depth || (s.Heads[l.Column] == l.Depth && distance[l.Key] > 0);
                Assert.That(s.Latched[i], Is.EqualTo(!open), $"latch {i}");
            }

            for (int c = 0; c < s.Columns.Length; c++)
            {
                if (s.TopOf(c) >= 0)
                {
                    Assert.That(s.Heads[c], Is.LessThan(s.BlockAt(c)));
                }
            }
        }
    }
}
