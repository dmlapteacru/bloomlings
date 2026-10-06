using System;
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
        /// A cream round side button of Home (§6.4) with a colored picture: the round button's domed cushion
        /// (<see cref="IconFace"/>) with the image <paramref name="picture"/> makes in its content, a square
        /// <paramref name="share"/> of the cushion wide, moving with the press. The button is the largest square in its
        /// rect.
        /// </summary>
        public static Button RoundPictureButton(string name, Transform parent, Func<Transform, Image> picture, float share, Action onClick)
        {
            GardenButton view = IconFace(name, parent, GardenLook.White, b => b.Height / 2f, square: true, raycast: true);
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
    }
}
