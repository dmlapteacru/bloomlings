using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Playtest.Check
{
    /// <summary>
    /// The round level screen's clock and animator (spec 006 T031, SC-007): every round level's saved solution from
    /// <c>content/game/rounds/proofs/</c> is played through <see cref="RoundPlay"/> frame by frame, as the level screen plays
    /// it, at ×1, at ×2 (the speed-up and Auto) and with uneven frame lengths; each run must win with the same final rules
    /// state as the reference's patient replay (<see cref="RoundReplay"/>), commit each command at its recorded model time,
    /// end with the animator showing the rules' state (no walker left, every cleared cube cleared), and its recorded
    /// commands must replay to the same state.
    /// </summary>
    internal static class RoundAnimatorChecks
    {
        public static IEnumerable<string> Run(string root, out int levels)
        {
            var failures = new List<string>();
            levels = 0;
            string folder = Path.Combine(root, "content/game/rounds");
            string proofs = Path.Combine(folder, "proofs");
            if (!Directory.Exists(proofs))
            {
                failures.Add("no round proofs in " + proofs);
                return failures;
            }

            string[] files = Directory.GetFiles(proofs, "L*.json");
            Array.Sort(files, StringComparer.Ordinal);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                RoundProof proof = RoundJson.ReadProof(File.ReadAllText(file));
                RoundLevel level = RoundJson.ReadLevel(File.ReadAllText(Path.Combine(folder, "levels", name)));
                levels++;
                string reference = Reference(level, proof);
                foreach ((string label, Func<int, double> frame, int speed) in new (string, Func<int, double>, int)[]
                {
                    ("x1 at 60 fps", _ => 1.0 / 60.0, 1),
                    ("x2 at 60 fps", _ => 1.0 / 60.0, 2),
                    ("x2 at uneven frames", k => (k % 7 == 0 ? 0.1 : 0.004 + ((k * 37 % 23) / 1000.0)), 2),
                })
                {
                    string? failure = Animated(level, proof, reference, frame, speed);
                    if (failure != null)
                    {
                        failures.Add(name + " " + label + ": " + failure);
                    }
                }
            }

            return failures;
        }

        /// <summary>
        /// The guided spotlights on the round levels (spec 006 FR-061, T030), as a new player meets them going from L1 up,
        /// each level played by its saved solution (patient frames at ×1): Level 1's entry and forced first tap on a top pod
        /// whose cubes are in reach, each booster's demo at its unlock level (Extra Slot L3, Shuffle L4, Bloom Burst L9 at the
        /// start; Return once a pod waits in a place with no walker on the way, from L6), and the blocked-entry spotlight on
        /// the first level that starts with a top pod's material out of reach. Prints where each step shows.
        /// </summary>
        public static IEnumerable<string> GuideScan(string root, out string summary)
        {
            var failures = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var shown = new List<string>();
            string folder = Path.Combine(root, "content/game/rounds");
            var unlockAt = new Dictionary<BoosterKind, int> { [BoosterKind.ExtraSlot] = 3, [BoosterKind.Shuffle] = 4, [BoosterKind.Return] = 6, [BoosterKind.BloomBurst] = 9 };
            for (int n = 1; File.Exists(Path.Combine(folder, "levels", $"L{n:0000}.json")); n++)
            {
                RoundLevel level = RoundJson.ReadLevel(File.ReadAllText(Path.Combine(folder, "levels", $"L{n:0000}.json")));
                RoundProof proof = RoundJson.ReadProof(File.ReadAllText(Path.Combine(folder, "proofs", $"L{n:0000}.json")));
                int number = n;
                Func<BoosterKind, bool> unlocked = kind => number >= unlockAt[kind];
                var play = new RoundPlay(level);
                IReadOnlyList<GuideStep> start = GuideTour.AtStart(n, seen.Contains, unlocked, play.Timeline);
                foreach (GuideStep step in start)
                {
                    shown.Add($"L{n} {step.Kind}{(step.Booster.HasValue ? " " + step.Booster.Value : string.Empty)}");
                    seen.Add(step.DemoId);
                }

                if (n == 1)
                {
                    int pod = GuideTour.FirstTapPod(play.Timeline);
                    if (start.Count < 2 || start[0].Kind != GuideKind.Entry || start[1].Kind != GuideKind.FirstTap || pod < 0)
                    {
                        failures.Add("Level 1 does not open on the entry and the forced first tap");
                    }
                }

                foreach (RoundCommand c in proof.Solution.Commands)
                {
                    for (int k = 0; k < 100_000 && play.Timeline.Pending; k++)
                    {
                        play.Advance(1.0 / 60.0, 1);
                        IReadOnlyList<GuideStep> later = GuideTour.WhenSettled(seen.Contains, unlocked, play.Timeline);
                        if (later.Count > 0)
                        {
                            shown.Add($"L{n} {later[0].Booster} (after {play.Inputs.Count} taps)");
                            seen.Add(later[0].DemoId);
                        }
                    }

                    play.Apply(c.Type == RoundCommandType.Tap ? RoundInput.Tap(c.Index, 0) : RoundInput.Bridge(c.Index, 0));
                }
            }

            foreach ((BoosterKind kind, int at) in unlockAt)
            {
                string first = shown.FirstOrDefault(s => s.Contains(" " + kind, StringComparison.Ordinal)) ?? string.Empty;
                if (kind != BoosterKind.Return && !first.StartsWith($"L{at} Booster", StringComparison.Ordinal))
                {
                    failures.Add($"{kind}'s demo does not open at L{at} ({(first.Length == 0 ? "never" : first)})");
                }
                else if (kind == BoosterKind.Return && first.Length == 0)
                {
                    failures.Add("Return's demo never finds a pod to send back on L6–L20");
                }
            }

            if (!shown.Any(s => s.Contains(" Blocked", StringComparison.Ordinal)))
            {
                failures.Add("no round level shows the blocked-entry spotlight");
            }

            summary = string.Join("; ", shown);
            return failures;
        }

        /// <summary>The reference: the saved solution's patient replay, its final signature.</summary>
        private static string Reference(RoundLevel level, RoundProof proof)
        {
            var replay = new RoundReplay(new RoundTimeline(level));
            foreach (RoundCommand c in proof.Solution.Commands)
            {
                replay.Execute(Input(c), patient: true);
            }

            replay.Finish();
            return RoundEvents.Signature(replay.Timeline);
        }

        private static RoundInput Input(RoundCommand c) => c.Type == RoundCommandType.Tap ? RoundInput.Tap(c.Index, c.AtUs) : RoundInput.Bridge(c.Index, c.AtUs);

        /// <summary>One frame-by-frame run of the solution, as the level screen plays it; null when it holds.</summary>
        private static string? Animated(RoundLevel level, RoundProof proof, string reference, Func<int, double> frame, int speed)
        {
            var play = new RoundPlay(level);
            int k = 0;
            void Frame() => play.Advance(frame(k++), speed);
            const int guard = 2_000_000;
            foreach (RoundCommand c in proof.Solution.Commands)
            {
                // The patient player: each command once the work before it has settled.
                while (play.Timeline.Pending && k < guard)
                {
                    Frame();
                }

                if (play.Timeline.Now != c.AtUs)
                {
                    return "command " + c + " came at model time " + play.Timeline.Now + " µs";
                }

                RoundCommitResult result = play.Apply(Input(c));
                if (!result.Accepted)
                {
                    return "command " + c + " refused: " + result.Reason;
                }

                if (!play.Animator.ShowsRulesState(play.Timeline, out string after))
                {
                    return "after " + c + ": " + after;
                }
            }

            while ((play.Timeline.Pending || play.Animator.Busy) && k < guard)
            {
                Frame();
            }

            if (play.Status != RoundStatus.Won)
            {
                return "ended " + play.Status;
            }

            if (RoundEvents.Signature(play.Timeline) != reference)
            {
                return "the final state differs from the reference replay";
            }

            if (!play.Animator.ShowsRulesState(play.Timeline, out string difference))
            {
                return "the animator does not show the rules' state: " + difference;
            }

            if (play.Animator.Walkers.Count != 0 || !play.Settled || !play.Auto)
            {
                return "not settled at the end (walkers " + play.Animator.Walkers.Count + ", Auto " + play.Auto + ")";
            }

            for (int cell = 0; cell < level.CellCount; cell++)
            {
                if (level.Prepare().Board[cell] >= 0 && play.Animator.ClearedAt(cell) < 0)
                {
                    return "cube " + cell + " never showed its clear";
                }
            }

            // The recorded commands (model times) replay to the same state.
            var again = new RoundReplay(new RoundTimeline(level));
            foreach (RoundInput input in play.Inputs)
            {
                if (!again.Execute(input).Accepted)
                {
                    return "the recorded " + input + " is refused on replay";
                }
            }

            again.Finish();
            return RoundEvents.Signature(again.Timeline) == reference ? null : "the recorded commands replay to another state";
        }
    }
}
