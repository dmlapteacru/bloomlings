using System;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The gameplay screen's own pieces in the reference look (spec 005 contracts/look.md §4.1), the twins of the playtest's
    /// <c>LevelScreen.TrayFrame</c>, <c>LevelScreen.TrayBand</c>, <c>SlotPainter.TargetGlow</c> and <c>PodPainter.Link</c>:
    /// the tray's parchment frame and bands, the golden target glow and the connected pods' link bar. Each is laid out
    /// from its own rect (<see cref="BoxLayout"/>); decorations never take taps.
    /// </summary>
    public static partial class UiKit
    {
        /// <summary>
        /// The tray's frame (<c>mat.parchment</c>, §4.1): the deep parchment seen in the grooves between its bands
        /// (<c>parchment.edge</c> mixed 45% toward <c>parchment.line</c>), a dark <c>parchment.line</c> outline 4 units wide
        /// and a soft halo on the lawn, corners 40 units round. The bands go over it (<see cref="TrayBand"/>).
        /// </summary>
        public static RectTransform TrayFrame(string name, Transform parent)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            float r = 0f;
            Image far = RoundRect("HaloFar", root, UiTheme.Of(C.GardenShadow.WithAlpha(0.06f)), _ => r + Units(9f));
            Image near = RoundRect("HaloNear", root, UiTheme.Of(C.GardenShadow.WithAlpha(0.12f)), _ => r + Units(4f));
            Image fill = RoundRect("Fill", root, UiTheme.Of(C.ParchmentEdge.Mix(C.ParchmentLine, 0.45f)), _ => r);
            Image line = RoundRing("Line", root, UiTheme.Of(C.ParchmentLine.Darken(0.22f)), _ => r, _ => Units(4f));
            layout.Add(far.rectTransform, b =>
            {
                r = Mathf.Min(Units(40f), b.Height / 2f);
                return b.Inset(-Units(9f));
            });
            layout.Add(near.rectTransform, b => b.Inset(-Units(4f)));
            layout.Add(fill.rectTransform, b => b);
            layout.Add(line.rectTransform, b => b);
            layout.Then(_ => ApplyShapes(far, near, fill, line));
            return root;
        }

        /// <summary>
        /// One band of the tray's parchment (<c>mat.parchment</c>, §4.1): a board a little deeper than a card's parchment so
        /// the cream plates and tiles stand out on it (<c>parchment.well</c> with light wood at the top to
        /// <c>parchment.edge</c> at the bottom), its edges aged darker toward the outline, a light bevel along its top and
        /// round its upper corners, and a thin <c>parchment.line</c> outline; corners <paramref name="radiusUnits"/> round.
        /// </summary>
        public static RectTransform TrayBand(string name, Transform parent, float radiusUnits)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            float r = 0f;
            Image fill = RoundGradient("Fill", root, C.ParchmentWell.Mix(C.WoodLight, 0.55f), C.ParchmentEdge.Mix(C.ParchmentWell, 0.4f), _ => r);
            Image aged = RoundFade("Aged", root, UiTheme.Of(C.ParchmentLine.WithAlpha(0.28f)), _ => r, _ => Units(26f));
            Image bevel = RoundRing("Bevel", root, Color.white, _ => Mathf.Max(0f, r - Units(1.5f)), _ => Units(3f));
            Gradient(bevel, UiTheme.Of(C.ParchmentTop.WithAlpha(0.8f)), UiTheme.Of(C.ParchmentTop.WithAlpha(0f)));
            Image line = RoundRing("Line", root, UiTheme.Of(C.ParchmentLine.WithAlpha(0.55f)), _ => r, _ => Units(2f));
            layout.Add(fill.rectTransform, b =>
            {
                r = Mathf.Min(Units(radiusUnits), b.Height / 2f);
                return b;
            });
            layout.Add(aged.rectTransform, b => b);
            layout.Add(bevel.rectTransform, b => b.Inset(Units(1.5f)));
            layout.Add(line.rectTransform, b => b);
            layout.Then(b =>
            {
                // The bevel shows along the top and round the upper corners only: it fades out just below them.
                Box ring = b.Inset(Units(1.5f));
                bevel.GetComponent<VerticalGradient>().Stop = Mathf.Clamp01((r + Units(6f)) / Mathf.Max(1f, ring.Height));
                bevel.SetVerticesDirty();
                ApplyShapes(fill, aged, bevel, line);
            });
            return root;
        }

        /// <summary>
        /// The golden glow around a plate that can be picked (Return's target, the selected booster's glow, FR-031): three
        /// <c>garden.glow</c> halos growing to 16% of the shorter side and a ring 4% wide, pulsing every <c>motion.glow</c>
        /// on unscaled time. Place it over the plate's box with <paramref name="radiusShare"/> the plate's corner radius
        /// over its shorter side. Never a touch target.
        /// </summary>
        public static RectTransform TargetGlow(string name, Transform parent, float radiusShare = 0.2f)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            root.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            var radii = new float[4];
            var images = new Image[4];
            float ringWidth = 0f;
            for (int ring = 3; ring >= 1; ring--)
            {
                int k = ring;
                Image halo = RoundRect("Halo" + k, root, UiTheme.Of(C.GardenGlow.WithAlpha(0.24f)), _ => radii[k]);
                images[k] = halo;
                layout.Add(halo.rectTransform, b =>
                {
                    float s = Mathf.Min(b.Width, b.Height);
                    float grow = s * 0.16f * k / 3f;
                    radii[k] = (s * radiusShare) + grow;
                    return b.Inset(-grow);
                });
            }

            // The ring: the playtest's stroke 4% wide centered 4% outside the plate.
            Image line = RoundRing("Ring", root, UiTheme.Of(C.GardenGlow), _ => radii[0], _ => ringWidth);
            images[0] = line;
            layout.Add(line.rectTransform, b =>
            {
                float s = Mathf.Min(b.Width, b.Height);
                ringWidth = s * 0.04f;
                radii[0] = (s * radiusShare) + (s * 0.06f);
                return b.Inset(-s * 0.06f);
            });
            layout.Then(_ => ApplyShapes(images));
            root.gameObject.AddComponent<GlowPulse>();
            return root;
        }

        /// <summary>
        /// The link bar between two connected pods (<c>pod.link</c>, FR-035): a rounded bar with a white rim and a light
        /// line along its top, riveted to each frame, over a soft shadow. Set it with <see cref="LinkBarView.Set"/>.
        /// Never a touch target.
        /// </summary>
        public static LinkBarView LinkBar(string name, Transform parent)
        {
            RectTransform root = UiFactory.Stretch(UiFactory.CreateRect(name, parent));
            var view = root.gameObject.AddComponent<LinkBarView>();
            view.Build();
            return view;
        }

        /// <summary>
        /// A booster tile's body (§3.7, <c>booster.tile</c>; the playtest's <c>Kit.BoosterBezel</c>): a cream face set in a
        /// cream-white bezel with a faint silver tint (<see cref="GardenLook.BoosterRim"/>, light at the top, 6.5% of the
        /// tile), a cream lip along its bottom (<see cref="GardenLook.BoosterLip"/>, 8.5%), a lighter middle feathered into
        /// the face, a faint shade inside the face's top edge and a light edge where the face meets the bezel, a soft tan
        /// outline (<see cref="GardenLook.BoosterLine"/>) and a soft shadow. The square is the largest in the rect, its
        /// corners <paramref name="radius"/> of it. It presses like the kit's faces (the face sinks into the lip and
        /// darkens); a disabled tile keeps its bezel and turns its face grey. Children go into
        /// <see cref="GardenButton.Content"/>: the face inside the bezel; <see cref="GardenButton.IconSide"/> is the tile's side.
        /// </summary>
        internal static GardenButton BoosterBezel(string name, Transform parent, Func<Box, float> radius, bool raycast)
        {
            Rgba rim = GardenLook.BoosterRim;
            var set = new ColorSet("set.booster.bezel", rim.Lighten(0.22f), rim.Lighten(0.62f), GardenLook.BoosterLip, GardenLook.BoosterLine);
            GardenButton view = NewButton(name, parent, set, raycast);
            view.GreyWhenDisabled = false;
            bool Enabled() => view.Button == null || view.Button.interactable;
            BoxLayout layout = BoxLayout.On(view.Body);
            Box Square(Box b) => Box.FromCenter(b.CenterX, b.CenterY, Mathf.Min(b.Width, b.Height), Mathf.Min(b.Width, b.Height));
            float side = 0f;
            float R() => Mathf.Min(radius(new Box(0f, 0f, side, side)), side / 2f);
            SoftShadow(layout, Square, b => Mathf.Min(radius(Square(b)), Mathf.Min(b.Width, b.Height) / 2f), 0.22f, 0.06f);

            RectTransform face = UiFactory.CreateRect("Face", view.Body);
            Image lip = RoundRect("Lip", face, Color.white, _ => R());
            UiFactory.Stretch(lip.rectTransform);
            RectTransform topBox = UiFactory.Stretch(UiFactory.CreateRect("TopBox", face));
            Image top = RoundRect("Bezel", topBox, Color.white, b => Mathf.Min(R(), b.Height / 2f));
            UiFactory.Stretch(top.rectTransform);
            Gradient(top, Color.white, Color.white);
            BoxLayout topLayout = BoxLayout.On(topBox);
            float bezel = 0f;
            float InnerRadius(Box b) => Mathf.Max(0f, Mathf.Min(R(), b.Height / 2f) - bezel);

            // The cream face inside the bezel, grey when disabled.
            float innerRadius = 0f;
            Image inner = RoundRect("Inner", topBox, Color.white, _ => innerRadius);
            Gradient(inner, Color.white, Color.white);
            view.AddExtra(inner, (s, d) =>
            {
                Rgba f = Enabled() ? C.CreamFace : C.CreamFace.Grey().Lighten(0.25f);
                return (f.Darken(0.02f + d), f.Darken(d));
            });
            topLayout.Add(inner.rectTransform, b =>
            {
                innerRadius = InnerRadius(b);
                return b.Inset(bezel);
            });

            // A lighter middle, feathered in, as on the reference's cream tiles.
            var domeRadii = new float[3];
            for (int k = 0; k < 3; k++)
            {
                int step = k;
                Image dome = RoundRect("Dome" + step, topBox, Color.white, _ => domeRadii[step]);
                Gradient(dome, Color.white, Color.clear);
                view.AddExtra(dome, (s, d) =>
                {
                    Rgba middle = Enabled() ? C.CreamTop : C.CreamTop.Grey().Lighten(0.3f);
                    return (middle.Darken(d).WithAlpha(0.35f), middle.WithAlpha(0f));
                });
                topLayout.Add(dome.rectTransform, b =>
                {
                    float inset = side * (0.1f + (0.05f * step));
                    domeRadii[step] = Mathf.Max(0f, InnerRadius(b) - inset);
                    return b.Inset(bezel + inset);
                });
            }

            // The face lies a little below the bezel: a faint shade inside its top edge and a light edge around it.
            Image shade = RoundRect("Shade", topBox, Color.white, _ => innerRadius);
            Gradient(shade, UiTheme.Of(C.GardenShadow.WithAlpha(0.08f)), UiTheme.Of(C.GardenShadow.WithAlpha(0f)));
            shade.GetComponent<VerticalGradient>().Stop = 0.14f;
            topLayout.Add(shade.rectTransform, b => b.Inset(bezel));
            float edge = 0f;
            Image light = RoundRing("Edge", topBox, UiTheme.Of(C.CreamTop.WithAlpha(0.9f)), _ => innerRadius + (edge / 2f), _ => edge);
            topLayout.Add(light.rectTransform, b => b.Inset(bezel - (edge / 2f)));
            topLayout.Then(_ => ApplyShapes(inner, shade, light, top));

            Image line = RoundRing("Line", face, Color.white, _ => R(), _ => Mathf.Max(Units(2f), side * 0.018f));
            UiFactory.Stretch(line.rectTransform);
            RectTransform content = UiFactory.Stretch(UiFactory.CreateRect("Content", face));
            view.BuildFace(line, lip, topBox, top, null, content, topLayout);
            layout.Then(box =>
            {
                Box f = Square(box);
                side = f.Width;
                bezel = side * 0.065f;
                edge = Mathf.Max(1f / Mathf.Max(0.0001f, PixelsPerUnit), side * 0.014f);
                BoxLayout.Place(face, f);
                float lipHeight = side * 0.085f;
                view.SetGeometry(Mathf.Max(Units(2f), side * 0.018f), lipHeight, lipHeight * 0.7f, R(), bezel, side);
            });
            return view;
        }

        private static void ApplyShapes(params Image[] images)
        {
            foreach (Image image in images)
            {
                RoundShape shape = image.GetComponent<RoundShape>();
                if (shape != null)
                {
                    shape.Apply();
                }
            }
        }
    }

    /// <summary>A glow's pulse (<see cref="UiKit.TargetGlow"/>): its group's alpha follows <see cref="GardenLook.Glow"/> on unscaled time.</summary>
    public sealed class GlowPulse : MonoBehaviour
    {
        private CanvasGroup? _group;

        private void OnEnable() => _group = GetComponent<CanvasGroup>();

        private void Update()
        {
            if (_group != null)
            {
                _group.alpha = GardenLook.Glow(Time.unscaledTime);
            }
        }
    }

    /// <summary>A link bar built by <see cref="UiKit.LinkBar"/>, stretched over the tray: it draws itself between two pod boxes.</summary>
    public sealed class LinkBarView : MonoBehaviour
    {
        private Image _shadow = null!;
        private Image _rim = null!;
        private Image _bar = null!;
        private Image _shine = null!;
        private readonly Image[] _rivetRims = new Image[2];
        private readonly Image[] _rivets = new Image[2];

        internal void Build()
        {
            _shadow = UiKit.RoundRect("Shadow", transform, UiTheme.Of(C.GardenShadow.WithAlpha(0.25f)));
            _rim = UiKit.RoundRect("Rim", transform, Color.white);
            _bar = UiKit.RoundRect("Bar", transform, UiTheme.Of(C.StateLink));
            _shine = UiKit.RoundRect("Shine", transform, UiTheme.Of(C.StateLink.Lighten(0.35f)));
            for (int i = 0; i < 2; i++)
            {
                _rivetRims[i] = UiKit.RoundRect("RivetRim" + i, transform, Color.white);
                _rivets[i] = UiKit.RoundRect("Rivet" + i, transform, UiTheme.Of(C.StateLink.Darken(0.15f)));
            }
        }

        /// <summary>
        /// Lays the bar between pod <paramref name="a"/> (left) and pod <paramref name="b"/> (right), boxes in the parent's
        /// top-down coordinates (the tray's pods, wider than tall): 42% down the pods, from 10% of a pod's height inside one
        /// frame (on its wooden member) to as far inside the other, 16% of that height thick, in the group's
        /// <paramref name="color"/>.
        /// </summary>
        public void Set(Box a, Box b, Color color)
        {
            float size = Mathf.Min(Mathf.Min(a.Width, b.Width), Mathf.Min(a.Height, b.Height));
            float y = a.Top + (a.Height * 0.42f);
            float x0 = a.Right - (size * 0.1f);
            float x1 = b.Left + (size * 0.1f);
            float bar = size * 0.16f;
            Place(_shadow, x0, x1, y + (bar * 0.25f), bar + (size * 0.05f));
            Place(_rim, x0, x1, y, bar + (size * 0.04f));
            Place(_bar, x0, x1, y, bar);
            Place(_shine, x0, x1, y - (bar * 0.18f), bar * 0.3f);
            _bar.color = color;
            Rgba link = UiTheme.ToRgba(color);
            _shine.color = UiTheme.Of(link.Lighten(0.35f));
            float[] xs = { x0, x1 };
            for (int i = 0; i < 2; i++)
            {
                BoxLayout.Place(_rivetRims[i].rectTransform, Box.FromCenter(xs[i], y, bar * 1.24f, bar * 1.24f));
                BoxLayout.Place(_rivets[i].rectTransform, Box.FromCenter(xs[i], y, bar * 0.92f, bar * 0.92f));
                _rivets[i].color = UiTheme.Of(link.Darken(0.15f));
            }
        }

        /// <summary>A horizontal line with round caps from <paramref name="x0"/> to <paramref name="x1"/>: a pill.</summary>
        private static void Place(Image image, float x0, float x1, float y, float width) =>
            BoxLayout.Place(image.rectTransform, new Box(x0 - (width / 2f), y - (width / 2f), x1 + (width / 2f), y + (width / 2f)));
    }
}
