using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Core.Simulation
{
    /// <summary>
    /// One attempt at a level: the only way to change the logical state (contracts/simulation-api.md). Every accepted
    /// command returns after the automatic work has reached its fixpoint, with the ordered event log (research R3).
    /// The same definition and the same accepted commands always give the same events, state and
    /// <see cref="StateHash"/> (FR-024).
    /// </summary>
    public sealed class LevelSession
    {
        private static readonly IRoundHook[] NoHooks = new IRoundHook[0];

        private readonly List<Command> _commandLog;
        private readonly IReadOnlyList<IRoundHook> _hooks;

        private LevelSession(LevelState state, List<Command> commandLog, IReadOnlyList<IRoundHook> hooks)
        {
            State = state;
            _commandLog = commandLog;
            _hooks = hooks;
            View = new LevelView(this);
        }

        /// <summary>
        /// Builds the board (<see cref="Boards.BoardBuilder"/>) and the start state. Throws
        /// <see cref="InvalidLevelException"/> for an inconsistent level, including a failed exact accounting (FR-023).
        /// </summary>
        public static LevelSession Load(LevelDefinition definition, BasePicture picture, SessionOptions options)
        {
            LevelState state = LevelState.Create(definition, picture, options);
            IReadOnlyList<IRoundHook> hooks = CreateHooks(state);
            state.Status = StatusEvaluator.Evaluate(state, hooks);
            return new LevelSession(state, new List<Command>(), hooks);
        }

        public LevelDefinition Definition => State.Definition;

        public BasePicture Picture => State.Picture;

        public SessionOptions Options => State.Options;

        public LevelView View { get; }

        public LevelStatus Status => State.Status;

        /// <summary>Zobrist hash of the logical state; equal states have equal hashes whatever the path.</summary>
        public ulong StateHash => State.StateHash;

        /// <summary>The accepted commands of this attempt, for replays, analytics and support.</summary>
        public IReadOnlyList<Command> CommandLog => _commandLog;

        internal LevelState State { get; private set; }

        /// <summary>A deep copy for the solver and Shuffle search.</summary>
        public LevelSession Clone() => new LevelSession(State.Clone(), new List<Command>(_commandLog), CreateHooks(State));

        /// <summary>The same validation as <see cref="Apply"/>, without changing anything (for button states).</summary>
        public CommandCheck Check(Command command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            switch (command)
            {
                case Restart _:
                    return CommandCheck.Allowed;
                case TapPod tap:
                    return CheckTap(tap);
                case UseExtraSlot _:
                case UseShuffle _:
                case UseReturn _:
                case UseBloomBurst _:
                    if (State.Status == LevelStatus.Won)
                    {
                        return CommandCheck.Refused(RejectReason.LevelNotPlaying);
                    }

                    // Boosters are implemented with US5 (T115–T117).
                    return CommandCheck.Refused(RejectReason.BoosterNotApplicable);
                default:
                    throw new NotSupportedException($"Unknown command {command.GetType().Name}.");
            }
        }

        /// <summary>Applies a command. A refused command changes nothing and returns its reason.</summary>
        public CommandResult Apply(Command command)
        {
            CommandCheck check = Check(command);
            if (!check.IsAllowed)
            {
                return CommandResult.Reject(check.Reason!.Value);
            }

            switch (command)
            {
                case Restart _:
                    State = LevelState.Create(State.Definition, State.Picture, State.Options);
                    State.Status = StatusEvaluator.Evaluate(State, _hooks);
                    _commandLog.Clear();
                    return CommandResult.Accept(Array.Empty<GameEvent>());
                case TapPod tap:
                    return ApplyTap(tap);
                default:
                    throw new NotSupportedException($"Unknown command {command.GetType().Name}.");
            }
        }

        /// <summary>The recoveries a Jam or Stuck screen may offer in the current state (FR-027).</summary>
        public IReadOnlyList<Recovery> EligibleRecoveries()
        {
            var recoveries = new List<Recovery>();
            if (Check(new UseExtraSlot()).IsAllowed)
            {
                recoveries.Add(Recovery.ExtraSlot);
            }

            if (Check(new UseShuffle()).IsAllowed)
            {
                recoveries.Add(Recovery.Shuffle);
            }

            for (int slot = 0; slot < Slots.WaitingSlots.Capacity; slot++)
            {
                if (Check(new UseReturn(slot)).IsAllowed)
                {
                    recoveries.Add(Recovery.Return);
                    break;
                }
            }

            foreach (Variants.VariantInfo info in State.Catalog.All)
            {
                if (Check(new UseBloomBurst(info.Id)).IsAllowed)
                {
                    recoveries.Add(Recovery.BloomBurst);
                    break;
                }
            }

            return recoveries;
        }

        /// <summary>The state hash recomputed from scratch (tests check it equals the incremental one).</summary>
        internal ulong ComputeFullStateHash() => State.ComputeFullHash();

        /// <summary>
        /// Tap validation order: NotExposed, Locked, NoFreeSlot, then LevelNotPlaying. A tap on a jammed board is thus
        /// refused with NoFreeSlot, which is the feedback the player needs (FR-014).
        /// </summary>
        private CommandCheck CheckTap(TapPod tap)
        {
            if (!State.PodIndex.TryGetValue(tap.PodId, out int pod))
            {
                return CommandCheck.Refused(RejectReason.NotExposed);
            }

            RejectReason? reason = State.CanCommit(pod);
            if (reason != null)
            {
                return CommandCheck.Refused(reason.Value);
            }

            return State.Status == LevelStatus.Playing ? CommandCheck.Allowed : CommandCheck.Refused(RejectReason.LevelNotPlaying);
        }

        private CommandResult ApplyTap(TapPod tap)
        {
            var events = new List<GameEvent>();
            int pod = State.PodIndex[tap.PodId];
            (int stack, int slot) = State.CommitPod(pod);
            events.Add(new PodCommitted(0, tap.PodId, slot, stack, 0));
            if (!State.Pods[pod].VariantRevealed)
            {
                State.RevealPod(pod);
                events.Add(new MysteryPodRevealed(0, tap.PodId, State.PodVariant(pod)));
            }

            Settle(events);
            _commandLog.Add(tap);
            return CommandResult.Accept(events);
        }

        private void Settle(List<GameEvent> events)
        {
            int round = Settler.Settle(State, _hooks, events);
            LevelStatus before = State.Status;
            LevelStatus after = StatusEvaluator.Evaluate(State, _hooks);
            State.Status = after;
            if (after == before)
            {
                return;
            }

            switch (after)
            {
                case LevelStatus.Won:
                    events.Add(new LevelWon(round, State.BoostersUsed));
                    break;
                case LevelStatus.Jammed:
                    events.Add(new LevelJammed(round, EligibleRecoveries()));
                    break;
                case LevelStatus.Stuck:
                    events.Add(new LevelStuck(round, EligibleRecoveries()));
                    break;
            }
        }

        /// <summary>The round hooks for the mechanics of this level, in a fixed order (US4 adds them).</summary>
        private static IReadOnlyList<IRoundHook> CreateHooks(LevelState state) => NoHooks;
    }
}
