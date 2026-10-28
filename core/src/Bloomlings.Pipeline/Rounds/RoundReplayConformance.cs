using System;
using System.Collections.Generic;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Pipeline.Rounds
{
    /// <summary>
    /// The conformance replay (spec 006 FR-030–FR-033, contracts/conformance-fixture.md) on <c>Bloomlings.Core.Rounds</c>:
    /// the saved solution window by window (events hash and signature hash after each command, then outcome, end time and
    /// final signature), every recorded random game of the §5 policy (outcome, end time, final signature, win count) and
    /// every owner attempt (refusals, outcome, end time, final signature). The first difference of each kind is reported in
    /// the contract's failure format; a solution window also carries the C# canonical log, to compare with
    /// <c>node tools/rounds/conformance.mjs solution levels/L####.json --window k</c>.
    /// </summary>
    public sealed class RoundReplayConformance : IRoundConformance
    {
        public RoundReplayConformance(RoundProfile? profile = null)
        {
            Profile = profile;
        }

        /// <summary>The timing profile replayed; null is <see cref="RoundProfile.Default"/>. A changed one must fail (SC-005).</summary>
        public RoundProfile? Profile { get; }

        public IReadOnlyList<string> Check(RoundLevel level, RoundProof proof, RoundConformanceFixture fixture)
        {
            var errors = new List<string>();
            string id = "L" + level.Number.ToString("0000");
            Solution(id, level, fixture, errors);
            Random(id, level, fixture, errors);
            Owner(id, level, fixture, errors);
            return errors;
        }

        private void Solution(string id, RoundLevel level, RoundConformanceFixture fixture, List<string> errors)
        {
            var t = new RoundTimeline(level, Profile);
            var log = new RoundEventLog();
            var replay = new RoundReplay(t, log);
            var texts = new List<string>();
            var signatures = new List<string>();
            for (int k = 0; k < fixture.Windows.Count; k++)
            {
                RoundCommand command = fixture.Windows[k].Command;
                replay.Finish();
                if (k > 0)
                {
                    texts.Add(log.Take());
                }

                RoundCommitResult result = replay.Execute(Input(command));
                if (!result.Accepted)
                {
                    errors.Add($"{id} solution window {k} ({command}): refused ({result.Reason})");
                    return;
                }

                signatures.Add(RoundEvents.SignatureHash(t));
            }

            replay.Finish();
            texts.Add(log.Take());

            for (int k = 0; k < fixture.Windows.Count; k++)
            {
                RoundFixtureWindow w = fixture.Windows[k];
                string events = RoundEvents.Sha256(texts[k]);
                if (events != w.Events)
                {
                    errors.Add($"{id} solution window {k} ({w.Command}): events differ. C# canonical log of the window:\n{texts[k]}");
                    return;
                }

                if (signatures[k] != w.Signature)
                {
                    errors.Add($"{id} solution window {k} ({w.Command}): signature after the command differs");
                    return;
                }
            }

            string outcome = RoundState.StatusText(t.State.Status);
            if (outcome != fixture.SolutionOutcome)
            {
                errors.Add($"{id} solution: outcome {outcome}, reference {fixture.SolutionOutcome}");
            }

            if (t.Now != fixture.SolutionEndUs)
            {
                errors.Add($"{id} solution: ends at {t.Now} µs, reference {fixture.SolutionEndUs} µs");
            }

            if (RoundEvents.SignatureHash(t) != fixture.SolutionFinalSignature)
            {
                errors.Add($"{id} solution: final signature differs");
            }
        }

        private void Random(string id, RoundLevel level, RoundConformanceFixture fixture, List<string> errors)
        {
            int wins = 0;
            bool reported = false;
            for (int n = 0; n < fixture.Games.Count; n++)
            {
                RoundFixtureGame game = fixture.Games[n];
                uint seed = RoundRandomPolicy.SeedOf(fixture.RandomSeedBase, n);
                if (seed != game.Seed)
                {
                    errors.Add($"{id} random game {n}: seed {seed}, reference {game.Seed}");
                    return;
                }

                RoundRandomGame played = RoundRandomPolicy.Play(level, seed, profile: Profile);
                if (played.Won)
                {
                    wins++;
                }

                string outcome = RoundState.StatusText(played.Status);
                string signature = RoundEvents.SignatureHash(played.Timeline);
                if (!reported && (outcome != game.Outcome || played.EndUs != game.EndUs || signature != game.FinalSignature))
                {
                    errors.Add($"{id} random game {n} (seed {seed}): {outcome} at {played.EndUs} µs, reference {game.Outcome} at {game.EndUs} µs"
                        + (signature != game.FinalSignature ? "; final signature differs" : string.Empty));
                    reported = true;
                }
            }

            if (wins != fixture.RandomWins)
            {
                errors.Add($"{id} random test: {wins} wins of {fixture.Games.Count}, reference {fixture.RandomWins}");
            }
        }

        private void Owner(string id, RoundLevel level, RoundConformanceFixture fixture, List<string> errors)
        {
            JArray? documented = fixture.Document["owner"] as JArray;
            for (int a = 0; a < fixture.Owner.Count; a++)
            {
                RoundFixtureAttempt attempt = fixture.Owner[a];
                JArray? commands = documented != null && a < documented.Count ? documented[a]["commands"] as JArray : null;
                var t = new RoundTimeline(level, Profile);
                var replay = new RoundReplay(t);
                string where = $"{id} owner attempt {attempt.Attempt} ({attempt.Source})";
                bool stopped = false;
                for (int c = 0; c < attempt.Commands.Count && !stopped; c++)
                {
                    RoundCommand command = attempt.Commands[c];
                    RoundCommitResult result = replay.Execute(Input(command));
                    string? refused = commands != null && c < commands.Count ? (string?)commands[c]?["refused"] : null;
                    string? reason = result.Accepted ? null : result.Reason;
                    if (reason != refused)
                    {
                        errors.Add($"{where} command {c} ({command}): {(reason == null ? "accepted" : "refused (" + reason + ")")}, reference {(refused == null ? "accepted" : "refused (" + refused + ")")}");
                        stopped = true;
                    }
                }

                if (stopped)
                {
                    continue;
                }

                replay.Finish();
                string outcome = RoundState.StatusText(t.State.Status).ToLowerInvariant();
                if (outcome != attempt.Outcome)
                {
                    errors.Add($"{where}: outcome {outcome}, reference {attempt.Outcome}");
                }
                else if (t.Now != attempt.EndUs)
                {
                    errors.Add($"{where}: ends at {t.Now} µs, reference {attempt.EndUs} µs");
                }
                else if (RoundEvents.SignatureHash(t) != attempt.FinalSignature)
                {
                    errors.Add($"{where}: final signature differs");
                }
            }
        }

        private static RoundInput Input(RoundCommand command) => command.Type switch
        {
            RoundCommandType.Tap => RoundInput.Tap(command.Index, command.AtUs),
            RoundCommandType.Bridge => RoundInput.Bridge(command.Index, command.AtUs),
            _ => throw new ArgumentOutOfRangeException(nameof(command), command.Type, "Unknown round command type."),
        };
    }

    /// <summary>The definition and placement rules of <see cref="RoundLevelValidator"/> (T008), exceptions applied.</summary>
    public sealed class RoundLevelValidatorRules : IRoundDefinitionRules
    {
        public IReadOnlyList<string> Check(RoundLevel level)
        {
            var errors = new List<string>();
            foreach (string e in RoundLevelValidator.Validate(level))
            {
                errors.Add($"L{level.Number:0000} {e}");
            }

            return errors;
        }
    }
}
