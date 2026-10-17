using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Owner-aligned Bloom with outfit-aware masks and two interchangeable scarves; offline 2D sprites only.</summary>
    public static partial class BloomV2Art
    {
        public const string PetalsId = "hat.bloom_petals";
        public const string BobId = "hat.bloom_bob";
        public const string PeachId = "hat.bloom_peach";
        public const string RoseId = "hat.bloom_rose";
        public const string BucketId = "hat.bloom_panama";
        public const string CapId = "hat.bloom_cap";
        public const string BeretId = "hat.bloom_beret";
        public const string EarmuffsId = "hat.bloom_earmuffs";
        public const string XmasId = "hat.bloom_xmas";
        public const string GlassesId = "glasses.bloom_round";
        public const string KnitId = "scarf.bloom_knit";
        public const string StripeId = "scarf.bloom_stripe";
        public const string JacketId = "clothes.bloom_jacket";
        public const string CoatId = "clothes.bloom_coat";
        public const string OverallsId = "clothes.bloom_overalls";
        public static readonly IReadOnlyList<string> HeadIds = new[] { PetalsId, BobId, PeachId, RoseId, BucketId, CapId, BeretId, EarmuffsId, XmasId };
        public static readonly IReadOnlyList<string> ClothesIds = new[] { JacketId, CoatId, OverallsId };
        public static readonly IReadOnlyList<string> ScarfIds = new[] { KnitId, StripeId };
        public static readonly IReadOnlyList<string> ItemIds = new[] { PetalsId, BobId, PeachId, RoseId, BucketId, CapId, BeretId, EarmuffsId, XmasId, GlassesId, KnitId, StripeId, JacketId, CoatId, OverallsId };
        public static IReadOnlyList<HeroFrame> AllFrames => Frames;
        private static int HeadIndex(string? id) => id switch
        {
            PetalsId => 1, BobId => 2, PeachId => 3, RoseId => 4, BucketId => 5, CapId => 6,
            BeretId => 7, EarmuffsId => 8, XmasId => 9, _ => 0,
        };
        private static int ClothesIndex(string? id) => id switch { JacketId => 1, CoatId => 2, OverallsId => 3, _ => 0 };
        private static int ScarfIndex(string? id) => id switch { KnitId => 1, StripeId => 2, _ => 0 };
        public static bool IsHead(string? id) => HeadIndex(id) != 0;
        public static bool IsClothes(string? id) => ClothesIndex(id) != 0;
        public static bool IsScarf(string? id) => ScarfIndex(id) != 0;
        public static bool IsItem(string? id) => IsHead(id) || IsClothes(id) || IsScarf(id) || id == GlassesId;
        public static HeroFrame? Layer(Outfit? outfit, int index)
        {
            int head = HeadIndex(outfit?.Hat?.Id), clothes = ClothesIndex(outfit?.Clothes?.Id), scarf = ScarfIndex(outfit?.Scarf?.Id);
            if (head == 0) head = 1; // Empty or old Head restores the stock petals, never the bald modeling scaffold.
            return index switch
            {
                0 => Frames[head],
                1 => clothes != 0 ? Frames[10 + head * 3 + clothes - 1] : (HeroFrame?)null,
                2 => scarf != 0 ? Frames[40 + (head * 4 + clothes) * 2 + scarf - 1] : (HeroFrame?)null,
                3 => outfit?.Glasses?.Id == GlassesId ? Frames[120 + (head * 4 + clothes) * 3 + scarf] : (HeroFrame?)null,
                _ => null,
            };
        }
        public static HeroFrame? Icon(string id)
        {
            int head = HeadIndex(id), clothes = ClothesIndex(id), scarf = ScarfIndex(id);
            return head != 0 ? Frames[239 + head] : clothes != 0 ? Frames[251 + clothes]
                : scarf != 0 ? Frames[249 + scarf] : id == GlassesId ? Frames[249] : (HeroFrame?)null;
        }
    }
}
