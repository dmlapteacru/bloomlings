using Bloomlings.Client.Meta.Wardrobe;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Archived FBX pilot states, retained for asset checks; runtime uses the original animated Twig.</summary>
    public static partial class TwigGearArt
    {
        public const string AcornHatId = "hat.twig_acorn";
        public const string FlowerCapId = "hat.twig_flower_cap";
        public static bool IsHat(string? id) => id == AcornHatId || id == FlowerCapId;
        public static HeroFrame Frame(Outfit? outfit) => Frames[outfit?.Hat?.Id == AcornHatId ? 1 : outfit?.Hat?.Id == FlowerCapId ? 2 : 0];
        public static HeroFrame? Icon(string id) => id == AcornHatId ? Frames[3] : id == FlowerCapId ? Frames[4] : (HeroFrame?)null;
    }
}
