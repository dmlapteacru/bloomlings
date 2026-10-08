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
    /// <c>Kit.Avatar</c>), placed by the kit's <see cref="AvatarLook"/>: a rounded square whose one border is the icon
    /// buttons' light wood rim (<see cref="UiKit.IconRim"/>) or, once one is shown, the frame in its place (a drawn frame,
    /// <see cref="ProfileFrames"/>, or the plain tinted band); the chosen avatar's picture fills the rounded square inside
    /// it (spec 005 FR-037, <see cref="OwnerArt.Avatar"/> in a rounded mask inside a thin <c>wood.line</c> ring, with no
    /// gap: the owner, 2026-10-06; it was round, with a cream lip and the frame over the rim; while it is missing, the
    /// avatar's family hero in its outfit on a soft green middle), the shown badge at its foot and the leaderboard marker
    /// at its shoulder. The profile page and its edit card use it too. The Wardrobe (its profile tab) and the leaderboard row use
    /// it, and Home's header shows it at the top right with its frame and badge (the owner's request of 2026-10-04; it was
    /// Home's Wardrobe button until the bottom menu, spec 005 FR-030, took that place). Never a touch target itself (its
    /// button is).
    /// </summary>
    public sealed class ProfileAvatar
    {
        private readonly BoxLayout _layout;
        private readonly RectTransform _rim;
        private readonly Image _frame;
        private readonly Image _framePicture;
        private readonly BloomlingFigure _figure;
        private readonly Image _badge;
        private readonly Image _marker;
        private readonly Image _middle;
        private readonly Image _mask;
        private readonly RawImage _picture;
        private ProfileFrameStyle? _frameStyle;
        private bool _framed;

        private ProfileAvatar(RectTransform root)
        {
            Rect = root;
            _layout = BoxLayout.On(root);

            // The default border: the icon buttons' wooden plate (spec 005 FR-044), hidden while a frame takes its place (the
            // playtest's order).
            _rim = UiFactory.Stretch(UiFactory.CreateRect("Rim", root));
            UiKit.RaisedPlate(BoxLayout.On(_rim), _rim, AvatarLook.Rim);

            // The picture's wood line, the green middle while the picture is missing, and the picture in a rounded mask.
            Image ring = Disc("Ring", root, C.WoodLine);
            _middle = UiKit.RoundGradient("Middle", root, GardenLook.Green.Top.Mix(C.CreamTop, 0.6f), GardenLook.Green.Face.Mix(C.CreamTop, 0.45f), b => AvatarLook.DiscRadiusOf(b, _framed));
            _middle.raycastTarget = false;
            _mask = Disc("PictureMask", root, Rgba.White);
            _mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            _picture = UiFactory.CreateRect("Picture", _mask.transform).gameObject.AddComponent<RawImage>();
            _picture.raycastTarget = false;
            UiFactory.Stretch(_picture.rectTransform);
            _mask.gameObject.SetActive(false);
            _layout
                .Add(ring.rectTransform, b => AvatarLook.Disc(b, _framed))
                .Add(_middle.rectTransform, PictureOf)
                .Add(_mask.rectTransform, PictureOf);

            // The family's 3D hero (spec 004 FR-017) while the avatar's picture is missing.
            _figure = BloomlingFigure.Create("Figure", root);
            _figure.Body.raycastTarget = false;
            _layout.Add(_figure.Rect, b => AvatarLook.Hero(PictureOf(b)));

            // The frame in the rim's place: the plain tinted frame, or a drawn frame's picture.
            _frame = Decoration(root, "Frame");
            _layout.Add(_frame.rectTransform, AvatarLook.ShapeFrame);
            _framePicture = Decoration(root, "FramePicture");
            _layout.Add(_framePicture.rectTransform, AvatarLook.Frame);

            // The badge at the bottom left, where it moved when Home's avatar carried the Wardrobe's shirt badge (2026-10-04).
            _badge = Decoration(root, "Badge");
            _layout.Add(_badge.rectTransform, AvatarLook.Badge);
            _marker = Decoration(root, "Marker");
            _layout.Add(_marker.rectTransform, AvatarLook.Marker);
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

            // A frame is the border: it takes the wood rim's place and the picture grows under its band.
            _framed = look?.Frame != null;
            _rim.gameObject.SetActive(!_framed);
            ShowFrame(look?.Frame);
            Show(_badge, look?.Badge);
            Show(_marker, look?.Marker);
            _layout.Apply();
        }

        /// <summary>A drawn frame's picture (<see cref="UiRaster.ProfileFrame"/>, at the rect's pixel size), else the tinted frame.</summary>
        private void ShowFrame(CosmeticItem? frame)
        {
            ProfileFrameStyle? style = frame == null ? null : ProfileFrames.StyleOf(frame.Shape);
            Show(_frame, style.HasValue ? null : frame);
            _framePicture.gameObject.SetActive(style.HasValue);
            if (style.HasValue && _frameStyle != style)
            {
                ProfileFrameStyle drawn = style.Value;
                string slot = ProfileFrames.Slot(drawn);
                PictureFit.On(_framePicture, (w, h) => ProceduralSprites.Picture(slot, Mathf.Min(w, h), Mathf.Min(w, h), (pw, ph) => ProfileFrames.Render(drawn, Mathf.Min(pw, ph))), square: true);
            }

            _frameStyle = style;
        }

        /// <summary>The ring's width round the picture for the avatar's box (at least 2 reference units).</summary>
        private float RingOf(Box avatar) => AvatarLook.Ring(AvatarLook.Disc(avatar, _framed), UiKit.Units(2f));

        /// <summary>The picture's rounded square: the disc inside its ring.</summary>
        private Box PictureOf(Box avatar) => AvatarLook.Picture(AvatarLook.Disc(avatar, _framed), RingOf(avatar));

        /// <summary>A rounded square of the avatar (the disc's line, the picture's mask): its corners following the rim's (<see cref="AvatarLook.DiscRadiusOf"/>).</summary>
        private Image Disc(string name, RectTransform root, Rgba color)
        {
            Image image = UiKit.RoundRect(name, root, UiTheme.Of(color), b => AvatarLook.DiscRadiusOf(b, _framed));
            image.raycastTarget = false;
            return image;
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
