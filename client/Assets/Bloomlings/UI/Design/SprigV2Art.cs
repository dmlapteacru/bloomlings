using System.Collections.Generic;
using Bloomlings.Client.Meta.Wardrobe;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Owner-aligned Sprig V2: shared base and outfit-aware depth masks, all offline-rendered 2D PNGs.</summary>
    public static partial class SprigV2Art
    {
        public const string LeafyId = "hat.sprig_leafy";
        public const string RosetteId = "hat.sprig_rosette";
        public const string SwirlId = "hat.sprig_swirl";
        public const string BucketId = "hat.sprig_panama";
        public const string CapId = "hat.sprig_daisy";
        public const string BeretId = "hat.sprig_beret";
        public const string EarmuffsId = "hat.sprig_earmuffs";
        public const string XmasId = "hat.sprig_xmas";
        public const string GlassesId = "glasses.sprig_round";
        public const string ScarfId = "scarf.sprig_knit";
        public const string JacketId = "clothes.sprig_jacket";
        public const string CoatId = "clothes.sprig_coat";
        public const string OverallsId = "clothes.sprig_overalls";
        public static readonly IReadOnlyList<string> HeadIds = new[] { LeafyId, RosetteId, SwirlId, BucketId, CapId, BeretId, EarmuffsId, XmasId };
        public static readonly IReadOnlyList<string> ClothesIds = new[] { JacketId, CoatId, OverallsId };
        public static readonly IReadOnlyList<string> ItemIds = new[] { LeafyId, RosetteId, SwirlId, BucketId, CapId, BeretId, EarmuffsId, XmasId, GlassesId, ScarfId, JacketId, CoatId, OverallsId };
        public static IReadOnlyList<HeroFrame> AllFrames => Frames;
        private static int HeadIndex(string? id) => id switch
        {
            LeafyId => 1, RosetteId => 2, SwirlId => 3, BucketId => 4, CapId => 5,
            BeretId => 6, EarmuffsId => 7, XmasId => 8, _ => 0,
        };
        private static int ClothesIndex(string? id) => id switch { JacketId => 1, CoatId => 2, OverallsId => 3, _ => 0 };
        public static bool IsHead(string? id) => HeadIndex(id) != 0;
        public static bool IsClothes(string? id) => ClothesIndex(id) != 0;
        public static bool IsItem(string? id) => IsHead(id) || IsClothes(id) || id == GlassesId || id == ScarfId;
        public static HeroFrame? Layer(Outfit? outfit, int index)
        {
            int head = HeadIndex(outfit?.Hat?.Id), clothes = ClothesIndex(outfit?.Clothes?.Id);
            if (head == 0) head = 1; // Empty saved Head means the stock leafy hairstyle, never the bald scaffold.
            bool scarf = outfit?.Scarf?.Id == ScarfId;
            return index switch
            {
                0 => Frames[head],
                1 => clothes != 0 ? Frames[9 + head * 3 + clothes - 1] : (HeroFrame?)null,
                2 => scarf ? Frames[36 + head * 4 + clothes] : (HeroFrame?)null,
                3 => outfit?.Glasses?.Id == GlassesId ? Frames[72 + (head * 4 + clothes) * 2 + (scarf ? 1 : 0)] : (HeroFrame?)null,
                _ => null,
            };
        }
        public static HeroFrame? Icon(string id)
        {
            int head = HeadIndex(id), clothes = ClothesIndex(id);
            return head != 0 ? Frames[143 + head] : clothes != 0 ? Frames[153 + clothes]
                : id == GlassesId ? Frames[152] : id == ScarfId ? Frames[153] : (HeroFrame?)null;
        }
    }
}
