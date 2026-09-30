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

    /// <summary>A Bloomling walking its route: the entry cell first, the target last.</summary>
    public sealed record Walker(IReadOnlyList<CellPos> Route, VariantId Variant, float Arrival);

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
    /// </summary>
    public sealed class LevelAnimator
    {
        public const float StepSeconds = 0.09f;
        public const float RestoreSeconds = 0.2f;
        public const float MinWaveSeconds = 0.3f;
        public const float MaxWaveSeconds = 1.6f;
        public const float ExitSeconds = 0.25f;
        public const float FadeSeconds = 0.2f;
        public const float FlightSeconds = 0.2f;
        private const float MaxRate = 4f;
        private const float BacklogSeconds = 1.5f;

        private readonly Queue<Wave> _waves = new Queue<Wave>();
        private readonly Dictionary<string, (int Progress, int Total, bool Triggered)> _specials = new Dictionary<string, (int, int, bool)>(StringComparer.Ordinal);
        private CellInfo[] _cells = Array.Empty<CellInfo>();
        private int _width;
        private Wave? _current;
        private float _time;
        private int _nextArrival;
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

        /// <summary>The current wave's clock, for the walkers.</summary>
        public float WaveTime => _time;

        /// <summary>Every event of the rules has been shown.</summary>
        public bool Settled => _current == null && _waves.Count == 0;

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

        public float Backlog
        {
            get
            {
                float total = _current == null ? 0f : Math.Max(0f, _current.Duration - _time);
                foreach (Wave wave in _waves)
                {
                    total += wave.Duration;
                }

                return total;
            }
        }

        public CellInfo Cell(CellPos pos) => _cells[(pos.Y * _width) + pos.X];

        public (int Progress, int Total, bool Triggered) Special(string id) =>
            _specials.TryGetValue(id, out (int, int, bool) state) ? state : (0, 0, true);

        /// <summary>Shows the level exactly as the rules have it now, with nothing pending (load, restart).</summary>
        public void Reset(LevelView view)
        {
            _waves.Clear();
            _current = null;
            _time = 0f;
            Walkers.Clear();
            Fades.Clear();
            Flights.Clear();
            HeldPodLocks.Clear();
            HeldSlotLocks.Clear();
            _width = view.Width;
            _cells = new CellInfo[view.Width * view.Height];
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
                        Commit(committed.SlotIndex, committed.PodId, shown, count);
                        if (!float.IsNaN(x))
                        {
                            Flights.Add(new Flight(x, y, committed.SlotIndex, shown, Now, false, committed.PodId));
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

            // The slots as the rules have them, with the counts before the queued work lands.
            for (int i = 0; i < Slots.Length; i++)
            {
                SlotLook slot = Slots[i];
                slot.Clear();
                slot.Pending.Clear();
                string? pod = view.PodInSlot(i);
                if (pod != null)
                {
                    PodInfo info = view.Pod(pod);
                    slot.PodId = pod;
                    slot.Variant = info.Variant;
                    slot.Count = info.Remaining + (pending.TryGetValue(pod, out int n) ? n : 0);
                }
            }

            foreach (PodCompleted done in completes)
            {
                SlotLook slot = Slots[done.SlotIndex];
                if (slot.PodId == null)
                {
                    slot.PodId = done.PodId;
                    slot.Variant = view.Pod(done.PodId).Variant;
                    slot.Count = pending.TryGetValue(done.PodId, out int n) ? n : 0;
                }
            }

            Enqueue(result.Events);
        }

        /// <summary>Shows everything pending at once, without walkers (before a booster changes the state).</summary>
        public void Flush(LevelView view)
        {
            _silent = true;
            if (_current != null)
            {
                Deliver(_current, all: true, view);
                End(_current, view);
                _current = null;
            }

            while (_waves.Count > 0)
            {
                Wave wave = _waves.Dequeue();
                Start(wave, view, withWalkers: false);
                Deliver(wave, all: true, view);
                End(wave, view);
            }

            Walkers.Clear();
            _time = 0f;
            foreach (SlotLook slot in Slots)
            {
                if (slot.IsLeaving)
                {
                    FinishLeave(slot);
                }
            }

            _silent = false;
        }

        /// <summary>Moves the animation on by <paramref name="realSeconds"/> of wall time.</summary>
        public void Advance(float realSeconds, LevelView view)
        {
            float rate = Math.Min(MaxRate, Speed * Math.Max(1f, Backlog / BacklogSeconds));
            float dt = realSeconds * rate;
            Now += dt;
            while (dt > 0f)
            {
                if (_current == null)
                {
                    if (_waves.Count == 0)
                    {
                        break;
                    }

                    _current = _waves.Dequeue();
                    _time = 0f;
                    _nextArrival = 0;
                    Start(_current, view, withWalkers: true);
                }

                float step = Math.Min(dt, _current.Duration - _time);
                _time += step;
                dt -= step;
                Deliver(_current, all: false, view);
                if (_time >= _current.Duration)
                {
                    Deliver(_current, all: true, view);
                    End(_current, view);
                    Walkers.Clear();
                    _current = null;
                }
            }

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

        private void FinishLeave(SlotLook slot)
        {
            slot.Clear();
            if (slot.Pending.Count > 0)
            {
                (string id, VariantId? variant, int count) = slot.Pending.Dequeue();
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
                    _waves.Enqueue(wave);
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
                        wave.Work.Add((clear, reveal, travel));
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

            // Arrivals in travel order; a walker's arrival is scaled into the wave's length.
            wave.Work.Sort((a, b) => a.Travel.CompareTo(b.Travel));
            foreach ((TileCleared clear, GameEvent? _, float travel) in wave.Work)
            {
                SlotLook? slot = SlotOf(clear.PodId);
                if (slot != null)
                {
                    slot.InFlight++;
                }

                if (withWalkers && Walkers.Count < 40)
                {
                    Walkers.Add(new Walker(clear.RouteFromEntry, clear.Variant, Math.Min(travel, wave.Duration - RestoreSeconds)));
                }
            }
        }

        private void Deliver(Wave wave, bool all, LevelView view)
        {
            while (_nextArrival < wave.Work.Count
                && (all || Math.Min(wave.Work[_nextArrival].Travel, wave.Duration - RestoreSeconds) <= _time))
            {
                (TileCleared clear, GameEvent? reveal, float _) = wave.Work[_nextArrival++];
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

                if (!_silent)
                {
                    Arrived?.Invoke(clear);
                }
            }

            if (all)
            {
                _nextArrival = 0;
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

        private sealed class Wave
        {
            public Wave(int round)
            {
                Round = round;
                Duration = MinWaveSeconds;
            }

            public int Round { get; }

            public float Duration { get; set; }

            public List<GameEvent> Start { get; } = new List<GameEvent>();

            public List<(TileCleared Clear, GameEvent? Reveal, float Travel)> Work { get; } = new List<(TileCleared, GameEvent?, float)>();

            public List<GameEvent> End { get; } = new List<GameEvent>();
        }
    }
}
