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
    /// playtest's <c>LevelScreen.TrayPanel</c> and <c>PodPainter</c>: the parchment tray across the screen with one band
    /// per row, and the pods of the Source stacks' columns, one after another and never on each other (the owner's
    /// gameplay rule, 2026-10-03; <see cref="ReferenceGameplayRegions.Pod"/>). Sizes are shares of the safe width or of
    /// the piece's own box, so they scale with the screen; each is laid out from its own rect (<see cref="BoxLayout"/>);
    /// decorations never take taps.
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
        /// A pod of the tray's grid (§6.1, <c>pod.card</c>; the playtest's <c>PodPainter</c>), filling a rect wider than
        /// tall (<see cref="Design.PodChip"/>): the dark wooden frame (<c>mat.wood.dark</c>) with no handle, its cream panel tinted
        /// by the variant (lightened 0.5 at the top and 0.8 at the bottom), the variant's sticker tile at the panel's left
        /// (<see cref="Design.PodChip.Tile"/>, with the owner's icon) and the plain count in the room right of it
        /// (<see cref="Design.PodChip.Count"/>, as large as the room allows). A waiting pod (<c>pod.deck</c>) shows the same parts
        /// muted but readable: the frame and panel under a parchment veil, the tile dimmed, the count softer. A pressed pod
        /// sinks, a locked one shows the padlock on a grey panel, a null variant is a mystery pod ("?" and its count). Set
        /// it with <see cref="GridPodView.Show"/>. Never a touch target itself.
        /// </summary>
        public static GridPodView GridPod(string name, Transform parent)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            var view = root.gameObject.AddComponent<GridPodView>();
            view.Build(layout);
            return view;
        }

        /// <summary>A variant's color as the pods tint with it (the catalog's color, <c>tile.mystery</c> when unknown).</summary>
        internal static Rgba PodColor(VariantId variant) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? Rgba.FromHex(info.ColorHex) : C.TileMystery;
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

    /// <summary>
    /// A pod of the tray's grid built by <see cref="UiKit.GridPod"/> (the playtest's <c>PodPainter</c> pod), its parts where
    /// <see cref="PodChip.In"/> places them in the rect: the soft shadow, the panel, the wooden frame, the veil of a waiting
    /// pod, the press shade, the sticker tile (with the veil of a waiting mystery tile), the padlock and the count.
    /// </summary>
    public sealed class GridPodView : MonoBehaviour
    {
        /// <summary>The frame's corner radius, as a share of the pod's height.</summary>
        public const float Radius = 0.22f;

        /// <summary>The count's type size as a share of its room's height (the reference's big dark digits; the width caps it).</summary>
        public const float CountFill = 1.1f;

        /// <summary>How far the variant's color is lightened for the top of the panel (the reference's tinted panels).</summary>
        public const float PanelTop = 0.5f;

        /// <summary>How far the variant's color is lightened for the bottom of the panel.</summary>
        public const float PanelBottom = 0.8f;

        /// <summary>How far a pressed pod's frame sinks, as a share of its height.</summary>
        public const float Sink = 0.06f;

        /// <summary>The <c>parchment.bottom</c> veil over a waiting pod's frame and panel (its tile and count stay clear of it).</summary>
        public const float VeilAlpha = 0.45f;

        private BoxLayout _layout = null!;
        private Image _shadow = null!;
        private Image _panel = null!;
        private Image _frame = null!;
        private Image _veil = null!;
        private Image _shade = null!;
        private Image _tileVeil = null!;
        private Image _lock = null!;
        private CandyTileView _tile = null!;
        private TextMeshProUGUI _count = null!;
        private float _radius;
        private float _panelRadius;
        private float _tileRadius;

        /// <summary>The pod's state.</summary>
        public PodLook Look { get; private set; }

        /// <summary>Whether the pod waits in its column under the exposed one (muted).</summary>
        public bool Waiting { get; private set; }

        /// <summary>The variant tile (committed pods fly from there).</summary>
        public CandyTileView Tile => _tile;

        /// <summary>The count label.</summary>
        public TextMeshProUGUI Count => _count;

        /// <summary>The padlock of a locked pod (it grows as the key lands).</summary>
        public Image Lock => _lock;

        internal void Build(BoxLayout layout)
        {
            _layout = layout;
            Transform root = layout.transform;
            _shadow = UiKit.RoundRect("Shadow", root, UiTheme.Of(C.GardenShadow.WithAlpha(0.26f)), _ => _radius);
            _panel = UiKit.RoundGradient("Panel", root, C.CreamTop, C.CreamFace, _ => _panelRadius);

            // The wooden frame with its corner and border as shares of the pod's height, whatever its width.
            _frame = UiFactory.CreateImage("Frame", root, null, Color.white);
            PictureFit.On(_frame, (w, h) => ProceduralSprites.Frame(WoodTone.Dark, w, h, Radius * h / Mathf.Max(1, w), PodChip.Border * h / Mathf.Max(1, w)), sliced: true);
            _veil = UiKit.RoundRect("Veil", root, UiTheme.Of(C.ParchmentBottom.WithAlpha(VeilAlpha)), _ => _radius);
            _shade = UiKit.RoundRect("PressShade", root, UiTheme.Of(C.GardenShadow.WithAlpha(0.08f)), _ => _radius);
            _tile = UiKit.CandyTile("Tile", root, null, TileStyle.Sticker);

            // The mystery tile has no dimmed picture: a veil of the parchment dims it like the others.
            _tileVeil = UiKit.RoundRect("TileVeil", root, UiTheme.Of(C.ParchmentBottom.WithAlpha(VeilAlpha)), _ => _tileRadius);
            _lock = UiKit.ShapeImage("Lock", root, "ui.lock", C.StateLock.Darken(0.2f));
            _count = UiKit.KitLabel("Count", root, string.Empty, T.Count, TextLook.Plain(C.InkBrown));
            layout.Then(Lay);
        }

        /// <summary>
        /// Shows the pod: its variant (null: a mystery), its count, its look (exposed, pressed or locked) and whether it
        /// waits under the exposed pod (<paramref name="waiting"/>; <see cref="PodLook.Next"/> waits too): muted but
        /// readable, its symbol and count still clear (spec 001 FR-013).
        /// </summary>
        public void Show(VariantId? variant, int count, PodLook look, bool waiting = false)
        {
            Look = look;
            Waiting = waiting || look == PodLook.Next;
            bool locked = look == PodLook.Locked;
            _shadow.color = UiTheme.Of(C.GardenShadow.WithAlpha(Waiting ? 0.12f : 0.26f));
            if (locked)
            {
                UiKit.Gradient(_panel, UiTheme.Of(C.StateLockBg.Lighten(0.2f)), UiTheme.Of(C.StateLockBg));
            }
            else if (variant.HasValue)
            {
                Rgba color = UiKit.PodColor(variant.Value);
                UiKit.Gradient(_panel, UiTheme.Of(color.Lighten(PanelTop)), UiTheme.Of(color.Lighten(PanelBottom)));
            }
            else
            {
                UiKit.Gradient(_panel, UiTheme.Of(C.CreamTop), UiTheme.Of(C.CreamFace));
            }

            _veil.gameObject.SetActive(Waiting);
            _shade.gameObject.SetActive(look == PodLook.Pressed);
            _lock.gameObject.SetActive(locked);
            _tile.gameObject.SetActive(!locked);
            _tileVeil.gameObject.SetActive(!locked && Waiting && !variant.HasValue);
            if (!locked)
            {
                _tile.Show(variant, Waiting ? TileState.Dimmed : TileState.Normal);
                _tile.Pressed = look == PodLook.Pressed;
            }

            _count.text = count.ToString(CultureInfo.InvariantCulture);
            _layout.Apply();
        }

        private void Lay(Box box)
        {
            float h = box.Height;
            bool pressed = Look == PodLook.Pressed;
            float sink = pressed ? h * Sink : 0f;
            Box frame = box.Offset(0f, sink);
            float border = h * PodChip.Border;
            _radius = Mathf.Min(h * Radius, h / 2f);
            _panelRadius = Mathf.Max(0f, _radius - (border * 0.8f));
            BoxLayout.Place(_shadow.rectTransform, frame.Offset(0f, h * (pressed ? 0.02f : 0.07f)).Inset(h * 0.04f, 0f));
            BoxLayout.Place(_panel.rectTransform, frame.Inset(border * 0.8f));
            BoxLayout.Place(_frame.rectTransform, frame);
            BoxLayout.Place(_veil.rectTransform, frame);
            BoxLayout.Place(_shade.rectTransform, frame);

            // The tile at the panel's left and the count right of it (PodChip, §6.1), sinking with the frame.
            PodChip chip = PodChip.In(box);
            Box tile = chip.Tile.Offset(0f, sink);
            BoxLayout.Place((RectTransform)_tile.transform, tile);
            _tileRadius = tile.Width * 0.2f;
            BoxLayout.Place(_tileVeil.rectTransform, tile);
            float g = tile.Width * 0.62f;
            BoxLayout.Place(_lock.rectTransform, Box.FromCenter(tile.CenterX, tile.CenterY, g, g));
            UiKit.PlaceCount(_count, chip.Count.Offset(0f, sink), Waiting || Look == PodLook.Locked, CountFill);
            foreach (Image image in new[] { _shadow, _panel, _veil, _shade, _tileVeil })
            {
                image.GetComponent<RoundShape>().Apply();
            }
        }
    }
}
