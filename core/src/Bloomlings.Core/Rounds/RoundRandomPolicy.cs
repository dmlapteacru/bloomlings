using System;
using System.Collections.Generic;

namespace Bloomlings.Core.Rounds
{
    /// <summary>
    /// The PRNG of the reference's random games (<c>level-stats/strategies.mjs rng</c>): xorshift32
    /// (<c>s ^= s &lt;&lt; 13; s ^= s &gt;&gt; 17; s ^= s &lt;&lt; 5</c> on 32 bits), seed 0 replaced by 1. A choice among
    /// <c>n</c> is <c>floor(s / 2³² × n)</c>, computed exactly as <c>(s × n) &gt;&gt; 32</c>.
    /// </summary>
    public struct RoundXorShift32
    {
        private uint _s;

        public RoundXorShift32(uint seed)
        {
            _s = seed == 0 ? 1u : seed;
        }

        /// <summary>The reference's seed conversion (<c>seed &gt;&gt;&gt; 0 || 1</c>): the low 32 bits, 0 → 1.</summary>
        public static RoundXorShift32 FromSeed(long seed) => new RoundXorShift32(unchecked((uint)seed));

        public uint State => _s;

        public uint Next()
        {
            uint s = _s;
            s ^= s << 13;
            s ^= s >> 17;
            s ^= s << 5;
            _s = s;
            return s;
        }

        /// <summary>A uniform index in [0, n): the reference's <c>Math.floor(r() * n)</c>, exactly.</summary>
        public int Choice(int n)
        {
            if (n <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(n));
            }

            return (int)(((ulong)Next() * (ulong)n) >> 32);
        }
    }

    /// <summary>One game of the random policy: its seed, outcome, end time and final state.</summary>
    public sealed class RoundRandomGame
    {
        internal RoundRandomGame(uint seed, bool won, RoundTimeline timeline, int choices)
        {
            Seed = seed;
            Won = won;
            Timeline = timeline;
            Choices = choices;
        }

        public uint Seed { get; }

        public bool Won { get; }

        /// <summary>The model time at the end, in µs.</summary>
        public long EndUs => Timeline.Now;

        public RoundStatus Status => Timeline.State.Status;

        /// <summary>The finished timeline (for the extended signature and the final state).</summary>
        public RoundTimeline Timeline { get; }

        /// <summary>How many random choices the game made.</summary>
        public int Choices { get; }
    }

    /// <summary>The record of a random test: the games, wins, share, the 95 % Wilson interval and the class of the share.</summary>
    public sealed class RoundRandomRun
    {
        internal RoundRandomRun(long seedBase, IReadOnlyList<RoundRandomGame> games, int wins)
        {
            SeedBase = seedBase;
            Games = games;
            Wins = wins;
        }

        public long SeedBase { get; }

        public IReadOnlyList<RoundRandomGame> Games { get; }

        public int Runs => Games.Count;

        public int Wins { get; }

        public double Share => Runs == 0 ? 0 : (double)Wins / Runs;

        public (double Low, double High) Wilson95 => RoundRandomPolicy.Wilson(Wins, Runs);

        public RoundClass? ClassOfShare => RoundRandomPolicy.ClassOfShare(Share);

        /// <summary>True when the interval crosses a class border (5 %, 20 % or 50 %).</summary>
        public bool Borderline => RoundRandomPolicy.CrossesBorder(Wilson95);
    }

    /// <summary>
    /// The extended random policy of rules 17 §5, a port of the reference's <c>measure.mjs randomGame</c> and
    /// <c>randomTest</c> (spec 006 FR-031, research R5). Before each choice the events of the moment are processed; the
    /// bot picks uniformly among all accepted legal actions (a pod, a whole link, each bridge site) and, when a
    /// state-changing step is scheduled (any step but arrive and return), one extra option: wait until that step. With no
    /// action and a scheduled step, time moves on by itself; with neither the game is lost. Game <c>n</c> of a test uses
    /// the seed <c>seedBase × 7919 + n × 104729 + 1</c>; a test plays 100 games and widens to 400 in total when the
    /// 95 % Wilson interval crosses a class border.
    /// </summary>
    public static class RoundRandomPolicy
    {
        public const int SeedFactor = 7919;

        public const int GameFactor = 104729;

        /// <summary>The reference's game guard: at most this many loop turns per game.</summary>
        public const int Guard = 200000;

        /// <summary>The seed of game <paramref name="game"/> of a test (reference <c>seed*7919+n*104729+1</c>).</summary>
        public static uint SeedOf(long seedBase, int game) => unchecked((uint)(seedBase * SeedFactor + (long)game * GameFactor + 1));

        /// <summary>
        /// Plays one game (reference <c>randomGame</c>). <paramref name="keep"/> optionally narrows the actions (the
        /// reference uses it to allow only one bridge site).
        /// </summary>
        public static RoundRandomGame Play(RoundLevel level, uint seed, Func<RoundAction, bool>? keep = null, RoundProfile? profile = null,
            bool checkInvariants = false)
        {
            var t = new RoundTimeline(level, profile) { CheckInvariants = checkInvariants };
            var r = new RoundXorShift32(seed);
            int choices = 0;
            for (int guard = 0; guard < Guard; guard++)
            {
                SettleNow(t);
                if (t.State.Status == RoundStatus.Won)
                {
                    return new RoundRandomGame(seed, true, t, choices);
                }

                List<RoundAction> moves = t.Legal();
                if (keep != null)
                {
                    moves.RemoveAll(a => !keep(a));
                }

                bool canWait = HasStateStep(t);
                if (moves.Count == 0)
                {
                    if (t.Pending)
                    {
                        WaitEvent(t);
                        continue;
                    }

                    return new RoundRandomGame(seed, false, t, choices);
                }

                int k = r.Choice(moves.Count + (canWait ? 1 : 0));
                choices++;
                if (k == moves.Count)
                {
                    WaitEvent(t);
                    continue;
                }

                t.Commit(moves[k]);
            }

            return new RoundRandomGame(seed, false, t, choices);
        }

        /// <summary>
        /// Plays a random test (reference <c>randomTest</c>): <paramref name="first"/> games, widened to
        /// <paramref name="max"/> in total when the Wilson interval crosses a class border.
        /// </summary>
        public static RoundRandomRun Test(RoundLevel level, long seedBase = 1, int first = 100, int max = 400, RoundProfile? profile = null)
        {
            var games = new List<RoundRandomGame>(max);
            int wins = 0;
            void PlayMore(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    RoundRandomGame g = Play(level, SeedOf(seedBase, games.Count), null, profile);
                    if (g.Won)
                    {
                        wins++;
                    }

                    games.Add(g);
                }
            }

            PlayMore(first);
            if (CrossesBorder(Wilson(wins, games.Count)) && max > first)
            {
                PlayMore(max - first);
            }

            return new RoundRandomRun(seedBase, games, wins);
        }

        /// <summary>The 95 % Wilson interval of <paramref name="wins"/> in <paramref name="runs"/> (reference <c>wilson</c>).</summary>
        public static (double Low, double High) Wilson(int wins, int runs, double z = 1.96)
        {
            if (runs == 0)
            {
                return (0, 1);
            }

            double p = (double)wins / runs;
            double d = 1 + z * z / runs;
            double c = p + z * z / (2.0 * runs);
            double h = z * Math.Sqrt(p * (1 - p) / runs + z * z / (4.0 * runs * runs));
            return (Math.Max(0, (c - h) / d), Math.Min(1, (c + h) / d));
        }

        /// <summary>The class of a win share (reference <c>classOfShare</c>); null below 5 % ("too hard").</summary>
        public static RoundClass? ClassOfShare(double share) =>
            share >= 0.5 ? RoundClass.Normal : share >= 0.2 ? RoundClass.Hard : share >= 0.05 ? RoundClass.SuperHard : (RoundClass?)null;

        internal static bool CrossesBorder((double Low, double High) ci) =>
            (ci.Low < 0.05 && ci.High >= 0.05) || (ci.Low < 0.2 && ci.High >= 0.2) || (ci.Low < 0.5 && ci.High >= 0.5);

        // Processes the steps of the current moment.
        private static void SettleNow(RoundTimeline t)
        {
            while (t.Pending && t.Steps[0].At <= t.Now)
            {
                t.Advance(0);
            }
        }

        // Waits until the next state-changing step has run, then processes that moment.
        private static void WaitEvent(RoundTimeline t)
        {
            while (t.Pending)
            {
                RoundStep e = t.Steps[0];
                t.Advance(Math.Max(0, e.At - t.Now));
                if (!e.IsCourierMove)
                {
                    break;
                }
            }

            SettleNow(t);
        }

        private static bool HasStateStep(RoundTimeline t)
        {
            foreach (RoundStep step in t.Steps)
            {
                if (!step.IsCourierMove)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
