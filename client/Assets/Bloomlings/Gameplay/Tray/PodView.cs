using System;
using System.Collections;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Tray
{
    /// <summary>
    /// The front pod of a Source stack's deck in the states of the design board's frame 12 (spec 002 FR-012) as the
    /// reference's wooden pod (spec 005 FR-021, contracts/look.md §3.7, §6.1; the playtest's <c>PodPainter.Front</c>,
    /// <see cref="UiKit.DeckPod"/>):
    /// <list type="bullet">
    /// <item><description>exposed: a dark wooden frame, its panel in the variant's color lightened, the variant's sticker
    /// tile and the big plain count below it;</description></item>
    /// <item><description>not exposed: the same pod dimmed toward the parchment, taking no tap;</description></item>
    /// <item><description>pressed: the frame sinks and squashes under the finger, and springs back;</description></item>
    /// <item><description>locked: the padlock on a grey panel;</description></item>
    /// <item><description>mystery: the lilac "?" tile with its count.</description></item>
    /// </list>
    /// The buried pods peek above it (<see cref="TrayView"/>); connected pods are joined by the tray's link bar. The variant
    /// reads first from its tile (color and symbol), then from the panel's tint, then from the count (spec 001 FR-012).
    /// </summary>
    public sealed class PodView : MonoBehaviour
    {
        private DeckPodView _pod = null!;
        private Image? _veil;
        private Button _button = null!;
        private PressMotion _press = null!;
        private Coroutine? _feedback;
        private VariantId? _variant;
        private int _count;
        private bool _dimmed;
        private bool _locked;
        private bool _pressedShown;

        public string PodId { get; private set; } = string.Empty;

        public RectTransform Rect => (RectTransform)transform;

        /// <summary>The pod's sticker tile (committed pods fly from there).</summary>
        public RectTransform TileRect => (RectTransform)_pod.Tile.transform;

        public static PodView Create(Transform parent, Action<string> onTap)
        {
            Image root = UiFactory.CreateImage("Pod", parent, null, Color.clear, raycast: true);
            var view = root.gameObject.AddComponent<PodView>();
            view._pod = UiKit.DeckPod("Frame", root.transform);
            UiFactory.Stretch((RectTransform)view._pod.transform);
            view._button = root.gameObject.AddComponent<Button>();
            view._button.transition = Selectable.Transition.None;
            view._button.targetGraphic = root;
            view._button.onClick.AddListener(() => onTap(view.PodId));
            view._press = root.gameObject.AddComponent<PressMotion>();
            view._press.Tile = true;
            return view;
        }

        /// <param name="interactive">Only exposed pods take taps (FR-011).</param>
        /// <param name="dimmed">A pod not exposed yet, dimmed toward the parchment.</param>
        /// <param name="lockShown">The lock is drawn (locked, or its key is still in flight).</param>
        /// <param name="linkColor">The connected group's color, or null; the tray draws the link bar between the frames.</param>
        public void Show(PodInfo pod, VariantVisualCatalog? visuals, bool interactive, bool dimmed, bool lockShown, Color? linkColor)
        {
            PodId = pod.Id;
            gameObject.SetActive(true);
            _button.interactable = interactive;
            _variant = pod.Variant;
            _count = pod.Remaining;
            _dimmed = dimmed;
            _locked = lockShown;
            _pressedShown = false;
            Redraw();
            _pod.Lock.transform.localScale = Vector3.one;
        }

        private PodLook Look => _locked ? PodLook.Locked : _dimmed ? PodLook.Next : _pressedShown ? PodLook.Pressed : PodLook.Exposed;

        private void Redraw()
        {
            PodLook look = Look;
            _pod.Show(_variant, _count, look);

            // The mystery tile has no dimmed picture: a veil of the parchment dims it like the others.
            bool veil = look == PodLook.Next && !_variant.HasValue;
            if (veil && _veil == null)
            {
                _veil = UiKit.RoundRect("Veil", _pod.Tile.transform, UiTheme.Of(C.ParchmentBottom.WithAlpha(0.45f)), b => b.Width * 0.2f);
                UiFactory.Stretch(_veil.rectTransform);
            }

            if (_veil != null)
            {
                _veil.gameObject.SetActive(veil);
            }
        }

        private void Update()
        {
            // The exposed pod's frame sinks while the finger is down (the playtest's PodLook.Pressed).
            bool pressed = _button.interactable && !_locked && !_dimmed && _press.Down;
            if (pressed != _pressedShown)
            {
                _pressedShown = pressed;
                Redraw();
            }
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Refused tap: a short shake that starts within the same frame (SC-008).</summary>
        public void Shake() => Play(ShakeRoutine());

        /// <summary>Accepted tap: a quick press pulse.</summary>
        public void Pulse() => Play(PulseRoutine());

        /// <summary>Its key landed: the padlock grows, then the pod shows unlocked and pulses.</summary>
        public void Unlock() => Play(UnlockRoutine());

        /// <summary>Shuffle: the pod turns over once in place.</summary>
        public void Spin() => Play(SpinRoutine());

        private void Play(IEnumerator routine)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_feedback != null)
            {
                StopCoroutine(_feedback);
                transform.localScale = Vector3.one;
                _pod.Lock.transform.localScale = Vector3.one;
            }

            _feedback = StartCoroutine(routine);
        }

        private IEnumerator ShakeRoutine()
        {
            RectTransform rect = Rect;
            Vector2 origin = rect.anchoredPosition;
            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                rect.anchoredPosition = origin + new Vector2(Mathf.Sin(t * 80f) * 10f * (1f - (t / 0.25f)), 0f);
                yield return null;
            }

            rect.anchoredPosition = origin;
            _feedback = null;
        }

        private IEnumerator PulseRoutine()
        {
            for (float t = 0f; t < 0.15f; t += Time.unscaledDeltaTime)
            {
                transform.localScale = Vector3.one * (1f - (0.12f * Mathf.Sin(t / 0.15f * Mathf.PI)));
                yield return null;
            }

            transform.localScale = Vector3.one;
            _feedback = null;
        }

        private IEnumerator UnlockRoutine()
        {
            Transform padlock = _pod.Lock.transform;
            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                padlock.localScale = Vector3.one * (1f + (t / 0.3f));
                yield return null;
            }

            padlock.localScale = Vector3.one;
            _locked = false;
            Redraw();
            yield return PulseRoutine();
        }

        private IEnumerator SpinRoutine()
        {
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                transform.localScale = new Vector3(Mathf.Cos(t / 0.35f * Mathf.PI * 2f), 1f, 1f);
                yield return null;
            }

            transform.localScale = Vector3.one;
            _feedback = null;
        }
    }
}
