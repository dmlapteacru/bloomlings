using System;
using System.Collections.Generic;
using System.IO;
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
