using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The pieces of the full-screen celebration (spec 005 FR-023, contracts/look.md §6.3: the win and the milestone in
    /// the reference layout): the big wooden sign whose letters fill the plank in one or two lines, the cream reward plate
    /// with the lotus on the pedestal's front, a reward on a cream tile with its amount in a cream pill, and the compact
    /// ×2 offer. Like the rest of the kit, each element lays its parts out from its own box (<see cref="BoxLayout"/>), and
    /// decorations never take taps.
    /// </summary>
    public static partial class UiKit
    {
        /// <summary>The sign letters' height over the plank's, for two lines (the reference's "Level / Complete!").</summary>
        public const float CelebrationSignTwoLines = 0.34f;

        /// <summary>The sign letters' height over the plank's, for one line ("Level 25").</summary>
        public const float CelebrationSignOneLine = 0.46f;

        /// <summary>The flower clusters' size over the sign's height on the full-screen celebration.</summary>
        public const float CelebrationFlowerShare = 1.05f;

        /// <summary>
        /// The celebration's wooden sign (§6.3, <c>ui.sign.wood</c> with <c>ui.sign.flowers</c>): <see cref="WoodSign"/>
        /// filling the rect, its letters in <c>ink.title</c> with the light emboss, and the flower clusters
        /// (<see cref="FlowerCluster"/>, <see cref="CelebrationFlowerShare"/> of the height; the owner's <c>flowers</c>
        /// picture, mirrored for the second) placed as on the reference: on the plank's top corners, the left one a little
        /// lower than the right, so the letters between them stay clear (<see cref="CelebrationLetterRoom"/>). As on the
        /// reference, a title too long for one line at <see cref="CelebrationSignTwoLines"/> of the plank's height breaks
        /// into two balanced lines ("Level / complete!"); a short one ("Level 25") takes one bigger line. The letters lie
        /// between the plank and the flowers. Never a touch target.
        /// </summary>
        public static CelebrationSignView CelebrationSign(string name, Transform parent, string text)
        {
            WoodSignView sign = WoodSign(name, parent, string.Empty, T.LevelHome, SignDecor.None);
            sign.Label.gameObject.SetActive(false);

            // The sign's own label stays hidden; the lines come after the plank and before the flowers.
            TextLook look = GardenLook.SignLetters(C.InkTitle);
            TextMeshProUGUI first = KitLabel("Line1", sign.transform, string.Empty, T.LevelHome, look);
            TextMeshProUGUI second = KitLabel("Line2", sign.transform, string.Empty, T.LevelHome, look);
            TextMeshProUGUI probe = KitLabel("Probe", sign.transform, string.Empty, T.LevelHome, look);
            probe.color = Color.clear;
            var view = sign.gameObject.AddComponent<CelebrationSignView>();
            BoxLayout layout = BoxLayout.On((RectTransform)sign.transform);
            view.Init(sign, first, second, probe, layout);
            view.Text = text;
            layout.Then(view.LayLetters);
            layout.Add(FlowerCluster("FlowersLeft", sign.transform, flipped: false).rectTransform, b =>
            {
                float size = CelebrationFlowerSize(b);
                return Box.FromCenter(b.Left + (size * 0.04f), b.Top + (b.Height * 0.18f), size, size);
            });
            layout.Add(FlowerCluster("FlowersRight", sign.transform, flipped: true).rectTransform, b =>
            {
                float size = CelebrationFlowerSize(b);
                return Box.FromCenter(b.Right - (size * 0.04f), b.Top + (b.Height * 0.1f), size, size);
            });
            return view;
        }

        /// <summary>
        /// The gold medal of "Milestone reached!" (§6.3; the leaderboard's rank medal without a number): two ribbon tails
        /// in <c>#E0A21A</c> under a <c>#FFC83D</c> disc, all outlined in <c>#B7790F</c>, with a small white star and a gloss
        /// on the disc. Square; place it at about 70% of its pill's height. Never a touch target.
        /// </summary>
        public static Image GoldMedal(string name, Transform parent)
        {
            Rgba line = Rgba.FromHex("#B7790F");
            Rgba ribbon = Rgba.FromHex("#E0A21A");
            Rgba gold = Rgba.FromHex("#FFC83D");
            Func<float, float, float> star = ShapeLibrary.Get("ui.star");
            float Disc(float x, float y) => Length(x, y + 0.22f) - 0.56f;
            float Ribbons(float x, float y) => Mathf.Min(Segment(x, y, -0.34f, 0.86f, -0.06f, 0.28f), Segment(x, y, 0.34f, 0.86f, 0.06f, 0.28f)) - 0.15f;
            var layers = new List<(Func<float, float, float> Sdf, Rgba Color)>
            {
                ((x, y) => Ribbons(x, y) - 0.08f, line),
                (Ribbons, ribbon),
                ((x, y) => Disc(x, y) - 0.08f, line),
                (Disc, gold),
                ((x, y) => star(x / 0.3f, (y + 0.22f) / 0.3f) * 0.3f, Rgba.White.WithAlpha(0.92f)),
                ((x, y) => Length(x + 0.24f, y + 0.5f) - 0.09f, Rgba.White.WithAlpha(0.55f)),
            };
            Image image = UiFactory.CreateImage(name, parent, ProceduralSprites.Baked("ui.medal/gold_rosette", 128, layers), Color.white);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static float Length(float x, float y) => Mathf.Sqrt((x * x) + (y * y));

        private static float Segment(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float t = Mathf.Clamp01((((px - ax) * dx) + ((py - ay) * dy)) / Mathf.Max(1e-6f, (dx * dx) + (dy * dy)));
            return Length(px - (ax + (t * dx)), py - (ay + (t * dy)));
        }

        /// <summary>
        /// The side of a celebration sign's flower cluster for the plank's box <paramref name="b"/>:
        /// <see cref="CelebrationFlowerShare"/> of its height, at most 40% of its width (a tall sign on a 21:9 phone).
        /// </summary>
        public static float CelebrationFlowerSize(Box b) => Mathf.Min(b.Height * CelebrationFlowerShare, b.Width * 0.4f);

        /// <summary>The width the celebration sign's letters may take: 82% of the plank, between the two flower clusters.</summary>
        public static float CelebrationLetterRoom(Box b)
        {
            return Mathf.Min(b.Width * 0.82f, b.Width - (0.9f * CelebrationFlowerSize(b)));
        }

        /// <summary>
        /// The reward plate on the win's pedestal (§6.3, <c>ui.pill.reward</c>; the playtest's <c>EndCards.RewardPill</c>):
        /// since spec 005 FR-047 a cream pill raised like the rows (<see cref="Slab"/>) holding the lotus (86% of its top's
        /// height) and the brown "+N" in <c>type.reward</c>, centered as a group, as the reference's "+50". Without the lotus
        /// the text stands alone. Never a touch target.
        /// </summary>
        public static RewardPlateView RewardPlate(string name, Transform parent)
        {
            Image root = Slab(name, parent, 0.5f);
            Image lotus = PetalIcon("Lotus", root.transform);
            TextMeshProUGUI label = KitLabel("Amount", root.transform, string.Empty, T.Reward, TextLook.Plain(C.InkBrown));
            var view = root.gameObject.AddComponent<RewardPlateView>();
            BoxLayout content = BoxLayout.On(root.rectTransform).Watch(label);
            view.Init(root, lotus, label, content);
            content.Then(b =>
            {
                // On the slab's top, the group as the playtest's Kit.CostPill lays it.
                var f = new Box(b.Left, b.Top, b.Right, b.Bottom - (b.Height * RaisedRowSide));
                float h = f.Height;
                bool hasLotus = lotus.gameObject.activeSelf;
                float icon = hasLotus ? h * 0.86f : 0f;
                float gap = hasLotus ? h * 0.16f : 0f;
                float size = h * 0.56f;
                float measured = KitText.Measure(label, size);
                float room = Mathf.Max(1f, f.Width - icon - gap - (h * 0.5f));
                float textWidth = Mathf.Min(measured > 0f ? measured : room, room);
                float start = f.CenterX - ((icon + gap + textWidth) / 2f);
                BoxLayout.Place(lotus.rectTransform, Box.FromCenter(start + (icon / 2f), f.CenterY, icon, icon));
                KitText.Place(label, T.Reward, start + icon + gap + (textWidth / 2f), f.CenterY, size, textWidth + 1f);
            });
            return view;
        }

        /// <summary>
        /// A reward on a cream tile (the milestone's rewards, the win's dropped booster; §4.4): <paramref name="icon"/> on
        /// the cream squircle face (radius 26%) raised on its wooden plate (spec 005 FR-047) filling the largest square at
        /// the rect's top, and <paramref name="amount"/>
        /// in a cream pill (36% of the tile tall) over the tile's bottom edge. The rect should be about 1.25 times as tall as
        /// wide. Never a touch target.
        /// </summary>
        public static RectTransform RewardTile(string name, Transform parent, Image icon, string amount)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            GardenButton face = RaisedButton("Tile", root, GardenLook.White, 0.26f, raycast: false, square: true);
            icon.transform.SetParent(face.Content, false);
            icon.raycastTarget = false;
            UiFactory.Place(icon.rectTransform, 0.02f, 0.02f, 0.98f, 0.98f);
            CostPillView pill = TextPill("Amount", root, CostKind.Charges, amount);
            TextMeshProUGUI label = PillText(pill);
            float tile = 0f;
            layout.Add((RectTransform)face.transform, b =>
            {
                tile = Mathf.Min(b.Width, b.Height / 1.25f);
                return Box.FromCenter(b.CenterX, b.Top + (tile / 2f), tile, tile);
            });
            layout.Watch(label).Add((RectTransform)pill.transform, b =>
            {
                float height = tile * 0.36f;
                float measured = KitText.Measure(label, height * 0.56f);
                float width = Mathf.Min(b.Width, Mathf.Max(tile * 0.9f, measured + (height * 1.1f)));
                return Box.FromCenter(b.CenterX, b.Top + tile + (height * 0.2f), width, height);
            });
            return root;
        }

        /// <summary>
        /// A dropped booster beside the win's reward plate (§6.3, the booster bar's language, §3.7): <paramref name="icon"/>
        /// on the cream squircle face (radius 26%) raised on its wooden plate (spec 005 FR-047) filling 90% of the largest
        /// square in the rect, and
        /// <paramref name="amount"/> ("+1") on the green count badge (42% of the tile) over its bottom-right corner. Never a
        /// touch target.
        /// </summary>
        public static RectTransform DropTile(string name, Transform parent, Image icon, string amount)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            GardenButton face = RaisedButton("Tile", root, GardenLook.White, 0.26f, raycast: false, square: true);
            icon.transform.SetParent(face.Content, false);
            icon.raycastTarget = false;
            UiFactory.Place(icon.rectTransform, 0.02f, 0.02f, 0.98f, 0.98f);
            TextMeshProUGUI count = CountBadge("Badge", root, out Image disc);
            count.text = amount;
            Box tile = default;
            layout.Add((RectTransform)face.transform, b =>
            {
                float side = Mathf.Min(b.Width, b.Height) * 0.9f;
                tile = Box.FromCenter(b.CenterX - (side * 0.04f), b.CenterY - (side * 0.04f), side, side);
                return tile;
            });
            layout.Add(disc.rectTransform, b =>
            {
                float size = tile.Width * 0.42f;
                return Box.FromCenter(tile.Right - (size * 0.25f), tile.Bottom - (size * 0.25f), size * 2f, size * 1.26f);
            });
            return root;
        }

        /// <summary>
        /// The ×2 reward offer beside the win's reward plate (§6.3, a rewarded ad, spec 001 FR-052): a cream squircle
        /// button (radius 30%) raised on its wooden plate (spec 005 FR-047) with the clapperboard's blue sticker (<see cref="AdMark"/>, 60% of its content's height) in its upper half and "×2"
        /// below it in <c>type.count</c> <c>ink.brown</c>. Not interactable, it fades to 55%.
        /// </summary>
        public static Button DoubleOffer(string name, Transform parent, Action onClick)
        {
            GardenButton view = RaisedButton(name, parent, GardenLook.White, 0.3f);
            view.GreyWhenDisabled = false;
            view.FadeWhenDisabled = true;
            RectTransform glyph = AdMark("Ad", view.Content);
            TextMeshProUGUI label = KitLabel("Label", view.Content, Loc.F("common.charges", 2), T.Count, TextLook.Plain(C.InkBrown));
            BoxLayout.On(view.Content).Then(f =>
            {
                float h = f.Height;
                BoxLayout.Place(glyph, Box.FromCenter(f.CenterX, f.Top + (h * 0.3f), h * 0.6f, h * 0.6f));
                KitText.Place(label, T.Count, f.CenterX, f.Top + (h * 0.78f), h * 0.46f, f.Width * 0.92f);
            });
            return Clickable(view, onClick);
        }
    }

    /// <summary>A celebration sign built by <see cref="UiKit.CelebrationSign"/>.</summary>
    public sealed class CelebrationSignView : MonoBehaviour
    {
        private WoodSignView _sign = null!;
        private TextMeshProUGUI _first = null!;
        private TextMeshProUGUI _second = null!;
        private TextMeshProUGUI _probe = null!;
        private BoxLayout _layout = null!;
        private string _text = string.Empty;

        /// <summary>The wooden sign under the letters (its own label stays hidden).</summary>
        public WoodSignView Sign => _sign;

        /// <summary>The sign's title; the letters lay out again when it changes.</summary>
        public string Text
        {
            get => _text;
            set
            {
                _text = value ?? string.Empty;
                if (_layout != null)
                {
                    _layout.Apply();
                }
            }
        }

        /// <summary>The lines shown, top to bottom (one or two).</summary>
        public IReadOnlyList<string> Lines => _second.gameObject.activeSelf ? new[] { _first.text, _second.text } : new[] { _first.text };

        internal void Init(WoodSignView sign, TextMeshProUGUI first, TextMeshProUGUI second, TextMeshProUGUI probe, BoxLayout layout)
        {
            _sign = sign;
            _first = first;
            _second = second;
            _probe = probe;
            _layout = layout;
        }

        /// <summary>Lays the letters out over the plank's box <paramref name="b"/>: two balanced lines, or one bigger line.</summary>
        internal void LayLetters(Box b)
        {
            if (b.Width <= 0f || b.Height <= 0f)
            {
                return;
            }

            float room = UiKit.CelebrationLetterRoom(b);
            float two = b.Height * UiKit.CelebrationSignTwoLines;
            List<string> lines = UiKit.BalancedLines(_probe, _text, two, room);
            if (lines.Count < 2)
            {
                _second.gameObject.SetActive(false);
                _first.text = _text;
                KitText.Place(_first, T.LevelHome, b.CenterX, b.CenterY - (b.Height * 0.04f), b.Height * UiKit.CelebrationSignOneLine, room);
                return;
            }

            // Both lines at one size, shrunk together when the longer one would not fit.
            _first.text = lines[0];
            _second.text = lines[1];
            _second.gameObject.SetActive(true);
            float widest = Mathf.Max(KitText.Measure(_first, two), KitText.Measure(_second, two));
            float size = widest > room ? two * room / widest : two;
            float cy = b.CenterY - (b.Height * 0.02f);
            KitText.Place(_first, T.LevelHome, b.CenterX, cy - (size * 0.47f), size, room);
            KitText.Place(_second, T.LevelHome, b.CenterX, cy + (size * 0.47f), size, room);
        }
    }

    /// <summary>A reward plate built by <see cref="UiKit.RewardPlate"/>.</summary>
    public sealed class RewardPlateView : MonoBehaviour
    {
        private BoxLayout _content = null!;

        /// <summary>The raised slab (it never takes taps).</summary>
        public Image Body { get; private set; } = null!;

        /// <summary>The lotus before the amount.</summary>
        public Image Lotus { get; private set; } = null!;

        /// <summary>The amount ("+50"); the plate re-centers it when it changes.</summary>
        public TextMeshProUGUI Label { get; private set; } = null!;

        internal void Init(Image body, Image lotus, TextMeshProUGUI label, BoxLayout content)
        {
            Body = body;
            Lotus = lotus;
            Label = label;
            _content = content;
        }

        /// <summary>Shows the lotus before the amount (Petals) or the text alone.</summary>
        public void ShowLotus(bool show)
        {
            Lotus.gameObject.SetActive(show);
            _content.Apply();
        }
    }
}
