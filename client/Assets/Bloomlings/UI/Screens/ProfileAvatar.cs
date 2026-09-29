using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The player's profile picture (FR-061, FR-063): a Bloomling in its outfit inside the shown frame, with the shown
    /// badge at its foot and the leaderboard marker at its shoulder. Home, the Wardrobe and the leaderboard row use it.
    /// </summary>
    public sealed class ProfileAvatar
    {
        private readonly Image _frame;
        private readonly BloomlingFigure _figure;
        private readonly Image _badge;
        private readonly Image _marker;

        private ProfileAvatar(RectTransform root)
        {
            Rect = root;
            Image disc = UiFactory.CreateImage("Disc", root, ProceduralSprites.Circle, UiTheme.Panel);
            disc.raycastTarget = false;
            UiFactory.Place(disc.rectTransform, 0.08f, 0.08f, 0.92f, 0.92f);
            _figure = BloomlingFigure.Create("Figure", root);
            _figure.Body.raycastTarget = false;
            UiFactory.Place(_figure.Rect, 0.22f, 0.16f, 0.78f, 0.72f);
            _frame = UiFactory.CreateImage("Frame", root, ProceduralSprites.Ring, UiTheme.SlotLocked);
            _frame.raycastTarget = false;
            UiFactory.Stretch(_frame.rectTransform);
            _badge = Decoration(root, "Badge", 0.6f, -0.04f, 0.98f, 0.34f);
            _marker = Decoration(root, "Marker", 0.62f, 0.66f, 0.98f, 1.02f);
        }

        public RectTransform Rect { get; }

        public static ProfileAvatar Create(string name, Transform parent) => new ProfileAvatar(UiFactory.CreateRect(name, parent));

        /// <param name="look">The shown frame, badge and marker (each may be none).</param>
        /// <param name="outfit">What the pictured Bloomling (a Bloom) wears.</param>
        public void Show(ProfileLook? look, Outfit? outfit)
        {
            _figure.Show(Family.Bloom, UiTheme.Accent, outfit);
            _frame.color = look?.Frame != null ? BloomlingFigure.Tint(look.Frame) : UiTheme.SlotLocked;
            Show(_badge, look?.Badge);
            Show(_marker, look?.Marker);
        }

        private static void Show(Image image, CosmeticItem? item)
        {
            if (item == null)
            {
                image.gameObject.SetActive(false);
                return;
            }

            image.sprite = ProceduralSprites.Accessory(item.Shape);
            image.color = BloomlingFigure.Tint(item);
            image.gameObject.SetActive(true);
        }

        private static Image Decoration(RectTransform root, string name, float x0, float y0, float x1, float y1)
        {
            Image image = UiFactory.CreateImage(name, root, null, Color.white);
            image.preserveAspect = true;
            image.raycastTarget = false;
            UiFactory.Place(image.rectTransform, x0, y0, x1, y1);
            image.gameObject.SetActive(false);
            return image;
        }
    }
}
