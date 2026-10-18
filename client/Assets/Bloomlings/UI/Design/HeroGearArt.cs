using System;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Shared selection for the owner's modular heroes; the runtime still draws cropped 2D sprites.</summary>
    public static class HeroGearArt
    {
        public const int LayerCount = 4;
        // All four families use fitted modular parts, baked into 2D layers.
        public static bool Supports(Family family) => family == Family.Sprig || family == Family.Bloom || family == Family.Drop || family == Family.Twig;
        public static HeroFrame? Layer(Family family, Outfit? outfit, int index)
        {
            if (!Supports(family)) return null;
            if (WardrobeFinishArt.TryLayer(family,outfit,index,false,out var finished)) return finished;
            if (WardrobeAdditionsArt.TryLayer(family,outfit,index,false,out var addition)) return addition;
            if (family == Family.Bloom && BloomExtrasArt.TryLayer(outfit,index,false,out var extra)) return extra;
            if (index == 3 && EyewearArt.IsItem(outfit?.Glasses?.Id)) return EyewearArt.Layer(family,outfit,false);
            return family == Family.Twig ? TwigV2Art.Layer(outfit, index) : family == Family.Sprig ? SprigV2Art.Layer(outfit, index) : family == Family.Bloom ? BloomV2Art.Layer(outfit, index) : DropV2Art.Layer(outfit, index);
        }
        public static bool HasBakedHat(Family family, Outfit? outfit) => WardrobeFinishArt.HasHead(family,outfit?.Hat?.Id) || WardrobeAdditionsArt.HasHead(family,outfit?.Hat?.Id) || family == Family.Twig && TwigV2Art.IsHead(outfit?.Hat?.Id) || family == Family.Sprig && SprigV2Art.IsHead(outfit?.Hat?.Id) || family == Family.Bloom && (BloomV2Art.IsHead(outfit?.Hat?.Id) || BloomExtrasArt.IsHead(outfit?.Hat?.Id)) || family == Family.Drop && DropV2Art.IsHead(outfit?.Hat?.Id);
        public static bool CanPreview(CosmeticItem item) => !item.Retired && (!item.MetaOnly || WardrobeFinishArt.IsItem(item.Id) || WardrobeAdditionsArt.IsItem(item.Id) || BloomExtrasArt.IsItem(item.Id) || EyewearArt.IsItem(item.Id) || TwigV2Art.IsItem(item.Id) || SprigV2Art.IsItem(item.Id) || BloomV2Art.IsItem(item.Id) || DropV2Art.IsItem(item.Id));
        // Keep the save intact. Only the previous model-specific 3D attachments are withheld from the current models.
        public static Outfit? DisplayOutfit(Outfit? outfit)
        {
            if (outfit == null) return null;
            bool unfitted = outfit.Skin?.MetaOnly == true || outfit.Hat?.MetaOnly == true
                || outfit.Trail?.MetaOnly == true || outfit.Expression?.MetaOnly == true
                || outfit.Glasses != null || outfit.Backpack != null || outfit.Scarf != null || outfit.Clothes != null;
            if (!unfitted) return outfit;
            Outfit shown = outfit.ForGameplay;
            return shown with
            {
                Hat = WardrobeFinishArt.IsHead(outfit.Hat?.Id) || WardrobeAdditionsArt.IsHead(outfit.Hat?.Id) || BloomExtrasArt.IsHead(outfit.Hat?.Id) || TwigV2Art.IsHead(outfit.Hat?.Id) || SprigV2Art.IsHead(outfit.Hat?.Id) || BloomV2Art.IsHead(outfit.Hat?.Id) || DropV2Art.IsHead(outfit.Hat?.Id) ? outfit.Hat : shown.Hat,
                Glasses = EyewearArt.IsItem(outfit.Glasses?.Id) || outfit.Glasses?.Id == TwigV2Art.GlassesId || outfit.Glasses?.Id == SprigV2Art.GlassesId || outfit.Glasses?.Id == BloomV2Art.GlassesId || outfit.Glasses?.Id == DropV2Art.GlassesId ? outfit.Glasses : null,
                Scarf = outfit.Scarf?.Id == TwigV2Art.ScarfId || outfit.Scarf?.Id == SprigV2Art.ScarfId || BloomV2Art.IsScarf(outfit.Scarf?.Id) || DropV2Art.IsScarf(outfit.Scarf?.Id) ? outfit.Scarf : null,
                Clothes = WardrobeFinishArt.IsClothes(outfit.Clothes?.Id) || WardrobeAdditionsArt.IsClothes(outfit.Clothes?.Id) || BloomExtrasArt.IsClothes(outfit.Clothes?.Id) || TwigV2Art.IsClothes(outfit.Clothes?.Id) || SprigV2Art.IsClothes(outfit.Clothes?.Id) || BloomV2Art.IsClothes(outfit.Clothes?.Id) || DropV2Art.IsClothes(outfit.Clothes?.Id) ? outfit.Clothes : null,
            };
        }
        public static HeroFrame? Icon(string id) => WardrobeFinishArt.Icon(CosmeticCatalog.CanonicalId(id)) ?? WardrobeAdditionsArt.Icon(CosmeticCatalog.CanonicalId(id)) ?? BloomExtrasArt.Icon(CosmeticCatalog.CanonicalId(id)) ?? EyewearArt.Icon(CosmeticCatalog.CanonicalId(id)) ?? TwigV2Art.Icon(CosmeticCatalog.CanonicalId(id)) ?? SprigV2Art.Icon(id) ?? BloomV2Art.Icon(id) ?? DropV2Art.Icon(id);
        /// <summary>A restrained common breath until the separate action feature is authored.</summary>
        public static Box PoseCell(Box cell, HeroPose pose, Family family)
        {
            int count = Math.Max(1, HeroMotion.FrameCount(family, pose.Clip));
            float wave = (float)Math.Sin(2 * Math.PI * pose.Index / count);
            float sx = 1 + wave * .003f, sy = 1 + wave * .006f;
            float foot = cell.Top + cell.Height * .9f;
            return new Box(cell.CenterX - cell.Width * sx / 2, foot - (foot - cell.Top) * sy,
                cell.CenterX + cell.Width * sx / 2, foot + (cell.Bottom - foot) * sy);
        }
    }
}
