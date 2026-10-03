using System;
using System.Collections;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Tray
{
    /// <summary>
    /// One pod of a Source stack's column in the states of the design board's frame 12 (spec 002 FR-012) as the
    /// reference's wooden pod (spec 005 FR-021, contracts/look.md §3.7, §6.1; the playtest's <c>PodPainter</c>,
    /// <see cref="UiKit.GridPod"/>), one after another in its column, never on another pod (the owner's gameplay rule,
    /// 2026-10-03):
    /// <list type="bullet">
    /// <item><description>exposed (the column's top row): a dark wooden frame, its panel tinted by the variant, the
    /// variant's sticker tile at its left and the big plain count at its right; the only pod that takes a tap (spec 001
    /// FR-011), through a touch box grown to <c>size.touch_min</c> (it may reach over the pod under it);</description></item>
    /// <item><description>waiting (the rows below): the same parts muted but readable, taking no tap;</description></item>
    /// <item><description>pressed: the frame sinks and squashes under the finger, and springs back;</description></item>
    /// <item><description>locked: the padlock on a grey panel;</description></item>
    /// <item><description>mystery: the lilac "?" tile with its count.</description></item>
    /// </list>
    /// When its column moves (the exposed pod left, a pod came back, the tray reshuffled), the pod slides from where it was
    /// drawn to its new row in <see cref="SlideSeconds"/>, easing out (<see cref="Place"/>): presentation only, its taps
    /// and touch box are at the new place at once. Connected pods are joined by the tray's link bar. The variant reads
    /// first from its tile (color and symbol), then from the panel's tint, then from the count (spec 001 FR-012).
    /// </summary>
    public sealed class PodView : MonoBehaviour
    {
        /// <summary>How long a pod slides to its new row when its column moves (ease out).</summary>
        public const float SlideSeconds = 0.18f;

        private GridPodView _pod = null!;
        private RectTransform _holder = null!;
        private CanvasGroup _fade = null!;
        private Image _touch = null!;
        private Button _button = null!;
        private PressMotion _press = null!;
        private Coroutine? _feedback;
        private Coroutine? _slide;
        private VariantId? _variant;
        private int _count;
        private bool _waiting;
        private bool _locked;
        private bool _pressedShown;
        private Box _box;
        private Box _visual;
        private Vector2 _shake;

        public string PodId { get; private set; } = string.Empty;

        /// <summary>The pod's place (its final row; taps, keys and tutorial pointers aim there).</summary>
        public RectTransform Rect => (RectTransform)transform;

        /// <summary>The pod's sticker tile (committed pods fly from there).</summary>
        public RectTransform TileRect => (RectTransform)_pod.Tile.transform;

        /// <summary>The side of the tile as drawn now, in canvas units (smaller while the pod slides up from a waiting row).</summary>
        public float TileSize => TileRect.rect.width * _holder.localScale.x;

        /// <summary>Whether the pod is drawn.</summary>
        public bool IsShown => gameObject.activeSelf;

        /// <summary>The Source stack whose column the pod was last placed in (-1 before it is placed).</summary>
        public int Stack { get; private set; } = -1;

        /// <summary>Where the pod is drawn now, in the tray area's top-down units (between two rows while it slides).</summary>
        public Box Visual => _visual;

        public static PodView Create(Transform parent, Action<string> onTap)
        {
            RectTransform root = UiFactory.CreateRect("Pod", parent);
            var view = root.gameObject.AddComponent<PodView>();

            // The touch box first (clear, behind the pod), then the pod in a holder the slide moves and scales.
            view._touch = UiFactory.CreateImage("Touch", root, null, Color.clear, raycast: true);
            view._holder = UiFactory.Stretch(UiFactory.CreateRect("Slide", root));
            view._fade = view._holder.gameObject.AddComponent<CanvasGroup>();
            view._fade.blocksRaycasts = false;
            view._pod = UiKit.GridPod("Frame", view._holder);
            UiFactory.Stretch((RectTransform)view._pod.transform);
            view._button = root.gameObject.AddComponent<Button>();
            view._button.transition = Selectable.Transition.None;
            view._button.targetGraphic = view._touch;
            view._button.onClick.AddListener(() => onTap(view.PodId));
            view._press = root.gameObject.AddComponent<PressMotion>();
            view._press.Tile = true;
            return view;
        }

        /// <param name="interactive">Only the exposed pod takes taps (FR-011).</param>
        /// <param name="waiting">A pod under the exposed one (or not exposed yet): muted, readable.</param>
        /// <param name="lockShown">The lock is drawn (locked, or its key is still in flight).</param>
        public void Show(PodInfo pod, bool interactive, bool waiting, bool lockShown)
        {
            PodId = pod.Id;
            gameObject.SetActive(true);
            _button.interactable = interactive;
            _touch.raycastTarget = interactive;
            _variant = pod.Variant;
            _count = pod.Remaining;
            _waiting = waiting;
            _locked = lockShown;
            _pressedShown = false;
            Redraw();
            _pod.Lock.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Places the pod in <paramref name="box"/> of Source stack <paramref name="stack"/>'s column (the tray area's
        /// top-down units) with its touch box <paramref name="touch"/>. With <paramref name="from"/> it slides there from
        /// that box (growing or shrinking with the row's height), with <paramref name="fadeIn"/> fading in as it comes; it
        /// is placed at once otherwise.
        /// </summary>
        public void Place(int stack, Box box, Box touch, Box? from = null, bool fadeIn = false)
        {
            Stack = stack;
            _box = box;
            BoxLayout.Place(Rect, box);
            BoxLayout.Place(_touch.rectTransform, touch.Offset(-box.Left, -box.Top));
            if (_slide != null)
            {
                StopCoroutine(_slide);
                _slide = null;
            }

            bool moves = from.HasValue && (fadeIn || _fade.alpha < 0.999f || Mathf.Abs(from.Value.CenterY - box.CenterY) > 0.5f || Mathf.Abs(from.Value.CenterX - box.CenterX) > 0.5f || Mathf.Abs(from.Value.Height - box.Height) > 0.5f);
            if (moves && isActiveAndEnabled)
            {
                _slide = StartCoroutine(SlideRoutine(from!.Value, fadeIn));
                return;
            }

            SetVisual(box, 1f);
        }

        private IEnumerator SlideRoutine(Box from, bool fadeIn)
        {
            // A pod still fading in when its column moves again keeps fading from where it was.
            float alpha = fadeIn ? 0f : _fade.alpha;
            for (float t = 0f; t < SlideSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / SlideSeconds;
                float e = 1f - ((1f - k) * (1f - k) * (1f - k));
                var at = new Box(
                    Mathf.Lerp(from.Left, _box.Left, e),
                    Mathf.Lerp(from.Top, _box.Top, e),
                    Mathf.Lerp(from.Right, _box.Right, e),
                    Mathf.Lerp(from.Bottom, _box.Bottom, e));
                SetVisual(at, Mathf.Lerp(alpha, 1f, e));
                yield return null;
            }

            SetVisual(_box, 1f);
            _slide = null;
        }

        /// <summary>
        /// Draws the pod over <paramref name="at"/> (its holder moved and scaled from the pod's place) at
        /// <paramref name="alpha"/>. A pod growing into the exposed row grows from the waiting size; one going down a row
        /// (Return) takes its smaller size at once, so it never reaches past its column.
        /// </summary>
        private void SetVisual(Box at, float alpha)
        {
            _visual = at;
            float scale = Mathf.Min(1f, at.Height / Mathf.Max(0.0001f, _box.Height));
            _holder.localScale = new Vector3(scale, scale, 1f);
            ApplyOffset();
            _fade.alpha = alpha;
        }

        private void ApplyOffset() =>
            _holder.anchoredPosition = new Vector2(_visual.CenterX - _box.CenterX, _box.CenterY - _visual.CenterY) + _shake;

        private PodLook Look => _locked ? PodLook.Locked : _waiting ? PodLook.Next : _pressedShown ? PodLook.Pressed : PodLook.Exposed;

        private void Redraw() => _pod.Show(_variant, _count, Look, _waiting);

        private void Update()
        {
            // The exposed pod's frame sinks while the finger is down (the playtest's PodLook.Pressed).
            bool pressed = _button.interactable && !_locked && !_waiting && _press.Down;
            if (pressed != _pressedShown)
            {
                _pressedShown = pressed;
                Redraw();
            }
        }

        public void Hide()
        {
            if (_slide != null)
            {
                StopCoroutine(_slide);
                _slide = null;
            }

            _shake = Vector2.zero;
            Stack = -1;
            gameObject.SetActive(false);
        }

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
                _shake = Vector2.zero;
                ApplyOffset();
            }

            _feedback = StartCoroutine(routine);
        }

        private IEnumerator ShakeRoutine()
        {
            float amplitude = UiKit.Units(10f);
            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                _shake = new Vector2(Mathf.Sin(t * 80f) * amplitude * (1f - (t / 0.25f)), 0f);
                ApplyOffset();
                yield return null;
            }

            _shake = Vector2.zero;
            ApplyOffset();
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
