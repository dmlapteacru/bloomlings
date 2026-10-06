using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>The five drawn profile frames (spec 005 FR-037 as amended 2026-10-06, contracts/look.md §6.11), free from Level 1.</summary>
    public enum ProfileFrameStyle
    {
        /// <summary>Wooden Ring: a ring of honey wood, its grain running round, with four brass nails.</summary>
        WoodRing,

        /// <summary>Leaf Ring: a green vine round the avatar, its leaves turning one way, out and in by turns.</summary>
        LeafRing,

        /// <summary>Flower Wreath: two twisted twigs with small leaves and eight blossoms, pink and white by turns.</summary>
        FlowerWreath,

        /// <summary>Stone Ring: ten sandy stone blocks on dark mortar, two of them mossy.</summary>
        StoneRing,

        /// <summary>Golden Ribbon: a gold satin band wound round, a gold star on top and a pink bow at the bottom.</summary>
        GoldenRibbon,
    }

    /// <summary>
    /// The profile avatar's geometry in both builds (spec 005 FR-037 as amended 2026-10-06: "the avatar must fill the
    /// whole circle"; the playtest's <c>Kit.Avatar</c>, Unity's <c>ProfileAvatar</c>): in the avatar's square box, a disc
    /// <c>1 - </c><see cref="LipShare"/> of its side at its top, on a cream lip showing under it; the avatar's picture
    /// fills the disc inside a thin <c>cream.line</c> ring (<see cref="RingShare"/>), with no cream gap; a drawn frame
    /// (<see cref="Frame"/>) or the plain tinted ring (<see cref="ShapeFrame"/>) lies over the disc's edge, the badge at
    /// its lower left and the leaderboard marker at its upper right. Engine-free.
    /// </summary>
    public static class AvatarLook
    {
        /// <summary>The lip under the disc, as a share of the avatar's side.</summary>
        public const float LipShare = 0.06f;

        /// <summary>How far a press sinks the disc into its lip, as a share of the lip.</summary>
        public const float TravelShare = 0.7f;

        /// <summary>The thin <c>cream.line</c> ring round the picture, as a share of the disc's side (at least 2 reference units).</summary>
        public const float RingShare = 0.03f;

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

        /// <summary>
        /// The disc: a circle <c>1 - </c><see cref="LipShare"/> of the side, centered across, at the box's top, sunk by a
        /// press of <paramref name="depth"/> (0 to 1; a release overshoots to -0.25) by <see cref="TravelShare"/> of the lip.
        /// </summary>
        public static Box Disc(Box avatar, float depth = 0f)
        {
            float s = Side(avatar);
            float d = s * (1f - LipShare);
            float top = avatar.CenterY - (s / 2f) + (s * LipShare * TravelShare * Math.Max(-0.25f, Math.Min(1f, depth)));
            return new Box(avatar.CenterX - (d / 2f), top, avatar.CenterX + (d / 2f), top + d);
        }

        /// <summary>The lip: the disc's circle at the box's bottom, showing as a crescent under the disc.</summary>
        public static Box Lip(Box avatar)
        {
            float s = Side(avatar);
            float d = s * (1f - LipShare);
            float bottom = avatar.CenterY + (s / 2f);
            return new Box(avatar.CenterX - (d / 2f), bottom - d, avatar.CenterX + (d / 2f), bottom);
        }

        /// <summary>The ring's width for a disc: <see cref="RingShare"/> of it, at least <paramref name="minimum"/> (2 reference units).</summary>
        public static float Ring(Box disc, float minimum) => Math.Max(minimum, disc.Width * RingShare);

        /// <summary>The avatar's picture: the whole disc inside its ring.</summary>
        public static Box Picture(Box disc, float ring) => disc.Inset(ring);

        /// <summary>The family hero standing in while the picture is missing: 0.82 of the picture, a little low.</summary>
        public static Box Hero(Box picture) => Box.FromCenter(picture.CenterX, picture.CenterY + (picture.Height * 0.02f), picture.Width * 0.82f, picture.Height * 0.82f);

        /// <summary>A drawn frame's picture: <see cref="FrameShare"/> of the disc, centered on it.</summary>
        public static Box Frame(Box disc) => Box.FromCenter(disc.CenterX, disc.CenterY, disc.Width * FrameShare, disc.Height * FrameShare);

        /// <summary>The plain tinted ring's box: <see cref="ShapeFrameShare"/> of the avatar's side, centered on the disc.</summary>
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
