using System;
using System.Collections;
using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Tray
{
    /// <summary>
    /// A Spirit Pod card in the states of the design board's frame 12 (spec 002 FR-012).
    /// <list type="bullet">
    /// <item><description>exposed: a raised card in the variant's light tint;</description></item>
    /// <item><description>buried (next in stack): greyed;</description></item>
    /// <item><description>locked: grey with a padlock;</description></item>
    /// <item><description>mystery: pink with "?";</description></item>
    /// <item><description>connected: a teal link mark.</description></item>
    /// </list>
    /// The card shows the variant's character (spec 004 FR-008): its whole shape is the variant's symbol, in the variant's
    /// colors, awake on an exposed pod and asleep in the stack, with "xN" in the bottom-right corner. That keeps spec 001
    /// FR-012's order of prominence: the character (icon and color in one), the count, then the family. Locked and
    /// mystery pods keep their padlock or "?" and the count pill.
    /// </summary>
    public sealed class PodView : MonoBehaviour
    {
        private static readonly Color MysteryCard = UiTheme.Of(DesignTokens.Colors.PodMystery);
        private static readonly Color MysteryMark = UiTheme.Of(DesignTokens.Colors.PodMysteryMark);

        private Image _edge = null!;
        private Image _card = null!;
        private Image _body = null!;
        private Image _icon = null!;
        private Image _countPill = null!;
        private TextMeshProUGUI _pillCount = null!;
        private Image _lock = null!;
        private Image _link = null!;
        private TextMeshProUGUI _count = null!;
        private Button _button = null!;
        private Coroutine? _feedback;

        public string PodId { get; private set; } = string.Empty;

        public RectTransform Rect => (RectTransform)transform;

        public static PodView Create(Transform parent, Action<string> onTap)
        {
            Image edge = UiKit.Rounded("Pod", parent, Color.white, 44f, raycast: true);
            var view = edge.gameObject.AddComponent<PodView>();
            view._edge = edge;
            view._card = UiKit.Rounded("Card", edge.transform, Color.white, 44f);
            // A volumetric 2D card (spec 003 FR-022): a thick lip below the card and a highlight band; never 3D.
            RectTransform card = UiFactory.Stretch(view._card.rectTransform);
            card.offsetMin = new Vector2(0f, UiKit.Units(DesignTokens.Garden.PodLip));
            Image highlight = UiFactory.CreateImage("Highlight", view._card.transform, ProceduralSprites.PillSprite, Color.white);
            UiFactory.Place(highlight.rectTransform, 0.09f, 0.75f, 0.91f, 0.95f);
            UiKit.Gradient(highlight, new Color(1f, 1f, 1f, 0.55f), new Color(1f, 1f, 1f, 0f));
            view._button = edge.gameObject.AddComponent<Button>();
            view._button.targetGraphic = view._card;
            view._button.onClick.AddListener(() => onTap(view.PodId));
            edge.gameObject.AddComponent<PressMotion>();

            // The character and its "xN" (spec 004 FR-008), placed by the kit on a square face.
            var face = new Box(0f, 0f, 1f, 1f);
            view._body = UiFactory.CreateImage("Character", view._card.transform, null, Color.white);
            view._body.preserveAspect = true;
            (float bx0, float by0, float bx1, float by1) = CharacterArt.Anchors(CharacterArt.OnCard(face), face);
            UiFactory.Place(view._body.rectTransform, bx0, by0, bx1, by1);
            view._icon = UiFactory.CreateImage("Icon", view._card.transform, null, Color.white);
            view._icon.preserveAspect = true;
            UiFactory.Place(view._icon.rectTransform, 0.3f, 0.26f, 0.7f, 0.62f);
            view._count = UiKit.Label("Count", view._card.transform, string.Empty, DesignTokens.Type.Count, UiTheme.Of(CharacterArt.CountColor), TextAlignmentOptions.Right, CharacterArt.CountLook);
            (float cx0, float cy0, float cx1, float cy1) = CharacterArt.Anchors(CharacterArt.CountBox(face), face);
            UiFactory.Place(view._count.rectTransform, cx0, cy0, cx1, cy1);

            // Locked and mystery pods keep the count in a pill (spec 002 FR-012).
            view._countPill = UiKit.Pill("CountPill", view._card.transform, UiTheme.Of(DesignTokens.Colors.BadgeCount));
            UiFactory.Place(view._countPill.rectTransform, 0.24f, 0.02f, 0.76f, 0.28f);
            view._pillCount = UiKit.Label("Count", view._countPill.transform, string.Empty, DesignTokens.Type.Count, Color.white);
            UiFactory.Place(view._pillCount.rectTransform, 0.06f, 0.04f, 0.94f, 0.96f);

            view._lock = UiFactory.CreateImage("Lock", view._card.transform, ProceduralSprites.Lock, UiTheme.LockGlyph);
            view._lock.preserveAspect = true;
            UiFactory.Place(view._lock.rectTransform, 0.28f, 0.36f, 0.72f, 0.8f);
            view._link = UiFactory.CreateImage("Link", view._card.transform, ProceduralSprites.Circle, UiTheme.LinkColor);
            view._link.preserveAspect = true;
            UiFactory.Place(view._link.rectTransform, 0.84f, 0.5f, 1.08f, 0.74f);
            return view;
        }

        /// <param name="interactive">Only exposed pods take taps (FR-011).</param>
        /// <param name="dimmed">A pod still in its stack, shown below the exposed one in its muted variant color (spec 003 FR-022a).</param>
        /// <param name="lockShown">The lock is drawn (locked, or its key is still in flight).</param>
        /// <param name="linkColor">The connected group's color, or null when the pod is not connected.</param>
        public void Show(PodInfo pod, VariantVisualCatalog? visuals, bool interactive, bool dimmed, bool lockShown, Color? linkColor)
        {
            PodId = pod.Id;
            gameObject.SetActive(true);
            _button.interactable = interactive;
            bool hidden = !pod.Variant.HasValue;
            bool character = !lockShown && !hidden;
            if (lockShown)
            {
                Tint(UiTheme.SlotLocked);
                _body.enabled = false;
                _icon.enabled = false;
            }
            else if (hidden)
            {
                Tint(dimmed ? Grey(MysteryCard) : MysteryCard);
                _body.enabled = false;
                _icon.enabled = true;
                _icon.sprite = ProceduralSprites.Question;
                _icon.color = dimmed ? UiTheme.Stuck : MysteryMark;
            }
            else
            {
                VariantVisual visual = visuals != null ? visuals.Get(pod.Variant!.Value) : VariantVisualCatalog.Default(pod.Variant!.Value);
                Rgba color = UiTheme.ToRgba(visual.Color);
                // A pod still in its stack keeps its variant color, muted, so what comes next reads at a glance
                // (spec 003 FR-022a).
                Rgba shown = dimmed ? DesignTokens.PodQueued(color) : color;
                Tint(UiTheme.Of(DesignTokens.PodCard(shown)));
                _card.sprite = visual.PodSkin ?? ProceduralSprites.RoundedSquare;
                ShowCharacter(_body, _icon, visual, dimmed ? CharacterMood.Asleep : CharacterMood.Happy);
            }

            // "xN" on a character pod; the count pill on a locked or mystery one.
            _count.gameObject.SetActive(character);
            _countPill.gameObject.SetActive(!character);
            _count.text = Loc.F("pod.count", pod.Remaining);
            _countPill.color = dimmed || lockShown ? UiTheme.Stuck : UiTheme.Of(DesignTokens.Colors.BadgeCount);
            _pillCount.text = pod.Remaining.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _lock.enabled = lockShown;
            _lock.transform.localScale = Vector3.one;
            _link.enabled = linkColor.HasValue;
            if (linkColor.HasValue)
            {
                _link.color = linkColor.Value;
            }
        }

        private void Tint(Color card)
        {
            _card.sprite = ProceduralSprites.RoundedSquare;
            _card.color = card;
            _edge.color = Color.Lerp(card, Color.black, 0.18f);
        }

        /// <summary>
        /// A variant's 2D character on a card (spec 004), or, without its picture, the spec 002 family silhouette in the
        /// variant color with the symbol in ink (FR-021). Shared by the pods and the Waiting Slots.
        /// </summary>
        public static void ShowCharacter(Image body, Image icon, VariantVisual visual, CharacterMood mood)
        {
            Sprite? picture = CharacterSprites.Character(visual.Id, mood);
            body.enabled = true;
            if (picture != null)
            {
                body.sprite = picture;
                body.color = Color.white;
                icon.enabled = false;
                return;
            }

            Rgba color = UiTheme.ToRgba(visual.Color);
            Rgba shown = mood == CharacterMood.Asleep ? DesignTokens.PodQueued(color) : mood == CharacterMood.Worried ? color.Grey().Mix(DesignTokens.Colors.StateStuck, 0.35f) : color;
            body.sprite = ProceduralSprites.Silhouette(visual.Family);
            body.color = UiTheme.Of(shown);
            icon.enabled = true;
            icon.sprite = visual.Icon;
            icon.color = UiTheme.Of(shown.Ink);
        }

        private static Color Grey(Color c)
        {
            float l = (0.299f * c.r) + (0.587f * c.g) + (0.114f * c.b);
            return new Color(l, l, l, c.a);
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

    }
}
