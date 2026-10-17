using Bloomlings.Client.Meta.Wardrobe;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>One unchanged bald Twig base, with fitted acorn or panama headwear rendered offline.</summary>
    public static partial class TwigModularArt
    {
        public const string AcornHatId = TwigGearArt.AcornHatId;
        public const string PanamaHatId = "hat.twig_panama";
        public static bool IsHat(string? id) => id == AcornHatId || id == PanamaHatId;
        public static HeroFrame Frame(Outfit? outfit) => Frames[outfit?.Hat?.Id == AcornHatId ? 1 : outfit?.Hat?.Id == PanamaHatId ? 2 : 0];
        public static HeroFrame? Icon(string id) => id == AcornHatId ? Frames[3] : id == PanamaHatId ? Frames[4] : (HeroFrame?)null;
    }
}
