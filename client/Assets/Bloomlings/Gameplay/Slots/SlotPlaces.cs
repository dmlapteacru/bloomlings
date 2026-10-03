using System;
using System.Collections.Generic;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.Gameplay.Slots
{
    /// <summary>What one Waiting Slot shows on screen (its place), and the pods waiting to show there.</summary>
    public sealed class SlotPlace
    {
        internal SlotPlace(int index)
        {
            Index = index;
        }

        public int Index { get; }

        /// <summary>The pod shown here (a finished one while it leaves), or null.</summary>
        public string? PodId { get; internal set; }

        /// <summary>Null while a mystery pod still shows "?".</summary>
        public VariantId? Variant { get; internal set; }

        /// <summary>The count shown: the pod's remaining work less the Bloomlings that arrived so far.</summary>
        public int Count { get; internal set; }

        /// <summary>The pod finished and is leaving its plate.</summary>
        public bool Leaving { get; internal set; }

        /// <summary>How many pods wait to show here.</summary>
        public int Waiting => Queue.Count;

        /// <summary>No pod shows here and none waits for it.</summary>
        public bool ShowsNone => PodId == null && Queue.Count == 0;

        internal Queue<(string PodId, VariantId? Variant, int Count)> Queue { get; } = new Queue<(string, VariantId?, int)>();

        internal void Clear()
        {
            PodId = null;
            Variant = null;
            Count = 0;
            Leaving = false;
        }

        internal void Show(string podId, VariantId? variant, int count)
        {
            PodId = podId;
            Variant = variant;
            Count = count;
            Leaving = false;
        }
    }

    /// <summary>
    /// Where each pod shows in the Waiting Slots row (engine-free; <see cref="SlotRowView"/> draws it; the playtest's
    /// <c>LevelAnimator.Place</c>). The rules free a finished pod's slot at once (FR-022) while its last Bloomlings may
    /// still be on their way and the pod still shows, so a pod the rules commit to that slot would land on the finishing
    /// one (the owner's report of 2026-10-03). A committed pod therefore shows in its rules' slot when that slot shows no
    /// pod and is usable (not locked by the rules, not still drawn locked until its key lands, not the absent extra slot),
    /// else in the first usable slot that shows no pod, else it waits in the rules' slot's queue (a rules' slot still
    /// drawn locked that shows nothing at all takes it at once, as before). A waiting pod counts
    /// down where it waits and is dropped if it finishes before it shows; a slot whose pod has left (or whose lock has
    /// opened) takes the next pod waiting in its own queue, else in any other. After a booster each pod that stays keeps
    /// its place (<see cref="Rebuild"/>). Where a pod shows never changes an outcome: the rules decide which
    /// slot it holds (a booster aimed at a shown pod uses <see cref="RulesSlotOf"/>).
    /// </summary>
    public sealed class SlotPlaces
    {
        private readonly SlotPlace[] _places;

        public SlotPlaces(int count = WaitingSlots.Capacity)
        {
            _places = new SlotPlace[count];
            for (int i = 0; i < count; i++)
            {
                _places[i] = new SlotPlace(i);
            }
        }

        public IReadOnlyList<SlotPlace> Places => _places;

        public SlotPlace this[int place] => _places[place];

        /// <summary>The rules' slot of a pod (for a booster aimed at the place it shows in), or −1 when the rules hold it in none.</summary>
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

        /// <summary>
        /// The pods to show for <see cref="Rebuild"/>: those the rules hold in a slot, with the counts before the work of
        /// <paramref name="settling"/> (a booster's events whose settle rounds are still to play) lands, then those that
        /// finish in those rounds (shown until their wave plays), so the timeline's decrements end at the logical counts.
        /// </summary>
        public static List<(string PodId, int RulesSlot, VariantId? Variant, int Count)> ToShow(LevelView view, IReadOnlyList<GameEvent>? settling)
        {
            var pending = new Dictionary<string, int>(StringComparer.Ordinal);
            var finishing = new List<PodCompleted>();
            if (settling != null)
            {
                foreach (GameEvent e in settling)
                {
                    if (e.Round > 0 && e is TileCleared clear)
                    {
                        pending[clear.PodId] = (pending.TryGetValue(clear.PodId, out int n) ? n : 0) + 1;
                    }
                    else if (e.Round > 0 && e is PodCompleted done)
                    {
                        finishing.Add(done);
                    }
                }
            }

            var pods = new List<(string PodId, int RulesSlot, VariantId? Variant, int Count)>();
            var held = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < view.SlotCapacity; i++)
            {
                string? podId = view.PodInSlot(i);
                if (podId != null)
                {
                    PodInfo pod = view.Pod(podId);
                    pods.Add((podId, i, pod.Variant, pod.Remaining + (pending.TryGetValue(podId, out int n) ? n : 0)));
                    held.Add(podId);
                }
            }

            foreach (PodCompleted done in finishing)
            {
                if (held.Add(done.PodId))
                {
                    pods.Add((done.PodId, done.SlotIndex, view.Pod(done.PodId).Variant, pending.TryGetValue(done.PodId, out int n) ? n : 0));
                }
            }

            return pods;
        }

        /// <summary>The place a pod shows in, or −1 (not shown, or only waiting).</summary>
        public int PlaceOf(string? podId)
        {
            for (int i = 0; podId != null && i < _places.Length; i++)
            {
                if (_places[i].PodId == podId)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// The place for a pod the rules put in slot <paramref name="rulesSlot"/>: that slot when it is usable and shows no
        /// pod, else the first usable place that shows none, else the rules' slot (where it waits).
        /// </summary>
        public int Choose(int rulesSlot, Func<int, bool> usable)
        {
            if (rulesSlot >= 0 && rulesSlot < _places.Length && usable(rulesSlot) && _places[rulesSlot].ShowsNone)
            {
                return rulesSlot;
            }

            for (int i = 0; i < _places.Length; i++)
            {
                if (usable(i) && _places[i].ShowsNone)
                {
                    return i;
                }
            }

            return rulesSlot;
        }

        /// <summary>
        /// A pod was committed to the rules' slot <paramref name="rulesSlot"/> with <paramref name="count"/> before any
        /// work: it shows in its place now (<paramref name="shown"/>), or waits in its queue. Returns the place, where its
        /// tile flies to.
        /// </summary>
        public int Commit(int rulesSlot, string podId, VariantId? variant, int count, Func<int, bool> usable, out bool shown)
        {
            int place = Choose(rulesSlot, usable);
            SlotPlace target = _places[place];
            shown = target.ShowsNone;
            if (shown)
            {
                target.Show(podId, variant, count);
            }
            else
            {
                target.Queue.Enqueue((podId, variant, count));
            }

            return place;
        }

        /// <summary>A mystery pod shows its variant (FR-039): the place it shows in, or −1 while it waits (its queue entry turns).</summary>
        public int Reveal(string podId, VariantId variant)
        {
            int place = PlaceOf(podId);
            if (place >= 0)
            {
                _places[place].Variant = variant;
                return place;
            }

            Requeue(podId, item => (item.PodId, variant, item.Count));
            return -1;
        }

        /// <summary>A Bloomling of the pod arrived: its count drops where it shows (the place), or where it waits (−1).</summary>
        public int Decrement(string podId)
        {
            int place = PlaceOf(podId);
            if (place >= 0)
            {
                _places[place].Count = Math.Max(0, _places[place].Count - 1);
                return place;
            }

            Requeue(podId, item => (item.PodId, item.Variant, Math.Max(0, item.Count - 1)));
            return -1;
        }

        /// <summary>
        /// The pod finished (PodCompleted): it starts leaving the place it shows in, which is returned (call
        /// <see cref="FinishLeave"/> when it has gone); a pod still waiting never shows (−1).
        /// </summary>
        public int Complete(string podId)
        {
            int place = PlaceOf(podId);
            if (place >= 0)
            {
                if (_places[place].Leaving)
                {
                    return -1;
                }

                _places[place].Leaving = true;
                return place;
            }

            Requeue(podId, _ => null);
            return -1;
        }

        /// <summary>The pod of <paramref name="place"/> has left: the place takes the next waiting pod (<see cref="TakeWaiting"/>); true when one moved in.</summary>
        public bool FinishLeave(int place)
        {
            _places[place].Clear();
            return TakeWaiting(place);
        }

        /// <summary>
        /// A place that shows no pod (its pod left, or its lock opened) takes the next pod waiting in its own queue, else in
        /// any other; true when one moved in.
        /// </summary>
        public bool TakeWaiting(int place)
        {
            SlotPlace slot = _places[place];
            if (slot.PodId != null)
            {
                return false;
            }

            SlotPlace? from = slot.Queue.Count > 0 ? slot : null;
            for (int i = 0; from == null && i < _places.Length; i++)
            {
                if (_places[i].Queue.Count > 0)
                {
                    from = _places[i];
                }
            }

            if (from == null)
            {
                return false;
            }

            (string id, VariantId? variant, int count) = from.Queue.Dequeue();
            slot.Show(id, variant, count);
            return true;
        }

        /// <summary>
        /// Shows exactly <paramref name="pods"/> (each with the rules' slot it holds, or the slot it finished in): each pod
        /// that shows already keeps its place, with its new variant and count; every other pod (one a booster removed, one
        /// leaving) frees its place, and every waiting pod is forgotten; a pod not shown yet then takes a place, as a
        /// committed one does. With nothing shown before (<see cref="Clear"/>), each pod shows in its rules' slot.
        /// </summary>
        public void Rebuild(IReadOnlyList<(string PodId, int RulesSlot, VariantId? Variant, int Count)> pods, Func<int, bool> usable)
        {
            var keep = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pod in pods)
            {
                keep.Add(pod.PodId);
            }

            foreach (SlotPlace place in _places)
            {
                place.Queue.Clear();
                if (place.PodId != null && (place.Leaving || !keep.Contains(place.PodId)))
                {
                    place.Clear();
                }
            }

            foreach ((string podId, int rulesSlot, VariantId? variant, int count) in pods)
            {
                int shown = PlaceOf(podId);
                if (shown >= 0)
                {
                    _places[shown].Variant = variant;
                    _places[shown].Count = count;
                }
                else
                {
                    Commit(rulesSlot, podId, variant, count, usable, out bool _);
                }
            }
        }

        /// <summary>Shows nothing (level start, restart).</summary>
        public void Clear()
        {
            foreach (SlotPlace place in _places)
            {
                place.Clear();
                place.Queue.Clear();
            }
        }

        /// <summary>How many usable places show no pod (the last one is marked as the jam risk).</summary>
        public int FreeOnScreen(Func<int, bool> usable)
        {
            int free = 0;
            for (int i = 0; i < _places.Length; i++)
            {
                free += usable(i) && _places[i].PodId == null ? 1 : 0;
            }

            return free;
        }

        /// <summary>Changes (or, given null, drops) a pod waiting in a queue.</summary>
        private void Requeue(string podId, Func<(string PodId, VariantId? Variant, int Count), (string, VariantId?, int)?> change)
        {
            foreach (SlotPlace place in _places)
            {
                if (place.Queue.Count == 0)
                {
                    continue;
                }

                var items = new List<(string PodId, VariantId? Variant, int Count)>(place.Queue);
                place.Queue.Clear();
                foreach ((string PodId, VariantId? Variant, int Count) item in items)
                {
                    (string, VariantId?, int)? kept = item.PodId == podId ? change(item) : item;
                    if (kept.HasValue)
                    {
                        place.Queue.Enqueue(kept.Value);
                    }
                }
            }
        }
    }
}
