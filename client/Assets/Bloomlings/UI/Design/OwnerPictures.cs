namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The names of the pictures the owner makes (spec 005 <c>pictures.md</c> B and C): backgrounds and the logo. Each
    /// build looks for the picture by its name and draws the code-drawn stand-in while it is missing (FR-019):
    /// <list type="bullet">
    /// <item><description>Unity loads <c>Resources/Backgrounds/{name}</c> and <c>Resources/Brand/{name}</c>.</description></item>
    /// <item><description>The playtest embeds the same folders and asks its painter for <c>bg/{name}</c> and
    /// <c>brand/{name}</c>.</description></item>
    /// </list>
    /// Engine-free.
    /// </summary>
    public static class OwnerPictures
    {
        /// <summary>The Resources folder of the backgrounds (<c>Art/Backgrounds/Resources/Backgrounds/</c>).</summary>
        public const string BackgroundFolder = "Backgrounds";

        /// <summary>The Resources folder of the logo (<c>Art/Brand/Resources/Brand/</c>).</summary>
        public const string BrandFolder = "Brand";

        /// <summary>B1: the Home garden diorama, without the heroes.</summary>
        public const string Home = "home";

        /// <summary>B6: the splash garden.</summary>
        public const string Splash = "splash";

        /// <summary>B7: the Wardrobe's garden arches.</summary>
        public const string Wardrobe = "wardrobe";

        /// <summary>C1: the wooden "Bloomlings" letters with leaves and flowers.</summary>
        public const string Logo = "logo";

        /// <summary>C2: the optional tagline.</summary>
        public const string Tagline = "tagline";

        /// <summary>The gameplay background of a theme (B2 to B5): <c>gameplay-daylight</c>, <c>gameplay-pond</c>, …</summary>
        public static string Gameplay(string themeId) => "gameplay-" + (themeId switch
        {
            "daylight_garden" => "daylight",
            "moonlit_garden" => "moonlit",
            _ => themeId,
        });

        /// <summary>The background picture of a backdrop scene; gameplay depends on the level's theme.</summary>
        public static string Background(BackdropScene scene, string themeId) => scene switch
        {
            BackdropScene.Home => Home,
            BackdropScene.Splash => Splash,
            _ => Gameplay(themeId),
        };
    }
}
