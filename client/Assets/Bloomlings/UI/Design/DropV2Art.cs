using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Verified Drop parts as cropped 2D layers; NativeDrop replaces the whole rounded modeling head.</summary>
    public static partial class DropV2Art
    {
        public const string NativeId = "hat.drop_native";
        public const string WaveId = "hat.drop_side_wave";
        public const string RipplesId = "hat.drop_twin_ripples";
        public const string WhirlpoolId = "hat.drop_whirlpool";
        public const string BucketId = "hat.drop_panama";
        public const string CapId = "hat.drop_cap";
        public const string BeretId = "hat.drop_beret";
        public const string EarmuffsId = "hat.drop_earmuffs";
        public const string XmasId = "hat.drop_xmas";
        public const string GlassesId = "glasses.drop_round";
        public const string KnitId = "scarf.drop_knit";
        public const string StripeId = "scarf.drop_stripe";
        public const string JacketId = "clothes.drop_jacket";
        public const string CoatId = "clothes.drop_coat";
        public const string OverallsId = "clothes.drop_overalls";
        public static readonly IReadOnlyList<string> HeadIds = new[] { NativeId, WaveId, RipplesId, WhirlpoolId, BucketId, CapId, BeretId, EarmuffsId, XmasId };
        public static readonly IReadOnlyList<string> ClothesIds = new[] { JacketId, CoatId, OverallsId };
        public static readonly IReadOnlyList<string> ScarfIds = new[] { KnitId, StripeId };
        public static readonly IReadOnlyList<string> ItemIds = new[] { NativeId, WaveId, RipplesId, WhirlpoolId, BucketId, CapId, BeretId, EarmuffsId, XmasId, GlassesId, KnitId, StripeId, JacketId, CoatId, OverallsId };
        public static IReadOnlyList<HeroFrame> AllFrames => Frames;
        private static int HeadIndex(string? id) => id switch
        {
            WaveId => 1, RipplesId => 2, WhirlpoolId => 3, BucketId => 4, CapId => 5,
            BeretId => 6, EarmuffsId => 7, XmasId => 8, _ => 0,
        };
        private static int ClothesIndex(string? id) => id switch { JacketId => 1, CoatId => 2, OverallsId => 3, _ => 0 };
        private static int ScarfIndex(string? id) => id switch { KnitId => 1, StripeId => 2, _ => 0 };
        public static bool IsHead(string? id) => id == NativeId || HeadIndex(id) != 0;
        public static bool IsClothes(string? id) => ClothesIndex(id) != 0;
        public static bool IsScarf(string? id) => ScarfIndex(id) != 0;
        public static bool IsItem(string? id) => IsHead(id) || IsClothes(id) || IsScarf(id) || id == GlassesId;
        public static HeroFrame? Layer(Outfit? outfit, int index)
        {
            // Empty / legacy Head restores the native droplet; the bald modeling head is never a game look.
            int head = HeadIndex(outfit?.Hat?.Id), clothes = ClothesIndex(outfit?.Clothes?.Id), scarf = ScarfIndex(outfit?.Scarf?.Id);
            return index switch
            {
                0 => Frames[head],
                1 => clothes != 0 ? Frames[9 + head * 3 + clothes - 1] : (HeroFrame?)null,
                2 => scarf != 0 ? Frames[36 + (head * 4 + clothes) * 2 + scarf - 1] : (HeroFrame?)null,
                3 => outfit?.Glasses?.Id == GlassesId ? Frames[108 + (head * 4 + clothes) * 3 + scarf] : (HeroFrame?)null,
                _ => null,
            };
        }
        public static HeroFrame? Icon(string id)
        {
            int clothes = ClothesIndex(id), scarf = ScarfIndex(id);
            return IsHead(id) ? Frames[216 + HeadIndex(id)] : clothes != 0 ? Frames[227 + clothes]
                : scarf != 0 ? Frames[225 + scarf] : id == GlassesId ? Frames[225] : (HeroFrame?)null;
        }
    }
}
