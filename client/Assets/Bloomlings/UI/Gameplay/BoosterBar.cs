using System;
using System.Collections;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Gameplay
{
    /// <summary>The state of one booster button.</summary>
    public readonly struct BoosterButtonState
    {
        public BoosterButtonState(bool unlocked, bool applicable, int charges, int price, bool affordable)
        {
            Unlocked = unlocked;
            Applicable = applicable;
            Charges = charges;
            Price = price;
            Affordable = affordable;
        }

        public bool Unlocked { get; }

        /// <summary>The level accepts it now (<c>LevelSession.Check</c>); otherwise the button is disabled (FR-046).</summary>
        public bool Applicable { get; }

        public int Charges { get; }

        public int Price { get; }

        public bool Affordable { get; }
    }

    /// <summary>
    /// The booster bar of the design board's frame 14 (spec 002 FR-014, T120).
    /// <list type="bullet">
    /// <item><description>Each booster appears at its unlock level as a round button in its own color (FR-042); a
    /// locked booster is not shown.</description></item>
    /// <item><description>A dark badge shows the owned charges. With none left, a Petal price shows instead.</description></item>
    /// <item><description>A booster the level cannot use now is greyed (FR-046).</description></item>
    /// <item><description>The booster whose target is being chosen is ringed. Return and Bloom Burst then ask for a
    /// target (a slot, a tile), and the controller runs that step.</description></item>
    /// </list>
    /// The HUD hides the whole bar before the first unlock.
    /// </summary>
    public sealed class BoosterBar : MonoBehaviour
    {
        private static readonly BoosterKind[] Order = { BoosterKind.ExtraSlot, BoosterKind.Shuffle, BoosterKind.Return, BoosterKind.BloomBurst };
        private static readonly string[] Ids = { "extra_slot", "shuffle", "return", "bloom_burst" };

        private readonly Button[] _buttons = new Button[4];
        private readonly Image[] _faces = new Image[4];
        private readonly Image[] _rings = new Image[4];
        private readonly TextMeshProUGUI[] _counts = new TextMeshProUGUI[4];
        private readonly GameObject[] _countBadges = new GameObject[4];
        private readonly TextMeshProUGUI[] _prices = new TextMeshProUGUI[4];
        private readonly GameObject[] _priceBadges = new GameObject[4];
        private readonly CanvasGroupLike[] _fade = new CanvasGroupLike[4];

        public static BoosterBar Create(RectTransform area, Action<BoosterKind> onPress)
        {
            var bar = area.gameObject.AddComponent<BoosterBar>();
            for (int i = 0; i < Order.Length; i++)
            {
                BoosterKind kind = Order[i];
                Color color = UiTheme.Of(DesignTokens.BoosterColor(Ids[i]));
                RectTransform place = UiFactory.Place(UiFactory.CreateRect(kind.ToString(), area), (i * 0.25f) + 0.02f, 0f, ((i + 1) * 0.25f) - 0.02f, 1f);
                bar._rings[i] = UiFactory.CreateImage("Targeting", place, ProceduralSprites.Circle, new Color(color.r, color.g, color.b, 0.3f));
                bar._rings[i].preserveAspect = true;
                UiFactory.Place(bar._rings[i].rectTransform, -0.05f, -0.1f, 1.05f, 1.1f);
                bar._rings[i].enabled = false;

                Image edge = UiFactory.CreateImage("Button", place, ProceduralSprites.Circle, UiTheme.Dark(color), raycast: true);
                edge.preserveAspect = true;
                UiFactory.Stretch(edge.rectTransform);
                Image face = UiFactory.CreateImage("Face", edge.transform, ProceduralSprites.Circle, color);
                face.preserveAspect = true;
                RectTransform faceRect = UiFactory.Stretch(face.rectTransform);
                faceRect.offsetMin = new Vector2(0f, UiKit.Units(7f));
                Image glyph = UiFactory.CreateImage("Glyph", face.transform, ProceduralSprites.Shape("booster." + Ids[i]), i == 3 ? UiTheme.PetalCenter : Color.white);
                glyph.preserveAspect = true;
                UiFactory.Place(glyph.rectTransform, 0.22f, 0.22f, 0.78f, 0.78f);
                var button = edge.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                button.onClick.AddListener(() => onPress(kind));
                edge.gameObject.AddComponent<PressMotion>();
                bar._buttons[i] = button;
                bar._faces[i] = face;
                bar._fade[i] = new CanvasGroupLike(edge, face, glyph);

                bar._counts[i] = UiKit.CountBadge("Count", place, out Image countDisc);
                UiFactory.Place(countDisc.rectTransform, 0.62f, -0.02f, 0.92f, 0.3f);
                bar._countBadges[i] = countDisc.gameObject;

                Image pricePill = UiKit.Pill("Price", place, UiTheme.Of(DesignTokens.Colors.BadgeCount));
                UiFactory.Place(pricePill.rectTransform, 0.44f, -0.02f, 1.02f, 0.3f);
                Image petal = UiKit.PetalIcon("Petal", pricePill.transform);
                UiFactory.Place(petal.rectTransform, 0.04f, 0.1f, 0.36f, 0.9f);
                bar._prices[i] = UiKit.Label("Amount", pricePill.transform, string.Empty, DesignTokens.Type.Badge, Color.white);
                UiFactory.Place(bar._prices[i].rectTransform, 0.36f, 0.05f, 0.96f, 0.95f);
                bar._priceBadges[i] = pricePill.gameObject;
            }

            return bar;
        }

        /// <summary>The button of a booster, for demo pointers.</summary>
        public RectTransform RectOf(BoosterKind kind) => (RectTransform)_buttons[Array.IndexOf(Order, kind)].transform;

        public void Refresh(Func<BoosterKind, BoosterButtonState> state)
        {
            for (int i = 0; i < Order.Length; i++)
            {
                BoosterButtonState s = state(Order[i]);
                Transform place = _buttons[i].transform.parent;
                place.gameObject.SetActive(s.Unlocked);
                bool enabled = s.Applicable && (s.Charges > 0 || s.Affordable);
                _buttons[i].interactable = enabled;
                _fade[i].SetAlpha(enabled ? 1f : 0.45f);
                _countBadges[i].SetActive(s.Charges > 0);
                _counts[i].text = s.Charges.ToString(CultureInfo.InvariantCulture);
                _priceBadges[i].SetActive(s.Charges <= 0);
                _prices[i].text = NumberText.Group(s.Price);
            }
        }

        /// <summary>Rings the button whose target the player is choosing (Return, Bloom Burst).</summary>
        public void SetTargeting(BoosterKind? kind)
        {
            for (int i = 0; i < Order.Length; i++)
            {
                _rings[i].enabled = kind == Order[i];
            }
        }

        /// <summary>A short pulse when a booster is used.</summary>
        public void Pulse(BoosterKind kind)
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(PulseRoutine(_buttons[Array.IndexOf(Order, kind)].transform));
            }
        }

        private static IEnumerator PulseRoutine(Transform target)
        {
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                target.localScale = Vector3.one * (1f + (0.1f * Mathf.Sin(t / 0.2f * Mathf.PI)));
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        /// <summary>Fades a button's graphics together (a disabled booster is greyed, not hidden).</summary>
        private readonly struct CanvasGroupLike
        {
            private readonly Graphic[] _graphics;
            private readonly float[] _alphas;

            public CanvasGroupLike(params Graphic[] graphics)
            {
                _graphics = graphics;
                _alphas = new float[graphics.Length];
                for (int i = 0; i < graphics.Length; i++)
                {
                    _alphas[i] = graphics[i].color.a;
                }
            }

            public void SetAlpha(float alpha)
            {
                for (int i = 0; i < _graphics.Length; i++)
                {
                    Color c = _graphics[i].color;
                    _graphics[i].color = new Color(c.r, c.g, c.b, _alphas[i] * alpha);
                }
            }
        }
    }
}
