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
    /// The profile avatar's geometry in both builds (spec 005 FR-037 as amended 2026-10-06; the playtest's
    /// <c>Kit.Avatar</c>, Unity's <c>ProfileAvatar</c>). The owner's requests of that day: the picture fills the avatar
    /// ("the icon in the profile must be stretched over the whole circle"), the avatar is a rounded square, and its border
    /// is one: "the decoration must be the main frame and border: there is a default border, a thick one, and a chosen
    /// frame takes its place" (it was drawn over the default one, and a cream lip under the picture showed as a second
    /// stripe at the bottom). In the avatar's square box of side <c>s</c>:
    /// <list type="bullet">
    /// <item><description>with no frame, the default border is the icon buttons' light wood rim (<see cref="Rim"/>,
    /// <see cref="GardenLook.IconRimShare"/> of the side, its corners <see cref="GardenLook.IconRadiusShare"/>), and the
    /// picture fills the rounded square inside it (<see cref="Disc"/>, 0.8 s);</description></item>
    /// <item><description>with a frame, the frame is the border instead (no rim): the picture fills a rounded square
    /// 0.9 s and the frame's band lies over its edge, where the rim's middle was (<see cref="Frame"/>,
    /// <see cref="ShapeFrame"/>), so a framed avatar takes the same room and shows as much picture.</description></item>
    /// </list>
    /// A thin <c>wood.line</c> ring (<see cref="RingShare"/>) edges the picture, the badge sits at its lower left and the
    /// leaderboard marker at its upper right. Engine-free.
    /// </summary>
    public static class AvatarLook
    {
        /// <summary>The thin <c>wood.line</c> ring round the picture, as a share of the disc's side (at least 2 reference units).</summary>
        public const float RingShare = 0.025f;

        /// <summary>The corner radius of a rounded square that is not an avatar's own disc (the leaderboard's portraits), as a share of its side.</summary>
        public const float DiscRadiusShare = (GardenLook.IconRadiusShare - GardenLook.IconRimShare) / GardenLook.IconRimFaceShare;

        /// <summary>A framed avatar's disc inside its box, as a share of the side on each side: half the rim.</summary>
        public const float FramedInset = GardenLook.IconRimShare / 2f;

        /// <summary>A frame's square picture, as a share of the avatar's side, centered on it.</summary>
        public const float FrameShare = 1.125f;

        /// <summary>
        /// The framed disc's edge in a frame's picture, from its middle, as a share of the picture's side (0.4): the rim's
        /// middle, half a side less half the rim, over <see cref="FrameShare"/>.
        /// </summary>
        public const float FrameEdge = (0.5f - FramedInset) / FrameShare;

        /// <summary>The corner radius of that edge in a frame's picture, as a share of the picture's side: the rim's middle line's.</summary>
        public const float FrameCorner = (GardenLook.IconRadiusShare - FramedInset) / FrameShare;

        /// <summary>The plain tinted frame (<c>cosmetic.frame</c>) as a share of the avatar's side: as a drawn frame.</summary>
        public const float ShapeFrameShare = FrameShare;

        /// <summary>The badge and the marker, as a share of the avatar's side.</summary>
        public const float BadgeShare = 0.36f;

        /// <summary>The avatar's side: the shorter side of its box.</summary>
        public static float Side(Box avatar) => Math.Min(avatar.Width, avatar.Height);

        /// <summary>The square the wood rim fills: the largest centered square of the avatar's box.</summary>
        public static Box Rim(Box avatar) => Box.FromCenter(avatar.CenterX, avatar.CenterY, Side(avatar), Side(avatar));

        /// <summary>The rim's corner radius: <see cref="GardenLook.IconRadiusShare"/> of the side.</summary>
        public static float RimRadius(Box avatar) => Side(avatar) * GardenLook.IconRadiusShare;

        /// <summary>
        /// The disc the picture fills: inside the wood rim (0.8 of the side), or with a frame (<paramref name="framed"/>)
        /// inside half the rim (0.9 of it), where the frame's band covers its edge.
        /// </summary>
        public static Box Disc(Box avatar, bool framed = false) => Rim(avatar).Inset(Side(avatar) * (framed ? FramedInset : GardenLook.IconRimShare));

        /// <summary>The disc's corner radius: the rim's corners less the inset, so the disc follows the rim.</summary>
        public static float DiscRadius(Box avatar, bool framed = false) => Side(avatar) * (GardenLook.IconRadiusShare - (framed ? FramedInset : GardenLook.IconRimShare));

        /// <summary>
        /// The corner radius of a box inside the disc, from its own side (the disc, the picture inside its ring): the
        /// share <see cref="DiscRadius"/> has of the disc, so a host can round a part from its own box.
        /// </summary>
        public static float DiscRadiusOf(Box part, bool framed = false)
        {
            float inset = framed ? FramedInset : GardenLook.IconRimShare;
            return Math.Min(part.Width, part.Height) * (GardenLook.IconRadiusShare - inset) / (1f - (2f * inset));
        }

        /// <summary>A rounded square's corner radius for a portrait that is not an avatar's own disc: <see cref="DiscRadiusShare"/> of its side.</summary>
        public static float Radius(Box disc) => Math.Min(disc.Width, disc.Height) * DiscRadiusShare;

        /// <summary>The ring's width for a disc: <see cref="RingShare"/> of it, at least <paramref name="minimum"/> (2 reference units).</summary>
        public static float Ring(Box disc, float minimum) => Math.Max(minimum, disc.Width * RingShare);

        /// <summary>The avatar's picture: the whole disc inside its ring.</summary>
        public static Box Picture(Box disc, float ring) => disc.Inset(ring);

        /// <summary>The family hero standing in while the picture is missing: 0.82 of the picture, a little low.</summary>
        public static Box Hero(Box picture) => Box.FromCenter(picture.CenterX, picture.CenterY + (picture.Height * 0.02f), picture.Width * 0.82f, picture.Height * 0.82f);

        /// <summary>A drawn frame's picture: <see cref="FrameShare"/> of the avatar's side, centered on it.</summary>
        public static Box Frame(Box avatar) => Box.FromCenter(avatar.CenterX, avatar.CenterY, Side(avatar) * FrameShare, Side(avatar) * FrameShare);

        /// <summary>The plain tinted frame's box: <see cref="ShapeFrameShare"/> of the avatar's side, centered on it.</summary>
        public static Box ShapeFrame(Box avatar) => Box.FromCenter(avatar.CenterX, avatar.CenterY, Side(avatar) * ShapeFrameShare, Side(avatar) * ShapeFrameShare);

        /// <summary>The badge's box: <see cref="BadgeShare"/> of the side, at the avatar's lower left.</summary>
        public static Box Badge(Box avatar) => Square(avatar, Side(avatar) * BadgeShare, -Side(avatar) * BadgeShare, Side(avatar) * BadgeShare);

        /// <summary>The leaderboard marker's box: <see cref="BadgeShare"/> of the side, at the avatar's upper right.</summary>
        public static Box Marker(Box avatar) => Square(avatar, Side(avatar) * BadgeShare, Side(avatar) * BadgeShare, -Side(avatar) * BadgeShare);

        private static Box Square(Box avatar, float side, float dx, float dy) => Box.FromCenter(avatar.CenterX + dx, avatar.CenterY + dy, side, side);
    }

    /// <summary>
    /// The drawn profile frames (spec 005 FR-037 as amended 2026-10-06): the <c>CosmeticCatalog</c> shape of each free
    /// frame names its style, its asset slot (<c>cosmetic.frame.{shape}</c>) and its picture
    /// (<see cref="UiRaster.ProfileFrame"/>), which both builds draw in place of the avatar's wood rim in
    /// <see cref="AvatarLook.Frame"/> (the playtest with <c>IPainter.Picture</c>, Unity with
    /// <c>ProceduralSprites.Picture</c>). Any other frame shape is the plain band its tint colors (<c>cosmetic.frame</c>).
    /// Engine-free.
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
