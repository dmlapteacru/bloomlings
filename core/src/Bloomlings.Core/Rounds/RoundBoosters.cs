using System;
using System.Collections.Generic;
using Bloomlings.Core.Random;

namespace Bloomlings.Core.Rounds
{
    /// <summary>
    /// The four boosters on round levels, as commands of the live timeline at its current model time (spec 006 FR-012,
    /// research R9, data model "Booster commands"). The reference engine has no boosters, so these rules are pinned by
    /// rule tests and golden replays, not by conformance; no proof uses them.
    ///
    /// Boosters are allowed in any status but Won, so the jam card can offer them when the places jam or nothing can be
    /// tapped (a jam or a stuck position is the rescue's moment).
    /// - <b>Extra Slot</b>: once per attempt, a sixth place at once, the last one.
    /// - <b>Return</b>: a pod waiting in a place with unsent work and no courier on the way (for a link, every member)
    ///   goes back to the top of its column with its unsent count; its place frees and its pending check step is dropped.
    ///   Cleared cells stay cleared.
    /// - <b>Shuffle</b>: the revealed single pods still in the columns trade positions (seeded, see
    ///   <see cref="ShuffleSeed"/>); hidden pods and links keep theirs, so hidden stay hidden and links stay paired at equal
    ///   depth in different columns. An arrangement is taken only when the patient solver finds a win from it, else the
    ///   order stays.
    /// - <b>Bloom Burst</b>: for a material with a revealed pod still to do, every cell of it that no courier reserved
    ///   clears, and the material's pods lose that many from their unsent counts (places by age, then the columns from the
    ///   top), which by exact accounting brings them all to 0. Pods in places then release by the normal release rule.
    ///   Cells reserved by a courier stay with it.
    /// </summary>
    public static class RoundBoosters
    {
        /// <summary>The number of places after Extra Slot.</summary>
        public const int ExtraCapacity = 6;

        /// <summary>The solver budget of a Shuffle's winnability check.</summary>
        public const int ShuffleNodeBudget = 20000;

        /// <summary>How many arrangements a Shuffle tries before it keeps the order.</summary>
        public const int ShuffleAttempts = 4;

        public static RoundRefusal CanExtraSlot(RoundTimeline t)
        {
            if (t.State.Status == RoundStatus.Won)
            {
                return RoundRefusal.LevelWon;
            }

            return t.ExtraSlotUsed || t.State.Slots.Length >= ExtraCapacity ? RoundRefusal.AlreadyUsed : RoundRefusal.None;
        }

        /// <summary>Adds the sixth place at the end.</summary>
        public static RoundCommitResult ExtraSlot(RoundTimeline t)
        {
            RoundRefusal reason = CanExtraSlot(t);
            if (reason != RoundRefusal.None)
            {
                return RoundCommitResult.Refused(reason);
            }

            t.ClearLogs();
            RoundState s = t.State;
            var slots = new int[s.Slots.Length + 1];
            Array.Copy(s.Slots, slots, s.Slots.Length);
            slots[slots.Length - 1] = -1;
            s.Slots = slots;
            t.ExtraSlotUsed = true;
            t.EventLog.Add(RoundEvent.ExtraSlotAdded(t.Now, slots.Length - 1));
            t.UpdateStatus();
            t.Assert();
            return new RoundCommitResult(RoundRefusal.None, t.EventLog.ToArray());
        }

        public static RoundRefusal CanReturn(RoundTimeline t, int pod)
        {
            RoundState s = t.State;
            if (s.Status == RoundStatus.Won)
            {
                return RoundRefusal.LevelWon;
            }

            if (pod < 0 || pod >= s.Level.Pods.Count)
            {
                return RoundRefusal.UnknownPod;
            }

            foreach (int member in s.Prepared.Groups[pod])
            {
                if (s.SlotOf(member) < 0 || s.Remaining[member] <= 0)
                {
                    return RoundRefusal.NotReturnable;
                }

                if (t.InFlight[member] > 0)
                {
                    return RoundRefusal.CourierInFlight;
                }
            }

            return RoundRefusal.None;
        }

        /// <summary>Sends the pod (with its link) back to the top of its column.</summary>
        public static RoundCommitResult Return(RoundTimeline t, int pod)
        {
            RoundRefusal reason = CanReturn(t, pod);
            if (reason != RoundRefusal.None)
            {
                return RoundCommitResult.Refused(reason);
            }

            t.ClearLogs();
            RoundState s = t.State;
            foreach (int member in s.Prepared.Groups[pod])
            {
                int slot = s.SlotOf(member);
                s.Slots[slot] = -1;
                int column = s.ColumnOf[member];
                List<int> stack = s.Columns[column];
                int at = stack.IndexOf(member);
                if (at < 0 || at >= s.Heads[column])
                {
                    throw new InvalidOperationException("A pod in a place is not among its column's taken pods.");
                }

                // The taken pods stay before the head; the returned one becomes the new top.
                stack.RemoveAt(at);
                s.Heads[column]--;
                stack.Insert(s.Heads[column], member);
                t.RemoveSteps(RoundStepType.Check, member);
                t.EventLog.Add(RoundEvent.Recall(t.Now, member, slot, column));
            }

            s.ColumnsChanged = true;
            t.UpdateStatus();
            t.Assert();
            return new RoundCommitResult(RoundRefusal.None, t.EventLog.ToArray());
        }

        /// <summary>The positions (column, index) of the pods a Shuffle may move: revealed single pods in the columns.</summary>
        private static List<(int Column, int Index)> Movable(RoundState s)
        {
            var positions = new List<(int, int)>();
            for (int c = 0; c < s.Columns.Length; c++)
            {
                List<int> stack = s.Columns[c];
                for (int k = s.Heads[c]; k < stack.Count; k++)
                {
                    int pod = stack[k];
                    if (s.Revealed[pod] && s.Prepared.Groups[pod].Length == 1)
                    {
                        positions.Add((c, k));
                    }
                }
            }

            return positions;
        }

        public static RoundRefusal CanShuffle(RoundTimeline t)
        {
            if (t.State.Status == RoundStatus.Won)
            {
                return RoundRefusal.LevelWon;
            }

            return Movable(t.State).Count < 2 ? RoundRefusal.NothingToShuffle : RoundRefusal.None;
        }

        /// <summary>
        /// The seed of a level's <paramref name="index"/>-th Shuffle in an attempt: the level's number and definition
        /// version mixed by SplitMix64, so the same commands always give the same arrangement.
        /// </summary>
        public static ulong ShuffleSeed(RoundLevel level, int index)
        {
            var mix = new SplitMix64(((ulong)(uint)level.Number << 32) ^ ((ulong)(uint)level.DefinitionVersion << 16) ^ (ulong)(uint)index ^ 0x5348_5546_464C_4552UL);
            return mix.Next();
        }

        /// <summary>Shuffles with the level's seed for this attempt's next Shuffle.</summary>
        public static RoundCommitResult Shuffle(RoundTimeline t) => Shuffle(t, ShuffleSeed(t.Level, t.ShufflesUsed));

        /// <summary>
        /// Shuffles with an explicit seed: up to <see cref="ShuffleAttempts"/> Fisher–Yates arrangements of the movable
        /// pods (xoshiro256**), the first one from which the patient solver finds a win is taken; if none is, the order
        /// stays (the event says whether it changed).
        /// </summary>
        public static RoundCommitResult Shuffle(RoundTimeline t, ulong seed, int budget = ShuffleNodeBudget, int attempts = ShuffleAttempts)
        {
            RoundRefusal reason = CanShuffle(t);
            if (reason != RoundRefusal.None)
            {
                return RoundCommitResult.Refused(reason);
            }

            RoundState s = t.State;
            List<(int Column, int Index)> positions = Movable(s);
            var original = new int[positions.Count];
            for (int i = 0; i < original.Length; i++)
            {
                original[i] = s.Columns[positions[i].Column][positions[i].Index];
            }

            var rng = new Xoshiro256StarStar(seed);
            var solver = new RoundSolver(budget);
            int[]? chosen = null;
            for (int attempt = 0; attempt < attempts && chosen == null; attempt++)
            {
                var order = (int[])original.Clone();
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = (int)rng.NextBelow((ulong)(i + 1));
                    int tmp = order[i];
                    order[i] = order[j];
                    order[j] = tmp;
                }

                if (SameMaterialsAndCounts(s, original, order))
                {
                    continue;
                }

                RoundTimeline trial = t.Copy();
                trial.CheckInvariants = false;
                Place(trial.State, positions, order);
                if (solver.Winnable(trial) == true)
                {
                    chosen = order;
                }
            }

            t.ClearLogs();
            if (chosen != null)
            {
                Place(s, positions, chosen);
                s.ColumnsChanged = true;
            }

            t.ShufflesUsed++;
            t.EventLog.Add(RoundEvent.Shuffled(t.Now, chosen != null));
            t.UpdateStatus();
            t.Assert();
            return new RoundCommitResult(RoundRefusal.None, t.EventLog.ToArray());
        }

        // An arrangement that shows the same material and count at every position changes nothing a player can see.
        private static bool SameMaterialsAndCounts(RoundState s, int[] a, int[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (s.Prepared.Colours[a[i]] != s.Prepared.Colours[b[i]] || s.Remaining[a[i]] != s.Remaining[b[i]])
                {
                    return false;
                }
            }

            return true;
        }

        private static void Place(RoundState s, List<(int Column, int Index)> positions, int[] order)
        {
            for (int i = 0; i < order.Length; i++)
            {
                s.Columns[positions[i].Column][positions[i].Index] = order[i];
                s.ColumnOf[order[i]] = positions[i].Column;
            }
        }

        public static RoundRefusal CanBloomBurst(RoundTimeline t, char code)
        {
            RoundState s = t.State;
            if (s.Status == RoundStatus.Won)
            {
                return RoundRefusal.LevelWon;
            }

            int material = RoundMaterial.IndexOf(code);
            if (material < 0 || !HasRevealedLivePod(s, material))
            {
                return RoundRefusal.MaterialNotRevealed;
            }

            int[] reserved = t.ReservedArray;
            for (int c = 0; c < s.Board.Length; c++)
            {
                if (s.Board[c] == material && reserved[c] < 0)
                {
                    return RoundRefusal.None;
                }
            }

            return RoundRefusal.NoFreeCells;
        }

        // A revealed pod of the material with unsent work, in a place or still in a column.
        private static bool HasRevealedLivePod(RoundState s, int material)
        {
            for (int p = 0; p < s.Remaining.Length; p++)
            {
                if (s.Prepared.Colours[p] == material && s.Revealed[p] && s.Remaining[p] > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Clears every free cell of the material and settles its pods' unsent counts.</summary>
        public static RoundCommitResult BloomBurst(RoundTimeline t, char code)
        {
            RoundRefusal reason = CanBloomBurst(t, code);
            if (reason != RoundRefusal.None)
            {
                return RoundCommitResult.Refused(reason);
            }

            t.ClearLogs();
            RoundState s = t.State;
            int material = RoundMaterial.IndexOf(code);
            int[] reserved = t.ReservedArray;
            var cells = new List<int>();
            for (int c = 0; c < s.Board.Length; c++)
            {
                if (s.Board[c] == material && reserved[c] < 0)
                {
                    cells.Add(c);
                }
            }

            t.EventLog.Add(RoundEvent.Burst(t.Now, code, cells.Count));
            foreach (int c in cells)
            {
                s.Board[c] = RoundLevel.Open;
                t.CountClear(c);
                t.EventLog.Add(RoundEvent.BurstCell(t.Now, c));
            }

            s.BoardChanged();

            // Settle the material's unsent counts: places by age first, then the columns from the top.
            int left = cells.Count;
            foreach (int pod in s.ActiveByAge())
            {
                left = Take(s, pod, material, left);
            }

            for (int c = 0; c < s.Columns.Length; c++)
            {
                List<int> stack = s.Columns[c];
                for (int k = s.Heads[c]; k < stack.Count; k++)
                {
                    left = Take(s, stack[k], material, left);
                }
            }

            if (left != 0)
            {
                throw new InvalidOperationException("Bloom Burst: work was created or lost.");
            }

            t.Dispatch();
            t.Assert();
            return new RoundCommitResult(RoundRefusal.None, t.EventLog.ToArray());
        }

        private static int Take(RoundState s, int pod, int material, int left)
        {
            if (left == 0 || s.Prepared.Colours[pod] != material)
            {
                return left;
            }

            int take = Math.Min(left, s.Remaining[pod]);
            s.Remaining[pod] -= take;
            return left - take;
        }
    }
}
