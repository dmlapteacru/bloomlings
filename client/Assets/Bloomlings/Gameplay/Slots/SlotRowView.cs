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
    /// which is empty again under it. Where each pod shows is <see cref="SlotPlaces"/>' choice: its slot in the rules when
    /// that plate shows no pod, else the first usable plate that shows none (the rules free a finished pod's slot while
    /// it still shows here), else it waits in a visual queue (R4), counting down, until a plate frees (since 2026-10-05 a
    /// tap goes in only when a plate shows no pod, <see cref="FreeOnScreen"/>, so the queue is a safety net). While Return
    /// chooses its slot, every plate showing a pod it can take back glows; on a jam every occupied plate shakes with a red
    /// ring.
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
        private readonly SlotPlaces _places = new SlotPlaces();
        private readonly HashSet<int> _heldLocks = new HashSet<int>();

        // Pods with Bloomlings on their way, drawn bright (also while they still wait for a plate).
        private readonly HashSet<string> _working = new HashSet<string>(System.StringComparer.Ordinal);
        private RectTransform _area = null!;
        private VariantVisualCatalog? _visuals;
        private LevelView? _lastView;
        private bool _targeting;

        /// <summary>
        /// A plate was tapped while targeting, by its place on screen (Return takes back the pod shown there, T120; its
        /// slot in the rules is <see cref="SlotPlaces.RulesSlotOf"/> of <see cref="PodAt"/>).
        /// </summary>
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

        /// <summary>The pod shown on a plate (one leaving included), or null.</summary>
        public string? PodAt(int place) => _places[place].PodId;

        /// <summary>The plate a pod shows on, or −1 (not committed, or still waiting for a plate).</summary>
        public int PlaceOf(string podId) => _places.PlaceOf(podId);

        /// <summary>
        /// How many usable plates show no pod now (a leaving pod still counts as shown): a tap needs one per pod it commits
        /// (the owner, 2026-10-05).
        /// </summary>
        public int FreeOnScreen(LevelView view) => _places.FreeOnScreen(slot => Usable(slot, view));

        /// <summary>Lets the plates showing a pod Return can take back take taps (Return's target) and glow, or stops it.</summary>
        public void SetTargeting(bool on)
        {
            _targeting = on;
            foreach (Slot slot in _slots)
            {
                ShowTarget(slot);
                slot.SetHighlight(null);
            }

            if (!on && _lastView != null)
            {
                UpdateStates(_lastView);
            }
        }

        /// <summary>Forgets locks held for flying keys (level start and restart).</summary>
        public void ReleaseLocks() => _heldLocks.Clear();

        /// <summary>Shows the logical state with each pod in its slot (level start, restart); locks held for a flying key stay.</summary>
        public void Reset(LevelView view)
        {
            StopAllCoroutines();
            _working.Clear();
            _places.Clear();
            Show(view, SlotPlaces.ToShow(view, null));
        }

        /// <summary>
        /// Shows the logical state after a booster (after the timeline was flushed): each pod that stays keeps its plate, a
        /// pod the booster removed frees its plate, and a pod not shown yet takes one, as a committed pod does.
        /// </summary>
        /// <param name="settling">
        /// The booster's events, whose settle rounds are still to play: the counts are shown before that work lands, and a
        /// pod that finishes in those rounds stays on its plate until its wave, so the timeline's decrements end at the
        /// logical counts.
        /// </param>
        public void Rebuild(LevelView view, IReadOnlyList<GameEvent> settling)
        {
            StopAllCoroutines();
            _working.Clear();
            Show(view, SlotPlaces.ToShow(view, settling));
        }

        /// <summary>
        /// A pod was committed to the rules' slot <paramref name="slotIndex"/> (immediate feedback, SC-008);
        /// <paramref name="count"/> is its count before any work. Returns the plate it shows on, or waits for: its tile flies
        /// there.
        /// </summary>
        /// <param name="variant">Null for a mystery pod: it shows the "?" tile until <see cref="RevealVariant"/> turns it (FR-039).</param>
        public int Commit(LevelView view, int slotIndex, string podId, VariantId? variant, int count)
        {
            int place = _places.Commit(slotIndex, podId, variant, count, slot => Usable(slot, view), out bool shown);
            if (shown)
            {
                Slot slot = _slots[place];
                slot.Occupy(variant, count);
                slot.SetWorking(_working.Contains(podId));
                Animate(UiFx.Pop(slot.Body, 1.12f, 0.16f));
                ShowTarget(slot);
            }

            return place;
        }

        /// <summary>A mystery pod shows its exact variant on commit (FR-039): its tile turns over.</summary>
        public void RevealVariant(string podId, VariantId variant)
        {
            // A pod still waiting for a plate shows its variant when it moves in.
            int place = _places.Reveal(podId, variant);
            if (place < 0)
            {
                return;
            }

            Slot slot = _slots[place];
            if (isActiveAndEnabled)
            {
                StartCoroutine(Flip(slot, variant));
            }
            else
            {
                slot.SetVariant(variant);
            }
        }

        /// <summary>World position of a slot, where a key for a locked slot lands (T107).</summary>
        public Vector3 SlotPosition(int slotIndex) => _slots[slotIndex].Frame.transform.position;

        public RectTransform SlotRect(int slotIndex) => _slots[slotIndex].Frame.rectTransform;

        /// <summary>The width of a slot's plate, for the flying pod.</summary>
        public float SlotSize => _slots[0].Frame.rectTransform.sizeDelta.x;

        /// <summary>
        /// World position of the tile on a plate: a committed pod's tile lands on the plate it shows on (<see cref="Commit"/>),
        /// and a returned pod's flies back from it.
        /// </summary>
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
                ShowMovedIn(_slots[slotIndex], _places.TakeWaiting(slotIndex));
                UpdateStates(view);
            }
        }

        /// <summary>
        /// A Bloomling of this pod finished one work unit: the count drops with a small bump (a pod still waiting for a
        /// plate counts down where it waits).
        /// </summary>
        public void Decrement(string podId)
        {
            int place = _places.Decrement(podId);
            if (place >= 0)
            {
                _slots[place].SetCount(_places[place].Count);
                Animate(UiFx.Pop(_slots[place].CountTransform, 1.25f, 0.15f));
            }
        }

        public void SetWorking(string podId, bool working)
        {
            if (working)
            {
                _working.Add(podId);
            }
            else
            {
                _working.Remove(podId);
            }

            int place = _places.PlaceOf(podId);
            if (place >= 0)
            {
                _slots[place].SetWorking(working);
            }
        }

        /// <summary>
        /// The pod left (PodCompleted): it puffs away off its plate, the plate empties, and a waiting pod moves in. A pod
        /// that finished while still waiting for a plate never shows.
        /// </summary>
        public void Complete(string podId)
        {
            _working.Remove(podId);
            int place = _places.Complete(podId);
            if (place < 0)
            {
                return;
            }

            if (isActiveAndEnabled)
            {
                StartCoroutine(Leave(_slots[place]));
            }
            else
            {
                FinishLeave(_slots[place]);
            }
        }

        /// <summary>The slots jammed or no pod can move (FR-027): every occupied plate shakes with a red ring.</summary>
        public void ShowBlocked()
        {
            foreach (Slot slot in _slots)
            {
                if (_places[slot.Index].PodId != null && isActiveAndEnabled)
                {
                    StartCoroutine(Shake(slot));
                }
            }
        }

        /// <summary>
        /// Slot availability (locked, absent), the plates' places and the jam-risk mark: the last usable plate that shows no
        /// pod on screen (the pods show where <see cref="SlotPlaces"/> put them, not always in their rules' slot).
        /// </summary>
        public void UpdateStates(LevelView view)
        {
            _lastView = view;
            int free = _places.FreeOnScreen(slot => Usable(slot, view));
            int present = 0;
            for (int i = 0; i < WaitingSlots.Capacity; i++)
            {
                if (view.SlotStateOf(i) != SlotState.Absent)
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
                bool risk = free == 1 && !locked && _places[i].PodId == null;
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

        /// <summary>A plate pods can show on: not locked (by the rules, or drawn locked until its key lands) and present.</summary>
        private bool Usable(int slot, LevelView view)
        {
            SlotState state = view.SlotStateOf(slot);
            return state != SlotState.Locked && state != SlotState.Absent && !_heldLocks.Contains(slot);
        }

        /// <summary>Draws every plate from <see cref="SlotPlaces"/> after it took the pods to show (no animation).</summary>
        private void Show(LevelView view, IReadOnlyList<(string PodId, int RulesSlot, VariantId? Variant, int Count)> pods)
        {
            _lastView = view;
            _places.Rebuild(pods, slot => Usable(slot, view));
            foreach (Slot slot in _slots)
            {
                SlotPlace place = _places[slot.Index];
                slot.Leaving = false;
                slot.SetHighlight(null);
                if (place.PodId != null)
                {
                    slot.Occupy(place.Variant, place.Count);
                }
                else
                {
                    slot.Clear();
                }

                ShowTarget(slot);
            }

            UpdateStates(view);
        }

        /// <summary>Return can take back the pod shown on this plate: one the rules still hold in a slot, not leaving.</summary>
        private void ShowTarget(Slot slot)
        {
            SlotPlace place = _places[slot.Index];
            bool target = _targeting && place.PodId != null && !place.Leaving && (_lastView == null || SlotPlaces.RulesSlotOf(_lastView, place.PodId) >= 0);
            slot.Frame.raycastTarget = target;
            slot.SetGlow(target);
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
            ShowMovedIn(slot, _places.TakeWaiting(slot.Index));
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

        /// <summary>The pod has left its plate: the next pod waiting for this plate, else for any other, moves in.</summary>
        private void FinishLeave(Slot slot)
        {
            slot.Leaving = false;
            slot.Clear();
            ShowMovedIn(slot, _places.FinishLeave(slot.Index));
            if (_lastView != null)
            {
                UpdateStates(_lastView);
            }
        }

        /// <summary>A pod waiting for a plate moved onto this one (its pod left, or its lock opened): it pops in.</summary>
        private void ShowMovedIn(Slot slot, bool moved)
        {
            if (moved)
            {
                SlotPlace place = _places[slot.Index];
                slot.Occupy(place.Variant, place.Count);
                slot.SetWorking(_working.Contains(place.PodId!));
                Animate(UiFx.Pop(slot.Body, 1.12f, 0.16f));
            }

            ShowTarget(slot);
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
        /// One slot's drawing: its positioned frame (the touch target of Return), the glow behind it, the base plate (empty,
        /// danger or locked, with the Extra Slot's "+") and the pod's plate over it (working or stuck), which pops, turns,
        /// shakes and puffs away. Which pod it shows is <see cref="SlotPlaces"/>' (its place of the same index).
        /// </summary>
        private sealed class Slot
        {
            private RectTransform _glow = null!;
            private SlotPlateView _base = null!;
            private SlotPlateView _pod = null!;
            private CanvasGroup _podFade = null!;
            private Image _highlight = null!;
            private VariantId? _variant;
            private int _count;
            private bool _hasPod;
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

            /// <summary>The pod's plate is puffing away (drawn bright, without the "+").</summary>
            public bool Leaving { get; set; }

            /// <summary>The pod's plate: it pops, shakes and puffs away.</summary>
            public Transform Body => _pod.transform;

            public Transform TileTransform => _pod.Tile.transform;

            public Transform LockIcon => _base.Lock.transform;

            public Transform CountTransform => _pod.Count.transform;

            public bool ShowsQuestion => _hasPod && !_variant.HasValue;

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

            public void Occupy(VariantId? variant, int count)
            {
                _hasPod = true;
                _variant = variant;
                _count = count;
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
                _count = count;
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
                if (!_hasPod)
                {
                    return;
                }

                _pod.Show(_working || Leaving ? SlotPlateState.Working : SlotPlateState.Stuck, _variant, _count, IsExtra && !Leaving);
            }

            public void SetAlpha(float alpha) => _podFade.alpha = alpha;

            public void Clear()
            {
                _hasPod = false;
                _count = 0;
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
