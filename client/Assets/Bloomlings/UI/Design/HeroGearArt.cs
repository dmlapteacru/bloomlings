using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Shared selection for the owner's modular heroes; the runtime still draws cropped 2D sprites.</summary>
    public static class HeroGearArt
    {
        public const int LayerCount = 3;
        // Owner's simplified-model trial. Legacy fitted parts remain archived until refitted to these bodies.
        public static bool Supports(Family family) => family == Family.Sprig || family == Family.Bloom || family == Family.Drop || family == Family.Twig;
        public static HeroFrame? Layer(Family family, Outfit? outfit, int index) => index == 0 && Supports(family) ? SimpleHeroArt.Frame(family) : (HeroFrame?)null;
        public static bool HasBakedHat(Family family, Outfit? outfit) => false;
        public static bool CanPreview(CosmeticItem item) => !item.MetaOnly;
        // Keep the save intact. Only the previous model-specific 3D attachments are withheld from this trial.
        public static Outfit? DisplayOutfit(Outfit? outfit)
        {
            if (outfit == null) return null;
            bool unfitted = outfit.Skin?.MetaOnly == true || outfit.Hat?.MetaOnly == true
                || outfit.Trail?.MetaOnly == true || outfit.Expression?.MetaOnly == true
                || outfit.Glasses != null || outfit.Backpack != null;
            return unfitted ? outfit.ForGameplay : outfit;
        }
        public static HeroFrame? Icon(string id) => SprigGearArt.Icon(id) ?? TwigGearArt.Icon(id);
        public static Box PoseCell(Box cell, HeroPose pose, Family family) => SprigGearArt.PoseCell(cell, pose, family);
    }
}
