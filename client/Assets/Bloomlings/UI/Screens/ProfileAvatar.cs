using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The player's profile picture (FR-061, FR-063) in the reference look (spec 005 §4.5; the playtest's Home
    /// <c>Avatar</c>): the hero's portrait in its outfit on a domed cream disc like the round buttons, with a soft green
    /// middle and a tan ring, inside the shown frame, with the shown badge at its foot and the leaderboard marker at its
    /// shoulder. The Wardrobe (its profile tab) and the leaderboard row use it; Home showed it as its Wardrobe button until
    /// the bottom menu took that place (spec 005 FR-030). Never a touch target itself (its button is).
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
            BoxLayout layout = BoxLayout.On(root);
            GardenButton disc = UiKit.IconFace("Disc", root, GardenLook.White, b => b.Height / 2f, square: true);
            layout.Add((RectTransform)disc.transform, b => b);
            Image middle = UiKit.RoundGradient("Middle", disc.Content, GardenLook.Green.Top.Mix(C.CreamTop, 0.6f), GardenLook.Green.Face.Mix(C.CreamTop, 0.45f));
            Image ring = UiKit.RoundRing("Ring", disc.Content, UiTheme.Of(C.CreamLine), null, _ => UiKit.Units(3f));
            BoxLayout.On(disc.Content).Add(middle.rectTransform, f => f.Inset(-disc.IconSide * 0.04f)).Add(ring.rectTransform, f => f.Inset(-disc.IconSide * 0.04f));

            // The family's 3D hero (spec 004 FR-017), in its picture's 512:576 shape, 70% of the avatar.
            _figure = BloomlingFigure.Create("Figure", root);
            _figure.Body.raycastTarget = false;
            layout.Add(_figure.Rect, b => Square(b, 0.7f, 0f, 0.02f));
            _frame = Decoration(root, "Frame");
            layout.Add(_frame.rectTransform, b => Square(b, 1.08f, 0f, 0f));
            _badge = Decoration(root, "Badge");
            // The badge at the bottom left: on Home the bottom right carries the Wardrobe's shirt badge (2026-10-04).
            layout.Add(_badge.rectTransform, b => Square(b, 0.36f, -0.36f, 0.36f));
            _marker = Decoration(root, "Marker");
            layout.Add(_marker.rectTransform, b => Square(b, 0.36f, 0.36f, -0.36f));
        }

        /// <summary>The family of the player's hero: the avatar's picture, and the hero at the left front of Home once the Wardrobe is open.</summary>
        public const Family HeroFamily = Family.Bloom;

        public RectTransform Rect { get; }

        public static ProfileAvatar Create(string name, Transform parent) => new ProfileAvatar(UiFactory.CreateRect(name, parent));

        /// <param name="look">The shown frame, badge and marker (each may be none).</param>
        /// <param name="outfit">What the pictured Bloomling (a Bloom) wears.</param>
        public void Show(ProfileLook? look, Outfit? outfit)
        {
            _figure.ShowHero(HeroFamily, outfit);
            Show(_frame, look?.Frame);
            Show(_badge, look?.Badge);
            Show(_marker, look?.Marker);
        }

        /// <summary>A square of <paramref name="share"/> of the avatar's size, its center moved by shares of the size.</summary>
        private static Box Square(Box b, float share, float dx, float dy)
        {
            float s = Mathf.Min(b.Width, b.Height);
            return Box.FromCenter(b.CenterX + (s * dx), b.CenterY + (s * dy), s * share, s * share);
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

        private static Image Decoration(RectTransform root, string name)
        {
            Image image = UiFactory.CreateImage(name, root, null, Color.white);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.gameObject.SetActive(false);
            return image;
        }
    }
}
