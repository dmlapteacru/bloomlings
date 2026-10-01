using System.Collections;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.Gameplay.Effects;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Slots
{
    /// <summary>
    /// The Waiting Slots (T044): 5 slots, plus the sixth when Extra Slot adds it (US5), drawn with a plus mark. Each shows
    /// its pod's variant, remaining count and state (FR-015, FR-070): a working pod is full size; a waiting pod is
    /// smaller with an hourglass; the last free usable slot is outlined with a "!" as a jam risk; on a jam every occupied
    /// slot shakes. The counts follow the event timeline: they drop, with a small bump, as Bloomlings arrive. A pod pops
    /// in when committed (a mystery pod shows "?" and flips over), and puffs away when done; a pod committed while its
    /// slot still animates the previous pod's exit waits in a visual queue (R4). A locked slot keeps its lock until its
    /// key lands.
    /// </summary>
    public sealed class SlotRowView : MonoBehaviour
    {
        private const float ExitSeconds = 0.22f;

        private readonly Slot[] _slots = new Slot[WaitingSlots.Capacity];
        private readonly HashSet<int> _heldLocks = new HashSet<int>();
        private RectTransform _area = null!;
        private VariantVisualCatalog? _visuals;
        private LevelView? _lastView;

        /// <summary>A slot was tapped while targeting (Return picks the pod to send back, T120).</summary>
        public event System.Action<int>? SlotTapped;

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
                button.targetGraphic = slot.Frame;
                int index = i;
                button.onClick.AddListener(() => view.SlotTapped?.Invoke(index));
                slot.Frame.raycastTarget = false;
            }

            return view;
        }

        /// <summary>Lets occupied slots take taps (Return's target) and outlines them, or stops it.</summary>
        public void SetTargeting(bool on)
        {
            foreach (Slot slot in _slots)
            {
                slot.Frame.raycastTarget = on && slot.PodId != null;
                slot.SetHighlight(on && slot.PodId != null ? UiTheme.Accent : (Color?)null);
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
                string? podId = view.PodInSlot(slot.Index);
                if (podId != null)
                {
                    PodInfo pod = view.Pod(podId);
                    slot.Occupy(podId, pod.Variant, pod.Remaining + (pending.TryGetValue(podId, out int n) ? n : 0), _visuals);
                }
            }

            foreach (PodCompleted done in finishing)
            {
                Slot slot = _slots[done.SlotIndex];
                if (slot.PodId == null)
                {
                    slot.Occupy(done.PodId, view.Pod(done.PodId).Variant, pending.TryGetValue(done.PodId, out int n) ? n : 0, _visuals);
                }
            }

            UpdateStates(view);
        }

        /// <summary>A pod was committed (immediate feedback, SC-008); <paramref name="count"/> is its count before any work.</summary>
        /// <param name="variant">Null for a mystery pod: it shows "?" until <see cref="RevealVariant"/> flips it (FR-039).</param>
        public void Commit(int slotIndex, string podId, VariantId? variant, int count)
        {
            Slot slot = _slots[slotIndex];
            if (slot.PodId == null && slot.Pending.Count == 0)
            {
                slot.Occupy(podId, variant, count, _visuals);
                Animate(UiFx.Pop(slot.Body, 1.12f, 0.16f));
            }
            else
            {
                slot.Pending.Enqueue((podId, variant, count));
            }
        }

        /// <summary>A mystery pod shows its exact variant on commit (FR-039): the card flips over.</summary>
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
                        slot.SetVariant(variant, _visuals);
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

        /// <summary>The side length of a slot card, for the flying pod.</summary>
        public float SlotSize => _slots[0].Frame.rectTransform.sizeDelta.x;

        /// <summary>Keeps a slot drawn locked until its key lands (the rules opened it already).</summary>
        public void HoldLock(int slotIndex) => _heldLocks.Add(slotIndex);

        /// <summary>The locked slot's key arrived (FR-039): the lock pops and the slot turns free.</summary>
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
                    Animate(UiFx.Pop(slot.CountTransform, 1.35f, 0.14f));
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

        /// <summary>The pod left (PodCompleted): it puffs away, the slot empties, and a queued pod moves in.</summary>
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

        /// <summary>The slots jammed or no pod can move (FR-027): every occupied slot shakes in the warning color.</summary>
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

        /// <summary>Slot availability (locked, absent) and the jam-risk mark from the logical state.</summary>
        public void UpdateStates(LevelView view)
        {
            _lastView = view;
            int free = 0;
            int present = 0;
            for (int i = 0; i < WaitingSlots.Capacity; i++)
            {
                SlotState state = view.SlotStateOf(i);
                if (state == SlotState.Free && !_heldLocks.Contains(i))
                {
                    free++;
                }

                if (state != SlotState.Absent)
                {
                    present++;
                }
            }

            float width = _area.rect.width / Mathf.Max(present, WaitingSlots.DefaultCount);
            float size = Mathf.Min(width * 0.86f, _area.rect.height * 0.9f);
            int column = 0;
            for (int i = 0; i < WaitingSlots.Capacity; i++)
            {
                Slot slot = _slots[i];
                SlotState state = view.SlotStateOf(i);
                bool wasAbsent = !slot.Frame.gameObject.activeSelf;
                slot.Frame.gameObject.SetActive(state != SlotState.Absent);
                if (state == SlotState.Absent)
                {
                    continue;
                }

                UiFactory.PlaceAbsolute(slot.Frame.rectTransform, new Vector2((column + 0.5f) * width, _area.rect.height / 2f), Vector2.one * size);
                column++;
                bool locked = state == SlotState.Locked || _heldLocks.Contains(i);
                slot.SetLocked(locked);
                bool risk = free == 1 && state == SlotState.Free && !locked && slot.PodId == null;
                slot.SetJamRisk(risk);
                if (wasAbsent && slot.IsExtra)
                {
                    // Extra Slot was just added (US5): it pops in.
                    Animate(UiFx.Pop(slot.Frame.transform, 1.25f, 0.3f));
                }
            }
        }

        private void Animate(IEnumerator routine)
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(routine);
            }
        }

        private IEnumerator Flip(Slot slot, VariantId variant)
        {
            Transform body = slot.Body;
            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.3f;
                if (k >= 0.5f && slot.ShowsQuestion)
                {
                    slot.SetVariant(variant, _visuals);
                }

                body.localScale = new Vector3(Mathf.Abs(1f - (2f * k)), 1f, 1f);
                yield return null;
            }

            slot.SetVariant(variant, _visuals);
            body.localScale = Vector3.one;
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
            yield return UiFx.Pop(slot.Body, 1.1f, 0.18f);
        }

        private IEnumerator Leave(Slot slot)
        {
            slot.Leaving = true;
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
                slot.Occupy(id, variant, count, _visuals);
                Animate(UiFx.Pop(slot.Body, 1.12f, 0.16f));
            }

            if (_lastView != null)
            {
                UpdateStates(_lastView);
            }
        }

        private IEnumerator Shake(Slot slot)
        {
            slot.SetHighlight(UiTheme.Warning);
            Transform body = slot.Body;
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                body.localEulerAngles = new Vector3(0f, 0f, 8f * Mathf.Sin(t * 50f) * (1f - (t / 0.4f)));
                yield return null;
            }

            body.localEulerAngles = Vector3.zero;
        }

        private sealed class Slot
        {
            public readonly Queue<(string PodId, VariantId? Variant, int Count)> Pending = new Queue<(string, VariantId?, int)>();

            private Image _body = null!;
            private Image _figure = null!;
            private Image _icon = null!;
            private Image _countPill = null!;
            private TextMeshProUGUI _pillCount = null!;
            private TextMeshProUGUI _count = null!;
            private VariantVisual? _visual;
            private Image _lock = null!;
            private Image _waiting = null!;
            private Image _risk = null!;
            private Image _danger = null!;
            private Color _ink = Color.white;
            private Color _tint = UiTheme.SlotEmpty;
            private Color _color = Color.white;
            private bool _working;

            public int Index { get; private set; }

            /// <summary>The sixth slot, only there after Extra Slot (drawn with a plus mark).</summary>
            public bool IsExtra { get; private set; }

            /// <summary>The positioned outer frame; its color is the state outline (jam risk, targeting, jam).</summary>
            public Image Frame { get; private set; } = null!;

            public string? PodId { get; private set; }

            public int Count { get; private set; }

            public bool Leaving { get; set; }

            public Transform Body => _body.transform;

            public Transform LockIcon => _lock.transform;

            public Transform CountTransform => _count.transform;

            public bool ShowsQuestion => _icon.sprite == ProceduralSprites.Question;

            public static Slot Create(Transform parent, int index, bool extra)
            {
                var slot = new Slot { Index = index, IsExtra = extra };
                slot.Frame = UiKit.Rounded($"Slot {index}", parent, Color.clear, 44f);

                // The sunk well's brown edge (spec 003 FR-022); the body inside is the well, or the pod's card.
                Image wellEdge = UiKit.Rounded("WellEdge", slot.Frame.transform, UiTheme.Of(DesignTokens.Colors.GardenWellEdge), 42f);
                UiFactory.Place(wellEdge.rectTransform, 0.055f, 0.055f, 0.945f, 0.945f);
                slot._body = UiKit.Rounded("Body", slot.Frame.transform, UiTheme.SlotEmpty, 40f);
                UiFactory.Place(slot._body.rectTransform, 0.07f, 0.07f, 0.93f, 0.93f);
                // The pod's character and "xN" (spec 004 FR-009), placed by the kit on a square face.
                var face = new Box(0f, 0f, 1f, 1f);
                slot._figure = UiFactory.CreateImage("Character", slot._body.transform, null, Color.white);
                slot._figure.preserveAspect = true;
                (float fx0, float fy0, float fx1, float fy1) = CharacterArt.Anchors(CharacterArt.OnCard(face), face);
                UiFactory.Place(slot._figure.rectTransform, fx0, fy0, fx1, fy1);
                slot._icon = UiFactory.CreateImage("Icon", slot._body.transform, null, Color.white);
                slot._icon.preserveAspect = true;
                UiFactory.Place(slot._icon.rectTransform, 0.3f, 0.26f, 0.7f, 0.62f);
                slot._count = UiKit.Label("Count", slot._body.transform, string.Empty, DesignTokens.Type.Count, UiTheme.Of(CharacterArt.CountColor), TextAlignmentOptions.Right, CharacterArt.CountLook);
                (float cx0, float cy0, float cx1, float cy1) = CharacterArt.Anchors(CharacterArt.CountBox(face), face);
                UiFactory.Place(slot._count.rectTransform, cx0, cy0, cx1, cy1);

                // A mystery pod keeps its count in a pill until its symbol shows.
                slot._countPill = UiKit.Pill("CountPill", slot._body.transform, UiTheme.Of(DesignTokens.Colors.BadgeCount));
                UiFactory.Place(slot._countPill.rectTransform, 0.22f, 0.02f, 0.78f, 0.28f);
                slot._pillCount = UiKit.Label("Count", slot._countPill.transform, string.Empty, DesignTokens.Type.Count, UiTheme.TextOnColor);
                UiFactory.Place(slot._pillCount.rectTransform, 0.06f, 0.04f, 0.94f, 0.96f);
                slot._lock = UiFactory.CreateImage("Lock", slot._body.transform, ProceduralSprites.Lock, UiTheme.LockGlyph);
                slot._lock.preserveAspect = true;
                UiFactory.Place(slot._lock.rectTransform, 0.25f, 0.25f, 0.75f, 0.75f);
                slot._waiting = UiFactory.CreateImage("Waiting", slot.Frame.transform, ProceduralSprites.Hourglass, UiTheme.TextSecondary);
                slot._waiting.preserveAspect = true;
                UiFactory.Place(slot._waiting.rectTransform, 0.7f, 0.7f, 1.02f, 1.02f);
                slot._danger = UiFactory.CreateImage("Danger", slot.Frame.transform, ProceduralSprites.Shape("slot.state.danger"), UiTheme.Warning);
                UiFactory.Stretch(slot._danger.rectTransform);
                slot._danger.enabled = false;
                slot._risk = UiFactory.CreateImage("JamRisk", slot.Frame.transform, ProceduralSprites.Exclamation, UiTheme.Warning);
                slot._risk.preserveAspect = true;
                UiFactory.Place(slot._risk.rectTransform, 0.3f, 0.3f, 0.7f, 0.7f);
                if (extra)
                {
                    Image plus = UiFactory.CreateImage("Extra", slot.Frame.transform, ProceduralSprites.Circle, UiTheme.Accent);
                    plus.preserveAspect = true;
                    UiFactory.Place(plus.rectTransform, -0.06f, 0.74f, 0.26f, 1.06f);
                    Image glyph = UiFactory.CreateImage("Plus", plus.transform, ProceduralSprites.Shape("ui.plus"), Color.white);
                    glyph.preserveAspect = true;
                    UiFactory.Place(glyph.rectTransform, 0.2f, 0.2f, 0.8f, 0.8f);
                }

                slot.Clear();
                return slot;
            }

            public void Occupy(string podId, VariantId? variant, int count, VariantVisualCatalog? visuals)
            {
                PodId = podId;
                SetAlpha(1f);
                SetVariant(variant, visuals);
                SetCount(count);
                SetWorking(false);
                _risk.enabled = false;
            }

            /// <summary>The pod's card (frame 13): its character on a light tint, or "?" for a mystery pod.</summary>
            public void SetVariant(VariantId? variant, VariantVisualCatalog? visuals)
            {
                if (variant.HasValue)
                {
                    VariantVisual visual = visuals != null ? visuals.Get(variant.Value) : VariantVisualCatalog.Default(variant.Value);
                    _visual = visual;
                    _body.sprite = visual.PodSkin ?? ProceduralSprites.RoundedSquare;
                    _color = visual.Color;
                    _tint = UiTheme.Of(DesignTokens.PodCard(UiTheme.ToRgba(visual.Color)));
                    _ink = visual.Ink;
                }
                else
                {
                    _visual = null;
                    _body.sprite = ProceduralSprites.RoundedSquare;
                    _color = UiTheme.Of(DesignTokens.Colors.PodMysteryMark);
                    _tint = UiTheme.Of(DesignTokens.Colors.PodMystery);
                    _figure.enabled = false;
                    _icon.sprite = ProceduralSprites.Question;
                    _icon.enabled = true;
                    _ink = _color;
                }

                _count.gameObject.SetActive(_visual.HasValue);
                _countPill.gameObject.SetActive(!_visual.HasValue);
                _countPill.enabled = true;
                SetWorking(_working);
            }

            public void SetCount(int count)
            {
                Count = count;
                _count.text = Loc.F("pod.count", count);
                _pillCount.text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            /// <summary>Working pods are bright; stuck (waiting) pods are greyed, smaller, and show an hourglass (frame 13).</summary>
            public void SetWorking(bool working)
            {
                _working = working;
                bool stuck = PodId != null && !working;
                _body.transform.localScale = Vector3.one * (working || PodId == null ? 1f : 0.92f);
                if (PodId != null)
                {
                    _body.color = stuck ? Grey(_tint) : _tint;
                    _countPill.color = stuck ? UiTheme.Stuck : UiTheme.Of(DesignTokens.Colors.BadgeCount);
                    if (_visual.HasValue)
                    {
                        // Happy while it works, worried and greyed while no tile it can clear is reachable.
                        Tray.PodView.ShowCharacter(_figure, _icon, _visual.Value, stuck ? CharacterMood.Worried : CharacterMood.Happy);
                    }
                }

                if (!_visual.HasValue)
                {
                    _icon.color = new Color(_ink.r, _ink.g, _ink.b, working ? 1f : 0.8f);
                }
                _waiting.enabled = stuck && Count > 0;
            }

            public void SetAlpha(float alpha)
            {
                Color body = _body.color;
                _body.color = new Color(body.r, body.g, body.b, alpha);
                Color figure = _figure.color;
                _figure.color = new Color(figure.r, figure.g, figure.b, alpha);
                Color icon = _icon.color;
                _icon.color = new Color(icon.r, icon.g, icon.b, alpha * (_working ? 1f : 0.8f));
                Color pill = _countPill.color;
                _countPill.color = new Color(pill.r, pill.g, pill.b, alpha);
                _count.alpha = alpha;
                _pillCount.alpha = alpha;
            }

            private static Color Grey(Color c)
            {
                float l = (0.299f * c.r) + (0.587f * c.g) + (0.114f * c.b);
                return new Color(l, l, l, c.a);
            }

            public void Clear()
            {
                PodId = null;
                Count = 0;
                _working = false;
                _body.sprite = ProceduralSprites.RoundedSquare;
                _body.color = UiTheme.SlotEmpty;
                _visual = null;
                _figure.enabled = false;
                _icon.enabled = false;
                _countPill.enabled = false;
                _count.text = string.Empty;
                _count.alpha = 1f;
                _pillCount.text = string.Empty;
                _pillCount.alpha = 1f;
                _waiting.enabled = false;
                _body.transform.localScale = Vector3.one;
                _body.transform.localEulerAngles = Vector3.zero;
            }

            public void SetLocked(bool locked)
            {
                _lock.enabled = locked;
                if (locked)
                {
                    _body.color = UiTheme.SlotLocked;
                }
                else if (PodId == null)
                {
                    _body.color = UiTheme.SlotEmpty;
                }
            }

            /// <summary>The last free usable slot: the red dashed danger frame and a "!" mark (frame 13; never color alone, FR-070).</summary>
            public void SetJamRisk(bool risk)
            {
                _risk.enabled = risk;
                _danger.enabled = risk;
            }

            public void SetHighlight(Color? color) => Frame.color = color ?? Color.clear;
        }
    }
}
