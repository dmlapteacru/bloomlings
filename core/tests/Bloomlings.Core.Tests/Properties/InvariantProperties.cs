using System.Collections.Generic;
using System.Linq;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Tests.Fixtures;
using Bloomlings.Core.Tray;
using Bloomlings.Core.Variants;
using FsCheck.NUnit;

namespace Bloomlings.Core.Tests.Properties
{
    /// <summary>
    /// Invariants over random small levels and random command sequences (T029). FsCheck supplies the seeds; the
    /// seeded generator in <see cref="RandomLevels"/> builds the picture, mapping, pods, tray and commands.
    /// </summary>
    public class InvariantProperties
    {
        private const int Runs = 300;

        [Property(MaxTest = Runs)]
        public bool PodCounts_AreNeverNegative(ulong seed) =>
            Replay(seed, (session, _) => session.View.PodIds.All(id => session.View.Pod(id).Remaining >= 0));

        [Property(MaxTest = Runs)]
        public bool RemainingPodWork_EqualsRemainingLayers_PerVariant(ulong seed) =>
            Replay(seed, (session, _) => AccountingHolds(session));

        [Property(MaxTest = Runs)]
        public bool SameCommands_GiveSameHashesAndEvents(ulong seed)
        {
            LevelSession a = RandomLevels.Create(seed);
            LevelSession b = RandomLevels.Create(seed);
            foreach (Command command in RandomLevels.Commands(seed, a))
            {
                CommandResult ra = a.Apply(command);
                CommandResult rb = b.Apply(command);
                if (ra.Accepted != rb.Accepted || a.StateHash != b.StateHash || Digest(ra) != Digest(rb))
                {
                    return false;
                }
            }

            return true;
        }

        [Property(MaxTest = Runs)]
        public bool WonAndJammed_AreNeverBothTrue(ulong seed) =>
            Replay(seed, (session, result) =>
            {
                bool won = result.Events.OfType<LevelWon>().Any();
                bool lost = result.Events.OfType<LevelJammed>().Any() || result.Events.OfType<LevelStuck>().Any();
                return !(won && lost) && (!won || session.Status == LevelStatus.Won);
            });

        [Property(MaxTest = Runs)]
        public bool IncrementalHash_EqualsFullRecompute(ulong seed) =>
            Replay(seed, (session, _) => session.StateHash == session.ComputeFullStateHash());

        [Property(MaxTest = Runs)]
        public bool RejectedCommands_ChangeNothing(ulong seed)
        {
            LevelSession session = RandomLevels.Create(seed);
            foreach (Command command in RandomLevels.Commands(seed, session))
            {
                ulong before = session.StateHash;
                int logBefore = session.CommandLog.Count;
                CommandResult result = session.Apply(command);
                if (!result.Accepted && (session.StateHash != before || session.CommandLog.Count != logBefore || result.Events.Count != 0))
                {
                    return false;
                }
            }

            return true;
        }

        [Property(MaxTest = Runs)]
        public bool Clones_AreIndependent(ulong seed)
        {
            LevelSession session = RandomLevels.Create(seed);
            ulong start = session.StateHash;
            LevelSession clone = session.Clone();
            foreach (Command command in RandomLevels.Commands(seed, clone))
            {
                clone.Apply(command);
            }

            return session.StateHash == start && session.CommandLog.Count == 0;
        }

        private static bool Replay(ulong seed, System.Func<LevelSession, CommandResult, bool> invariant)
        {
            LevelSession session = RandomLevels.Create(seed);
            foreach (Command command in RandomLevels.Commands(seed, session))
            {
                CommandResult result = session.Apply(command);
                if (!invariant(session, result))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AccountingHolds(LevelSession session)
        {
            var remaining = new Dictionary<VariantId, int>();
            foreach (PodDef pod in session.Definition.Pods)
            {
                PodView view = session.View.Pod(pod.Id);
                if (view.Location == PodLocation.Tray || view.Location == PodLocation.Slot)
                {
                    remaining[pod.Variant] = (remaining.TryGetValue(pod.Variant, out int n) ? n : 0) + view.Remaining;
                }
            }

            foreach (VariantInfo info in VariantCatalog.Default.All)
            {
                int pods = remaining.TryGetValue(info.Id, out int n) ? n : 0;
                if (pods != session.State.Board.CountLayers(info.Id))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Digest(CommandResult result) => EventText.Digest(result.Events.Select(EventText.Format));
    }
}
