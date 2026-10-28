using System;
using System.Collections.Generic;

namespace Bloomlings.Core.Rounds
{
    /// <summary>The result of a timeline command: accepted with its events, or refused with its reason.</summary>
    public readonly struct RoundCommitResult
    {
        internal RoundCommitResult(RoundRefusal refusal, IReadOnlyList<RoundEvent> events)
        {
            Refusal = refusal;
            Events = events;
        }

        public bool Accepted => Refusal == RoundRefusal.None;

        public RoundRefusal Refusal { get; }

        /// <summary>The reference's reason string ("no-slot" …), or null when accepted.</summary>
        public string? Reason => RoundRules.ReasonText(Refusal);

        /// <summary>The command's events: its own (commit, reveal, bridge or a booster's) followed by the departures.</summary>
        public IReadOnlyList<RoundEvent> Events { get; }

        internal static RoundCommitResult Refused(RoundRefusal refusal) => new RoundCommitResult(refusal, Array.Empty<RoundEvent>());
    }

    /// <summary>Where a courier is drawn at a moment (presentation only; cell units, x right, y up from the bottom row).</summary>
    public readonly struct RoundCourierPose
    {
        public RoundCourierPose(int id, int pod, double x, double y, bool carrying, bool acting)
        {
            Id = id;
            Pod = pod;
            X = x;
            Y = y;
            Carrying = carrying;
            Acting = acting;
        }

        public int Id { get; }

        public int Pod { get; }

        public double X { get; }

        public double Y { get; }

        /// <summary>On the way back from its cleared cell.</summary>
        public bool Carrying { get; }

        /// <summary>At its cell, working, before the clear.</summary>
        public bool Acting { get; }
    }

    /// <summary>
    /// The authoritative live simulation of a round level, a line-by-line port of the reference
    /// <c>tools/rounds/engine/timeline.cjs</c> with model time in integer microseconds (spec 006 FR-001 to FR-011,
    /// research R2, R3, R8; data model RoundTimeline).
    ///
    /// A place stores its pod's unsent work. A cell is reserved only when a courier leaves for it; walking couriers
    /// outlive their pod's place and may overlap later commands. <see cref="Commit"/> applies a tap or a bridge at the
    /// current model time, reveals tops and dispatches. <see cref="Advance"/> moves model time on and processes the due
    /// steps moment by moment (clear, arrive, return, release, check; then courier id or pod), dispatching after each
    /// moment. Model time stands still while nothing is pending, as in the reference; a client that records commands on
    /// a running clock keeps that idle time itself (see <see cref="RoundReplay"/>).
    ///
    /// Display speed is not a rules input: a client advances by real elapsed time × its display speed. The events of the
    /// last command or advance are in <see cref="LastEvents"/> for an animator; <see cref="Couriers"/> and
    /// <see cref="PoseOf"/> give the walkers. The invariants of the reference's <c>assert</c> are checked after every
    /// transition while <see cref="CheckInvariants"/> is set.
    /// </summary>
    public sealed class RoundTimeline
    {
        private readonly List<RoundStep> _steps = new List<RoundStep>();
        private readonly SortedList<int, RoundCourier> _jobs = new SortedList<int, RoundCourier>();
        private readonly List<RoundEvent> _events = new List<RoundEvent>();
        private readonly List<int> _lastClears = new List<int>();
        private int[] _reserved;
        private int[] _inFlight;
        private long[] _departReady;

        /// <summary>Starts a timeline on a copy of <paramref name="state"/> at model time 0.</summary>
        public RoundTimeline(RoundState state, RoundProfile? profile = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Profile = profile ?? RoundProfile.Default;
            State = state.Clone();
            _reserved = Filled(State.Board.Length, -1);
            _inFlight = new int[State.Level.Pods.Count];
            _departReady = new long[State.Level.Pods.Count];
            UpdateStatus();
            Assert();
        }

        /// <summary>Starts a fresh attempt of <paramref name="level"/> (reference <c>new Timeline(create(level))</c>).</summary>
        public RoundTimeline(RoundLevel level, RoundProfile? profile = null)
            : this(RoundRules.Create(level), profile)
        {
        }

        private RoundTimeline(RoundLevel level, RoundSnapshot snapshot, RoundProfile? profile)
        {
            Profile = profile ?? RoundProfile.Default;
            State = RoundRules.Create(level);
            _reserved = Array.Empty<int>();
            _inFlight = Array.Empty<int>();
            _departReady = Array.Empty<long>();
            Restore(snapshot);
        }

        public RoundProfile Profile { get; }

        /// <summary>The rules state (owned by the timeline; read it, never change it).</summary>
        public RoundState State { get; private set; }

        public RoundLevel Level => State.Level;

        /// <summary>Model time in µs.</summary>
        public long Now { get; private set; }

        /// <summary>The number of cells cleared so far.</summary>
        public int ProcessedCells { get; private set; }

        /// <summary>Whether Extra Slot was used in this attempt (once per level).</summary>
        public bool ExtraSlotUsed { get; internal set; }

        /// <summary>How many Shuffles this attempt has applied (each seeds the next differently).</summary>
        public int ShufflesUsed { get; internal set; }

        /// <summary>Check the reference's invariants after every transition (on by default).</summary>
        public bool CheckInvariants { get; set; } = true;

        /// <summary>True while any step is scheduled.</summary>
        public bool Pending => _steps.Count > 0;

        /// <summary>The scheduled steps in the order they will run.</summary>
        public IReadOnlyList<RoundStep> Steps => _steps;

        /// <summary>The couriers on the way, by id.</summary>
        public IList<RoundCourier> Couriers => _jobs.Values;

        /// <summary>Per cell, the id of the courier that reserved it, or -1.</summary>
        public IReadOnlyList<int> Reserved => _reserved;

        /// <summary>Per pod, its couriers still on the way (out or back).</summary>
        public IReadOnlyList<int> InFlight => _inFlight;

        /// <summary>Per pod, the model time of its next allowed departure.</summary>
        public IReadOnlyList<long> DepartReady => _departReady;

        /// <summary>The events of the last <see cref="Commit"/>, booster or <see cref="Advance"/>, in emission order.</summary>
        public IReadOnlyList<RoundEvent> LastEvents => _events;

        /// <summary>The cells cleared by the last advance, in order.</summary>
        public IReadOnlyList<int> LastClears => _lastClears;

        /// <summary>
        /// The tail (Auto) flag: every column is empty and no bridge is left to choose. Presentation only (FR-011): the
        /// client shows the rest faster; the rules do not change.
        /// </summary>
        public bool Tail => State.QueueEmpty && (State.Level.Bridge == null || State.Bridge >= 0);

        /// <summary>The status a player sees: Playing while work is pending (shown as working), else the settled one.</summary>
        public RoundStatus Status => State.Status;

        /// <summary>The free places.</summary>
        public int FreeSlots => State.FreeSlots;

        /// <summary>Whether some legal action is accepted now.</summary>
        public bool HasChoice => RoundRules.Legal(State).Count > 0;

        public RoundRefusal Check(RoundAction action) => RoundRules.Check(State, action);

        public List<RoundAction> Legal() => RoundRules.Legal(State);

        /// <summary>
        /// Applies a tap or a bridge at the current model time (reference <c>commit</c>): a link admission compacts the
        /// places first; each member takes the first free place and its column's head moves down; a sealed pod reveals on
        /// commit; then every hidden top pod in depth mode reveals, and work is dispatched.
        /// </summary>
        public RoundCommitResult Commit(RoundAction action)
        {
            RoundRefusal reason = Check(action);
            if (reason != RoundRefusal.None)
            {
                return RoundCommitResult.Refused(reason);
            }

            ClearLogs();
            RoundState s = State;
            if (action.Type == RoundActionType.Tap)
            {
                int[] group = s.Prepared.Groups[action.Pod];
                if (group.Length > 1)
                {
                    RoundRules.CompactSlots(s);
                }

                foreach (int pod in group)
                {
                    int column = -1;
                    for (int c = 0; c < s.Columns.Length; c++)
                    {
                        if (s.TopOf(c) == pod)
                        {
                            column = c;
                            break;
                        }
                    }

                    if (column < 0)
                    {
                        throw new InvalidOperationException("A committed pod is not on top of a column.");
                    }

                    int slot = Array.IndexOf(s.Slots, -1);
                    s.Heads[column]++;
                    s.Slots[slot] = pod;
                    s.Ages[pod] = ++s.Serial;
                    _events.Add(RoundEvent.Commit(Now, pod, slot));
                    if (!s.Revealed[pod])
                    {
                        s.Revealed[pod] = true;
                        _events.Add(RoundEvent.Reveal(Now, pod, RoundMystery.Sealed));
                    }
                }
            }
            else
            {
                RoundBridgeChoice site = s.Level.Bridge![action.Choice];
                s.Board[site.Cell] = RoundLevel.Open;
                s.Bridge = action.Choice;
                s.BoardChanged();
                _events.Add(RoundEvent.BridgeBuilt(Now, site.Cell));
            }

            RoundRules.RevealTops(s, _events, Now);
            Dispatch();
            Assert();
            return new RoundCommitResult(RoundRefusal.None, _events.ToArray());
        }

        /// <summary>
        /// Moves model time on by <paramref name="deltaUs"/> (reference <c>advance</c>). Every due step runs, one moment
        /// at a time, followed by a dispatch; a clear frees its cell, a return ends its courier, a release empties its
        /// link's places. While work stays pending, time ends at now + delta; when the last step ran, time stays at that
        /// step. Returns whether the state changed (a clear, a release or a departure).
        /// </summary>
        public bool Advance(long deltaUs)
        {
            if (deltaUs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaUs), "Time cannot go back.");
            }

            long end = deltaUs > long.MaxValue - Now ? long.MaxValue : Now + deltaUs;
            return AdvanceTo(end);
        }

        /// <summary>Runs every scheduled step until nothing is pending (the reference's <c>advance(1e9)</c>).</summary>
        public bool AdvanceUntilIdle() => AdvanceTo(long.MaxValue);

        private bool AdvanceTo(long end)
        {
            ClearLogs();
            if (!Pending)
            {
                return false;
            }

            bool changed = false;
            RoundState s = State;
            var due = new List<RoundStep>();
            while (_steps.Count > 0 && _steps[0].At <= end)
            {
                Now = _steps[0].At;
                int k = 0;
                while (k < _steps.Count && _steps[k].At <= Now)
                {
                    k++;
                }

                due.Clear();
                for (int i = 0; i < k; i++)
                {
                    due.Add(_steps[i]);
                }

                _steps.RemoveRange(0, k);
                foreach (RoundStep e in due)
                {
                    if (e.Type == RoundStepType.Check)
                    {
                        continue;
                    }

                    if (e.Type == RoundStepType.Release)
                    {
                        int[] group = s.Prepared.Groups[e.Key];
                        foreach (int p in group)
                        {
                            if (s.Remaining[p] != 0)
                            {
                                throw new InvalidOperationException("Early pair release.");
                            }
                        }

                        foreach (int pod in group)
                        {
                            int slot = s.SlotOf(pod);
                            if (slot < 0)
                            {
                                throw new InvalidOperationException("Missing released occupant.");
                            }

                            s.Slots[slot] = -1;
                            _events.Add(RoundEvent.Complete(Now, pod, slot));
                        }

                        changed = true;
                        continue;
                    }

                    if (!_jobs.TryGetValue(e.Key, out RoundCourier? j))
                    {
                        throw new InvalidOperationException("Event without courier.");
                    }

                    if (e.Type == RoundStepType.Arrive)
                    {
                        _events.Add(RoundEvent.ArriveAtCell(Now, j));
                    }
                    else if (e.Type == RoundStepType.Clear)
                    {
                        _events.Add(RoundEvent.ClearCell(Now, j));
                        if (_reserved[j.Cell] != j.Id || s.Board[j.Cell] != s.Prepared.Colours[j.Pod])
                        {
                            throw new InvalidOperationException("Invalid reservation.");
                        }

                        _reserved[j.Cell] = -1;
                        s.Board[j.Cell] = RoundLevel.Open;
                        s.BoardChanged();
                        ProcessedCells++;
                        _lastClears.Add(j.Cell);
                        changed = true;
                    }
                    else
                    {
                        _events.Add(RoundEvent.ReturnHome(Now, j));
                        _inFlight[j.Pod]--;
                        _jobs.Remove(j.Id);
                    }
                }

                int serial = _serial;
                Dispatch();
                if (serial != _serial)
                {
                    changed = true;
                }
            }

            if (Pending && end > Now)
            {
                Now = end;
            }

            UpdateStatus();
            Assert();
            return changed;
        }

        private int _serial;

        /// <summary>
        /// Sends couriers (reference <c>dispatch</c>): active pods by age, oldest first; each takes frontier cells of its
        /// exact material that no courier has reserved, in frontier order, while its departure time has come, one
        /// departure per gap; a pod left waiting for its gap gets one check step. Then a link whose members have all sent
        /// their work schedules one release after the release lag.
        /// </summary>
        internal void Dispatch()
        {
            RoundState s = State;
            int[] frontier = s.Reach().TargetArray;
            int[] active = ActiveWithWork(s);
            long gap = Tail ? Profile.TailDepartureGap : Profile.DepartureGap;
            var available = new List<int>();
            foreach (int pod in active)
            {
                sbyte colour = s.Prepared.Colours[pod];
                available.Clear();
                foreach (int c in frontier)
                {
                    if (s.Board[c] == colour && _reserved[c] < 0)
                    {
                        available.Add(c);
                    }
                }

                if (available.Count == 0)
                {
                    continue;
                }

                int next = 0;
                while (s.Remaining[pod] > 0 && next < available.Count && _departReady[pod] <= Now)
                {
                    int cell = available[next++];
                    int[] route = RoundRules.RouteTo(s, cell);
                    int id = ++_serial;
                    long distance = route.Length - 1;
                    long start = Now;
                    long arrive = start + Math.Max(Profile.MinOut, distance * Profile.OutPerCell);
                    long clear = arrive + Profile.Act;
                    long done = clear + Math.Max(Profile.MinReturn, distance * Profile.ReturnPerCell);
                    for (int r = 0; r < route.Length - 1; r++)
                    {
                        if (s.Board[route[r]] != RoundLevel.Open)
                        {
                            throw new InvalidOperationException("Closed route at dispatch.");
                        }
                    }

                    var job = new RoundCourier(id, pod, cell, start, arrive, clear, done, route, available.Count - next + 1);
                    _jobs.Add(id, job);
                    _reserved[cell] = id;
                    _inFlight[pod]++;
                    s.Remaining[pod]--;
                    _departReady[pod] = start + gap;
                    _events.Add(RoundEvent.Depart(job));
                    Schedule(new RoundStep(RoundStepType.Arrive, arrive, id));
                    Schedule(new RoundStep(RoundStepType.Clear, clear, id));
                    Schedule(new RoundStep(RoundStepType.Return, done, id));
                }

                if (s.Remaining[pod] > 0 && next < available.Count && !HasStep(RoundStepType.Check, pod))
                {
                    Schedule(new RoundStep(RoundStepType.Check, _departReady[pod], pod));
                }
            }

            var seen = new List<int>();
            foreach (int pod in s.Slots)
            {
                if (pod < 0)
                {
                    continue;
                }

                int[] group = s.Prepared.Groups[pod];
                int first = group[0];
                if (seen.Contains(first))
                {
                    continue;
                }

                seen.Add(first);
                bool sent = true;
                foreach (int p in group)
                {
                    if (s.Remaining[p] != 0)
                    {
                        sent = false;
                        break;
                    }
                }

                if (sent && !HasStep(RoundStepType.Release, first))
                {
                    Schedule(new RoundStep(RoundStepType.Release, Now + Profile.ReleaseLag, first));
                }
            }

            // The steps stay sorted: each new one was inserted at its place (the reference sorts the whole list here).
            UpdateStatus();
        }

        private static int[] ActiveWithWork(RoundState s)
        {
            var active = new List<int>(s.Slots.Length);
            foreach (int p in s.Slots)
            {
                if (p >= 0 && s.Remaining[p] > 0)
                {
                    active.Add(p);
                }
            }

            int[] result = active.ToArray();
            int[] ages = s.Ages;
            Array.Sort(result, (a, b) => ages[a].CompareTo(ages[b]));
            return result;
        }

        // Inserts a step after every step that runs before it or ties with it. The order is total (time, type, key), so
        // this gives exactly the reference's sorted list.
        private void Schedule(RoundStep step)
        {
            int lo = 0;
            int hi = _steps.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (_steps[mid].CompareTo(step) <= 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            _steps.Insert(lo, step);
        }

        internal bool HasStep(RoundStepType type, int key)
        {
            foreach (RoundStep step in _steps)
            {
                if (step.Type == type && step.Key == key)
                {
                    return true;
                }
            }

            return false;
        }

        internal void RemoveSteps(RoundStepType type, int key) => _steps.RemoveAll(e => e.Type == type && e.Key == key);

        /// <summary>The reference's status rule, with Playing while steps are pending.</summary>
        internal void UpdateStatus()
        {
            State.Status = Pending ? RoundStatus.Playing : RoundRules.StatusOf(State);
        }

        internal void ClearLogs()
        {
            _events.Clear();
            _lastClears.Clear();
        }

        internal List<RoundEvent> EventLog => _events;

        internal int[] ReservedArray => _reserved;

        internal int[] InFlightArray => _inFlight;

        internal void CountClear(int cell)
        {
            ProcessedCells++;
            _lastClears.Add(cell);
        }

        /// <summary>
        /// Where a courier is drawn at model time <paramref name="at"/> (the reference's <c>couriers</c>): along its route
        /// out until it arrives, still while it acts, then back along the route until it is home.
        /// </summary>
        public RoundCourierPose PoseOf(RoundCourier j, long at)
        {
            bool back = at >= j.Clear;
            double t = back ? (double)(j.Done - at) / (j.Done - j.Clear) : Math.Min(1.0, (double)(at - j.Start) / (j.Arrive - j.Start));
            int last = j.RouteArray.Length - 1;
            double v = Math.Max(0.0, t) * Math.Max(0, last);
            int step = Math.Min(last, (int)Math.Floor(v));
            int a = j.RouteArray[step];
            int b = j.RouteArray[Math.Min(step + 1, last)];
            double f = v - step;
            int w = State.Level.Width;
            double x = a % w + (b % w - a % w) * f;
            double y = a / w + (b / w - a / w) * f;
            return new RoundCourierPose(j.Id, j.Pod, x, y, back, at >= j.Arrive && !back);
        }

        /// <summary>Every courier's pose at the current model time.</summary>
        public List<RoundCourierPose> CourierPoses()
        {
            var poses = new List<RoundCourierPose>(_jobs.Count);
            foreach (RoundCourier j in _jobs.Values)
            {
                poses.Add(PoseOf(j, Now));
            }

            return poses;
        }

        /// <summary>
        /// The reference's invariants (<c>assert</c>): cells of each material = unsent work + outbound reservations;
        /// flight counts match the couriers; no orphan reservation; every live pod is in exactly one column or place, and
        /// every pod with unsent work is live; nothing is scheduled or reserved when nothing is pending.
        /// </summary>
        public void Assert()
        {
            if (!CheckInvariants)
            {
                return;
            }

            RoundState s = State;
            var cells = new int[RoundMaterial.Codes.Length];
            var work = new int[RoundMaterial.Codes.Length];
            foreach (sbyte c in s.Board)
            {
                if (c >= 0)
                {
                    cells[c]++;
                }
            }

            for (int p = 0; p < s.Remaining.Length; p++)
            {
                if (s.Remaining[p] < 0)
                {
                    throw new InvalidOperationException("Negative quota.");
                }

                work[s.Prepared.Colours[p]] += s.Remaining[p];
            }

            var flights = new int[s.Remaining.Length];
            var owner = Filled(s.Board.Length, -1);
            foreach (RoundCourier j in _jobs.Values)
            {
                flights[j.Pod]++;
                if (j.Clear > Now)
                {
                    if (owner[j.Cell] >= 0 || _reserved[j.Cell] != j.Id)
                    {
                        throw new InvalidOperationException("Duplicate/missing cell reservation.");
                    }

                    owner[j.Cell] = j.Id;
                    work[s.Prepared.Colours[j.Pod]]++;
                }
            }

            for (int c = 0; c < cells.Length; c++)
            {
                if (cells[c] != work[c])
                {
                    throw new InvalidOperationException("Conservation: board = unsent + outbound.");
                }
            }

            for (int p = 0; p < flights.Length; p++)
            {
                if (_inFlight[p] < 0 || _inFlight[p] != flights[p])
                {
                    throw new InvalidOperationException("Flight count mismatch.");
                }
            }

            for (int c = 0; c < _reserved.Length; c++)
            {
                if (_reserved[c] >= 0 && owner[c] != _reserved[c])
                {
                    throw new InvalidOperationException("Orphan reservation.");
                }
            }

            var live = new int[s.Remaining.Length];
            for (int c = 0; c < s.Columns.Length; c++)
            {
                List<int> stack = s.Columns[c];
                for (int k = s.Heads[c]; k < stack.Count; k++)
                {
                    live[stack[k]]++;
                }
            }

            foreach (int p in s.Slots)
            {
                if (p >= 0)
                {
                    live[p]++;
                }
            }

            for (int p = 0; p < live.Length; p++)
            {
                if (live[p] > 1 || (s.Remaining[p] > 0 && live[p] == 0))
                {
                    throw new InvalidOperationException("Pod location mismatch.");
                }
            }

            if (!Pending)
            {
                if (_jobs.Count > 0)
                {
                    throw new InvalidOperationException("Lost scheduled work.");
                }

                foreach (int id in _reserved)
                {
                    if (id >= 0)
                    {
                        throw new InvalidOperationException("Lost scheduled work.");
                    }
                }
            }
        }

        /// <summary>A deep copy of everything the timeline holds, for save and resume or a look-ahead copy.</summary>
        public RoundSnapshot Snapshot()
        {
            RoundState s = State;
            var columns = new int[s.Columns.Length][];
            for (int c = 0; c < columns.Length; c++)
            {
                columns[c] = s.Columns[c].ToArray();
            }

            var jobs = new RoundCourier[_jobs.Count];
            for (int i = 0; i < jobs.Length; i++)
            {
                jobs[i] = _jobs.Values[i].Copy();
            }

            return new RoundSnapshot(
                (sbyte[])s.Board.Clone(),
                (int[])s.Remaining.Clone(),
                columns,
                (int[])s.ColumnOf.Clone(),
                s.ColumnsChanged,
                (int[])s.Heads.Clone(),
                (int[])s.Slots.Clone(),
                (int[])s.Ages.Clone(),
                (bool[])s.Revealed.Clone(),
                s.Serial,
                s.Bridge,
                s.Status,
                _steps.ToArray(),
                Now,
                _serial,
                ProcessedCells,
                jobs,
                (int[])_reserved.Clone(),
                (int[])_inFlight.Clone(),
                (long[])_departReady.Clone(),
                ExtraSlotUsed,
                ShufflesUsed);
        }

        /// <summary>Replaces everything with a snapshot of a timeline of the same level (reference checkpoint load).</summary>
        public void Restore(RoundSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            RoundState fresh = RoundRules.Create(State.Level);
            int pods = fresh.Remaining.Length;
            if (snapshot.Board.Length != fresh.Board.Length || snapshot.Remaining.Length != pods || snapshot.Columns.Length != fresh.Columns.Length
                || snapshot.Ages.Length != pods || snapshot.Revealed.Length != pods || snapshot.InFlight.Length != pods
                || snapshot.DepartReady.Length != pods || snapshot.Reserved.Length != fresh.Board.Length || snapshot.Now < 0)
            {
                throw new ArgumentException("The snapshot does not belong to this level.", nameof(snapshot));
            }

            foreach (RoundStep step in snapshot.Steps)
            {
                if (step.At < snapshot.Now)
                {
                    throw new ArgumentException("Invalid event time.", nameof(snapshot));
                }
            }

            Array.Copy(snapshot.Board, fresh.Board, fresh.Board.Length);
            Array.Copy(snapshot.Remaining, fresh.Remaining, pods);
            for (int c = 0; c < fresh.Columns.Length; c++)
            {
                fresh.Columns[c].Clear();
                fresh.Columns[c].AddRange(snapshot.Columns[c]);
            }

            Array.Copy(snapshot.ColumnOf, fresh.ColumnOf, pods);
            fresh.ColumnsChanged = snapshot.ColumnsChanged;
            Array.Copy(snapshot.Heads, fresh.Heads, fresh.Heads.Length);
            fresh.Slots = (int[])snapshot.Slots.Clone();
            Array.Copy(snapshot.Ages, fresh.Ages, pods);
            Array.Copy(snapshot.Revealed, fresh.Revealed, pods);
            fresh.Serial = snapshot.Serial;
            fresh.Bridge = snapshot.Bridge;
            fresh.Status = snapshot.Status;
            fresh.BoardChanged();
            State = fresh;

            _steps.Clear();
            _steps.AddRange(snapshot.Steps);
            Now = snapshot.Now;
            _serial = snapshot.CourierSerial;
            ProcessedCells = snapshot.ProcessedCells;
            _jobs.Clear();
            foreach (RoundCourier j in snapshot.Couriers)
            {
                _jobs.Add(j.Id, j.Copy());
            }

            _reserved = (int[])snapshot.Reserved.Clone();
            _inFlight = (int[])snapshot.InFlight.Clone();
            _departReady = (long[])snapshot.DepartReady.Clone();
            ExtraSlotUsed = snapshot.ExtraSlotUsed;
            ShufflesUsed = snapshot.ShufflesUsed;
            ClearLogs();
            UpdateStatus();
            Assert();
        }

        /// <summary>A new timeline of <paramref name="level"/> resumed from a snapshot.</summary>
        public static RoundTimeline FromSnapshot(RoundLevel level, RoundSnapshot snapshot, RoundProfile? profile = null) =>
            new RoundTimeline(level, snapshot, profile);

        /// <summary>An independent copy (the same profile, everything deep-copied).</summary>
        public RoundTimeline Copy()
        {
            var copy = FromSnapshot(State.Level, Snapshot(), Profile);
            copy.CheckInvariants = CheckInvariants;
            return copy;
        }

        private static int[] Filled(int length, int value)
        {
            var a = new int[length];
            for (int i = 0; i < length; i++)
            {
                a[i] = value;
            }

            return a;
        }
    }

    /// <summary>A deep copy of a <see cref="RoundTimeline"/> (data model RoundTimeline plus RoundState).</summary>
    public sealed class RoundSnapshot
    {
        internal RoundSnapshot(sbyte[] board, int[] remaining, int[][] columns, int[] columnOf, bool columnsChanged, int[] heads, int[] slots,
            int[] ages, bool[] revealed, int serial, int bridge, RoundStatus status, RoundStep[] steps, long now, int courierSerial,
            int processedCells, RoundCourier[] couriers, int[] reserved, int[] inFlight, long[] departReady, bool extraSlotUsed, int shufflesUsed)
        {
            Board = board;
            Remaining = remaining;
            Columns = columns;
            ColumnOf = columnOf;
            ColumnsChanged = columnsChanged;
            Heads = heads;
            Slots = slots;
            Ages = ages;
            Revealed = revealed;
            Serial = serial;
            Bridge = bridge;
            Status = status;
            Steps = steps;
            Now = now;
            CourierSerial = courierSerial;
            ProcessedCells = processedCells;
            Couriers = couriers;
            Reserved = reserved;
            InFlight = inFlight;
            DepartReady = departReady;
            ExtraSlotUsed = extraSlotUsed;
            ShufflesUsed = shufflesUsed;
        }

        public sbyte[] Board { get; }

        public int[] Remaining { get; }

        public int[][] Columns { get; }

        public int[] ColumnOf { get; }

        public bool ColumnsChanged { get; }

        public int[] Heads { get; }

        public int[] Slots { get; }

        public int[] Ages { get; }

        public bool[] Revealed { get; }

        public int Serial { get; }

        public int Bridge { get; }

        public RoundStatus Status { get; }

        public RoundStep[] Steps { get; }

        public long Now { get; }

        public int CourierSerial { get; }

        public int ProcessedCells { get; }

        public RoundCourier[] Couriers { get; }

        public int[] Reserved { get; }

        public int[] InFlight { get; }

        public long[] DepartReady { get; }

        public bool ExtraSlotUsed { get; }

        public int ShufflesUsed { get; }
    }
}
