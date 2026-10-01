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
    /// The booster bar of the design board's frame 14 (spec 002 FR-014, T120) as booster tiles (spec 003 FR-031,
    /// contracts/booster-tile.md).
    /// <list type="bullet">
    /// <item><description>Each booster appears at its unlock level as a tile: a cream plate, a raised tile in its color
    /// and a large light icon (FR-042); a locked booster is not shown.</description></item>
    /// <item><description>A "×N" badge shows the owned charges. With none left, a price tag and a green "+" show
    /// instead.</description></item>
    /// <item><description>A booster the level cannot use now is greyed and not pressable (FR-046).</description></item>
    /// <item><description>The booster whose target is being chosen is raised with a pulsing golden ring. Return and
    /// Bloom Burst then ask for a target (a slot, a tile), and the controller runs that step.</description></item>
    /// </list>
    /// The HUD hides the whole bar before the first unlock.
    /// </summary>
    public sealed class BoosterBar : MonoBehaviour
    {
        private static readonly BoosterKind[] Order = { BoosterKind.ExtraSlot, BoosterKind.Shuffle, BoosterKind.Return, BoosterKind.BloomBurst };
        private static readonly string[] Ids = { "extra_slot", "shuffle", "return", "bloom_burst" };

        private readonly Button[] _buttons = new Button[4];
        private readonly RectTransform[] _tiles = new RectTransform[4];
        private readonly GameObject[] _glows = new GameObject[4];
        private readonly TextMeshProUGUI[] _counts = new TextMeshProUGUI[4];
        private readonly GameObject[] _countBadges = new GameObject[4];
        private readonly TextMeshProUGUI[] _prices = new TextMeshProUGUI[4];
        private readonly GameObject[] _priceBadges = new GameObject[4];
        private readonly CanvasGroup[] _fade = new CanvasGroup[4];
        private readonly BoosterTileState[] _states = new BoosterTileState[4];
        private BoosterKind? _targeting;

        public static BoosterBar Create(RectTransform area, Action<BoosterKind> onPress)
        {
            var bar = area.gameObject.AddComponent<BoosterBar>();
            for (int i = 0; i < Order.Length; i++)
            {
                BoosterKind kind = Order[i];
                RectTransform place = UiFactory.Place(UiFactory.CreateRect(kind.ToString(), area), (i * 0.25f) + 0.02f, 0f, ((i + 1) * 0.25f) - 0.02f, 1f);
                RectTransform tile = UiFactory.Place(UiFactory.CreateRect("Tile", place), 0.06f, 0.08f, 0.94f, 0.96f);
                var fitter = tile.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = DesignTokens.Size.BoosterTileWidth / DesignTokens.Size.BoosterTileHeight;
                bar._tiles[i] = tile;
                bar._fade[i] = tile.gameObject.AddComponent<CanvasGroup>();

                // The selected glow behind the tile: a golden halo and ring, pulsing (contracts/booster-tile.md layer 1).
                RectTransform glow = UiFactory.Stretch(UiFactory.CreateRect("Glow", tile));
                Image halo = UiKit.Rounded("Halo", glow, UiTheme.Of(DesignTokens.Colors.GardenGlow.WithAlpha(0.3f)), 60f);
                UiFactory.Place(halo.rectTransform, -0.18f, -0.18f, 1.18f, 1.18f);
                Image ring = UiKit.Rounded("Ring", glow, UiTheme.Of(DesignTokens.Colors.GardenGlow), 46f);
                UiFactory.Place(ring.rectTransform, -0.04f, -0.04f, 1.04f, 1.04f);
                glow.gameObject.AddComponent<GlowPulse>();
                glow.gameObject.SetActive(false);
                bar._glows[i] = glow.gameObject;

                // The plate and the raised tile in the booster's color, with its icon (layers 2–5).
                GardenButton view = UiKit.Garden("Button", tile, GardenLook.Booster(Ids[i]), DesignTokens.Size.BoosterTileHeight, radiusUnits: 40f);
                view.TileSquash = true;
                Image glyph = UiKit.GardenGlyph(view, view.Content, "booster." + Ids[i]);
                UiFactory.Place(glyph.rectTransform, 0.16f, 0.12f, 0.84f, 0.88f);
                if (Ids[i] == "bloom_burst")
                {
                    glyph.color = UiTheme.PetalCenter;
                }

                var button = view.gameObject.AddComponent<Button>();
                button.targetGraphic = view.Top;
                button.onClick.AddListener(() => onPress(kind));
                view.Button = button;
                bar._buttons[i] = button;

                // The tags (layer 6): the charges badge at the top-right corner, or the price tag below with a green plus.
                bar._counts[i] = UiKit.CountBadge("Count", tile, out Image countDisc);
                UiFactory.Place(countDisc.rectTransform, 0.66f, 0.72f, 1.08f, 1.08f);
                bar._countBadges[i] = countDisc.gameObject;

                RectTransform price = UiFactory.Stretch(UiFactory.CreateRect("Price", tile));
                Image tagLine = UiKit.Pill("Tag", price, UiTheme.Of(DesignTokens.Colors.GardenOutline));
                UiFactory.Place(tagLine.rectTransform, 0.18f, -0.16f, 0.82f, 0.14f);
                Image tagFace = UiKit.Pill("Face", tagLine.transform, UiTheme.Of(GardenLook.Cream.Face));
                UiFactory.Stretch(tagFace.rectTransform);
                tagFace.rectTransform.offsetMin = new Vector2(UiKit.Units(3f), UiKit.Units(3f));
                tagFace.rectTransform.offsetMax = new Vector2(-UiKit.Units(3f), -UiKit.Units(3f));
                Image petal = UiKit.PetalIcon("Petal", tagFace.transform);
                UiFactory.Place(petal.rectTransform, 0.04f, 0.08f, 0.38f, 0.92f);
                bar._prices[i] = UiKit.Label("Amount", tagFace.transform, string.Empty, DesignTokens.Type.Badge, UiTheme.Of(DesignTokens.Colors.GardenLabelPlain));
                UiFactory.Place(bar._prices[i].rectTransform, 0.36f, 0.05f, 0.96f, 0.95f);
                Image plusLine = UiFactory.CreateImage("PlusLine", price, ProceduralSprites.Circle, UiTheme.Of(GardenLook.Green.Line));
                plusLine.preserveAspect = true;
                UiFactory.Place(plusLine.rectTransform, 0.72f, 0.72f, 1.04f, 1.04f);
                Image plusFace = UiFactory.CreateImage("Plus", plusLine.transform, ProceduralSprites.Circle, UiTheme.Of(GardenLook.Green.Face));
                plusFace.preserveAspect = true;
                UiFactory.Place(plusFace.rectTransform, 0.06f, 0.1f, 0.94f, 0.98f);
                Image plusGlyph = UiFactory.CreateImage("Glyph", plusFace.transform, ProceduralSprites.Shape("ui.plus"), Color.white);
                plusGlyph.preserveAspect = true;
                UiFactory.Place(plusGlyph.rectTransform, 0.22f, 0.22f, 0.78f, 0.78f);
                bar._priceBadges[i] = price.gameObject;
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
                Transform place = _tiles[i].parent;
                place.gameObject.SetActive(s.Unlocked);
                _states[i] = new BoosterTileState(s.Charges, s.Price, _targeting == Order[i], s.Applicable, s.Charges > 0 || s.Affordable);
                Show(i);
                _counts[i].text = "×" + s.Charges.ToString(CultureInfo.InvariantCulture);
                _prices[i].text = NumberText.Group(s.Price);
            }
        }

        /// <summary>Raises and rings the tile whose target the player is choosing (Return, Bloom Burst).</summary>
        public void SetTargeting(BoosterKind? kind)
        {
            _targeting = kind;
            for (int i = 0; i < Order.Length; i++)
            {
                BoosterTileState s = _states[i];
                _states[i] = new BoosterTileState(s.Charges, s.Price, kind == Order[i], s.Usable, s.Affordable);
                Show(i);
            }
        }

        /// <summary>Draws a tile in its state (contracts/booster-tile.md "States").</summary>
        private void Show(int i)
        {
            BoosterTileState s = _states[i];
            _buttons[i].interactable = !s.Disabled;
            _fade[i].alpha = s.Disabled ? 0.45f : 1f;
            _glows[i].SetActive(s.Selected);
            _tiles[i].anchoredPosition = new Vector2(0f, s.Selected ? UiKit.Units(12f) : 0f);
            _countBadges[i].SetActive(s.ShowsCharges);
            _priceBadges[i].SetActive(s.ShowsPrice);
        }

        /// <summary>A short pulse when a booster is used.</summary>
        public void Pulse(BoosterKind kind)
        {
            if (isActiveAndEnabled)
            {
                StartCoroutine(PulseRoutine(_tiles[Array.IndexOf(Order, kind)]));
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

        /// <summary>The selected glow's pulse: 55% to 100% every <c>motion.glow</c>, on unscaled time.</summary>
        private sealed class GlowPulse : MonoBehaviour
        {
            private CanvasGroup? _group;

            private void OnEnable() => _group ??= gameObject.AddComponent<CanvasGroup>();

            private void Update()
            {
                if (_group != null)
                {
                    _group.alpha = GardenLook.Glow(Time.unscaledTime);
                }
            }
        }
    }
}
