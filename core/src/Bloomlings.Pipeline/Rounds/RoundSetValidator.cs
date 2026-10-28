using System;
using System.Collections.Generic;
using System.Linq;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Pipeline.Rounds
{
    /// <summary>What <see cref="RoundSetValidator"/> runs besides the always-on file and set checks.</summary>
    public sealed class RoundValidationOptions
    {
        public IRoundDefinitionRules DefinitionRules { get; init; } = RoundSeams.DefinitionRules;

        public IRoundConformance Conformance { get; init; } = RoundSeams.Conformance;

        /// <summary>Replay every fixture (validate, publish); import leaves it to validate.</summary>
        public bool RunConformance { get; init; } = true;

        /// <summary>The palette's normal-vision gate (FR-047).</summary>
        public bool RunReadability { get; init; } = true;
    }

    /// <summary>The issues of one validation, and the palette pairs when readability ran.</summary>
    public sealed record RoundValidation(IReadOnlyList<RoundIssue> Issues, IReadOnlyList<RoundColourPair>? Pairs)
    {
        public IEnumerable<RoundIssue> Errors => Issues.Where(i => i.IsError);

        public bool Passed => !Issues.Any(i => i.IsError);
    }

    /// <summary>
    /// The checks of <c>rounds validate</c> (contracts/pipeline-rounds-cli.md, T037, T038) over a loaded
    /// <see cref="RoundContent"/>:
    /// <list type="bullet">
    /// <item>schemas (while loading) and the definition rules (<see cref="IRoundDefinitionRules"/>, T008);</item>
    /// <item>levels numbered 1..N without gaps;</item>
    /// <item>picture ↔ board: the level's rows, entry and bridge sites are the board the reference builds round its picture;</item>
    /// <item>proof completeness: the proof matches its level, its commands fit the level, its random test is consistent
    /// (share, Wilson interval, run count, class of the share = the level's class), its class checks are present;</item>
    /// <item>the fixture matches its proof (windows = commands, games = runs, wins), and replays
    /// (<see cref="IRoundConformance"/>);</item>
    /// <item>every level, proof and fixture carries the same engine fingerprints as the fixture set;</item>
    /// <item>the palette's normal-vision readability (<see cref="RoundReadability"/>).</item>
    /// </list>
    /// A documented exception (the level's <c>exception</c>) is accepted only for the rule it names.
    /// </summary>
    public static class RoundSetValidator
    {
        /// <summary>The rule a level's exception must name to keep a class that differs from its random test's.</summary>
        public const string ClassRule = "class matches the random test";

        /// <summary>The class checks a proof must carry; <c>pressure</c> is reported when missing but does not block.</summary>
        public static readonly IReadOnlyList<string> RequiredChecks = new[] { "opening", "forced", "hold", "greedyLoses", "hiddenMatters", "bridgeKind", "bridgeSingle" };

        public static RoundValidation Validate(RoundContent content, RoundValidationOptions options)
        {
            var issues = new List<RoundIssue>(content.Issues);

            // Levels 1..N, no gaps: the game plays them in sequence.
            List<int> numbers = content.Levels.Keys.ToList();
            if (numbers.Count == 0)
            {
                issues.Add(new RoundIssue("set", "sequence", "no levels"));
            }
            else
            {
                var missing = Enumerable.Range(1, numbers[numbers.Count - 1]).Except(numbers).ToList();
                if (missing.Count > 0)
                {
                    issues.Add(new RoundIssue("set", "sequence", $"levels must be 1..{numbers[numbers.Count - 1]} without gaps; missing {string.Join(", ", missing.Take(20).Select(RoundContent.Where))}{(missing.Count > 20 ? " …" : string.Empty)}"));
                }
            }

            RoundEngineVersion? reference = ReferenceEngine(content);
            var usedPictures = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<int, RoundFile<RoundLevel>> entry in content.Levels)
            {
                string where = RoundContent.Where(entry.Key);
                RoundLevel? level = entry.Value.Value;
                if (level == null)
                {
                    continue;
                }

                foreach (string message in options.DefinitionRules.Check(level))
                {
                    issues.Add(new RoundIssue(where, "definition", message));
                }

                Engine(where, "level", level.Engine, reference, issues);

                // Picture ↔ board.
                content.Pictures.TryGetValue(level.PictureId!, out RoundFile<RoundPicture>? pictureFile);
                usedPictures.Add(level.PictureId!);
                if (pictureFile == null)
                {
                    issues.Add(new RoundIssue(where, "picture", $"picture {level.PictureId} v{level.PictureVersion} is missing"));
                }
                else if (pictureFile.Value != null)
                {
                    if (pictureFile.Value.Version != level.PictureVersion)
                    {
                        issues.Add(new RoundIssue(where, "picture", $"uses {level.PictureId} v{level.PictureVersion}, but pictures/{level.PictureId}.json is v{pictureFile.Value.Version}"));
                    }
                    else
                    {
                        issues.AddRange(PictureBoard(where, level, pictureFile.Value));
                    }
                }

                // Proof.
                content.Proofs.TryGetValue(entry.Key, out RoundFile<RoundProof>? proofFile);
                RoundProof? proof = proofFile?.Value;
                if (proofFile == null)
                {
                    issues.Add(new RoundIssue(where, "proof", "the proof is missing"));
                }
                else if (proof != null)
                {
                    issues.AddRange(Proof(where, level, proof));
                    Engine(where, "proof", proof.Engine, reference, issues);
                }

                // Fixture and its replay.
                content.Fixtures.TryGetValue(entry.Key, out RoundFile<RoundConformanceFixture>? fixtureFile);
                RoundConformanceFixture? fixture = fixtureFile?.Value;
                if (fixtureFile == null)
                {
                    issues.Add(new RoundIssue(where, "fixture", "the conformance fixture is missing"));
                }
                else if (fixture != null)
                {
                    Engine(where, "fixture", fixture.Engine, reference, issues);
                    if (fixture.DefinitionVersion != level.DefinitionVersion)
                    {
                        issues.Add(new RoundIssue(where, "fixture", $"fixture of definitionVersion {fixture.DefinitionVersion}, level is {level.DefinitionVersion}"));
                    }

                    if (proof != null)
                    {
                        issues.AddRange(Fixture(where, proof, fixture));
                        if (options.RunConformance)
                        {
                            foreach (string message in options.Conformance.Check(level, proof, fixture))
                            {
                                issues.Add(new RoundIssue(where, "conformance", message));
                            }
                        }
                    }
                }
            }

            foreach (int number in content.Proofs.Keys.Where(n => !content.Levels.ContainsKey(n)))
            {
                issues.Add(new RoundIssue(RoundContent.Where(number), "proof", "a proof without its level"));
            }

            foreach (int number in content.Fixtures.Keys.Where(n => !content.Levels.ContainsKey(n)))
            {
                issues.Add(new RoundIssue(RoundContent.Where(number), "fixture", "a fixture without its level"));
            }

            foreach (string id in content.Pictures.Keys.Where(id => !usedPictures.Contains(id)))
            {
                issues.Add(new RoundIssue(id, "picture", "no level uses this picture", IsError: false));
            }

            IReadOnlyList<RoundColourPair>? pairs = null;
            if (options.RunReadability)
            {
                pairs = RoundReadability.Analyze();
                issues.AddRange(RoundReadability.Gate(pairs));
            }

            return new RoundValidation(issues, pairs);
        }

        /// <summary>
        /// The picture ↔ board check: the level's board is exactly the reference's <c>boardOf</c> of its picture and its
        /// overlay, if any (size, rows, the entry, the bridge sites and anchors).
        /// </summary>
        public static IEnumerable<RoundIssue> PictureBoard(string where, RoundLevel level, RoundPicture picture)
        {
            RoundBoardShape board;
            try
            {
                board = RoundPictureBoard.Of(picture, level.Overlay);
            }
            catch (ArgumentException e)
            {
                return new[] { new RoundIssue(where, "picture-board", e.Message) };
            }

            return PictureBoard(where, level, picture, board);
        }

        private static IEnumerable<RoundIssue> PictureBoard(string where, RoundLevel level, RoundPicture picture, RoundBoardShape board)
        {
            string what = level.Overlay == null ? $"picture {picture.Id} v{picture.Version}" : $"picture {picture.Id} v{picture.Version} with its overlay";
            if (board.Width != level.Width || board.Height != level.Height)
            {
                yield return new RoundIssue(where, "picture-board", $"board {level.Width}×{level.Height}, but {what} ({picture.Width}×{picture.Height}) makes {board.Width}×{board.Height}");
                yield break;
            }

            for (int y = 0; y < board.Height; y++)
            {
                if (!string.Equals(board.RowsBottomFirst[y], level.RowsBottomFirst[y], StringComparison.Ordinal))
                {
                    yield return new RoundIssue(where, "picture-board", $"row {y} (bottom first) is '{level.RowsBottomFirst[y]}', {what} makes '{board.RowsBottomFirst[y]}'");
                    yield break;
                }
            }

            if (!board.Entries.SequenceEqual(level.Entries))
            {
                yield return new RoundIssue(where, "picture-board", $"entries [{string.Join(", ", level.Entries)}], {what} makes [{string.Join(", ", board.Entries)}]");
            }

            string Sites(IReadOnlyList<RoundBridgeChoice>? sites) => sites == null ? "none" : string.Join(", ", sites.Select(c => $"{c.Cell}/{c.Anchor}"));
            if (Sites(board.Bridge) != Sites(level.Bridge))
            {
                yield return new RoundIssue(where, "picture-board", $"bridge sites (cell/anchor) {Sites(level.Bridge)}, {what} makes {Sites(board.Bridge)}");
            }
        }

        /// <summary>Proof completeness against its level.</summary>
        public static IEnumerable<RoundIssue> Proof(string where, RoundLevel level, RoundProof proof)
        {
            if (proof.DefinitionVersion != level.DefinitionVersion)
            {
                yield return new RoundIssue(where, "proof", $"proof of definitionVersion {proof.DefinitionVersion}, level is {level.DefinitionVersion}");
            }

            if (!RoundJson.SameEngine(proof.Engine, level.Engine))
            {
                yield return new RoundIssue(where, "proof", "the proof's engine fingerprints differ from the level's");
            }

            long last = 0;
            IReadOnlyList<RoundCommand> commands = proof.Solution.Commands;
            for (int i = 0; i < commands.Count; i++)
            {
                RoundCommand command = commands[i];
                if (command.Type == RoundCommandType.Tap && command.Index >= level.Pods.Count)
                {
                    yield return new RoundIssue(where, "proof", $"solution command {i}: {command} names pod {command.Index} of {level.Pods.Count}");
                }
                else if (command.Type == RoundCommandType.Bridge && (level.Bridge == null || command.Index >= level.Bridge.Count))
                {
                    yield return new RoundIssue(where, "proof", $"solution command {i}: {command} names a bridge site the level does not have");
                }

                if (command.AtUs < last)
                {
                    yield return new RoundIssue(where, "proof", $"solution command {i}: {command} is earlier than the command before it ({last} µs)");
                }

                last = Math.Max(last, command.AtUs);
            }

            if (proof.Solution.EndUs < last)
            {
                yield return new RoundIssue(where, "proof", $"the solution ends at {proof.Solution.EndUs} µs, before its last command ({last} µs)");
            }

            RoundRandomTest random = proof.RandomTest;
            if (random.Wins > random.Runs)
            {
                yield return new RoundIssue(where, "proof", $"random test: {random.Wins} wins of {random.Runs} runs");
                yield break;
            }

            double share = (double)random.Wins / random.Runs;
            if (Math.Abs((double)random.Share - share) > 0.0005 + 1e-9)
            {
                yield return new RoundIssue(where, "proof", $"random test: share {random.Share} is not {random.Wins}/{random.Runs}");
            }

            (double low, double high) = Wilson(random.Wins, random.Runs);
            if (Math.Abs((double)random.WilsonLow - low) > 0.0011 || Math.Abs((double)random.WilsonHigh - high) > 0.0011)
            {
                yield return new RoundIssue(where, "proof", $"random test: wilson95 [{random.WilsonLow}, {random.WilsonHigh}] is not the interval of {random.Wins}/{random.Runs} ([{low:0.000}, {high:0.000}])");
            }

            bool crosses = CrossesBorder(low, high);
            if (random.Runs == 100 && crosses)
            {
                yield return new RoundIssue(where, "proof", "random test: the interval of 100 runs crosses a class border, so the test needs 400 runs (17 §5)");
            }

            if (random.Borderline.HasValue && random.Borderline.Value != crosses)
            {
                yield return new RoundIssue(where, "proof", $"random test: borderline is {random.Borderline.Value.ToString().ToLowerInvariant()}, the interval says {crosses.ToString().ToLowerInvariant()}");
            }

            RoundClass? measured = ClassOfShare(share);
            if (measured == null)
            {
                yield return new RoundIssue(where, "proof", $"random test: share {share:0.000} is below every class (too hard)");
            }
            else if (measured != random.ClassOfShare)
            {
                yield return new RoundIssue(where, "proof", $"random test: classOfShare {RoundJson.ClassName(random.ClassOfShare)}, the share {share:0.000} gives {RoundJson.ClassName(measured.Value)}");
            }

            if (random.ClassOfShare != level.Class && !Excepted(level, ClassRule))
            {
                yield return new RoundIssue(where, "proof", $"{ClassRule}: the level is {RoundJson.ClassName(level.Class)}, its random test {RoundJson.ClassName(random.ClassOfShare)}");
            }

            foreach (string check in RequiredChecks.Where(c => !proof.HasCheck(c)))
            {
                yield return new RoundIssue(where, "proof", $"the class check '{check}' is missing");
            }

            if (!proof.HasCheck("pressure"))
            {
                yield return new RoundIssue(where, "proof", "the class check 'pressure' is missing", IsError: false);
            }

            if (proof.Audit.Result == RoundAudit.OkWithException && level.Exception == null)
            {
                yield return new RoundIssue(where, "proof", "the audit is 'OK with exception', but the level has no exception");
            }
            else if (proof.Audit.Result == RoundAudit.Ok && level.Exception != null)
            {
                yield return new RoundIssue(where, "proof", $"the level has an exception ({level.Exception.Rule}), but the audit says 'OK'", IsError: false);
            }
        }

        /// <summary>The fixture's structure against its proof (the replay itself is <see cref="IRoundConformance"/>'s).</summary>
        public static IEnumerable<RoundIssue> Fixture(string where, RoundProof proof, RoundConformanceFixture fixture)
        {
            IReadOnlyList<RoundCommand> commands = proof.Solution.Commands;
            if (fixture.Windows.Count != commands.Count)
            {
                yield return new RoundIssue(where, "fixture", $"{fixture.Windows.Count} solution windows for {commands.Count} solution commands");
            }
            else
            {
                for (int i = 0; i < commands.Count; i++)
                {
                    if (!fixture.Windows[i].Command.Equals(commands[i]))
                    {
                        yield return new RoundIssue(where, "fixture", $"solution window {i} is {fixture.Windows[i].Command}, the proof's command is {commands[i]}");
                        break;
                    }
                }
            }

            if (fixture.SolutionOutcome != "Won" || fixture.SolutionEndUs != proof.Solution.EndUs)
            {
                yield return new RoundIssue(where, "fixture", $"the fixture's solution ends {fixture.SolutionOutcome} at {fixture.SolutionEndUs} µs, the proof's Won at {proof.Solution.EndUs} µs");
            }

            RoundRandomTest random = proof.RandomTest;
            if (fixture.RandomSeedBase != random.SeedBase || fixture.RandomRuns != random.Runs)
            {
                yield return new RoundIssue(where, "fixture", $"random games of seed base {fixture.RandomSeedBase} × {fixture.RandomRuns}, the proof's {random.SeedBase} × {random.Runs}");
            }

            if (fixture.Games.Count != random.Runs)
            {
                yield return new RoundIssue(where, "fixture", $"{fixture.Games.Count} random games recorded for {random.Runs} runs");
            }

            int won = fixture.Games.Count(g => g.Outcome == "Won");
            if (fixture.RandomWins != won || fixture.RandomWins != random.Wins)
            {
                yield return new RoundIssue(where, "fixture", $"random wins: fixture says {fixture.RandomWins}, its games {won}, the proof {random.Wins}");
            }
        }

        /// <summary>The 95 % Wilson interval as <c>tools/rounds/measure.mjs</c> computes it (z = 1.96), clamped to 0..1.</summary>
        public static (double Low, double High) Wilson(int wins, int runs)
        {
            if (runs == 0)
            {
                return (0, 1);
            }

            const double z = 1.96;
            double p = (double)wins / runs;
            double d = 1 + (z * z / runs);
            double c = p + (z * z / (2 * runs));
            double h = z * Math.Sqrt((p * (1 - p) / runs) + (z * z / (4.0 * runs * runs)));
            return (Math.Max(0, (c - h) / d), Math.Min(1, (c + h) / d));
        }

        /// <summary>The class of a random-test share (17 §5): Normal ≥ 50 %, Hard ≥ 20 %, Super Hard ≥ 5 %, else none.</summary>
        public static RoundClass? ClassOfShare(double share) =>
            share >= 0.5 ? RoundClass.Normal : share >= 0.2 ? RoundClass.Hard : share >= 0.05 ? RoundClass.SuperHard : (RoundClass?)null;

        private static bool CrossesBorder(double low, double high) => new[] { 0.05, 0.2, 0.5 }.Any(x => low < x && high >= x);

        private static bool Excepted(RoundLevel level, string rule) =>
            level.Exception != null && string.Equals(level.Exception.Rule, rule, StringComparison.Ordinal);

        /// <summary>The fixture set's engine: the first fixture's, or the first level's when there are no fixtures.</summary>
        private static RoundEngineVersion? ReferenceEngine(RoundContent content) =>
            content.Fixtures.Values.Select(f => f.Value?.Engine).FirstOrDefault(e => e != null)
            ?? content.Levels.Values.Select(l => l.Value?.Engine).FirstOrDefault(e => e != null);

        private static void Engine(string where, string what, RoundEngineVersion? engine, RoundEngineVersion? reference, List<RoundIssue> issues)
        {
            if (reference != null && !RoundJson.SameEngine(engine, reference))
            {
                issues.Add(new RoundIssue(where, "engine", $"the {what}'s engine fingerprints differ from the fixture set's (rules {Short(engine?.Rules)} vs {Short(reference.Rules)}, timeline {Short(engine?.Timeline)} vs {Short(reference.Timeline)})"));
            }
        }

        private static string Short(string? hash) => hash == null ? "none" : hash.Substring(0, Math.Min(8, hash.Length));
    }
}
