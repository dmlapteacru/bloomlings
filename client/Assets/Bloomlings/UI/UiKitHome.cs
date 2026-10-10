using System;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// Home's pieces in the reference layout (spec 005 FR-024, contracts/look.md §6.4): the cream round side buttons that
    /// carry a colored picture instead of a brown glyph (the Store's lotus, the Daily Challenge's sun), and the green check
    /// over a side button's corner (the Daily Challenge done today). Like the rest of the kit, each element lays its parts
    /// out from its own box (<see cref="BoxLayout"/>), and decorations never take taps.
    /// </summary>
    public static partial class UiKit
    {
        /// <summary>
        /// A cream side button of Home (§6.4) with a colored picture: the icon buttons' rounded square in its wood rim
        /// (<see cref="IconFace"/>) with the image <paramref name="picture"/> makes in its content, a square
        /// <paramref name="share"/> of the cushion wide, moving with the press. The button is the largest square in its
        /// rect.
        /// </summary>
        public static Button RoundPictureButton(string name, Transform parent, Func<Transform, Image> picture, float share, Action onClick)
        {
            GardenButton view = IconFace(name, parent, GardenLook.White, GardenLook.IconRimFaceRadius, square: true, raycast: true, rim: true);
            Image image = picture(view.Content);
            image.raycastTarget = false;
            BoxLayout.On(view.Content).Add(image.rectTransform, f =>
            {
                float g = view.IconSide * share;
                return Box.FromCenter(f.CenterX, f.CenterY, g, g);
            });
            return Clickable(view, onClick);
        }

        /// <summary>
        /// The green check badge (the worn outfit's, §4.6) over the top-right corner of a round button (the Daily
        /// Challenge done today, §6.4): a disc 36% of the button, its center inside the corner. Never a touch target; show
        /// or hide the returned object.
        /// </summary>
        public static GameObject CornerCheck(string name, Transform button)
        {
            (RectTransform root, BoxLayout layout) = Element(name, button);
            UiFactory.Stretch(root);
            CheckBadge(layout, b =>
            {
                float side = Mathf.Min(b.Width, b.Height);
                float s = side * 0.36f;
                return Box.FromCenter(b.CenterX + (side * 0.36f), b.CenterY - (side * 0.36f), s, s);
            });
            return root.gameObject;
        }

        /// <summary>
        /// Every rewarded ad's mark (spec 005 FR-051; the playtest's <c>Kit.AdMark</c>): the clapperboard as a blue sticker
        /// (<see cref="Design.GardenLook.AdMarkLayers"/>: its dark outline, the white under its stripes, the striped blue
        /// board), each layer filling the returned rect, never a touch target. Fade a greyed button's with the rect's
        /// <see cref="CanvasGroup"/> (<see cref="Design.GardenLook.AdMarkDisabledAlpha"/>).
        /// </summary>
        public static RectTransform AdMark(string name, Transform parent)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            root.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            foreach ((string shape, Rgba color) in GardenLook.AdMarkLayers)
            {
                Image layer = ShapeImage(shape, root, shape, color);
                layer.raycastTarget = false;
                layout.Add(layer.rectTransform, b => b);
            }

            return root;
        }

        /// <summary>
        /// The "!" notification badge (<c>ui.badge.alert</c>; spec 005 FR-050, the playtest's <c>Kit.AlertBadge</c>): the
        /// check badge's recipe in red, a glossy <c>set.red</c> ball in a white ring (10% of the ball a side) with a white
        /// "!" (<c>ui.alert</c>, 60% of the ball) over its darker line, over a soft shadow. The returned rect is the ball;
        /// place it (<see cref="Design.DailyRewardCard.BadgeDisc"/>) and scale it for the pulse. Never a touch target.
        /// </summary>
        public static RectTransform AlertBadge(string name, Transform parent)
        {
            (RectTransform root, BoxLayout layout) = Element(name, parent);
            ColorSet red = GardenLook.Red;
            Box Outer(Box b) => b.Inset(-b.Width * 0.1f);
            SoftShadow(layout, Outer, b => b.Width / 2f, 0.25f, 0.06f);
            Image ring = RoundRect("AlertRing", root, Color.white);
            ring.raycastTarget = false;
            Image ball = UiFactory.CreateImage("AlertBall", root, null, Color.white);
            ball.raycastTarget = false;
            PictureFit.On(ball, (w, h) => ProceduralSprites.Ball(red, Mathf.Min(w, h)), square: true);
            Image line = ShapeImage("AlertLine", root, "ui.alert", red.Line);
            Image mark = ShapeImage("Alert", root, "ui.alert", Rgba.White);
            line.raycastTarget = false;
            mark.raycastTarget = false;
            Box Mark(Box b) => Box.FromCenter(b.CenterX, b.CenterY, b.Width * 0.6f, b.Width * 0.6f);
            layout.Add(ring.rectTransform, Outer);
            layout.Add(ball.rectTransform, b => b);
            layout.Add(line.rectTransform, b => Mark(b).Offset(0f, b.Width * 0.03f));
            layout.Add(mark.rectTransform, Mark);
            return root;
        }
    }
}
