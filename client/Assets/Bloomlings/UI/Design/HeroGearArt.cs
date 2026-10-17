using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Shared selection for the owner's modular heroes; the runtime still draws cropped 2D sprites.</summary>
    public static class HeroGearArt
    {
        public const int LayerCount = 4;
        // Twig V2 uses the owner's aligned parts; the other three retain their simplified bases.
        public static bool Supports(Family family) => family == Family.Sprig || family == Family.Bloom || family == Family.Drop || family == Family.Twig;
        public static HeroFrame? Layer(Family family, Outfit? outfit, int index)
        {
            if (!Supports(family)) return null;
            return family == Family.Twig ? TwigV2Art.Layer(outfit, index) : index == 0 ? SimpleHeroArt.Frame(family) : (HeroFrame?)null;
        }
        public static bool HasBakedHat(Family family, Outfit? outfit) => family == Family.Twig && TwigV2Art.IsHead(outfit?.Hat?.Id);
        public static bool CanPreview(CosmeticItem item) => !item.MetaOnly || TwigV2Art.IsItem(item.Id);
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
                Hat = TwigV2Art.IsHead(outfit.Hat?.Id) ? outfit.Hat : shown.Hat,
                Glasses = outfit.Glasses?.Id == TwigV2Art.GlassesId ? outfit.Glasses : null,
                Scarf = outfit.Scarf?.Id == TwigV2Art.ScarfId ? outfit.Scarf : null,
                Clothes = TwigV2Art.IsClothes(outfit.Clothes?.Id) ? outfit.Clothes : null,
            };
        }
        public static HeroFrame? Icon(string id) => TwigV2Art.Icon(CosmeticCatalog.CanonicalId(id)) ?? SprigGearArt.Icon(id);
        public static Box PoseCell(Box cell, HeroPose pose, Family family) => SprigGearArt.PoseCell(cell, pose, family);
    }
}
