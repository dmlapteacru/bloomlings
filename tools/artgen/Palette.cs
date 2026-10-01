using Bloomlings.Client.UI.Design;

namespace Bloomlings.ArtGen
{
    /// <summary>
    /// The character art's own colors (contracts/character-look.md): the fixed accents an artist would pick (leaf greens,
    /// cap browns, the stump top, the face) and the per-variant drawing colors. Body colors come from the variant catalog.
    /// </summary>
    public static class Palette
    {
        public static readonly Rgba Ink = Rgba.FromHex("#3A2418");
        public static readonly Rgba Blush = Rgba.FromHex("#FF6F8E");
        public static readonly Rgba Muted = Rgba.FromHex("#A7A39A");
        public static readonly Rgba SproutGreen = Rgba.FromHex("#3FA33A");
        public static readonly Rgba Sprout = Rgba.FromHex("#6DBE45");
        public static readonly Rgba SepalGreen = Rgba.FromHex("#5BAA3A");
        public static readonly Rgba WoodTop = Rgba.FromHex("#E9C08A");
        public static readonly Rgba WoodBark = Rgba.FromHex("#C58A52");
        public static readonly Rgba CapBrown = Rgba.FromHex("#5A3418");
        public static readonly Rgba Lichen = Rgba.FromHex("#B9D58A");

        /// <summary>
        /// The drawing color of a variant: the catalog color, a little lighter for dark variants so the face reads (the hue
        /// stays).
        /// </summary>
        public static Rgba BodyColor(string iconId, Rgba color) => iconId switch
        {
            "moss" => color.Lighten(0.22f),
            "bud" => color.Lighten(0.2f),
            "log" => color.Mix(WoodBark, 0.55f),
            "acorn" => color.Lighten(0.06f),
            "berry" => color.Lighten(0.15f),
            "vine" => color.Darken(0.04f),
            _ => color,
        };
    }
}
