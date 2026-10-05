using System;
using System.Collections.Generic;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Variants;

namespace Bloomlings.Playtest
{
    /// <summary>What one Waiting Slot shows.</summary>
    public sealed class SlotLook
    {
        public string? PodId { get; set; }

        /// <summary>Null while a mystery pod still shows "?".</summary>
        public VariantId? Variant { get; set; }

        public int Count { get; set; }

        /// <summary>Bloomlings of this pod on their way.</summary>
        public int InFlight { get; set; }

        public float PoppedAt { get; set; } = -10f;

        public float BumpedAt { get; set; } = -10f;

        /// <summary>When the pod started leaving (done), or a negative value.</summary>
        public float LeavingAt { get; set; } = -1f;

        public float RevealAt { get; set; } = -10f;

        public Queue<(string PodId, VariantId? Variant, int Count)> Pending { get; } = new Queue<(string, VariantId?, int)>();

        public bool IsLeaving => LeavingAt >= 0f;

        public void Clear()
        {
            PodId = null;
            Variant = null;
            Count = 0;
            InFlight = 0;
            LeavingAt = -1f;
        }
    }

    /// <summary>
    /// A Bloomling walking its route (the entry cell first, the target last): it sets off at <paramref name="Start"/> on
    /// the animation clock and arrives <paramref name="Arrival"/> seconds later.
    /// </summary>
    public sealed record Walker(IReadOnlyList<CellPos> Route, VariantId Variant, float Start, float Arrival);

    /// <summary>A cleared tile shrinking away (its look before the clear).</summary>
    public sealed record Fade(CellPos Cell, CellInfo Look, float Start);

    /// <summary>A pod card flying from the tray to its slot (or back, for Return).</summary>
    public sealed record Flight(float FromX, float FromY, int Slot, VariantId? Variant, float Start, bool ToTray, string PodId);

    /// <summary>
    /// The playtest's presentation timeline, like the Unity client's EventTimeline (research R4): the rules resolve a
    /// tap at once, and this plays the result as waves, one per settle round. Each cleared tile keeps its old look until
    /// its Bloomling arrives, slot counts drop as they land, a finished pod leaves at the end of its wave, a key's lock
    /// stays until the key's wave, and the win or jam card waits for the last wave. 2× speed and backlog compression
    /// (up to 4×) only change the pace. No rule lives here: every change comes from the core's events.
    /// <para>
    /// The waves of one tap play one after another, as its rounds do; the waves of different taps play side by side, so
    /// pods committed one after another work at the same time (spec 001 FR-018, the owner's report of 2026-10-03). A wave
    /// waits only for what it depends on: every cell it walks over or clears must have shown its earlier changes first
    /// (a tile cleared by an earlier tap, a layer revealed under it). A committed pod shows in the first slot that is
    /// free on screen: the rules free a finished pod's slot at once (FR-022), while its Bloomlings may still be on their
    /// way, so the next pod would otherwise land in a slot that still shows the last one. Where a pod shows never changes
    /// an outcome: the rules decide which slot it holds.
    /// </para>
    /// </summary>
    public sealed class LevelAnimator
    {
        // The clearing pace, halved on the owner's requests of 2026-10-03 (it was 0.09 s a step and waves of 0.3–1.6 s)
        // and again of 2026-10-05 (0.18 s a step and waves of 0.6–3.2 s); the restore keeps its time.
        public const float StepSeconds = 0.36f;
        public const float RestoreSeconds = 0.2f;
        public const float MinWaveSeconds = 1.2f;
        public const float MaxWaveSeconds = 6.4f;
        public const float ExitSeconds = 0.25f;
        public const float FadeSeconds = 0.2f;
        public const float FlightSeconds = 0.2f;
        private const float MaxRate = 4f;

        // The backlog beyond which the timeline plays faster (taps far quicker than the Bloomlings walk), doubled with the
        // pace on 2026-10-05 (it was 6 s), so the slower clearing is not sped up again.
        private const float BacklogSeconds = 12f;

        private readonly List<Wave> _waves = new List<Wave>();
        private readonly Dictionary<string, (int Progress, int Total, bool Triggered)> _specials = new Dictionary<string, (int, int, bool)>(StringComparer.Ordinal);
        private CellInfo[] _cells = Array.Empty<CellInfo>();

        // Per cell: when its last change queued so far shows (a later wave touching it starts after that).
        private float[] _ready = Array.Empty<float>();

        // Per special and per pod: when the last wave queued so far that changes it ends (their end events keep the
        // rules' order: a special's progress, a pod's last Bloomling before it leaves).
        private readonly Dictionary<string, float> _keyReady = new Dictionary<string, float>(StringComparer.Ordinal);
        private int _width;
        private bool _silent;

        public SlotLook[] Slots { get; } = CreateSlots();

        /// <summary>A Bloomling reached its tile (sound cue); not raised while flushing.</summary>
        public event Action<TileCleared>? Arrived;

        /// <summary>An end-of-wave event was shown (pod done, key, special, win, jam); not raised while flushing.</summary>
        public event Action<GameEvent>? Shown;

        /// <summary>Pods and slots drawn locked until their key's wave plays (the rules opened them already).</summary>
        public HashSet<string> HeldPodLocks { get; } = new HashSet<string>(StringComparer.Ordinal);

        public HashSet<int> HeldSlotLocks { get; } = new HashSet<int>();

        public List<Walker> Walkers { get; } = new List<Walker>();

        public List<Fade> Fades { get; } = new List<Fade>();

        public List<Flight> Flights { get; } = new List<Flight>();

        /// <summary>1 or 2 (the 2× toggle).</summary>
        public float Speed { get; set; } = 1f;

        /// <summary>Animation clock in timeline seconds.</summary>
        public float Now { get; private set; }

        /// <summary>Every event of the rules has been shown.</summary>
        public bool Settled => _waves.Count == 0;

        /// <summary>How many waves are playing now (more than one when the waves of several taps play side by side).</summary>
        public int Playing
        {
            get
            {
                int n = 0;
                foreach (Wave wave in _waves)
                {
                    n += wave.Started ? 1 : 0;
                }

                return n;
            }
        }

        /// <summary>Nothing moves any more (the screen can stop redrawing).</summary>
        public bool Idle
        {
            get
            {
                if (!Settled || Fades.Count > 0 || Flights.Count > 0)
                {
                    return false;
                }

                foreach (SlotLook slot in Slots)
                {
                    if (slot.IsLeaving || Now - slot.PoppedAt < 0.2f || Now - slot.BumpedAt < 0.2f || Now - slot.RevealAt < 0.35f)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>How long, in timeline seconds, until every queued wave has played.</summary>
        public float Backlog
        {
            get
            {
                float end = Now;
                foreach (Wave wave in _waves)
                {
                    end = Math.Max(end, wave.EndAt);
                }

                return end - Now;
            }
        }

        public CellInfo Cell(CellPos pos) => _cells[(pos.Y * _width) + pos.X];

        public (int Progress, int Total, bool Triggered) Special(string id) =>
            _specials.TryGetValue(id, out (int, int, bool) state) ? state : (0, 0, true);

        /// <summary>Shows the level exactly as the rules have it now, with nothing pending (load, restart).</summary>
        public void Reset(LevelView view)
        {
            _waves.Clear();
            _keyReady.Clear();
            Walkers.Clear();
            Fades.Clear();
            Flights.Clear();
            HeldPodLocks.Clear();
            HeldSlotLocks.Clear();
            _width = view.Width;
            _cells = new CellInfo[view.Width * view.Height];
            _ready = new float[_cells.Length];
            for (int y = 0; y < view.Height; y++)
            {
                for (int x = 0; x < view.Width; x++)
                {
                    _cells[(y * _width) + x] = view.Cell(new CellPos(x, y));
                }
            }

            _specials.Clear();
            foreach (SpecialInfo special in view.Specials)
            {
                _specials[special.Id] = (special.Progress, special.Total, special.Triggered);
            }

            for (int i = 0; i < Slots.Length; i++)
            {
                Slots[i].Clear();
                Slots[i].Pending.Clear();
                string? pod = view.PodInSlot(i);
                if (pod != null)
                {
                    PodInfo info = view.Pod(pod);
                    Slots[i].PodId = pod;
                    Slots[i].Variant = info.Variant;
                    Slots[i].Count = info.Remaining;
                }
            }
        }

        /// <summary>
        /// A tap was applied: its commits show at once (immediate feedback, SC-008), flying from the tray, and its settle
        /// rounds are queued. <paramref name="before"/> holds each committed pod's count, shown variant and tray position.
        /// </summary>
        public void Tapped(CommandResult result, LevelView view, IReadOnlyDictionary<string, (int Count, VariantId? Variant, float X, float Y)> before)
        {
            HoldLocks(result.Events);
            foreach (GameEvent e in result.Events)
            {
                if (e.Round != 0)
                {
                    break;
                }

                switch (e)
                {
                    case PodCommitted committed:
                        (int count, VariantId? shown, float x, float y) = before.TryGetValue(committed.PodId, out var seen)
                            ? seen
                            : (view.Pod(committed.PodId).Remaining, view.Pod(committed.PodId).Variant, float.NaN, float.NaN);
                        int place = Place(committed.SlotIndex, view);
                        Commit(place, committed.PodId, shown, count);
                        if (!float.IsNaN(x))
                        {
                            Flights.Add(new Flight(x, y, place, shown, Now, false, committed.PodId));
                        }

                        break;
                    case MysteryPodRevealed revealed:
                        Reveal(revealed.PodId, revealed.Variant);
                        break;
                }
            }

            Enqueue(result.Events);
        }

        /// <summary>
        /// A booster was applied (after <see cref="Flush"/>): its own changes show at once (burst tiles puff away, the
        /// slots and tray take their new shape), and the rounds it set off are queued.
        /// </summary>
        public void Boosted(CommandResult result, LevelView view)
        {
            HoldLocks(result.Events);
            var pending = new Dictionary<string, int>(StringComparer.Ordinal);
            var completes = new List<PodCompleted>();
            foreach (GameEvent e in result.Events)
            {
                switch (e)
                {
                    case VariantBurst burst when e.Round == 0:
                        foreach (CellPos cell in burst.Cells)
                        {
                            Fades.Add(new Fade(cell, Cell(cell), Now));
                            _cells[Index(cell)] = view.Cell(cell);
                        }

                        break;
                    case TileCleared clear when e.Round > 0:
                        pending[clear.PodId] = (pending.TryGetValue(clear.PodId, out int n) ? n : 0) + 1;
                        break;
                    case PodCompleted done when e.Round > 0:
                        completes.Add(done);
                        break;
                    case KeyCollected key when e.Round == 0:
                        ApplyEnd(key, view);
                        break;
                    case LockOpened opened when e.Round == 0:
                        ApplyEnd(opened, view);
                        break;
                }
            }

            // The slots as the rules have them, with the counts before the queued work lands: each pod that stays keeps
            // its place on screen, a pod the booster took away leaves its place, and a pod not shown yet takes one.
            var kept = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < view.SlotCapacity; i++)
            {
                string? pod = view.PodInSlot(i);
                if (pod != null)
                {
                    kept.Add(pod);
                }
            }

            foreach (PodCompleted done in completes)
            {
                kept.Add(done.PodId);
            }

            foreach (SlotLook slot in Slots)
            {
                slot.Pending.Clear();
                if (slot.PodId != null && !kept.Contains(slot.PodId))
                {
                    slot.Clear();
                }
            }

            for (int i = 0; i < view.SlotCapacity; i++)
            {
                string? pod = view.PodInSlot(i);
                if (pod != null)
                {
                    Show(pod, i, view.Pod(pod).Remaining + (pending.TryGetValue(pod, out int n) ? n : 0), view);
                }
            }

            foreach (PodCompleted done in completes)
            {
                if (SlotOf(done.PodId) == null)
                {
                    Show(done.PodId, done.SlotIndex, pending.TryGetValue(done.PodId, out int n) ? n : 0, view);
                }
            }

            Enqueue(result.Events);
        }

        /// <summary>Shows everything pending at once, without walkers (before a booster changes the state).</summary>
        public void Flush(LevelView view)
        {
            _silent = true;
            Play(float.PositiveInfinity, view, live: false);
            _waves.Clear();
            Walkers.Clear();
            Array.Clear(_ready, 0, _ready.Length);
            _keyReady.Clear();
            foreach (SlotLook slot in Slots)
            {
                if (slot.IsLeaving)
                {
                    FinishLeave(slot);
                }
            }

            _silent = false;
        }

        /// <summary>
        /// Moves the animation on by <paramref name="realSeconds"/> of wall time: every wave start, arrival and wave end
        /// due by then happens in time order, each at its own moment of the clock.
        /// </summary>
        public void Advance(float realSeconds, LevelView view)
        {
            float rate = Math.Min(MaxRate, Speed * Math.Max(1f, Backlog / BacklogSeconds));
            float until = Now + (realSeconds * rate);
            Play(until, view, live: true);
            Now = Math.Max(Now, until);

            foreach (SlotLook slot in Slots)
            {
                if (slot.IsLeaving && Now - slot.LeavingAt >= ExitSeconds)
                {
                    FinishLeave(slot);
                }
            }

            Fades.RemoveAll(f => Now - f.Start >= FadeSeconds);
            Flights.RemoveAll(f => Now - f.Start >= FlightSeconds);
        }

        /// <summary>
        /// Plays every wave end, Bloomling arrival and wave start due by <paramref name="until"/> in time order (in that
        /// order on a tie, so what finishes at a moment shows before what starts then). <paramref name="live"/> moves the
        /// clock to each and sends the Bloomlings walking; <see cref="Flush"/> plays them all at once without either.
        /// </summary>
        private void Play(float until, LevelView view, bool live)
        {
            while (true)
            {
                Wave? next = null;
                int kind = 0;
                float at = float.PositiveInfinity;
                foreach (Wave wave in _waves)
                {
                    (float t, int k) = !wave.Started ? (wave.StartAt, 2)
                        : wave.NextArrival < wave.Work.Count ? (wave.Work[wave.NextArrival].At, 1)
                        : (wave.EndAt, 0);
                    if (next == null || t < at || (t == at && k < kind))
                    {
                        next = wave;
                        kind = k;
                        at = t;
                    }
                }

                if (next == null || at > until)
                {
                    break;
                }

                if (live)
                {
                    Now = Math.Max(Now, at);
                }

                if (kind == 2)
                {
                    Start(next, view, withWalkers: live);
                }
                else if (kind == 1)
                {
                    DeliverNext(next, view);
                }
                else
                {
                    Deliver(next, all: true, view);
                    End(next, view);
                    Walkers.RemoveAll(w => next.Walkers.Contains(w));
                    _waves.Remove(next);
                }
            }
        }

        private static SlotLook[] CreateSlots()
        {
            var slots = new SlotLook[WaitingSlots.Capacity];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new SlotLook();
            }

            return slots;
        }

        private int Index(CellPos cell) => (cell.Y * _width) + cell.X;

        // A walk's length: its travel, scaled into the wave's length.
        private static float ArrivalOf(Wave wave, Job job) => Math.Min(job.Travel, wave.Duration - RestoreSeconds);

        /// <summary>
        /// The slot on screen a pod the rules put in slot <paramref name="slot"/> shows in: that slot when it shows no
        /// pod, else the first usable slot that shows none (the rules' slot may still show a finished pod whose Bloomlings
        /// are on their way), else the rules' slot, where it waits until the shown pod leaves.
        /// </summary>
        private int Place(int slot, LevelView view)
        {
            if (Usable(slot, view) && Empty(Slots[slot]))
            {
                return slot;
            }

            for (int i = 0; i < Slots.Length && i < view.SlotCapacity; i++)
            {
                if (Usable(i, view) && Empty(Slots[i]))
                {
                    return i;
                }
            }

            return slot;
        }

        /// <summary>A slot pods can show in: not locked (by the rules, or on screen until its key's wave) and present.</summary>
        private bool Usable(int slot, LevelView view)
        {
            SlotState state = view.SlotStateOf(slot);
            return state != SlotState.Locked && state != SlotState.Absent && !HeldSlotLocks.Contains(slot);
        }

        private static bool Empty(SlotLook slot) => slot.PodId == null && slot.Pending.Count == 0;

        /// <summary>Shows a pod in its place (or a free place near the rules' slot) unless it shows already.</summary>
        private void Show(string podId, int slot, int count, LevelView view)
        {
            SlotLook? shown = SlotOf(podId);
            if (shown != null)
            {
                shown.Variant = view.Pod(podId).Variant;
                shown.Count = count;
                return;
            }

            SlotLook place = Slots[Place(slot, view)];
            if (place.PodId != null)
            {
                place.Pending.Enqueue((podId, view.Pod(podId).Variant, count));
                return;
            }

            place.PodId = podId;
            place.Variant = view.Pod(podId).Variant;
            place.Count = count;
        }

        /// <summary>Changes (or, given null, drops) a pod waiting in a slot's queue.</summary>
        private void Requeue(string podId, Func<(string PodId, VariantId? Variant, int Count), (string, VariantId?, int)?> change)
        {
            foreach (SlotLook slot in Slots)
            {
                if (slot.Pending.Count == 0)
                {
                    continue;
                }

                var items = new List<(string PodId, VariantId? Variant, int Count)>(slot.Pending);
                slot.Pending.Clear();
                foreach (var item in items)
                {
                    var kept = item.PodId == podId ? change(item) : item;
                    if (kept.HasValue)
                    {
                        slot.Pending.Enqueue(kept.Value);
                    }
                }
            }
        }

        /// <summary>
        /// How many usable slots show no pod now (a leaving pod still counts as shown): a tap needs one per pod it commits
        /// (the owner, 2026-10-05), so quick taps cannot stack pods behind the ones still working.
        /// </summary>
        public int FreeOnScreen(LevelView view)
        {
            int free = 0;
            for (int i = 0; i < Slots.Length && i < view.SlotCapacity; i++)
            {
                free += Usable(i, view) && Empty(Slots[i]) ? 1 : 0;
            }

            return free;
        }

        /// <summary>The slot on screen a pod shows in, or −1.</summary>
        public int PlaceOf(string podId)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].PodId == podId)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The rules' slot of a pod (for a booster aimed at the slot it shows in), or −1.</summary>
        public static int RulesSlotOf(LevelView view, string? podId)
        {
            for (int i = 0; podId != null && i < view.SlotCapacity; i++)
            {
                if (view.PodInSlot(i) == podId)
                {
                    return i;
                }
            }

            return -1;
        }

        private void Commit(int slotIndex, string podId, VariantId? variant, int count)
        {
            SlotLook slot = Slots[slotIndex];
            if (slot.PodId == null && slot.Pending.Count == 0)
            {
                slot.PodId = podId;
                slot.Variant = variant;
                slot.Count = count;
                slot.PoppedAt = Now;
            }
            else
            {
                slot.Pending.Enqueue((podId, variant, count));
            }
        }

        /// <summary>A mystery pod shows its variant on commit (FR-039): "?" flips over.</summary>
        private void Reveal(string podId, VariantId variant)
        {
            foreach (SlotLook slot in Slots)
            {
                if (slot.PodId == podId)
                {
                    slot.Variant = variant;
                    slot.RevealAt = Now;
                    return;
                }

                if (slot.Pending.Count > 0)
                {
                    var items = new List<(string, VariantId?, int)>(slot.Pending);
                    slot.Pending.Clear();
                    foreach ((string id, VariantId? v, int count) in items)
                    {
                        slot.Pending.Enqueue((id, id == podId ? variant : v, count));
                    }
                }
            }
        }

        /// <summary>A finished pod has left: the slot takes the next pod waiting in its own queue, else in any other one.</summary>
        private void FinishLeave(SlotLook slot)
        {
            slot.Clear();
            SlotLook? from = slot.Pending.Count > 0 ? slot : null;
            foreach (SlotLook other in Slots)
            {
                if (from == null && other.Pending.Count > 0)
                {
                    from = other;
                }
            }

            if (from != null)
            {
                (string id, VariantId? variant, int count) = from.Pending.Dequeue();
                slot.PodId = id;
                slot.Variant = variant;
                slot.Count = count;
                slot.PoppedAt = Now;
            }
        }

        /// <summary>Locks the rules opened in this command stay drawn until their wave plays.</summary>
        private void HoldLocks(IReadOnlyList<GameEvent> events)
        {
            foreach (GameEvent e in events)
            {
                if (e is LockOpened opened && e.Round > 0)
                {
                    if (opened.TargetKind == LockTargetKind.Pod)
                    {
                        HeldPodLocks.Add(opened.TargetId);
                    }
                    else if (opened.TargetKind == LockTargetKind.Slot && int.TryParse(opened.TargetId, out int slot))
                    {
                        HeldSlotLocks.Add(slot);
                    }
                }
            }
        }

        private void Enqueue(IReadOnlyList<GameEvent> events)
        {
            var waves = new List<Wave>();
            Wave? wave = null;
            for (int i = 0; i < events.Count; i++)
            {
                GameEvent e = events[i];
                if (e.Round == 0)
                {
                    continue;
                }

                if (wave == null || wave.Round != e.Round)
                {
                    wave = new Wave(e.Round);
                    waves.Add(wave);
                }

                switch (e)
                {
                    case TileCleared clear:
                        GameEvent? reveal = null;
                        if (i + 1 < events.Count && IsRevealOf(events[i + 1], clear.Cell))
                        {
                            reveal = events[++i];
                        }

                        float travel = Math.Max(1, clear.RouteFromEntry.Count) * StepSeconds;
                        wave.Work.Add(new Job(clear, reveal, travel));
                        wave.Duration = Math.Clamp(Math.Max(wave.Duration, travel + RestoreSeconds), MinWaveSeconds, MaxWaveSeconds);
                        break;
                    case MysteryTileRevealed:
                        wave.Start.Add(e);
                        break;
                    default:
                        wave.End.Add(e);
                        break;
                }
            }

            // This tap's waves one after another, from now. Each Bloomling sets off on its own as soon as it may: when every
            // cell of its route, and its target, has shown its earlier change (a tile an earlier tap clears, a layer revealed
            // under it), so it may follow an earlier tap's Bloomlings closely while their waves still play, whatever its
            // wave's other Bloomlings wait for (the owner, 2026-10-04). The wave ends after its last arrival, and late enough
            // that its end events keep the rules' order.
            float earliest = Now;
            foreach (Wave w in waves)
            {
                float start = earliest;
                foreach (CellPos cell in CellsAt(w.Start))
                {
                    start = Math.Max(start, _ready[Index(cell)]);
                }

                float end = start + w.Duration;
                foreach (Job job in w.Work)
                {
                    IReadOnlyList<CellPos> route = job.Clear.RouteFromEntry;
                    float arrival = ArrivalOf(w, job);
                    float go = start;
                    for (int j = 0; j < route.Count; j++)
                    {
                        // The walker reaches route cell j at (j + 1) / count of its walk (BoardPainter.DrawWalkers).
                        float reach = arrival * (j + 1) / route.Count;
                        go = Math.Max(go, _ready[Index(route[j])] + StepMargin - reach);
                    }

                    go = Math.Max(go, _ready[Index(job.Clear.Cell)] + StepMargin - arrival);
                    job.Go = go;
                    job.At = go + arrival;
                    end = Math.Max(end, job.At + RestoreSeconds);
                }

                // Arrivals in time order (ties: the shorter walk first).
                w.Work.Sort((x, y) => x.At != y.At ? x.At.CompareTo(y.At) : x.Travel.CompareTo(y.Travel));

                foreach (CellPos cell in CellsAt(w.End))
                {
                    end = Math.Max(end, _ready[Index(cell)]);
                }

                foreach (string key in KeysAtEnd(w))
                {
                    if (_keyReady.TryGetValue(key, out float ready))
                    {
                        end = Math.Max(end, ready + KeyGap);
                    }
                }

                if (w.End.Exists(e => e is LevelWon || e is LevelJammed || e is LevelStuck))
                {
                    // The level's outcome (its sound; the end card waits for Settled) shows after all an earlier tap still plays.
                    foreach (Wave other in _waves)
                    {
                        end = Math.Max(end, other.EndAt + KeyGap);
                    }
                }

                w.StartAt = start;
                w.EndAt = end;
                foreach (string key in KeysAtEnd(w))
                {
                    _keyReady[key] = Math.Max(_keyReady.TryGetValue(key, out float k) ? k : 0f, end);
                }

                foreach (Job job in w.Work)
                {
                    string key = "pod:" + job.Clear.PodId;
                    _keyReady[key] = Math.Max(_keyReady.TryGetValue(key, out float k) ? k : 0f, end);
                    int cell = Index(job.Clear.Cell);
                    _ready[cell] = Math.Max(_ready[cell], job.At);
                }

                foreach (CellPos cell in CellsAt(w.Start))
                {
                    _ready[Index(cell)] = Math.Max(_ready[Index(cell)], start);
                }

                foreach (CellPos cell in CellsAt(w.End))
                {
                    _ready[Index(cell)] = Math.Max(_ready[Index(cell)], end);
                }

                _waves.Add(w);
                earliest = end;
            }
        }

        // How much later than the last wave changing the same special or pod a wave's end comes.
        private const float KeyGap = 0.001f;

        // How long after a cell's earlier change has shown a later Bloomling may step on it.
        private const float StepMargin = 0.05f;

        /// <summary>The specials and pods a wave's end events change, which must show in the rules' order.</summary>
        private static IEnumerable<string> KeysAtEnd(Wave wave)
        {
            foreach (GameEvent e in wave.End)
            {
                switch (e)
                {
                    case SpecialProgressed progressed:
                        yield return "special:" + progressed.SpecialId;
                        break;
                    case SpecialTriggered triggered:
                        yield return "special:" + triggered.SpecialId;
                        break;
                    case PodCompleted done:
                        yield return "pod:" + done.PodId;
                        break;
                }
            }
        }

        /// <summary>The cells a wave's start or end events change on screen (a revealed mystery tile, a key, a special's cells).</summary>
        private static IEnumerable<CellPos> CellsAt(List<GameEvent> events)
        {
            foreach (GameEvent e in events)
            {
                switch (e)
                {
                    case MysteryTileRevealed revealed:
                        yield return revealed.Cell;
                        break;
                    case KeyCollected key:
                        yield return key.Cell;
                        break;
                    case SpecialTriggered triggered:
                        foreach (CellPos cell in triggered.EffectCells)
                        {
                            yield return cell;
                        }

                        break;
                }
            }
        }

        private void Start(Wave wave, LevelView view, bool withWalkers)
        {
            foreach (GameEvent e in wave.Start)
            {
                if (e is MysteryTileRevealed revealed)
                {
                    CellInfo old = Cell(revealed.Cell);
                    _cells[Index(revealed.Cell)] = new CellInfo(old.Kind, revealed.Variant, old.Next, old.RemainingLayers, false, old.KeyId, old.SpecialId, old.IsEntry);
                }
            }

            // Each walker sets off at its own time (Job.Go, it shows from then) and arrives at Job.At; the work is sorted by
            // arrival when the wave is queued.
            wave.Started = true;
            foreach (Job job in wave.Work)
            {
                TileCleared clear = job.Clear;
                SlotLook? slot = SlotOf(clear.PodId);
                if (slot != null)
                {
                    slot.InFlight++;
                }

                if (withWalkers && Walkers.Count < 40)
                {
                    var walker = new Walker(clear.RouteFromEntry, clear.Variant, job.Go, job.At - job.Go);
                    Walkers.Add(walker);
                    wave.Walkers.Add(walker);
                }
            }
        }

        /// <summary>The next Bloomling of a started wave arrives.</summary>
        private void DeliverNext(Wave wave, LevelView view) => Arrive(wave, wave.NextArrival++, view);

        /// <summary>Every Bloomling of a wave still on its way arrives (its end, or a flush).</summary>
        private void Deliver(Wave wave, bool all, LevelView view)
        {
            while (all && wave.NextArrival < wave.Work.Count)
            {
                Arrive(wave, wave.NextArrival++, view);
            }
        }

        private void Arrive(Wave wave, int index, LevelView view)
        {
            {
                TileCleared clear = wave.Work[index].Clear;
                GameEvent? reveal = wave.Work[index].Reveal;
                CellInfo old = Cell(clear.Cell);
                Fades.Add(new Fade(clear.Cell, old, Now));
                _cells[Index(clear.Cell)] = reveal switch
                {
                    LayerRevealed layer => new CellInfo(
                        CellKind.Target,
                        layer.NewTopVariant,
                        view.Cell(clear.Cell).Visible == layer.NewTopVariant ? view.Cell(clear.Cell).Next : null,
                        Math.Max(1, old.RemainingLayers - 1),
                        false,
                        old.KeyId,
                        null,
                        old.IsEntry),
                    _ => new CellInfo(CellKind.Open, null, null, 0, false, null, null, old.IsEntry),
                };

                SlotLook? slot = SlotOf(clear.PodId);
                if (slot != null)
                {
                    slot.Count = Math.Max(0, slot.Count - 1);
                    slot.InFlight = Math.Max(0, slot.InFlight - 1);
                    slot.BumpedAt = Now;
                }
                else
                {
                    // A pod still waiting for a slot free on screen counts down where it waits.
                    Requeue(clear.PodId, item => (item.PodId, item.Variant, Math.Max(0, item.Count - 1)));
                }

                if (!_silent)
                {
                    Arrived?.Invoke(clear);
                }
            }
        }

        private void End(Wave wave, LevelView view)
        {
            foreach (GameEvent e in wave.End)
            {
                ApplyEnd(e, view);
            }
        }

        private void ApplyEnd(GameEvent e, LevelView view)
        {
            if (!_silent)
            {
                Shown?.Invoke(e);
            }

            switch (e)
            {
                case PodCompleted done:
                    SlotLook? slot = SlotOf(done.PodId);
                    if (slot != null)
                    {
                        slot.LeavingAt = Now;
                    }
                    else
                    {
                        // Finished before a slot freed on screen for it: it never shows.
                        Requeue(done.PodId, _ => null);
                    }

                    break;
                case KeyCollected key:
                    CellInfo old = Cell(key.Cell);
                    _cells[Index(key.Cell)] = new CellInfo(old.Kind, old.Visible, old.Next, old.RemainingLayers, old.MysteryHidden, null, old.SpecialId, old.IsEntry);
                    break;
                case LockOpened opened:
                    if (opened.TargetKind == LockTargetKind.Pod)
                    {
                        HeldPodLocks.Remove(opened.TargetId);
                    }
                    else if (opened.TargetKind == LockTargetKind.Slot && int.TryParse(opened.TargetId, out int index))
                    {
                        HeldSlotLocks.Remove(index);
                    }

                    break;
                case SpecialProgressed progressed:
                    _specials[progressed.SpecialId] = (progressed.Progress, progressed.Total, false);
                    break;
                case SpecialTriggered triggered:
                    (int progress, int total, bool _) = Special(triggered.SpecialId);
                    _specials[triggered.SpecialId] = (progress, total, true);
                    foreach (CellPos cell in triggered.EffectCells)
                    {
                        _cells[Index(cell)] = view.Cell(cell);
                    }

                    foreach (SpecialInfo special in view.Specials)
                    {
                        if (special.Id == triggered.SpecialId)
                        {
                            foreach (CellPos cell in special.Cells)
                            {
                                _cells[Index(cell)] = view.Cell(cell);
                            }
                        }
                    }

                    break;
            }
        }

        private SlotLook? SlotOf(string podId)
        {
            foreach (SlotLook slot in Slots)
            {
                if (slot.PodId == podId)
                {
                    return slot;
                }
            }

            return null;
        }

        private static bool IsRevealOf(GameEvent e, CellPos cell) =>
            (e is CellOpened opened && opened.Cell == cell) || (e is LayerRevealed layer && layer.Cell == cell);

        /// <summary>One Bloomling's work in a wave: the tile it clears, what shows under it, its walk and its times.</summary>
        private sealed class Job
        {
            public Job(TileCleared clear, GameEvent? reveal, float travel)
            {
                Clear = clear;
                Reveal = reveal;
                Travel = travel;
            }

            public TileCleared Clear { get; }

            public GameEvent? Reveal { get; }

            public float Travel { get; }

            /// <summary>When it sets off on the animation clock.</summary>
            public float Go { get; set; }

            /// <summary>When it arrives (its tile changes).</summary>
            public float At { get; set; }
        }

        private sealed class Wave
        {
            public Wave(int round)
            {
                Round = round;
                Duration = MinWaveSeconds;
            }

            public int Round { get; }

            public float Duration { get; set; }

            /// <summary>When the wave starts on the animation clock (after its tap's earlier waves and every change it depends on).</summary>
            public float StartAt { get; set; }

            /// <summary>When it ends (its end events show): after its last arrival and in the rules' order.</summary>
            public float EndAt { get; set; }

            public bool Started { get; set; }

            /// <summary>The next of <see cref="Work"/> to arrive.</summary>
            public int NextArrival { get; set; }

            /// <summary>The walkers it set off (they go when it ends).</summary>
            public List<Walker> Walkers { get; } = new List<Walker>();

            public List<GameEvent> Start { get; } = new List<GameEvent>();

            public List<Job> Work { get; } = new List<Job>();

            public List<GameEvent> End { get; } = new List<GameEvent>();
        }
    }
}
