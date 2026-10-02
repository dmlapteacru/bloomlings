using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The reference gameplay tray's own pieces (spec 005 FR-020, FR-021; contracts/look.md §6.1 "Drawn"), the twins of the
    /// playtest's <c>LevelScreen.TrayPanel</c> and <c>PodPainter.Front</c>, <c>PodPainter.Buried</c> and
    /// <c>PodPainter.EmptyDeck</c>: the parchment tray across the screen with one band per row, a deck's front pod and its
    /// buried pods. Sizes are shares of the safe width or of the piece's own box, so they scale with the screen; each is
    /// laid out from its own rect (<see cref="BoxLayout"/>); decorations never take taps.
    /// </summary>
    public static partial class UiKit
    {
        /// <summary>
        /// The tray (<c>mat.parchment</c>): a frame of deep parchment (<c>parchment.edge</c> mixed 55% toward
        /// <c>parchment.line</c>) with a dark outline and a soft shadow rising onto the lawn, and over it one band of warm
        /// parchment per row, parted by grooves. Place it over the tray's box (its bottom corners fall below the screen) and
        /// set its bands with <see cref="TrayPanelView.Set"/>.
        /// </summary>
        public static TrayPanelView TrayPanel(string name, Transform parent)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<TrayPanelView>();
            view.Build(layout);
            return view;
        }

        /// <summary>
        /// The front pod of a deck (§6.1, <c>pod.card</c>; the playtest's <c>PodPainter.Front</c>): the dark wooden frame
        /// filling the rect with no handle (the buried pods peek above it), its panel in the variant's color lightened 0.5
        /// at the top and 0.8 at the bottom, the sticker tile and the big count below it as <see cref="PodDeck"/> places
        /// them. Queued pods are dimmed, a pressed one sinks, a locked one shows the padlock on a grey panel; a null variant
        /// is a mystery pod. Set it with <see cref="DeckPodView.Show"/>.
        /// </summary>
        public static DeckPodView DeckPod(string name, Transform parent)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<DeckPodView>();
            view.Build(layout);
            return view;
        }

        /// <summary>
        /// A buried pod of a deck (<c>pod.deck</c>, FR-021; the playtest's <c>PodPainter.Buried</c>): the same wooden frame
        /// filling the rect, of which only its band shows above the pod in front: the frame's top edge and a strip of the
        /// pod's variant color with its small symbol as a dark silhouette with a light halo. A hidden mystery pod shows a
        /// lilac strip with a white "?", a locked one a grey strip with the padlock. Set it with
        /// <see cref="BuriedPodView.Show"/>. Never a touch target.
        /// </summary>
        public static BuriedPodView BuriedPod(string name, Transform parent)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<BuriedPodView>();
            view.Build(layout);
            return view;
        }

        /// <summary>A variant's color as the deck pieces tint with it (the catalog's color, <c>tile.mystery</c> when unknown).</summary>
        internal static Rgba DeckColor(VariantId variant) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? Rgba.FromHex(info.ColorHex) : C.TileMystery;

        /// <summary>
        /// The pod's wooden frame shared by the deck pieces (the playtest's <c>Kit.PodFrame</c> without the handle): the
        /// soft shadow, the inner panel and the frame picture, laid out in <paramref name="box"/> of width w: radius 18%,
        /// border 11%; a pressed frame sinks 3.5% of its width.
        /// </summary>
        internal static (Image Shadow, Image Panel, Image Frame) DeckFrame(Transform root, Func<float> radius, Func<float> panelRadius)
        {
            Image shadow = RoundRect("Shadow", root, UiTheme.Of(C.GardenShadow.WithAlpha(0.26f)), _ => radius());
            Image panel = RoundGradient("Panel", root, C.CreamTop, C.CreamFace, _ => panelRadius());
            Image frame = UiFactory.CreateImage("Frame", root, null, Color.white);
            PictureFit.On(frame, (w, h) => ProceduralSprites.Frame(WoodTone.Dark, w, h), sliced: true);
            return (shadow, panel, frame);
        }
    }

    /// <summary>The tray built by <see cref="UiKit.TrayPanel"/>: its frame and up to three bands.</summary>
    public sealed class TrayPanelView : MonoBehaviour
    {
        /// <summary>The tray's frame showing round its bands, as a share of the safe width.</summary>
        public const float Margin = 0.012f;

        /// <summary>The groove between two bands, as a share of the safe width.</summary>
        public const float Groove = 0.009f;

        /// <summary>The corner radius of the bands after the first, as a share of the safe width.</summary>
        public const float BandRadius = 0.03f;

        private const int MaxBands = 3;

        private readonly Image[] _shadows = new Image[3];
        private readonly float[] _shadowRadii = new float[3];
        private readonly Band[] _bands = new Band[MaxBands];
        private BoxLayout _layout = null!;
        private Image _fill = null!;
        private Image _line = null!;
        private float _radius;
        private float _lineWidth;
        private float _wShare = 1f;
        private float _radiusShare;
        private float[] _cuts = Array.Empty<float>();

        internal void Build(BoxLayout layout)
        {
            _layout = layout;
            Transform root = layout.transform;
            for (int k = 0; k < 3; k++)
            {
                int index = k;
                _shadows[k] = UiKit.RoundRect("Shadow" + k, root, UiTheme.Of(C.GardenShadow.WithAlpha(0.07f)), _ => _shadowRadii[index]);
            }

            _fill = UiKit.RoundRect("Fill", root, UiTheme.Of(C.ParchmentEdge.Mix(C.ParchmentLine, 0.55f)), _ => _radius);
            _line = UiKit.RoundRing("Line", root, UiTheme.Of(C.ParchmentLine.Darken(0.25f)), _ => _radius, _ => _lineWidth);
            for (int i = 0; i < MaxBands; i++)
            {
                _bands[i] = new Band(root, i);
            }

            layout.Then(Lay);
        }

        /// <summary>
        /// Lays the tray out for a <paramref name="frame"/> box (the tray's box, its bottom corners below the screen) in any
        /// units: <paramref name="w"/> is the safe width its sizes scale by, <paramref name="radius"/> its top corners'
        /// radius, and <paramref name="cuts"/> the y of the grooves between its bands (at most two), top down.
        /// </summary>
        public void Set(Box frame, float w, float radius, IReadOnlyList<float> cuts)
        {
            float width = Mathf.Max(0.0001f, frame.Width);
            float height = Mathf.Max(0.0001f, frame.Height);
            _wShare = w / width;
            _radiusShare = radius / width;
            int count = Mathf.Min(cuts.Count, MaxBands - 1);
            _cuts = new float[count];
            for (int i = 0; i < count; i++)
            {
                _cuts[i] = (cuts[i] - frame.Top) / height;
            }

            _layout.Apply();
        }

        private void Lay(Box box)
        {
            float w = box.Width * _wShare;
            _radius = Mathf.Min(box.Width * _radiusShare, box.Height / 2f);
            float px = 1f / Mathf.Max(0.0001f, UiKit.PixelsPerUnit);

            // The soft shadow rising onto the lawn: three layers grown up and sideways by 1% of the width each.
            for (int k = 0; k < 3; k++)
            {
                float g = w * 0.01f * (3 - k);
                _shadowRadii[k] = _radius + g;
                BoxLayout.Place(_shadows[k].rectTransform, new Box(box.Left - g, box.Top - g, box.Right + g, box.Bottom));
            }

            _lineWidth = Mathf.Max(2f * px, w * 0.004f);
            BoxLayout.Place(_fill.rectTransform, box);
            BoxLayout.Place(_line.rectTransform, box);

            float margin = w * Margin;
            float groove = Mathf.Max(2f * px, w * Groove);
            float top = box.Top + margin;
            for (int i = 0; i < MaxBands; i++)
            {
                bool shown = i <= _cuts.Length;
                _bands[i].Root.gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }

                float bottom = i < _cuts.Length ? box.Top + (_cuts[i] * box.Height) - (groove / 2f) : box.Bottom;
                float corner = i == 0 ? _radius - margin : w * BandRadius;
                _bands[i].Lay(new Box(box.Left + margin, top, box.Right - margin, bottom), corner, w, px);
                top = i < _cuts.Length ? box.Top + (_cuts[i] * box.Height) + (groove / 2f) : top;
            }

            foreach (Image image in _shadows)
            {
                image.GetComponent<RoundShape>().Apply();
            }

            _fill.GetComponent<RoundShape>().Apply();
            _line.GetComponent<RoundShape>().Apply();
        }

        /// <summary>
        /// One band of the tray's parchment: <c>parchment.well</c> with light wood at the top to <c>parchment.edge</c> at the
        /// bottom, its edges aged darker, a light bevel along its top (the light line under each groove) and a thin
        /// <c>parchment.line</c> outline (the playtest's <c>LevelScreen.TrayBand</c>).
        /// </summary>
        private sealed class Band
        {
            private readonly Image _fill;
            private readonly Image _aged;
            private readonly Image _bevel;
            private readonly Image _line;
            private float _radius;
            private float _aging;
            private float _bevelWidth;
            private float _lineWidth;

            public Band(Transform parent, int index)
            {
                Root = UiFactory.CreateRect("Band" + index, parent);
                _fill = UiKit.RoundGradient("Fill", Root, C.ParchmentWell.Mix(C.WoodLight, 0.55f), C.ParchmentEdge.Mix(C.ParchmentWell, 0.4f), _ => _radius);
                _aged = UiKit.RoundFade("Aged", Root, UiTheme.Of(C.ParchmentLine.WithAlpha(0.28f)), _ => _radius, _ => _aging);
                _bevel = UiKit.RoundRing("Bevel", Root, Color.white, _ => Mathf.Max(0f, _radius - (_bevelWidth / 2f)), _ => _bevelWidth);
                UiKit.Gradient(_bevel, UiTheme.Of(C.ParchmentTop.WithAlpha(0.8f)), UiTheme.Of(C.ParchmentTop.WithAlpha(0f)));
                _line = UiKit.RoundRing("Line", Root, UiTheme.Of(C.ParchmentLine.WithAlpha(0.55f)), _ => _radius, _ => _lineWidth);
            }

            public RectTransform Root { get; }

            public void Lay(Box box, float radius, float w, float px)
            {
                _radius = Mathf.Max(0f, Mathf.Min(radius, box.Height / 2f));
                _aging = w * 0.024f;
                _bevelWidth = Mathf.Max(px, w * 0.003f);
                _lineWidth = Mathf.Max(px, w * 0.002f);
                BoxLayout.Place(Root, box);
                var local = new Box(0f, 0f, box.Width, box.Height);
                BoxLayout.Place(_fill.rectTransform, local);
                BoxLayout.Place(_aged.rectTransform, local);
                Box ring = local.Inset(_bevelWidth / 2f);
                BoxLayout.Place(_bevel.rectTransform, ring);
                BoxLayout.Place(_line.rectTransform, local);

                // The bevel shows along the top and round the upper corners only: it fades out just below them.
                _bevel.GetComponent<VerticalGradient>().Stop = Mathf.Clamp01((_radius + (w * 0.006f)) / Mathf.Max(1f, ring.Height));
                _bevel.SetVerticesDirty();
                foreach (Image image in new[] { _fill, _aged, _bevel, _line })
                {
                    image.GetComponent<RoundShape>().Apply();
                }
            }
        }
    }

    /// <summary>A deck's front pod built by <see cref="UiKit.DeckPod"/>.</summary>
    public sealed class DeckPodView : MonoBehaviour
    {
        /// <summary>The count's type size as a share of its room under the tile (the reference's big dark digits).</summary>
        public const float CountFill = 1.2f;

        /// <summary>How far the variant's color is lightened for the top of the panel (the reference's tinted panels).</summary>
        public const float PanelTop = 0.5f;

        /// <summary>How far the variant's color is lightened for the bottom of the panel, under the count.</summary>
        public const float PanelBottom = 0.8f;

        private BoxLayout _layout = null!;
        private Image _shadow = null!;
        private Image _panel = null!;
        private Image _frame = null!;
        private Image _veil = null!;
        private Image _shade = null!;
        private Image _lock = null!;
        private CandyTileView _tile = null!;
        private TextMeshProUGUI _count = null!;
        private float _radius;
        private float _panelRadius;

        /// <summary>The pod's state.</summary>
        public PodLook Look { get; private set; }

        /// <summary>The variant tile.</summary>
        public CandyTileView Tile => _tile;

        /// <summary>The count label.</summary>
        public TextMeshProUGUI Count => _count;

        /// <summary>The padlock of a locked pod (it grows as the key lands).</summary>
        public Image Lock => _lock;

        internal void Build(BoxLayout layout)
        {
            _layout = layout;
            Transform root = layout.transform;
            (_shadow, _panel, _frame) = UiKit.DeckFrame(root, () => _radius, () => _panelRadius);
            _veil = UiKit.RoundRect("Veil", root, UiTheme.Of(C.ParchmentBottom.WithAlpha(0.45f)), _ => _radius);
            _shade = UiKit.RoundRect("PressShade", root, UiTheme.Of(C.GardenShadow.WithAlpha(0.08f)), _ => _radius);
            _tile = UiKit.CandyTile("Tile", root, null, TileStyle.Sticker);
            _lock = UiKit.ShapeImage("Lock", root, "ui.lock", C.StateLock.Darken(0.2f));
            _count = UiKit.KitLabel("Count", root, string.Empty, T.Count, TextLook.Plain(C.InkBrown));
            layout.Then(Lay);
        }

        /// <summary>Shows the pod: its variant (null: a mystery), its count and its look.</summary>
        public void Show(VariantId? variant, int count, PodLook look)
        {
            Look = look;
            bool queued = look == PodLook.Next;
            bool locked = look == PodLook.Locked;
            _shadow.color = UiTheme.Of(C.GardenShadow.WithAlpha(queued ? 0.12f : 0.26f));
            if (locked)
            {
                UiKit.Gradient(_panel, UiTheme.Of(C.StateLockBg.Lighten(0.2f)), UiTheme.Of(C.StateLockBg));
            }
            else if (variant.HasValue && !queued)
            {
                Rgba color = UiKit.DeckColor(variant.Value);
                UiKit.Gradient(_panel, UiTheme.Of(color.Lighten(PanelTop)), UiTheme.Of(color.Lighten(PanelBottom)));
            }
            else
            {
                UiKit.Gradient(_panel, UiTheme.Of(C.CreamTop), UiTheme.Of(C.CreamFace));
            }

            _veil.gameObject.SetActive(queued);
            _shade.gameObject.SetActive(look == PodLook.Pressed);
            _lock.gameObject.SetActive(locked);
            _tile.gameObject.SetActive(!locked);
            if (!locked)
            {
                _tile.Show(variant, queued ? TileState.Dimmed : TileState.Normal);
                _tile.Pressed = look == PodLook.Pressed;
            }

            _count.text = count.ToString(CultureInfo.InvariantCulture);
            _layout.Apply();
        }

        /// <summary>
        /// The deck box a front pod filling <paramref name="front"/> belongs to (<see cref="PodDeck.Front"/> is its bottom
        /// <see cref="PodDeck.FrontShare"/>).
        /// </summary>
        public static Box DeckOf(Box front) => new Box(front.Left, front.Bottom - (front.Height / PodDeck.FrontShare), front.Right, front.Bottom);

        private void Lay(Box box)
        {
            float w = box.Width;
            bool pressed = Look == PodLook.Pressed;
            float sink = pressed ? w * 0.035f : 0f;
            Box frame = box.Offset(0f, sink);
            _radius = w * 0.18f;
            float border = w * PodDeck.Border;
            _panelRadius = Mathf.Max(0f, _radius - (border * 0.8f));
            BoxLayout.Place(_shadow.rectTransform, frame.Offset(0f, w * (pressed ? 0.01f : 0.05f)).Inset(w * 0.03f, 0f));
            BoxLayout.Place(_panel.rectTransform, frame.Inset(border * 0.8f));
            BoxLayout.Place(_frame.rectTransform, frame);
            BoxLayout.Place(_veil.rectTransform, frame);
            BoxLayout.Place(_shade.rectTransform, frame);

            // The tile and the count where the deck places them (§6.1), sinking with the frame.
            PodDeck deck = PodDeck.In(DeckOf(box));
            Box tile = deck.Tile.Offset(0f, sink);
            BoxLayout.Place((RectTransform)_tile.transform, tile);
            float g = tile.Width * 0.62f;
            BoxLayout.Place(_lock.rectTransform, Box.FromCenter(tile.CenterX, tile.CenterY, g, g));
            UiKit.PlaceCount(_count, deck.Count.Offset(0f, sink), Look == PodLook.Next || Look == PodLook.Locked, CountFill);
            foreach (Image image in new[] { _shadow, _panel, _veil, _shade })
            {
                image.GetComponent<RoundShape>().Apply();
            }
        }
    }

    /// <summary>A buried pod built by <see cref="UiKit.BuriedPod"/>.</summary>
    public sealed class BuriedPodView : MonoBehaviour
    {
        /// <summary>The frame's top edge, as a share of its band; the variant's strip fills the rest.</summary>
        public const float BandRail = 0.3f;

        private BoxLayout _layout = null!;
        private Image _shadow = null!;
        private Image _panel = null!;
        private Image _frame = null!;
        private Image _stripLine = null!;
        private Image _strip = null!;
        private Image _symbol = null!;
        private Image _lock = null!;
        private Image _shade = null!;
        private float _radius;
        private float _panelRadius;
        private float _stripRadius;
        private float _stripLineRadius;
        private float _shadeRadius;

        /// <summary>The pod shown, or null.</summary>
        public string? PodId { get; set; }

        /// <summary>The variant shown (null: a hidden mystery pod).</summary>
        public VariantId? Variant { get; private set; }

        /// <summary>Whether the padlock shows.</summary>
        public bool Locked { get; private set; }

        /// <summary>How deep the pod lies (1 or 2): each depth sits a little further in the shade.</summary>
        public int Depth { get; private set; } = 1;

        /// <summary>Where the small symbol is (flights and keys aim there).</summary>
        public RectTransform Symbol => _symbol.rectTransform;

        internal void Build(BoxLayout layout)
        {
            _layout = layout;
            Transform root = layout.transform;
            (_shadow, _panel, _frame) = UiKit.DeckFrame(root, () => _radius, () => _panelRadius);
            _stripLine = UiKit.RoundRect("StripLine", root, Color.white, _ => _stripLineRadius);
            _strip = UiKit.RoundRect("Strip", root, Color.white, _ => _stripRadius);
            _symbol = UiFactory.CreateImage("Symbol", root, null, Color.white);
            _symbol.preserveAspect = true;
            _lock = UiKit.ShapeImage("Lock", root, "ui.lock", C.StateLock.Darken(0.25f));
            _shade = UiKit.RoundRect("Shade", root, UiTheme.Of(C.GardenShadow.WithAlpha(0.06f)), _ => _shadeRadius);
            layout.Then(Lay);
        }

        /// <summary>Shows the buried pod: its variant (null: a hidden mystery pod), whether it is locked and its depth.</summary>
        public void Show(VariantId? variant, bool locked, int depth)
        {
            Variant = variant;
            Locked = locked;
            Depth = Mathf.Clamp(depth, 1, 2);
            Rgba? tint = variant.HasValue && !locked ? UiKit.DeckColor(variant.Value) : (Rgba?)null;
            if (tint.HasValue)
            {
                UiKit.Gradient(_panel, UiTheme.Of(tint.Value.Lighten(DeckPodView.PanelTop)), UiTheme.Of(tint.Value.Lighten(DeckPodView.PanelBottom)));
            }
            else
            {
                UiKit.Gradient(_panel, UiTheme.Of(C.CreamTop), UiTheme.Of(C.CreamFace));
            }

            Rgba color = locked ? C.StateLockBg : tint ?? C.TileMystery;
            _stripLine.color = UiTheme.Of(color.Darken(0.45f));
            UiKit.Gradient(_strip, UiTheme.Of(color.Lighten(0.4f)), UiTheme.Of(color.Lighten(0.12f)));
            _lock.gameObject.SetActive(locked);
            _symbol.gameObject.SetActive(!locked);
            if (!locked)
            {
                // Small, the symbol reads best as a dark silhouette with a light halo (as the board's small gems); a hidden
                // mystery pod shows a white "?" outlined in the lilac's dark shade.
                _symbol.sprite = variant.HasValue && VariantCatalog.Default.TryGet(variant.Value, out VariantInfo info)
                    ? ProceduralSprites.Haloed(ShapeLibrary.SymbolId(info.IconId), color.Darken(0.52f), color.Lighten(0.55f), 0.14f)
                    : ProceduralSprites.Haloed("tile.mystery", Rgba.White, color.Darken(0.45f), 0.16f);
                _symbol.color = Color.white;
            }

            _shade.color = UiTheme.Of(C.GardenShadow.WithAlpha(0.06f * Depth));
            _layout.Apply();
        }

        /// <summary>
        /// The band of a buried pod filling <paramref name="frame"/>: from its top down to the top of the pod in front of it
        /// (<see cref="PodDeck.Raise"/> of the deck's height).
        /// </summary>
        public static Box BandOf(Box frame) => new Box(frame.Left, frame.Top, frame.Right, frame.Top + (frame.Height * PodDeck.Raise / PodDeck.FrontShare));

        private void Lay(Box box)
        {
            float w = box.Width;
            _radius = w * 0.18f;
            float border = w * PodDeck.Border;
            _panelRadius = Mathf.Max(0f, _radius - (border * 0.8f));
            BoxLayout.Place(_shadow.rectTransform, box.Offset(0f, w * 0.05f).Inset(w * 0.03f, 0f));
            BoxLayout.Place(_panel.rectTransform, box.Inset(border * 0.8f));
            BoxLayout.Place(_frame.rectTransform, box);

            // The strip runs from under the frame's top edge into the pod in front, which covers its lower part.
            Box band = BandOf(box);
            float inset = w * PodDeck.Border * 0.8f;
            float top = band.Top + (band.Height * BandRail);
            var strip = new Box(box.Left + inset, top, box.Right - inset, band.Bottom + (band.Height * 0.6f));
            _stripRadius = Mathf.Min(strip.Height / 2f, w * 0.06f);
            float px = 1f / Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            float line = Mathf.Max(px, w * 0.012f);
            _stripLineRadius = _stripRadius + line;
            BoxLayout.Place(_stripLine.rectTransform, strip.Inset(-line));
            BoxLayout.Place(_strip.rectTransform, strip);

            // The symbol's shape spans about 70% of its box, so the box is as tall as the strip's visible part.
            float size = band.Bottom - top;
            Box symbol = Box.FromCenter(band.CenterX, (top + band.Bottom) / 2f, size, size);
            BoxLayout.Place(_symbol.rectTransform, symbol);
            BoxLayout.Place(_lock.rectTransform, symbol);

            // The buried pods sit a little in the shade of the one in front.
            _shadeRadius = w * 0.18f;
            BoxLayout.Place(_shade.rectTransform, new Box(box.Left, band.Top, box.Right, band.Bottom));
            foreach (Image image in new[] { _shadow, _panel, _stripLine, _strip, _shade })
            {
                image.GetComponent<RoundShape>().Apply();
            }
        }
    }
}
