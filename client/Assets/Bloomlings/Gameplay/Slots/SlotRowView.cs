using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Slots;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Slots
{
    /// <summary>
    /// The Waiting Slots (T044): 5 slots, plus the sixth when Extra Slot adds it (US5). Each shows its pod's variant,
    /// remaining count and whether it is working or waiting (FR-015). The counts follow the event timeline: they drop
    /// as Bloomlings arrive. A pod committed while its slot still animates the previous pod's exit waits in a visual
    /// queue (R4). When only one usable slot is free, it is highlighted as a jam risk (FR-070).
    /// </summary>
    public sealed class SlotRowView : MonoBehaviour
    {
        private readonly Slot[] _slots = new Slot[WaitingSlots.Capacity];
        private RectTransform _area = null!;
        private VariantVisualCatalog? _visuals;

        /// <summary>A slot was tapped while targeting (Return picks the pod to send back, T120).</summary>
        public event System.Action<int>? SlotTapped;

        public static SlotRowView Create(RectTransform area, VariantVisualCatalog? visuals)
        {
            var view = area.gameObject.AddComponent<SlotRowView>();
            view._area = area;
            view._visuals = visuals;
            for (int i = 0; i < WaitingSlots.Capacity; i++)
            {
                Slot slot = Slot.Create(area, i);
                view._slots[i] = slot;
                Button button = slot.Frame.gameObject.AddComponent<Button>();
                button.targetGraphic = slot.Frame;
                int index = i;
                button.onClick.AddListener(() => view.SlotTapped?.Invoke(index));
                slot.Frame.raycastTarget = false;
            }

            return view;
        }

        /// <summary>Lets occupied slots take taps (Return's target) or stops it.</summary>
        public void SetTargeting(bool on)
        {
            foreach (Slot slot in _slots)
            {
                slot.Frame.raycastTarget = on && slot.PodId != null;
                slot.SetJamRisk(on && slot.PodId != null);
            }
        }

        /// <summary>Resets every slot to the logical state (level start and restart).</summary>
        public void Reset(LevelView view)
        {
            foreach (Slot slot in _slots)
            {
                slot.Clear();
                slot.Pending.Clear();
                string? podId = view.PodInSlot(slot.Index);
                if (podId != null)
                {
                    PodInfo pod = view.Pod(podId);
                    slot.Occupy(podId, pod.Variant, pod.Remaining, _visuals);
                }
            }

            UpdateStates(view);
        }

        /// <summary>A pod was committed (immediate feedback); <paramref name="count"/> is its count before any work.</summary>
        public void Commit(int slotIndex, string podId, VariantId? variant, int count)
        {
            Slot slot = _slots[slotIndex];
            if (slot.PodId == null && slot.Pending.Count == 0)
            {
                slot.Occupy(podId, variant, count, _visuals);
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
                }
            }
        }

        /// <summary>World position of a slot, where a key for a locked slot lands (T107).</summary>
        public Vector3 SlotPosition(int slotIndex) => _slots[slotIndex].Frame.transform.position;

        public RectTransform SlotRect(int slotIndex) => _slots[slotIndex].Frame.rectTransform;

        /// <summary>The locked slot's key arrived (FR-039): the lock pops and the slot turns free.</summary>
        public void PlayUnlock(int slotIndex, LevelView view)
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(Unlock(_slots[slotIndex], view));
            }
            else
            {
                UpdateStates(view);
            }
        }

        private System.Collections.IEnumerator Flip(Slot slot, VariantId variant)
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

        private System.Collections.IEnumerator Unlock(Slot slot, LevelView view)
        {
            Transform icon = slot.LockIcon;
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                icon.localScale = Vector3.one * (1f + (t / 0.35f));
                yield return null;
            }

            icon.localScale = Vector3.one;
            UpdateStates(view);
        }

        /// <summary>A Bloomling of this pod finished one work unit.</summary>
        public void Decrement(string podId)
        {
            foreach (Slot slot in _slots)
            {
                if (slot.PodId == podId)
                {
                    slot.SetCount(slot.Count - 1);
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

        /// <summary>The pod left (PodCompleted): the slot empties, and a queued pod moves in.</summary>
        public void Complete(string podId)
        {
            foreach (Slot slot in _slots)
            {
                if (slot.PodId == podId)
                {
                    slot.Clear();
                    if (slot.Pending.Count > 0)
                    {
                        (string id, VariantId? variant, int count) = slot.Pending.Dequeue();
                        slot.Occupy(id, variant, count, _visuals);
                    }

                    return;
                }
            }
        }

        /// <summary>Slot availability (locked, absent) and the jam-risk highlight from the logical state.</summary>
        public void UpdateStates(LevelView view)
        {
            int free = 0;
            int present = 0;
            for (int i = 0; i < WaitingSlots.Capacity; i++)
            {
                SlotState state = view.SlotStateOf(i);
                if (state == SlotState.Free)
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
                slot.Frame.gameObject.SetActive(state != SlotState.Absent);
                if (state == SlotState.Absent)
                {
                    continue;
                }

                UiFactory.PlaceAbsolute(slot.Frame.rectTransform, new Vector2((column + 0.5f) * width, _area.rect.height / 2f), Vector2.one * size);
                column++;
                slot.SetLocked(state == SlotState.Locked);
                slot.SetJamRisk(free == 1 && state == SlotState.Free);
            }
        }

        private sealed class Slot
        {
            public readonly Queue<(string PodId, VariantId? Variant, int Count)> Pending = new Queue<(string, VariantId?, int)>();

            private Image _body = null!;
            private Image _icon = null!;
            private TextMeshProUGUI _count = null!;
            private Image _lock = null!;

            public int Index { get; private set; }

            /// <summary>The positioned outer frame; its color is the jam-risk highlight.</summary>
            public Image Frame { get; private set; } = null!;

            public string? PodId { get; private set; }

            public int Count { get; private set; }

            public Transform Body => _body.transform;

            public Transform LockIcon => _lock.transform;

            public bool ShowsQuestion => _icon.sprite == ProceduralSprites.Question;

            public static Slot Create(Transform parent, int index)
            {
                var slot = new Slot { Index = index };
                slot.Frame = UiFactory.CreateImage($"Slot {index}", parent, ProceduralSprites.RoundedSquare, Color.clear);
                slot._body = UiFactory.CreateImage("Body", slot.Frame.transform, ProceduralSprites.RoundedSquare, UiTheme.SlotEmpty);
                UiFactory.Place(slot._body.rectTransform, 0.05f, 0.05f, 0.95f, 0.95f);
                slot._icon = UiFactory.CreateImage("Icon", slot._body.transform, null, Color.white);
                slot._icon.preserveAspect = true;
                UiFactory.Place(slot._icon.rectTransform, 0.15f, 0.38f, 0.85f, 0.92f);
                slot._count = UiFactory.CreateText("Count", slot._body.transform, string.Empty, 52f, UiTheme.TextOnColor);
                slot._count.fontStyle = FontStyles.Bold;
                UiFactory.Place(slot._count.rectTransform, 0f, 0.02f, 1f, 0.4f);
                slot._lock = UiFactory.CreateImage("Lock", slot._body.transform, ProceduralSprites.Lock, UiTheme.Text);
                UiFactory.Place(slot._lock.rectTransform, 0.25f, 0.25f, 0.75f, 0.75f);
                slot.Clear();
                return slot;
            }

            public void Occupy(string podId, VariantId? variant, int count, VariantVisualCatalog? visuals)
            {
                PodId = podId;
                SetVariant(variant, visuals);
                SetCount(count);
                SetWorking(false);
            }

            public void SetVariant(VariantId? variant, VariantVisualCatalog? visuals)
            {
                if (variant.HasValue)
                {
                    VariantVisual visual = visuals != null ? visuals.Get(variant.Value) : VariantVisualCatalog.Default(variant.Value);
                    _body.color = visual.Color;
                    _icon.sprite = visual.Icon;
                }
                else
                {
                    _body.color = UiTheme.SlotLocked;
                    _icon.sprite = ProceduralSprites.Question;
                }

                _icon.enabled = true;
            }

            public void SetCount(int count)
            {
                Count = count;
                _count.text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            /// <summary>Working pods are drawn full size; waiting pods are slightly smaller and faded.</summary>
            public void SetWorking(bool working)
            {
                _body.transform.localScale = Vector3.one * (working || PodId == null ? 1f : 0.92f);
                _icon.color = new Color(1f, 1f, 1f, working ? 1f : 0.75f);
            }

            public void Clear()
            {
                PodId = null;
                Count = 0;
                _body.color = UiTheme.SlotEmpty;
                _icon.enabled = false;
                _count.text = string.Empty;
                _body.transform.localScale = Vector3.one;
            }

            public void SetLocked(bool locked)
            {
                _lock.enabled = locked;
                if (locked)
                {
                    _body.color = UiTheme.SlotLocked;
                }
            }

            public void SetJamRisk(bool risk) => Frame.color = risk ? UiTheme.Warning : Color.clear;
        }
    }
}
