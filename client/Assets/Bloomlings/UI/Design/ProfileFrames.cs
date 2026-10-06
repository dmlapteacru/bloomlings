using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The five drawn profile frames (spec 005 FR-037 as amended 2026-10-06, contracts/look.md §6.11), free from Level 1:
    /// rounded-square frames round the avatar (they were rings while the avatar was round; their catalog ids keep the
    /// names they had then).
    /// </summary>
    public enum ProfileFrameStyle
    {
        /// <summary>Wooden Frame: a rounded-square band of honey wood, its grain running round, with four brass nails at its corners.</summary>
        WoodRing,

        /// <summary>Leaf Frame: a green vine round the avatar, its leaves turning one way, out and in by turns.</summary>
        LeafRing,

        /// <summary>Flower Wreath: two twisted twigs with small leaves and eight blossoms, pink at the sides and white at the corners.</summary>
        FlowerWreath,

        /// <summary>Stone Frame: twelve sandy stone blocks on dark mortar, one at each corner, two of them mossy.</summary>
        StoneRing,

        /// <summary>Golden Ribbon: a gold satin band wound round, a gold star on top and a pink bow at the bottom.</summary>
        GoldenRibbon,
    }

    /// <summary>
    /// The profile avatar's geometry in both builds (spec 005 FR-037 as amended 2026-10-06: "the avatar must fill the
    /// whole circle", then the owner, later that day: the avatar became a rounded square in the icon buttons' wood rim;
    /// the playtest's <c>Kit.Avatar</c>, Unity's <c>ProfileAvatar</c>): in the avatar's square box, the icon buttons'
    /// light wood rim (<see cref="GardenLook.IconRimShare"/>, its corners <see cref="GardenLook.IconRadiusShare"/> of the
    /// side), and inside it a rounded square <c>1 - </c><see cref="LipShare"/> of the rest at its top (the disc, its
    /// corners <see cref="DiscRadiusShare"/> of its side), on a cream lip showing under it; the avatar's picture fills the
    /// disc inside a thin <c>cream.line</c> ring (<see cref="RingShare"/>), with no cream gap; a drawn frame
    /// (<see cref="Frame"/>) or the plain tinted frame (<see cref="ShapeFrame"/>) lies over the disc's edge and the rim, the
    /// badge at its lower left and the leaderboard marker at its upper right. Engine-free.
    /// </summary>
    public static class AvatarLook
    {
        /// <summary>The lip under the disc, as a share of the side inside the rim.</summary>
        public const float LipShare = 0.06f;

        /// <summary>How far a press sinks the disc into its lip, as a share of the lip.</summary>
        public const float TravelShare = 0.7f;

        /// <summary>The thin <c>cream.line</c> ring round the picture, as a share of the disc's side (at least 2 reference units).</summary>
        public const float RingShare = 0.03f;

        /// <summary>
        /// The disc's corner radius, as a share of its side: the rim's corners less the rim (0.34 - 0.1 of the avatar's
        /// side, over the disc's 0.752 of it), so the disc follows the rim.
        /// </summary>
        public const float DiscRadiusShare = 0.32f;

        /// <summary>A drawn frame's square picture, as a share of the disc's side, centered on the disc.</summary>
        public const float FrameShare = 1.25f;

        /// <summary>The disc's edge in a drawn frame's picture, from its middle, as a share of the picture's side.</summary>
        public const float FrameEdge = 0.5f / FrameShare;

        /// <summary>The plain tinted frame (<c>cosmetic.frame</c>) as a share of the avatar's side, centered on the disc.</summary>
        public const float ShapeFrameShare = 1.08f;

        /// <summary>The badge and the marker, as a share of the avatar's side.</summary>
        public const float BadgeShare = 0.36f;

        /// <summary>The avatar's side: the shorter side of its box.</summary>
        public static float Side(Box avatar) => Math.Min(avatar.Width, avatar.Height);

        /// <summary>The square the wood rim fills: the largest centered square of the avatar's box.</summary>
        public static Box Rim(Box avatar) => Box.FromCenter(avatar.CenterX, avatar.CenterY, Side(avatar), Side(avatar));

        /// <summary>The rim's corner radius: <see cref="GardenLook.IconRadiusShare"/> of the side.</summary>
        public static float RimRadius(Box avatar) => Side(avatar) * GardenLook.IconRadiusShare;

        /// <summary>The square inside the rim, where the disc and its lip lie.</summary>
        public static Box Inner(Box avatar) => GardenLook.IconRimFace(Rim(avatar));

        /// <summary>
        /// The disc: a rounded square <c>1 - </c><see cref="LipShare"/> of the side inside the rim, centered across, at
        /// that square's top, sunk by a press of <paramref name="depth"/> (0 to 1; a release overshoots to -0.25) by
        /// <see cref="TravelShare"/> of the lip.
        /// </summary>
        public static Box Disc(Box avatar, float depth = 0f)
        {
            Box inner = Inner(avatar);
            float s = inner.Width;
            float d = s * (1f - LipShare);
            float top = inner.Top + (s * LipShare * TravelShare * Math.Max(-0.25f, Math.Min(1f, depth)));
            return new Box(inner.CenterX - (d / 2f), top, inner.CenterX + (d / 2f), top + d);
        }

        /// <summary>The lip: the disc's rounded square at the bottom inside the rim, showing as a band under the disc.</summary>
        public static Box Lip(Box avatar)
        {
            Box inner = Inner(avatar);
            float d = inner.Width * (1f - LipShare);
            return new Box(inner.CenterX - (d / 2f), inner.Bottom - d, inner.CenterX + (d / 2f), inner.Bottom);
        }

        /// <summary>A disc's (or the lip's) corner radius: <see cref="DiscRadiusShare"/> of its side.</summary>
        public static float Radius(Box disc) => Math.Min(disc.Width, disc.Height) * DiscRadiusShare;

        /// <summary>The ring's width for a disc: <see cref="RingShare"/> of it, at least <paramref name="minimum"/> (2 reference units).</summary>
        public static float Ring(Box disc, float minimum) => Math.Max(minimum, disc.Width * RingShare);

        /// <summary>The avatar's picture: the whole disc inside its ring.</summary>
        public static Box Picture(Box disc, float ring) => disc.Inset(ring);

        /// <summary>The family hero standing in while the picture is missing: 0.82 of the picture, a little low.</summary>
        public static Box Hero(Box picture) => Box.FromCenter(picture.CenterX, picture.CenterY + (picture.Height * 0.02f), picture.Width * 0.82f, picture.Height * 0.82f);

        /// <summary>A drawn frame's picture: <see cref="FrameShare"/> of the disc, centered on it.</summary>
        public static Box Frame(Box disc) => Box.FromCenter(disc.CenterX, disc.CenterY, disc.Width * FrameShare, disc.Height * FrameShare);

        /// <summary>The plain tinted frame's box: <see cref="ShapeFrameShare"/> of the avatar's side, centered on the disc.</summary>
        public static Box ShapeFrame(Box avatar, Box disc) => Square(disc, Side(avatar) * ShapeFrameShare, 0f, 0f);

        /// <summary>The badge's box: <see cref="BadgeShare"/> of the side, at the disc's lower left.</summary>
        public static Box Badge(Box avatar, Box disc) => Square(disc, Side(avatar) * BadgeShare, -Side(avatar) * BadgeShare, Side(avatar) * BadgeShare);

        /// <summary>The leaderboard marker's box: <see cref="BadgeShare"/> of the side, at the disc's upper right.</summary>
        public static Box Marker(Box avatar, Box disc) => Square(disc, Side(avatar) * BadgeShare, Side(avatar) * BadgeShare, -Side(avatar) * BadgeShare);

        private static Box Square(Box disc, float side, float dx, float dy) => Box.FromCenter(disc.CenterX + dx, disc.CenterY + dy, side, side);
    }

    /// <summary>
    /// The drawn profile frames (spec 005 FR-037 as amended 2026-10-06): the <c>CosmeticCatalog</c> shape of each free
    /// frame names its style, its asset slot (<c>cosmetic.frame.{shape}</c>) and its picture
    /// (<see cref="UiRaster.ProfileFrame"/>), which both builds draw over the avatar's disc in <see cref="AvatarLook.Frame"/>
    /// (the playtest with <c>IPainter.Picture</c>, Unity with <c>ProceduralSprites.Picture</c>). Any other frame shape is
    /// the plain ring its tint colors (<c>cosmetic.frame</c>). Engine-free.
    /// </summary>
    public static class ProfileFrames
    {
        /// <summary>The styles in the edit card's order.</summary>
        public static ProfileFrameStyle[] All { get; } =
        {
            ProfileFrameStyle.WoodRing,
            ProfileFrameStyle.LeafRing,
            ProfileFrameStyle.FlowerWreath,
            ProfileFrameStyle.StoneRing,
            ProfileFrameStyle.GoldenRibbon,
        };

        /// <summary>The drawn style of a catalog frame shape, or null for the plain tinted ring.</summary>
        public static ProfileFrameStyle? StyleOf(string shape) => shape switch
        {
            "wood_ring" => ProfileFrameStyle.WoodRing,
            "leaf_ring" => ProfileFrameStyle.LeafRing,
            "flower_wreath" => ProfileFrameStyle.FlowerWreath,
            "stone_ring" => ProfileFrameStyle.StoneRing,
            "golden_ribbon" => ProfileFrameStyle.GoldenRibbon,
            _ => null,
        };

        /// <summary>The catalog shape of a style.</summary>
        public static string ShapeOf(ProfileFrameStyle style) => style switch
        {
            ProfileFrameStyle.WoodRing => "wood_ring",
            ProfileFrameStyle.LeafRing => "leaf_ring",
            ProfileFrameStyle.FlowerWreath => "flower_wreath",
            ProfileFrameStyle.StoneRing => "stone_ring",
            _ => "golden_ribbon",
        };

        /// <summary>The asset slot of a style (also its picture's cache key): <c>cosmetic.frame.wood_ring</c>, ….</summary>
        public static string Slot(ProfileFrameStyle style) => style switch
        {
            ProfileFrameStyle.WoodRing => "cosmetic.frame.wood_ring",
            ProfileFrameStyle.LeafRing => "cosmetic.frame.leaf_ring",
            ProfileFrameStyle.FlowerWreath => "cosmetic.frame.flower_wreath",
            ProfileFrameStyle.StoneRing => "cosmetic.frame.stone_ring",
            _ => "cosmetic.frame.golden_ribbon",
        };

        /// <summary>A style's square picture of <paramref name="size"/> pixels (straight-alpha RGBA rows from the top).</summary>
        public static byte[] Render(ProfileFrameStyle style, int size) => UiRaster.ProfileFrame(size, style);
    }
}
