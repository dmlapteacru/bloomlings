using System.Collections;
using System.Collections.Generic;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.Gameplay.Effects;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Slots
{
    /// <summary>
    /// The Waiting Slots (T044) as the reference's cream portrait plates in the first row of the tray (spec 005 FR-020,
    /// contracts/look.md §3.7, §4.1, §6.1; the playtest's <c>SlotPainter</c>, <see cref="UiKit.SlotPlate"/>), placed in the
    /// boxes <see cref="CellsFor"/> gives: 5 slots, plus the sixth when Extra Slot adds it (US5), marked with a green "+",
    /// which narrows the others. Each shows its pod's variant tile and remaining count and its state (FR-015, FR-070):
    /// <list type="bullet">
    /// <item><description>empty: a slightly sunk plate with a dashed inner outline;</description></item>
    /// <item><description>working: a raised plate with the variant's sticker tile and its plain count below;</description></item>
    /// <item><description>stuck (waiting): the tile in grey with the hourglass;</description></item>
    /// <item><description>locked: a grey plate with the padlock, kept until its key lands;</description></item>
    /// <item><description>danger: the last free usable slot, its dashed outline red with "!" (never color alone).</description></item>
    /// </list>
    /// The counts follow the event timeline: they drop, with a small bump, as Bloomlings arrive. A pod pops onto its plate
    /// when committed (a mystery pod shows the "?" tile, which turns over to its variant), and puffs away off the plate,
    /// which is empty again under it; a pod committed while its slot still animates the previous pod's exit waits in a
    /// visual queue (R4). While Return chooses its slot, every plate it can take a pod back from glows; on a jam every
    /// occupied slot shakes with a red ring.
    /// </summary>
    public sealed class SlotRowView : MonoBehaviour
    {
        /// <summary>
        /// A plate's width for its height: the reference's portrait plates, 0.165 W wide in a 0.19 W row (spec 005
        /// contracts/look.md §6.1).
        /// </summary>
        public const float PlateAspect = 0.165f / 0.19f;

        private const float ExitSeconds = 0.22f;

        private readonly Slot[] _slots = new Slot[WaitingSlots.Capacity];
        private readonly HashSet<int> _heldLocks = new HashSet<int>();
        private RectTransform _area = null!;
        private VariantVisualCatalog? _visuals;
        private LevelView? _lastView;

        /// <summary>A slot was tapped while targeting (Return picks the pod to send back, T120).</summary>
        public event System.Action<int>? SlotTapped;

        /// <summary>
        /// The plates for a number of Waiting Slots, in the slot row's top-down canvas units (the HUD's
        /// <c>GameplayHud.SlotCells</c>: the reference's portrait plates spread across the row, spec 005 §6.1); null lays
        /// them out with <see cref="Cells"/>.
        /// </summary>
        public System.Func<int, IReadOnlyList<Box>>? CellsFor { get; set; }

        public static SlotRowView Create(RectTransform area, VariantVisualCatalog? visuals)
        {
            var view = area.gameObject.AddComponent<SlotRowView>();
            view._area = area;
            view._visuals = visuals;
            for (int i = 0; i < WaitingSlots.Capacity; i++)
            {
                Slot slot = Slot.Create(area, i, extra: i >= WaitingSlots.DefaultCount);
                view._slots[i] = slot;
                Button button = slot.Frame.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = slot.Frame;
                int index = i;
                button.onClick.AddListener(() => view.SlotTapped?.Invoke(index));
                slot.Frame.raycastTarget = false;
            }

            return view;
        }

        /// <summary>Lets occupied slots take taps (Return's target) and makes them glow, or stops it.</summary>
        public void SetTargeting(bool on)
        {
            foreach (Slot slot in _slots)
            {
                bool target = on && slot.PodId != null && !slot.Leaving;
                slot.Frame.raycastTarget = target;
                slot.SetGlow(target);
                slot.SetHighlight(null);
            }

            if (!on && _lastView != null)
            {
                UpdateStates(_lastView);
            }
        }

        /// <summary>Forgets locks held for flying keys (level start and restart).</summary>
        public void ReleaseLocks() => _heldLocks.Clear();

        /// <summary>Resets every slot to the logical state (level start, restart, boosters); locks held for a flying key stay.</summary>
        /// <param name="settling">
        /// The events of a booster whose settle rounds are still to play: the counts are shown before that work lands, and
        /// a pod that finishes in those rounds stays in its slot until its wave, so the timeline's decrements end at the
        /// logical counts.
        /// </param>
        public void Reset(LevelView view, IReadOnlyList<GameEvent>? settling = null)
        {
            StopAllCoroutines();
            var pending = new Dictionary<string, int>(System.StringComparer.Ordinal);
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

            foreach (Slot slot in _slots)
            {
                slot.Clear();
                slot.Pending.Clear();
                slot.SetHighlight(null);
                string? podId = view.PodInSlot(slot.Index);
                if (podId != null)
                {
                    PodInfo pod = view.Pod(podId);
                    slot.Occupy(podId, pod.Variant, pod.Remaining + (pending.TryGetValue(podId, out int n) ? n : 0));
                }
            }

            foreach (PodCompleted done in finishing)
            {
                Slot slot = _slots[done.SlotIndex];
                if (slot.PodId == null)
                {
                    slot.Occupy(done.PodId, view.Pod(done.PodId).Variant, pending.TryGetValue(done.PodId, out int n) ? n : 0);
                }
            }

            UpdateStates(view);
        }

        /// <summary>A pod was committed (immediate feedback, SC-008); <paramref name="count"/> is its count before any work.</summary>
        /// <param name="variant">Null for a mystery pod: it shows the "?" tile until <see cref="RevealVariant"/> turns it (FR-039).</param>
        public void Commit(int slotIndex, string podId, VariantId? variant, int count)
        {
            Slot slot = _slots[slotIndex];
            if (slot.PodId == null && slot.Pending.Count == 0)
            {
                slot.Occupy(podId, variant, count);
                Animate(UiFx.Pop(slot.Body, 1.12f, 0.16f));
            }
            else
            {
                slot.Pending.Enqueue((podId, variant, count));
            }
        }

        /// <summary>A mystery pod shows its exact variant on commit (FR-039): its tile turns over.</summary>
        public void RevealVariant(string podId, VariantId variant)
        {
            foreach (Slot slot in _slots)
            {
                if (slot.PodId == podId)
                {
                    if (isActiveAndEnabled)
                    {
                        StartCoroutine(Flip(slot, variant));
                    }
                    else
                    {
                        slot.SetVariant(variant);
                    }

                    return;
                }
            }

            // Still queued behind an exiting pod: it will show its variant when it moves in.
            foreach (Slot slot in _slots)
            {
                var items = new List<(string PodId, VariantId? Variant, int Count)>(slot.Pending);
                slot.Pending.Clear();
                foreach ((string id, VariantId? v, int count) in items)
                {
                    slot.Pending.Enqueue((id, id == podId ? variant : v, count));
                }
            }
        }

        /// <summary>World position of a slot, where a key for a locked slot lands (T107) and a committed pod flies to.</summary>
        public Vector3 SlotPosition(int slotIndex) => _slots[slotIndex].Frame.transform.position;

        public RectTransform SlotRect(int slotIndex) => _slots[slotIndex].Frame.rectTransform;

        /// <summary>The width of a slot's plate, for the flying pod.</summary>
        public float SlotSize => _slots[0].Frame.rectTransform.sizeDelta.x;

        /// <summary>World position of the tile on a working slot's plate: a committed pod's tile lands there.</summary>
        public Vector3 TilePosition(int slotIndex) => _slots[slotIndex].TileTransform.position;

        /// <summary>The side of the tile on a working slot's plate, in canvas units (the flying tile ends at it).</summary>
        public float TileSize => ((RectTransform)_slots[0].TileTransform).rect.width;

        /// <summary>Keeps a slot drawn locked until its key lands (the rules opened it already).</summary>
        public void HoldLock(int slotIndex) => _heldLocks.Add(slotIndex);

        /// <summary>The locked slot's key arrived (FR-039): the padlock grows and the slot turns free.</summary>
        public void PlayUnlock(int slotIndex, LevelView view)
        {
            _heldLocks.Remove(slotIndex);
            if (isActiveAndEnabled)
            {
                StartCoroutine(Unlock(_slots[slotIndex], view));
            }
            else
            {
                UpdateStates(view);
            }
        }

        /// <summary>A Bloomling of this pod finished one work unit: the count drops with a small bump.</summary>
        public void Decrement(string podId)
        {
            foreach (Slot slot in _slots)
            {
                if (slot.PodId == podId)
                {
                    slot.SetCount(slot.Count - 1);
                    Animate(UiFx.Pop(slot.CountTransform, 1.25f, 0.15f));
                    return;
                }
            }
        }

        public void SetWorking(string podId, bool working)
        {
            foreach (Slot slot in _slots)
            {
                if (slot.PodId == podId)
                {
                    slot.SetWorking(working);
                }
            }
        }

        /// <summary>The pod left (PodCompleted): it puffs away off its plate, the slot empties, and a queued pod moves in.</summary>
        public void Complete(string podId)
        {
            foreach (Slot slot in _slots)
            {
                if (slot.PodId == podId && !slot.Leaving)
                {
                    if (isActiveAndEnabled)
                    {
                        StartCoroutine(Leave(slot));
                    }
                    else
                    {
                        FinishLeave(slot);
                    }

                    return;
                }
            }
        }

        /// <summary>The slots jammed or no pod can move (FR-027): every occupied slot shakes with a red ring.</summary>
        public void ShowBlocked()
        {
            foreach (Slot slot in _slots)
            {
                if (slot.PodId != null && isActiveAndEnabled)
                {
                    StartCoroutine(Shake(slot));
                }
            }
        }

        /// <summary>Slot availability (locked, absent), the plates' places and the jam-risk mark from the logical state.</summary>
        public void UpdateStates(LevelView view)
        {
            _lastView = view;
            int free = 0;
            int present = 0;
            for (int i = 0; i < WaitingSlots.Capacity; i++)
            {
                SlotState state = view.SlotStateOf(i);
                if (state == SlotState.Free && !_heldLocks.Contains(i) && _slots[i].PodId == null)
                {
                    free++;
                }

                if (state != SlotState.Absent)
                {
                    present++;
                }
            }

            Rect area = _area.rect;
            IReadOnlyList<Box> cells = CellsFor?.Invoke(present) ?? Cells(new Box(0f, 0f, area.width, area.height), present, UiKit.Units(1f));
            int column = 0;
            for (int i = 0; i < WaitingSlots.Capacity; i++)
            {
                Slot slot = _slots[i];
                SlotState state = view.SlotStateOf(i);
                bool wasAbsent = !slot.Frame.gameObject.activeSelf;
                slot.Frame.gameObject.SetActive(state != SlotState.Absent && column < cells.Count);
                if (state == SlotState.Absent || column >= cells.Count)
                {
                    continue;
                }

                BoxLayout.Place(slot.Frame.rectTransform, cells[column]);
                column++;
                bool locked = state == SlotState.Locked || _heldLocks.Contains(i);
                bool risk = free == 1 && state == SlotState.Free && !locked && slot.PodId == null;
                slot.SetBase(locked, risk);
                if (wasAbsent && slot.IsExtra)
                {
                    // Extra Slot was just added (US5): it pops in.
                    Animate(UiFx.Pop(slot.Frame.transform, 1.25f, 0.3f));
                }
            }
        }

        /// <summary>
        /// The plates of the slot row when no <see cref="CellsFor"/> is given, in the area's top-down coordinates: as tall
        /// as the band allows (less 12 units for their shadows), <see cref="PlateAspect"/> as wide, a quarter of a plate
        /// apart and centered; narrower when they do not fit. <paramref name="unit"/> is one reference unit.
        /// </summary>
        public static Box[] Cells(Box area, int count, float unit)
        {
            var cells = new Box[System.Math.Max(0, count)];
            if (count <= 0)
            {
                return cells;
            }

            float height = area.Height - (12f * unit);
            float width = height * PlateAspect;
            float gap = width * 0.27f;
            float fit = (area.Width - (12f * unit)) / ((count * 1.27f) - 0.27f);
            if (fit < width)
            {
                width = fit;
                gap = width * 0.27f;
            }

            float total = (width * count) + (gap * (count - 1));
            float x = area.CenterX - (total / 2f);
            float top = area.CenterY - (height / 2f) - (2f * unit);
            for (int i = 0; i < count; i++)
            {
                cells[i] = new Box(x, top, x + width, top + height);
                x += width + gap;
            }

            return cells;
        }

        private void Animate(IEnumerator routine)
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(routine);
            }
        }

        /// <summary>The "?" tile turns over to the variant (the playtest's reveal flip): only the tile narrows about its middle.</summary>
        private IEnumerator Flip(Slot slot, VariantId variant)
        {
            Transform tile = slot.TileTransform;
            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.3f;
                if (k >= 0.5f && slot.ShowsQuestion)
                {
                    slot.SetVariant(variant);
                }

                tile.localScale = new Vector3(Mathf.Max(0.04f, Mathf.Abs(Mathf.Cos(k * Mathf.PI))), 1f, 1f);
                yield return null;
            }

            slot.SetVariant(variant);
            tile.localScale = Vector3.one;
        }

        private IEnumerator Unlock(Slot slot, LevelView view)
        {
            Transform icon = slot.LockIcon;
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                icon.localScale = Vector3.one * (1f + (t / 0.35f));
                yield return null;
            }

            icon.localScale = Vector3.one;
            UpdateStates(view);
            yield return UiFx.Pop(slot.Frame.transform, 1.1f, 0.18f);
        }

        private IEnumerator Leave(Slot slot)
        {
            slot.Leaving = true;
            slot.Frame.raycastTarget = false;
            slot.SetGlow(false);
            slot.ShowBaseUnder();
            slot.SetWorking(true);
            Transform body = slot.Body;
            for (float t = 0f; t < ExitSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / ExitSeconds;
                body.localScale = Vector3.one * (1f + (0.3f * k));
                slot.SetAlpha(1f - k);
                yield return null;
            }

            FinishLeave(slot);
        }

        private void FinishLeave(Slot slot)
        {
            slot.Leaving = false;
            slot.Clear();
            if (slot.Pending.Count > 0)
            {
                (string id, VariantId? variant, int count) = slot.Pending.Dequeue();
                slot.Occupy(id, variant, count);
                Animate(UiFx.Pop(slot.Body, 1.12f, 0.16f));
            }

            if (_lastView != null)
            {
                UpdateStates(_lastView);
            }
        }

        private IEnumerator Shake(Slot slot)
        {
            slot.SetHighlight(UiTheme.Of(C.StateDanger));
            Transform body = slot.Body;
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                body.localEulerAngles = new Vector3(0f, 0f, 8f * Mathf.Sin(t * 50f) * (1f - (t / 0.4f)));
                yield return null;
            }

            body.localEulerAngles = Vector3.zero;
        }

        /// <summary>
        /// One slot: its positioned frame (the touch target of Return), the glow behind it, the base plate (empty, danger
        /// or locked, with the Extra Slot's "+") and the pod's plate over it (working or stuck), which pops, turns, shakes
        /// and puffs away.
        /// </summary>
        private sealed class Slot
        {
            public readonly Queue<(string PodId, VariantId? Variant, int Count)> Pending = new Queue<(string, VariantId?, int)>();

            private RectTransform _glow = null!;
            private SlotPlateView _base = null!;
            private SlotPlateView _pod = null!;
            private CanvasGroup _podFade = null!;
            private Image _highlight = null!;
            private VariantId? _variant;
            private bool _working;
            private bool _locked;
            private bool _risk;
            private float _highlightRadius;
            private float _highlightWidth;

            public int Index { get; private set; }

            /// <summary>The sixth slot, only there after Extra Slot (marked with a green "+").</summary>
            public bool IsExtra { get; private set; }

            /// <summary>The positioned slot (clear; it takes Return's tap).</summary>
            public Image Frame { get; private set; } = null!;

            public string? PodId { get; private set; }

            public int Count { get; private set; }

            public bool Leaving { get; set; }

            /// <summary>The pod's plate: it pops, shakes and puffs away.</summary>
            public Transform Body => _pod.transform;

            public Transform TileTransform => _pod.Tile.transform;

            public Transform LockIcon => _base.Lock.transform;

            public Transform CountTransform => _pod.Count.transform;

            public bool ShowsQuestion => PodId != null && !_variant.HasValue;

            public static Slot Create(Transform parent, int index, bool extra)
            {
                var slot = new Slot { Index = index, IsExtra = extra };
                slot.Frame = UiFactory.CreateImage($"Slot {index}", parent, null, Color.clear);
                Transform frame = slot.Frame.transform;
                slot._glow = UiFactory.Stretch(UiKit.TargetGlow("Glow", frame));
                slot._glow.gameObject.SetActive(false);
                slot._base = UiKit.SlotPlate("Base", frame);
                UiFactory.Stretch((RectTransform)slot._base.transform);
                slot._pod = UiKit.SlotPlate("Pod", frame);
                UiFactory.Stretch((RectTransform)slot._pod.transform);
                slot._podFade = slot._pod.gameObject.AddComponent<CanvasGroup>();
                slot._podFade.blocksRaycasts = false;
                slot._highlight = UiKit.RoundRing("Highlight", frame, Color.white, _ => slot._highlightRadius, _ => slot._highlightWidth);
                BoxLayout.On((RectTransform)frame).Add(slot._highlight.rectTransform, b =>
                {
                    float s = Mathf.Min(b.Width, b.Height);
                    slot._highlightWidth = s * 0.04f;
                    slot._highlightRadius = s * 0.26f;
                    return b.Inset(-s * 0.06f);
                });
                slot._highlight.gameObject.SetActive(false);
                slot.Clear();
                return slot;
            }

            public void Occupy(string podId, VariantId? variant, int count)
            {
                PodId = podId;
                _variant = variant;
                Count = count;
                _working = false;
                SetAlpha(1f);
                Body.localScale = Vector3.one;
                Body.localEulerAngles = Vector3.zero;
                TileTransform.localScale = Vector3.one;
                _pod.gameObject.SetActive(true);
                _base.gameObject.SetActive(false);
                ShowPod();
            }

            /// <summary>The pod's tile: its exact variant, or the lilac "?" tile of a mystery pod.</summary>
            public void SetVariant(VariantId? variant)
            {
                _variant = variant;
                ShowPod();
            }

            public void SetCount(int count)
            {
                Count = count;
                ShowPod();
            }

            /// <summary>Working pods are bright; stuck (waiting) pods show their tile in grey with the hourglass (frame 13).</summary>
            public void SetWorking(bool working)
            {
                _working = working;
                ShowPod();
            }

            private void ShowPod()
            {
                if (PodId == null)
                {
                    return;
                }

                _pod.Show(_working || Leaving ? SlotPlateState.Working : SlotPlateState.Stuck, _variant, Count, IsExtra && !Leaving);
            }

            public void SetAlpha(float alpha) => _podFade.alpha = alpha;

            public void Clear()
            {
                PodId = null;
                Count = 0;
                _working = false;
                _variant = null;
                _pod.gameObject.SetActive(false);
                Body.localScale = Vector3.one;
                Body.localEulerAngles = Vector3.zero;
                SetAlpha(1f);
                _base.gameObject.SetActive(true);
                ShowBase();
            }

            /// <summary>The base plate's state: locked (padlock), the jam risk (red dashes and "!"), or empty.</summary>
            public void SetBase(bool locked, bool risk)
            {
                _locked = locked;
                _risk = risk;
                ShowBase();
            }

            /// <summary>While the pod puffs away, the empty plate shows under it.</summary>
            public void ShowBaseUnder()
            {
                _base.gameObject.SetActive(true);
                ShowBase();
            }

            private void ShowBase()
            {
                SlotPlateState state = _locked ? SlotPlateState.Locked : _risk ? SlotPlateState.Danger : SlotPlateState.Empty;
                _base.Show(state, extra: IsExtra);
            }

            /// <summary>The golden glow of a plate Return can take its pod back from.</summary>
            public void SetGlow(bool on) => _glow.gameObject.SetActive(on);

            /// <summary>A ring in <paramref name="color"/> around the plate (a jam); null removes it.</summary>
            public void SetHighlight(Color? color)
            {
                _highlight.gameObject.SetActive(color.HasValue);
                if (color.HasValue)
                {
                    _highlight.color = color.Value;
                    _highlight.GetComponent<RoundShape>().Apply();
                }
            }
        }
    }
}
