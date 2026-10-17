using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Owner-aligned V2 parts, rendered as shared base and depth-masked accessory layers.</summary>
    public static partial class TwigV2Art
    {
        public const string AcornId = "hat.twig_acorn";
        public const string BucketId = "hat.twig_panama";
        public const string HairId = "hat.twig_hair";
        public const string HatHairId = "hat.twig_hat_hair";
        public const string BeretId = "hat.twig_beret";
        public const string EarmuffsId = "hat.twig_earmuffs";
        public const string XmasId = "hat.twig_xmas";
        public const string GlassesId = "glasses.twig_round";
        public const string ScarfId = "scarf.twig_knit";
        public static readonly IReadOnlyList<string> HeadIds = new[] { AcornId, BucketId, HairId, HatHairId, BeretId, EarmuffsId, XmasId };
        public static readonly IReadOnlyList<string> ItemIds = new[] { AcornId, BucketId, HairId, HatHairId, BeretId, EarmuffsId, XmasId, GlassesId, ScarfId };
        public static IReadOnlyList<HeroFrame> AllFrames => Frames;
        private static int HeadIndex(string? id) => id switch
        {
            AcornId => 1, BucketId => 2, HairId => 3, HatHairId => 4, BeretId => 5, EarmuffsId => 6, XmasId => 7, _ => 0,
        };
        public static bool IsHead(string? id) => HeadIndex(id) != 0;
        public static bool IsItem(string? id) => IsHead(id) || id == GlassesId || id == ScarfId;
        public static HeroFrame? Layer(Outfit? outfit, int index)
        {
            int head = HeadIndex(outfit?.Hat?.Id);
            return index switch
            {
                0 => Frames[head],
                1 => outfit?.Scarf?.Id == ScarfId ? Frames[8 + head] : (HeroFrame?)null,
                2 => outfit?.Glasses?.Id == GlassesId ? Frames[16 + head] : (HeroFrame?)null,
                _ => null,
            };
        }
        public static HeroFrame? Icon(string id)
        {
            int head = HeadIndex(id);
            return head != 0 ? Frames[23 + head] : id == GlassesId ? Frames[31] : id == ScarfId ? Frames[32] : (HeroFrame?)null;
        }
    }
}
