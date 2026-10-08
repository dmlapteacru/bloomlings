using System;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The gameplay screen's own pieces in the reference look (spec 005 contracts/look.md §4.1), the twins of the playtest's
    /// <c>LevelScreen.TrayFrame</c>, <c>LevelScreen.TrayBand</c>, <c>SlotPainter.TargetGlow</c>, <c>PodPainter.Link</c> and
    /// <c>PodPainter.LinkBadge</c>: the tray's parchment frame and bands, the golden target glow and the connected pods'
    /// link bar and badge. Each is laid out
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
        /// The link badge of a connected pod (<c>pod.link</c>, spec 005 FR-043; the playtest's <c>PodPainter.LinkBadge</c>):
        /// the <see cref="UiRaster.LinkBadge"/> picture, a white chain on a disc of the group's <paramref name="color"/>,
        /// fitted square to its rect. Never a touch target.
        /// </summary>
        public static Image LinkBadge(string name, Transform parent, Rgba color)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            SetLinkBadge(image, color);
            return image;
        }

        /// <summary>Shows a link badge (<see cref="LinkBadge"/>) in a group's <paramref name="color"/>.</summary>
        public static void SetLinkBadge(Image badge, Rgba color) =>
            PictureFit.On(
                badge,
                (w, h) => ProceduralSprites.Picture("pod.link.badge" + color.Hex, Mathf.Min(w, h), Mathf.Min(w, h), (pw, ph) => UiRaster.LinkBadge(Mathf.Min(pw, ph), color)),
                square: true);

        /// <summary>
        /// A booster tile's body (§3.7, <c>booster.tile</c>; the playtest's <c>Kit.BoosterBezel</c>): since spec 005 FR-047
        /// (the owner, 2026-10-08: "every button with a rim as the new ones") a cream face raised on its wooden plate as the
        /// buttons (<see cref="RaisedButton"/>), the largest square in the rect, its corners <paramref name="radiusShare"/> of
        /// it, sinking when pressed; a disabled tile's face is grey (<see cref="ColorSet.Disabled"/>). Children go into
        /// <see cref="GardenButton.Content"/>.
        /// </summary>
        internal static GardenButton BoosterBezel(string name, Transform parent, float radiusShare, bool raycast) =>
            RaisedButton(name, parent, GardenLook.Cream, radiusShare, raycast: raycast, square: true);

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
