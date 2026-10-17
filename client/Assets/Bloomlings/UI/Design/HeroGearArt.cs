using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Shared selection for the owner's modular heroes; the runtime still draws cropped 2D sprites.</summary>
    public static class HeroGearArt
    {
        public const int LayerCount = 3;
        // Simplified heroes; Twig has a shared bald base and two refitted headwear options.
        public static bool Supports(Family family) => family == Family.Sprig || family == Family.Bloom || family == Family.Drop || family == Family.Twig;
        public static HeroFrame? Layer(Family family, Outfit? outfit, int index)
        {
            if (index != 0 || !Supports(family)) return null;
            return family == Family.Twig ? TwigModularArt.Frame(outfit) : SimpleHeroArt.Frame(family);
        }
        public static bool HasBakedHat(Family family, Outfit? outfit) => family == Family.Twig && TwigModularArt.IsHat(outfit?.Hat?.Id);
        public static bool CanPreview(CosmeticItem item) => !item.MetaOnly || TwigModularArt.IsHat(item.Id);
        // Keep the save intact. Only the previous model-specific 3D attachments are withheld from this trial.
        public static Outfit? DisplayOutfit(Outfit? outfit)
        {
            if (outfit == null) return null;
            bool unfitted = outfit.Skin?.MetaOnly == true || outfit.Hat?.MetaOnly == true
                || outfit.Trail?.MetaOnly == true || outfit.Expression?.MetaOnly == true
                || outfit.Glasses != null || outfit.Backpack != null;
            if (!unfitted) return outfit;
            Outfit shown = outfit.ForGameplay;
            return TwigModularArt.IsHat(outfit.Hat?.Id) ? shown with { Hat = outfit.Hat } : shown;
        }
        public static HeroFrame? Icon(string id) => TwigModularArt.Icon(id) ?? SprigGearArt.Icon(id) ?? TwigGearArt.Icon(id);
        public static Box PoseCell(Box cell, HeroPose pose, Family family) => SprigGearArt.PoseCell(cell, pose, family);
    }
}
