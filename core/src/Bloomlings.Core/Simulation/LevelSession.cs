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
        private LevelView? _view;

        private LevelSession(LevelState state, List<Command> commandLog, IReadOnlyList<IRoundHook> hooks)
        {
            State = state;
            _commandLog = commandLog;
            _hooks = hooks;
        }

        /// <summary>
        /// Builds the board (<see cref="Boards.BoardBuilder"/>) and the start state. Throws
        /// <see cref="InvalidLevelException"/> for an inconsistent level, including a failed exact accounting (FR-023).
        /// </summary>
        public static LevelSession Load(LevelDefinition definition, BasePicture picture, SessionOptions options)
        {
            LevelState state = LevelState.Create(definition, picture, options);
            IReadOnlyList<IRoundHook> hooks = CreateHooks(state);
            Start(state, hooks);
            return new LevelSession(state, new List<Command>(), hooks);
        }

        /// <summary>
        /// Loads a hypothetical version of a level in which mystery tiles and mystery pods have other variants (the
        /// player-information fairness check, R8). Throws <see cref="InvalidLevelException"/> when the assignment breaks
        /// exact accounting, so only worlds the player cannot rule out load.
        /// </summary>
        public static LevelSession LoadHypothesis(LevelDefinition definition, BasePicture picture, SessionOptions options, MysteryAssignment assignment)
        {
            if (assignment == null)
            {
                throw new ArgumentNullException(nameof(assignment));
            }

            LevelState state = LevelState.Create(definition, picture, options, assignment);
            IReadOnlyList<IRoundHook> hooks = CreateHooks(state);
            Start(state, hooks);
            return new LevelSession(state, new List<Command>(), hooks);
        }

        public LevelDefinition Definition => State.Definition;

        public BasePicture Picture => State.Picture;

        public SessionOptions Options => State.Options;

        /// <summary>The read-only view of this session (made when first asked for; it holds no state of its own).</summary>
        public LevelView View => _view ??= new LevelView(this);

        public LevelStatus Status => State.Status;

        /// <summary>Boosters used in this attempt: the clean-clear bonus needs none (FR-041).</summary>
        public int BoostersUsed => State.BoostersUsed;

        /// <summary>Zobrist hash of the logical state; equal states have equal hashes whatever the path.</summary>
        public ulong StateHash => State.StateHash;

        /// <summary>The accepted commands of this attempt, for replays, analytics and support.</summary>
        public IReadOnlyList<Command> CommandLog => _commandLog;

        internal LevelState State { get; private set; }

        /// <summary>A deep copy for the solver and Shuffle search.</summary>
        public LevelSession Clone() => new LevelSession(State.Clone(), new List<Command>(_commandLog), _hooks);

        /// <summary>
        /// One step of a search (<see cref="Search.StateSearch"/>, the solver): a copy of this session with
        /// <paramref name="command"/> applied, or null when the command is refused. The copy has exactly the state,
        /// status and <see cref="StateHash"/> that <see cref="Clone"/> followed by <see cref="Apply"/> gives, and this
        /// session does not change. A tap builds no event log (nobody reads it during a search), and the copy keeps no
        /// <see cref="CommandLog"/>: it is for exploring states, never for replays or presentation.
        /// </summary>
        public LevelSession? SearchChild(Command command) => SearchChild(command, null);

        /// <summary>
        /// As <see cref="SearchChild(Command)"/>; with <paramref name="reuse"/>, a session from an earlier search step of
        /// the same loaded level that the caller is done with, the child is written into it instead of a new copy (then
        /// the caller must not use what it held). The child is the same either way.
        /// </summary>
        public LevelSession? SearchChild(Command command, LevelSession? reuse)
        {
            if (command is TapPod tap)
            {
                if (!Check(tap).IsAllowed)
                {
                    return null;
                }

                LevelSession child;
                if (reuse != null && !ReferenceEquals(reuse, this) && ReferenceEquals(reuse._hooks, _hooks) && reuse.State.TryCopyFrom(State))
                {
                    child = reuse;
                    child._commandLog.Clear();
                }
                else
                {
                    child = new LevelSession(State.Clone(), new List<Command>(), _hooks);
                }

                child.CommitTap(tap, null);
                return child;
            }

            LevelSession copy = Clone();
            return copy.Apply(command).Accepted ? copy : null;
        }

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
                    // Boosters also work on a Jammed or Stuck board: they are the recoveries (FR-027).
                    if (State.Status == LevelStatus.Won)
                    {
                        return CommandCheck.Refused(RejectReason.LevelNotPlaying);
                    }

                    bool applicable = command switch
                    {
                        UseExtraSlot _ => Boosters.ExtraSlot.CanApply(State),
                        UseShuffle _ => Boosters.ShufflePlanner.CanApply(State),
                        UseReturn r => Boosters.Return.CanApply(State, r.SlotIndex),
                        UseBloomBurst b => Boosters.BloomBurst.CanApply(State, b.Variant),
                        _ => false,
                    };
                    return applicable ? CommandCheck.Allowed : CommandCheck.Refused(RejectReason.BoosterNotApplicable);
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
                    Start(State, _hooks);
                    _commandLog.Clear();
                    return CommandResult.Accept(Array.Empty<GameEvent>());
                case TapPod tap:
                    return ApplyTap(tap);
                default:
                    return ApplyBooster(command);
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
            CommitTap(tap, events);
            _commandLog.Add(tap);
            return CommandResult.Accept(events);
        }

        /// <summary>A checked tap and its settle; <paramref name="events"/> is null in a search (<see cref="SearchChild"/>).</summary>
        private void CommitTap(TapPod tap, List<GameEvent>? events)
        {
            // A connected pod commits its whole group, each member into its own slot (FR-035).
            foreach (int pod in State.CommitOrder(State.PodIndex[tap.PodId]))
            {
                (int stack, int slot) = State.CommitPod(pod);
                events?.Add(new PodCommitted(0, State.PodId(pod), slot, stack, 0));
                if (!State.Pods[pod].VariantRevealed)
                {
                    State.RevealPod(pod);
                    events?.Add(new MysteryPodRevealed(0, State.PodId(pod), State.PodVariant(pod)));
                }
            }

            Settle(events);
        }

        /// <summary>Applies a checked booster (FR-043 to FR-050); the charge itself is the client's economy (FR-048).</summary>
        private CommandResult ApplyBooster(Command command)
        {
            var events = new List<GameEvent>();
            switch (command)
            {
                case UseExtraSlot _:
                    Boosters.ExtraSlot.Apply(State, events);
                    break;
                case UseReturn r:
                    Boosters.Return.Apply(State, r.SlotIndex, events);
                    break;
                case UseBloomBurst b:
                    Boosters.BloomBurst.Apply(State, b.Variant, _hooks, events);
                    break;
                case UseShuffle _:
                    IReadOnlyList<IReadOnlyList<int>> layout = Boosters.ShufflePlanner.Plan(this);
                    State.RearrangeTray(layout);
                    State.IncrementShuffleUses();
                    var stacks = new List<IReadOnlyList<string>>();
                    for (int s = 0; s < State.Tray.StackCount; s++)
                    {
                        stacks.Add(View.Stack(s));
                    }

                    events.Add(new TrayShuffled(0, stacks));
                    break;
                default:
                    throw new NotSupportedException($"Unknown command {command.GetType().Name}.");
            }

            State.BoostersUsed++;
            Settle(events);
            _commandLog.Add(command);
            return CommandResult.Accept(events);
        }

        /// <summary>Shuffle's relaxed problem: commit a tray unit from any depth, then settle.</summary>
        internal LevelSession RelaxedChild(int[] unit)
        {
            LevelSession child = Clone();
            foreach (int pod in unit)
            {
                child.State.MoveToTop(pod);
            }

            foreach (int pod in child.State.CommitOrder(unit[0]))
            {
                child.State.CommitPod(pod);
                if (!child.State.Pods[pod].VariantRevealed)
                {
                    child.State.RevealPod(pod);
                }
            }

            // Nobody reads the relaxed problem's events.
            child.Settle(null);
            return child;
        }

        /// <summary>A copy with the tray rearranged (Shuffle verification); its status is evaluated again.</summary>
        internal LevelSession WithTray(IReadOnlyList<IReadOnlyList<int>> stacksTopFirst)
        {
            LevelSession child = Clone();
            child.State.RearrangeTray(stacksTopFirst);
            child.State.Status = StatusEvaluator.Evaluate(child.State, _hooks);
            return child;
        }

        /// <summary>Settles and evaluates the status; without <paramref name="events"/> (a search) no event is built.</summary>
        private void Settle(List<GameEvent>? events)
        {
            int round = Settler.Settle(State, _hooks, events);
            LevelStatus before = State.Status;
            LevelStatus after = StatusEvaluator.Evaluate(State, _hooks);
            State.Status = after;
            if (after == before || events == null)
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

        private static void Start(LevelState state, IReadOnlyList<IRoundHook> hooks)
        {
            foreach (IRoundHook hook in hooks)
            {
                hook.OnStart(state);
            }

            state.Status = StatusEvaluator.Evaluate(state, hooks);
        }

        /// <summary>
        /// The round hooks for the mechanics this level uses, in a fixed order: mystery tiles reveal before allocation,
        /// keys are collected before specials check their conditions. Hooks are stateless, so clones share them.
        /// </summary>
        private static IReadOnlyList<IRoundHook> CreateHooks(LevelState state)
        {
            var hooks = new List<IRoundHook>(3);
            if (state.Mechanics.HasMystery)
            {
                hooks.Add(Mechanics.Mystery.Instance);
            }

            if (state.Mechanics.HasKeys)
            {
                hooks.Add(Mechanics.KeysAndLocks.Instance);
            }

            if (state.Mechanics.Specials.Length > 0)
            {
                hooks.Add(Mechanics.Specials.Instance);
            }

            return hooks.Count == 0 ? NoHooks : hooks.ToArray();
        }
    }
}
