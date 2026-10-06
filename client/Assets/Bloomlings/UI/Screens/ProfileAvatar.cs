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
    /// <c>Kit.Avatar</c>), placed by the kit's <see cref="AvatarLook"/>: a soft shadow and the cream lip under a round disc
    /// that the chosen avatar's picture fills (spec 005 FR-037, <see cref="OwnerArt.Avatar"/> in a round mask inside a thin
    /// <c>cream.line</c> ring, with no cream gap: the owner, 2026-10-06; while it is missing, the avatar's family hero in
    /// its outfit on a soft green middle), the shown frame over the disc's edge (a drawn frame,
    /// <see cref="ProfileFrames"/>, or the plain tinted ring), the shown badge at its foot and the leaderboard marker at its
    /// shoulder. The profile page and its edit card use it too. The Wardrobe (its profile tab) and the leaderboard row use
    /// it, and Home's header shows it at the top right with its frame and badge (the owner's request of 2026-10-04; it was
    /// Home's Wardrobe button until the bottom menu, spec 005 FR-030, took that place). Never a touch target itself (its
    /// button is).
    /// </summary>
    public sealed class ProfileAvatar
    {
        private readonly Image _frame;
        private readonly Image _framePicture;
        private readonly BloomlingFigure _figure;
        private readonly Image _badge;
        private readonly Image _marker;
        private readonly Image _middle;
        private readonly Image _mask;
        private readonly RawImage _picture;
        private ProfileFrameStyle? _frameStyle;

        private ProfileAvatar(RectTransform root)
        {
            Rect = root;
            BoxLayout layout = BoxLayout.On(root);
            ColorSet set = GardenLook.White;

            // The shadow, the lip in its outline and the disc's ring (the playtest's order).
            UiKit.SoftShadow(layout, AvatarLook.Lip, b => AvatarLook.Lip(b).Width / 2f, 0.2f, 0.06f);
            Image lipLine = Disc("LipLine", root, set.Line);
            Image lip = Disc("Lip", root, set.Lip);
            Image ring = Disc("Ring", root, set.Line);
            _middle = UiKit.RoundGradient("Middle", root, GardenLook.Green.Top.Mix(C.CreamTop, 0.6f), GardenLook.Green.Face.Mix(C.CreamTop, 0.45f));
            _middle.raycastTarget = false;

            // The avatar's picture in a round mask filling the disc inside its ring.
            _mask = UiFactory.CreateImage("PictureMask", root, ProceduralSprites.Circle, Color.white);
            _mask.raycastTarget = false;
            _mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            _picture = UiFactory.CreateRect("Picture", _mask.transform).gameObject.AddComponent<RawImage>();
            _picture.raycastTarget = false;
            UiFactory.Stretch(_picture.rectTransform);
            _mask.gameObject.SetActive(false);
            layout
                .Add(lipLine.rectTransform, AvatarLook.Lip)
                .Add(lip.rectTransform, b => AvatarLook.Lip(b).Inset(RingOf(b)))
                .Add(ring.rectTransform, b => AvatarLook.Disc(b))
                .Add(_middle.rectTransform, PictureOf)
                .Add(_mask.rectTransform, PictureOf);

            // The family's 3D hero (spec 004 FR-017) while the avatar's picture is missing.
            _figure = BloomlingFigure.Create("Figure", root);
            _figure.Body.raycastTarget = false;
            layout.Add(_figure.Rect, b => AvatarLook.Hero(PictureOf(b)));

            // The frame over the disc's edge: the plain tinted ring, or a drawn frame's picture.
            _frame = Decoration(root, "Frame");
            layout.Add(_frame.rectTransform, b => AvatarLook.ShapeFrame(b, AvatarLook.Disc(b)));
            _framePicture = Decoration(root, "FramePicture");
            layout.Add(_framePicture.rectTransform, b => AvatarLook.Frame(AvatarLook.Disc(b)));

            // The badge at the bottom left, where it moved when Home's avatar carried the Wardrobe's shirt badge (2026-10-04).
            _badge = Decoration(root, "Badge");
            layout.Add(_badge.rectTransform, b => AvatarLook.Badge(b, AvatarLook.Disc(b)));
            _marker = Decoration(root, "Marker");
            layout.Add(_marker.rectTransform, b => AvatarLook.Marker(b, AvatarLook.Disc(b)));
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

            ShowFrame(look?.Frame);
            Show(_badge, look?.Badge);
            Show(_marker, look?.Marker);
        }

        /// <summary>A drawn frame's picture (<see cref="UiRaster.ProfileFrame"/>, at the rect's pixel size), else the tinted ring.</summary>
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
        private static float RingOf(Box avatar) => AvatarLook.Ring(AvatarLook.Disc(avatar), UiKit.Units(2f));

        /// <summary>The picture's circle: the disc inside its ring.</summary>
        private static Box PictureOf(Box avatar) => AvatarLook.Picture(AvatarLook.Disc(avatar), RingOf(avatar));

        private static Image Disc(string name, RectTransform root, Rgba color)
        {
            Image image = UiKit.RoundRect(name, root, UiTheme.Of(color));
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
