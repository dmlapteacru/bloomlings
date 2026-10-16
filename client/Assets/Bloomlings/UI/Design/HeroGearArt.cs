using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Shared selection for the owner's modular heroes; the runtime still draws cropped 2D sprites.</summary>
    public static class HeroGearArt
    {
        public const int LayerCount = 3;
        public static bool Supports(Family family) => family == Family.Sprig || family == Family.Twig;
        public static HeroFrame? Layer(Family family, Outfit? outfit, int index) => family switch
        {
            Family.Sprig => SprigGearArt.Layer(outfit, index),
            Family.Twig => index == 0 ? TwigGearArt.Frame(outfit) : (HeroFrame?)null,
            _ => null,
        };
        public static bool HasBakedHat(Family family, Outfit? outfit) => family switch
        {
            Family.Sprig => SprigGearArt.IsCap(outfit),
            Family.Twig => TwigGearArt.IsHat(outfit?.Hat?.Id),
            _ => false,
        };
        public static HeroFrame? Icon(string id) => SprigGearArt.Icon(id) ?? TwigGearArt.Icon(id);
        public static Box PoseCell(Box cell, HeroPose pose, Family family) => SprigGearArt.PoseCell(cell, pose, family);
    }
}
