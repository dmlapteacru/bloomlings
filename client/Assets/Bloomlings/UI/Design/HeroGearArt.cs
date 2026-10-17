using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Shared selection for the owner's modular heroes; the runtime still draws cropped 2D sprites.</summary>
    public static class HeroGearArt
    {
        public const int LayerCount = 3;
        // Twig uses the original Heroes.glb motion set again; replacement heads are an authoring study.
        public static bool Supports(Family family) => family == Family.Sprig;
        public static HeroFrame? Layer(Family family, Outfit? outfit, int index) => family switch
        {
            Family.Sprig => SprigGearArt.Layer(outfit, index),
            _ => null,
        };
        public static bool HasBakedHat(Family family, Outfit? outfit) => family switch
        {
            Family.Sprig => SprigGearArt.IsCap(outfit),
            _ => false,
        };
        public static HeroFrame? Icon(string id) => SprigGearArt.Icon(id);
        public static Box PoseCell(Box cell, HeroPose pose, Family family) => SprigGearArt.PoseCell(cell, pose, family);
    }
}
