using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>
    /// The player's profile picture (FR-061, FR-063) in the reference look (spec 005 §4.5; the playtest's
    /// <c>Kit.Avatar</c>): the chosen avatar's round picture (spec 005 FR-037, <see cref="OwnerArt.Avatar"/> in a round
    /// mask; while it is missing, the avatar's family hero in its outfit on a soft green middle) on a domed cream disc like
    /// the round buttons, in a tan ring, inside the shown frame, with the shown badge at its foot and the leaderboard marker
    /// at its shoulder. The profile page and its edit card use it too. The Wardrobe (its profile tab) and the leaderboard row use it, and Home's header shows it at the top right
    /// with its frame and badge (the owner's request of 2026-10-04; it was Home's Wardrobe button until the bottom menu,
    /// spec 005 FR-030, took that place). Never a touch target itself (its button is).
    /// </summary>
    public sealed class ProfileAvatar
    {
        private readonly Image _frame;
        private readonly BloomlingFigure _figure;
        private readonly Image _badge;
        private readonly Image _marker;
        private readonly Image _middle;
        private readonly Image _mask;
        private readonly RawImage _picture;

        private ProfileAvatar(RectTransform root)
        {
            Rect = root;
            BoxLayout layout = BoxLayout.On(root);
            GardenButton disc = UiKit.IconFace("Disc", root, GardenLook.White, b => b.Height / 2f, square: true);
            layout.Add((RectTransform)disc.transform, b => b);
            _middle = UiKit.RoundGradient("Middle", disc.Content, GardenLook.Green.Top.Mix(C.CreamTop, 0.6f), GardenLook.Green.Face.Mix(C.CreamTop, 0.45f));

            // The avatar's picture in a round mask over the face (spec 005 FR-037: OwnerPictures.AvatarPicture of the face,
            // as the playtest's Kit.AvatarPicture), in its thin cream ring.
            _mask = UiFactory.CreateImage("PictureMask", disc.Content, ProceduralSprites.Circle, Color.white);
            _mask.raycastTarget = false;
            _mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            _picture = UiFactory.CreateRect("Picture", _mask.transform).gameObject.AddComponent<RawImage>();
            _picture.raycastTarget = false;
            UiFactory.Stretch(_picture.rectTransform);
            _mask.gameObject.SetActive(false);
            Image ring = UiKit.RoundRing("Ring", disc.Content, UiTheme.Of(C.CreamLine), null, _ => UiKit.Units(3f));
            BoxLayout.On(disc.Content)
                .Add(_middle.rectTransform, f => OwnerPictures.AvatarPicture(f))
                .Add(_mask.rectTransform, f => OwnerPictures.AvatarPicture(f))
                .Add(ring.rectTransform, f => OwnerPictures.AvatarPicture(f));

            // The family's 3D hero (spec 004 FR-017), in its picture's 512:576 shape, 70% of the avatar.
            _figure = BloomlingFigure.Create("Figure", root);
            _figure.Body.raycastTarget = false;
            layout.Add(_figure.Rect, b => Square(b, 0.7f, 0f, 0.02f));
            _frame = Decoration(root, "Frame");
            layout.Add(_frame.rectTransform, b => Square(b, 1.08f, 0f, 0f));
            _badge = Decoration(root, "Badge");
            // The badge at the bottom left, where it moved when Home's avatar carried the Wardrobe's shirt badge (2026-10-04).
            layout.Add(_badge.rectTransform, b => Square(b, 0.36f, -0.36f, 0.36f));
            _marker = Decoration(root, "Marker");
            layout.Add(_marker.rectTransform, b => Square(b, 0.36f, 0.36f, -0.36f));
        }

        /// <summary>The family of the player's hero: the avatar's picture, and the hero at the left front of Home once the Wardrobe is open (the kit's <see cref="CharacterArt.ProfileHero"/>).</summary>
        public const Family HeroFamily = CharacterArt.ProfileHero;

        public RectTransform Rect { get; }

        public static ProfileAvatar Create(string name, Transform parent) => new ProfileAvatar(UiFactory.CreateRect(name, parent));

        /// <param name="look">The shown frame, badge and marker (each may be none).</param>
        /// <param name="outfit">What the pictured hero wears while the avatar's picture is missing.</param>
        /// <param name="avatar">The chosen avatar (spec 005 FR-037); none shows the profile hero.</param>
        public void Show(ProfileLook? look, Outfit? outfit, AvatarItem? avatar = null)
        {
            Texture2D? picture = avatar == null ? null : OwnerArt.Avatar(avatar.Picture);
            _picture.texture = picture;
            _mask.gameObject.SetActive(picture != null);
            _middle.gameObject.SetActive(picture == null);
            _figure.Rect.gameObject.SetActive(picture == null);
            if (picture == null)
            {
                _figure.ShowHero(avatar?.Family ?? HeroFamily, outfit);
            }

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
