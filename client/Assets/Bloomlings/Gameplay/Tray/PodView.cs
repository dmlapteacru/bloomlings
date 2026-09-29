using System;
using System.Collections;
using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Tray
{
    /// <summary>
    /// A Spirit Pod card. In order of prominence (FR-012): the exact variant icon, the variant color, the count and
    /// the family silhouette, plus its state: locked, mystery ("?" and count) or connected (FR-013).
    /// </summary>
    public sealed class PodView : MonoBehaviour
    {
        private Image _body = null!;
        private Image _icon = null!;
        private Image _family = null!;
        private Image _lock = null!;
        private Image _link = null!;
        private TextMeshProUGUI _count = null!;
        private Button _button = null!;
        private Coroutine? _feedback;

        public string PodId { get; private set; } = string.Empty;

        public RectTransform Rect => (RectTransform)transform;

        public static PodView Create(Transform parent, Action<string> onTap)
        {
            Image body = UiFactory.CreateImage("Pod", parent, ProceduralSprites.RoundedSquare, Color.white, raycast: true);
            var view = body.gameObject.AddComponent<PodView>();
            view._body = body;
            view._button = body.gameObject.AddComponent<Button>();
            view._button.targetGraphic = body;
            view._button.onClick.AddListener(() => onTap(view.PodId));

            view._icon = UiFactory.CreateImage("Icon", body.transform, null, Color.white);
            view._icon.preserveAspect = true;
            UiFactory.Place(view._icon.rectTransform, 0.1f, 0.34f, 0.62f, 0.9f);
            view._count = UiFactory.CreateText("Count", body.transform, string.Empty, 56f, UiTheme.TextOnColor);
            UiFactory.Place(view._count.rectTransform, 0.05f, 0.02f, 0.7f, 0.36f);
            view._count.fontStyle = FontStyles.Bold;
            view._family = UiFactory.CreateImage("Family", body.transform, null, new Color(1f, 1f, 1f, 0.75f));
            view._family.preserveAspect = true;
            UiFactory.Place(view._family.rectTransform, 0.66f, 0.06f, 0.95f, 0.4f);
            view._lock = UiFactory.CreateImage("Lock", body.transform, ProceduralSprites.Lock, UiTheme.Text);
            UiFactory.Place(view._lock.rectTransform, 0.62f, 0.6f, 0.98f, 0.98f);
            view._link = UiFactory.CreateImage("Link", body.transform, ProceduralSprites.Ring, UiTheme.EntryMarker);
            UiFactory.Place(view._link.rectTransform, -0.08f, 0.8f, 0.2f, 1.08f);
            return view;
        }

        /// <param name="interactive">Only exposed pods take taps (FR-011).</param>
        /// <param name="dimmed">Buried pods are drawn dimmer and smaller behind the exposed one.</param>
        /// <param name="lockShown">The lock is drawn (locked, or its key is still in flight).</param>
        /// <param name="linkColor">The connected group's color, or null when the pod is not connected.</param>
        public void Show(PodInfo pod, VariantVisualCatalog? visuals, bool interactive, bool dimmed, bool lockShown, Color? linkColor)
        {
            PodId = pod.Id;
            gameObject.SetActive(true);
            _button.interactable = interactive;
            Color ink;
            if (pod.Variant.HasValue)
            {
                VariantVisual visual = visuals != null ? visuals.Get(pod.Variant.Value) : VariantVisualCatalog.Default(pod.Variant.Value);
                _body.sprite = visual.PodSkin ?? ProceduralSprites.RoundedSquare;
                _body.color = Dim(visual.Color, dimmed);
                _icon.sprite = visual.Icon;
                _family.sprite = ProceduralSprites.Silhouette(visual.Family);
                _family.enabled = true;
                ink = visual.Ink;
            }
            else
            {
                _body.sprite = ProceduralSprites.RoundedSquare;
                _body.color = Dim(UiTheme.SlotLocked, dimmed);
                _icon.sprite = ProceduralSprites.Question;
                _family.enabled = false;
                ink = Color.white;
            }

            _icon.color = ink;
            _count.color = ink;
            _family.color = new Color(ink.r, ink.g, ink.b, 0.75f);
            _count.text = pod.Remaining.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _lock.enabled = lockShown;
            _lock.transform.localScale = Vector3.one;
            _link.enabled = linkColor.HasValue;
            if (linkColor.HasValue)
            {
                _link.color = linkColor.Value;
            }
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Refused tap: a short shake that starts within the same frame (SC-008).</summary>
        public void Shake() => Play(ShakeRoutine());

        /// <summary>Accepted tap: a quick press pulse.</summary>
        public void Pulse() => Play(PulseRoutine());

        /// <summary>Its key landed: the lock grows and fades, then the card pulses.</summary>
        public void Unlock() => Play(UnlockRoutine());

        /// <summary>Shuffle: the card turns over once in place.</summary>
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
                _lock.transform.localScale = Vector3.one;
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
            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                _lock.transform.localScale = Vector3.one * (1f + (t / 0.3f));
                yield return null;
            }

            _lock.enabled = false;
            _lock.transform.localScale = Vector3.one;
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

        private static Color Dim(Color color, bool dimmed) => dimmed ? Color.Lerp(color, UiTheme.Background, 0.45f) : color;
    }
}
