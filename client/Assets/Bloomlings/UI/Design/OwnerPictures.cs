namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The names of the pictures the owner makes (spec 005 <c>pictures.md</c> B, C and D): backgrounds, the logo, the
    /// booster icons and the leaf decorations. Each build looks for the picture by its name and draws the code-drawn
    /// stand-in while it is missing (FR-019, FR-027):
    /// <list type="bullet">
    /// <item><description>Unity loads <c>Resources/Backgrounds/{name}</c>, <c>Resources/Brand/{name}</c>,
    /// <c>Resources/Icons/{name}</c> and <c>Resources/Decor/{name}</c> (<c>OwnerArt</c>).</description></item>
    /// <item><description>The playtest embeds the same folders and asks its painter for <c>bg/{name}</c>,
    /// <c>brand/{name}</c>, <c>icon/{name}</c> and <c>decor/{name}</c>.</description></item>
    /// </list>
    /// A leaf picture is drawn for the LEFT end (or the top-left corner); the right end mirrors it left to right, and a
    /// button's bottom-right corner turns it half way. Engine-free.
    /// </summary>
    public static class OwnerPictures
    {
        /// <summary>The Resources folder of the backgrounds (<c>Art/Backgrounds/Resources/Backgrounds/</c>).</summary>
        public const string BackgroundFolder = "Backgrounds";

        /// <summary>The Resources folder of the logo (<c>Art/Brand/Resources/Brand/</c>).</summary>
        public const string BrandFolder = "Brand";

        /// <summary>The Resources folder of the booster icons (<c>Art/Icons/Resources/Icons/</c>).</summary>
        public const string IconFolder = "Icons";

        /// <summary>The Resources folder of the leaf decorations (<c>Art/Decor/Resources/Decor/</c>).</summary>
        public const string DecorFolder = "Decor";

        /// <summary>B1: the Home garden diorama, without the heroes.</summary>
        public const string Home = "home";

        /// <summary>B6: the splash garden.</summary>
        public const string Splash = "splash";

        /// <summary>B7: the Wardrobe's garden arches (Unity only: the playtest has no Wardrobe screen).</summary>
        public const string Wardrobe = "wardrobe";

        /// <summary>B8: the full-screen win's garden, calm in the middle with soft light from it.</summary>
        public const string Win = "win";

        /// <summary>C1: the wooden "Bloomlings" letters with leaves and flowers.</summary>
        public const string Logo = "logo";

        /// <summary>C2: the optional tagline. Neither build shows it yet (its slot <c>brand.tagline</c> is kept for later).</summary>
        public const string Tagline = "tagline";

        /// <summary>D5: the clover/ivy cluster on the left end of a wooden sign (mirrored for the right end).</summary>
        public const string Ivy = "ivy";

        /// <summary>D6: the leaves with white flowers on the top-left of the "Level complete!" sign (mirrored for the other end).</summary>
        public const string Flowers = "flowers";

        /// <summary>D7: the sprig with a white flower on a main button's top-left corner (turned half way for the bottom right).</summary>
        public const string ButtonLeaves = "button-leaves";

        /// <summary>D8: the leaf cluster at the left end of the drawn wordmark (mirrored for the right).</summary>
        public const string LogoLeaves = "logo-leaves";

        /// <summary>The booster ids with an icon picture (D1 to D4).</summary>
        public static readonly string[] Boosters = { "extra_slot", "shuffle", "return", "bloom_burst" };

        /// <summary>The leaf pictures (D5 to D8), in the Decor folder.</summary>
        public static readonly string[] Decor = { Ivy, Flowers, ButtonLeaves, LogoLeaves };

        /// <summary>D1 to D4: a booster's icon (<c>booster-extra_slot</c>, <c>booster-shuffle</c>, …), in the Icons folder.</summary>
        public static string BoosterIcon(string boosterId) => "booster-" + boosterId;

        /// <summary>
        /// The asset slot a picture of <c>pictures.md</c> B, C or D fills (<c>home</c> → <c>bg.home</c>,
        /// <c>gameplay-pond</c> → <c>bg.theme.pond</c>, <c>logo</c> → <c>brand.wordmark</c>, <c>booster-shuffle</c> →
        /// <c>booster.shuffle</c>, <c>ivy</c> → <c>ui.sign.ivy</c>).
        /// </summary>
        public static string SlotOf(string picture) => picture switch
        {
            Home => "bg.home",
            Splash => "bg.splash",
            Wardrobe => "bg.wardrobe",
            Win => "bg.win",
            Logo => "brand.wordmark",
            Tagline => "brand.tagline",
            Ivy => "ui.sign.ivy",
            Flowers => "ui.sign.flowers",
            ButtonLeaves => "ui.deco.garden",
            LogoLeaves => "ui.logo.wood",
            "gameplay-daylight" => "bg.theme.daylight_garden",
            "gameplay-moonlit" => "bg.theme.moonlit_garden",
            _ when picture.StartsWith("gameplay-", System.StringComparison.Ordinal) => "bg.theme." + picture.Substring("gameplay-".Length),
            _ when picture.StartsWith("booster-", System.StringComparison.Ordinal) => "booster." + picture.Substring("booster-".Length),
            _ => throw new System.ArgumentException("Not an owner picture of pictures.md B, C or D: " + picture, nameof(picture)),
        };

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
            BackdropScene.Win => Win,
            _ => Gameplay(themeId),
        };

        /// <summary>
        /// The background picture a scene shows, given which pictures <paramref name="exists"/>: the splash takes the Home
        /// garden (B1) while its own picture (B6) is missing, so it turns into Home without a jump; every other scene its
        /// own picture (<see cref="Background"/>).
        /// </summary>
        public static string Resolve(BackdropScene scene, string themeId, System.Func<string, bool> exists) =>
            scene == BackdropScene.Splash && !exists(Splash) ? Home : Background(scene, themeId);

        /// <summary>
        /// The height share of the middle of the round stone disc's top painted in the owner's win picture (B8; its back
        /// rim at 0.562, its front rim at 0.607): the full-screen win anchors the picture at the top and zooms it so the
        /// disc lies under the hero's feet (<see cref="WinZoom"/>).
        /// </summary>
        public const float WinDiscShare = 0.585f;

        /// <summary>
        /// The zoom over cover-fitting of the owner's win picture (<paramref name="picW"/> × <paramref name="picH"/>) drawn
        /// top-anchored and centered across <paramref name="screen"/>, so its painted disc (<see cref="WinDiscShare"/>) lies on
        /// <paramref name="stageY"/> (where the hero's feet stand): max(1, (stageY − screen top) /
        /// (<see cref="WinDiscShare"/> × picH × cover)).
        /// </summary>
        public static float WinZoom(Box screen, float stageY, int picW = 852, int picH = 1846)
        {
            float cover = System.Math.Max(screen.Width / System.Math.Max(1f, picW), screen.Height / System.Math.Max(1f, picH));
            return System.Math.Max(1f, (stageY - screen.Top) / System.Math.Max(1f, WinDiscShare * picH * cover));
        }
    }
}
