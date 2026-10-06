namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The names of the pictures the owner makes (spec 005 <c>pictures.md</c> B, C, D and G): backgrounds, the logo, the
    /// booster icons, the leaf decorations, the bottom menu's icons, the variant icons and the currency lotus. Each build looks for the picture by
    /// its name and draws the code-drawn stand-in while it is missing (FR-019, FR-027):
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

        /// <summary>The Resources folder of the booster icons, the variant icons and the lotus (<c>Art/Icons/Resources/Icons/</c>).</summary>
        public const string IconFolder = "Icons";

        /// <summary>The Resources folder of the leaf decorations (<c>Art/Decor/Resources/Decor/</c>).</summary>
        public const string DecorFolder = "Decor";

        /// <summary>
        /// The Resources folder of the profile's avatar pictures (pictures.md I, spec 005 FR-037:
        /// <c>Art/Avatars/Resources/Avatars/</c>, opaque JPEG named by <c>AvatarCatalog</c>); the playtest asks its painter
        /// for <c>avatar/{name}</c>.
        /// </summary>
        public const string AvatarFolder = "Avatars";

        /// <summary>The asset slot every avatar picture fills (the whole avatar disc inside its ring: <see cref="AvatarLook.Picture"/>).</summary>
        public const string AvatarSlot = "ui.avatar";

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

        /// <summary>The variant icon ids with icon pictures (G9–G24): the eight launch variants (the expansion keeps its drawn symbols).</summary>
        public static readonly string[] Variants = { "leaf", "moss", "flower", "bud", "drop", "dew", "log", "acorn" };

        /// <summary>
        /// G9–G16: a variant's detailed icon (<c>variant-leaf</c>, …), in the Icons folder: the sticker tiles of the pods,
        /// the Waiting Slots, the jam row, the kit sheet and the demos.
        /// </summary>
        public static string VariantIcon(string iconId) => "variant-" + iconId;

        /// <summary>
        /// G17–G24: a variant's simplified icon (<c>field-leaf</c>, …), in the Icons folder: the small board tiles and the
        /// finished picture's flat tiles.
        /// </summary>
        public static string FieldIcon(string iconId) => "field-" + iconId;

        /// <summary>The Petals currency's lotus, in the Icons folder: wherever the lotus shows (the stand-in is <c>GardenLook.Lotus</c>).</summary>
        public const string CurrencyLotus = "currency-lotus";

        /// <summary>
        /// D9–D13: the bottom menu's icon of a place (<c>nav-shop</c>, <c>nav-wardrobe</c>, <c>nav-home</c>,
        /// <c>nav-leaderboard</c>, <c>nav-collection</c>), in the Icons folder (spec 005 FR-030; the stand-in is the place's
        /// glyph, <see cref="BottomNav.Fallback"/>).
        /// </summary>
        public static string NavIcon(NavPlace place) => "nav-" + BottomNav.Key(place);

        /// <summary>
        /// The asset slot a picture of <c>pictures.md</c> B, C, D or G fills (<c>home</c> → <c>bg.home</c>,
        /// <c>gameplay-pond</c> → <c>bg.theme.pond</c>, <c>logo</c> → <c>brand.wordmark</c>, <c>booster-shuffle</c> →
        /// <c>booster.shuffle</c>, <c>ivy</c> → <c>ui.sign.ivy</c>, <c>variant-leaf</c> → <c>tile.icon.leaf</c>,
        /// <c>field-leaf</c> → <c>tile.gem.leaf</c>, <c>currency-lotus</c> → <c>currency.petal</c>, <c>nav-home</c> →
        /// <c>icon.nav.home</c>, <c>promo-noads-sprig</c> → <c>ui.promo.no_ads</c>).
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
            CurrencyLotus => "currency.petal",
            "gameplay-daylight" => "bg.theme.daylight_garden",
            "gameplay-moonlit" => "bg.theme.moonlit_garden",
            _ when picture.StartsWith("gameplay-", System.StringComparison.Ordinal) => "bg.theme." + picture.Substring("gameplay-".Length),
            _ when picture.StartsWith("booster-", System.StringComparison.Ordinal) => "booster." + picture.Substring("booster-".Length),
            _ when picture.StartsWith("variant-", System.StringComparison.Ordinal) => IconSlot(picture.Substring("variant-".Length)),
            _ when picture.StartsWith("field-", System.StringComparison.Ordinal) => GemSlot(picture.Substring("field-".Length)),
            _ when picture.StartsWith("nav-", System.StringComparison.Ordinal) => "icon.nav." + picture.Substring("nav-".Length),
            _ when picture.StartsWith("promo-noads-", System.StringComparison.Ordinal) => HomePromo.Slot(PromoScene.NoAds),
            _ when picture.StartsWith("promo-daily-", System.StringComparison.Ordinal) => HomePromo.Slot(PromoScene.Daily),
            _ => throw new System.ArgumentException("Not an owner picture of pictures.md B, C, D or G: " + picture, nameof(picture)),
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
        /// Where the stone disc of the owner's win picture (B8) lies: the middle of its top, as a share of the picture's
        /// height. The win stands the hero on it instead of a drawn pedestal (<see cref="TopAnchored"/>).
        /// </summary>
        public const float WinStageShare = 0.6f;

        /// <summary>
        /// The box of a background picture of <paramref name="width"/> × <paramref name="height"/> drawn over
        /// <paramref name="screen"/> from its top, centered across: at least cover-sized, and as large as it takes for the
        /// point at <paramref name="share"/> of its height (the win picture's stone disc, <see cref="WinStageShare"/>) to
        /// land on <paramref name="stageY"/> (where the layout's pedestal top is). Its foot may run below the screen.
        /// </summary>
        public static Box TopAnchored(Box screen, int width, int height, float stageY, float share)
        {
            float w = System.Math.Max(1, width);
            float h = System.Math.Max(1, height);
            float cover = System.Math.Max(screen.Width / w, screen.Height / h);
            float scale = System.Math.Max(cover, (stageY - screen.Top) / System.Math.Max(1f, share * h));
            return new Box(screen.CenterX - (w * scale / 2f), screen.Top, screen.CenterX + (w * scale / 2f), screen.Top + (h * scale));
        }

        /// <summary>
        /// The picture a backdrop scene shows (<see cref="Background"/>), except that the splash shows the Home garden
        /// while its own picture (B6) is missing, so the splash turns into Home without a jump. <paramref name="exists"/>
        /// tells whether the host has a picture of that name.
        /// </summary>
        public static string Resolve(BackdropScene scene, string themeId, System.Func<string, bool> exists) =>
            scene == BackdropScene.Splash && !exists(Splash) ? Home : Background(scene, themeId);

        /// <summary>
        /// The height share of the middle of the round stone disc's top painted in the owner's win picture (B8, the calm
        /// garden of 2026-10-04; its back rim at 0.577, its front rim at 0.625; 0.58 on the first picture): the full-screen win anchors the picture at the top and zooms it so the
        /// disc lies under the hero's feet (<see cref="WinZoom"/>).
        /// </summary>
        public const float WinDiscShare = WinStageShare;

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

        // ---- Variant icons on candy tiles (pictures.md G9–G24) ----

        /// <summary>The slot of a variant's detailed icon picture (<see cref="VariantIcon"/>): <c>tile.icon.leaf</c>, …</summary>
        public static string IconSlot(string iconId) => "tile.icon." + iconId;

        /// <summary>The slot of a variant's simplified board icon picture (<see cref="FieldIcon"/>): <c>tile.gem.leaf</c>, …</summary>
        public static string GemSlot(string iconId) => "tile.gem." + iconId;

        /// <summary>
        /// The icon picture a candy tile of <paramref name="iconId"/> draws over its face (spec 005 contracts/look.md §3.1):
        /// the simplified field icon on board and flat tiles (G17–G24), the detailed icon on sticker tiles (G9–G16); null
        /// for the mystery tile, whose "?" stays drawn. Whether the file exists is the host's question: without it the tile
        /// keeps its drawn symbol.
        /// </summary>
        public static string? TileIcon(string iconId, TileStyle style, TileState state) =>
            state == TileState.Mystery || iconId == "mystery" ? null : style == TileStyle.Sticker ? VariantIcon(iconId) : FieldIcon(iconId);

        /// <summary>
        /// The share of a board or flat tile's side that its field icon picture's box takes: the pictures keep about 4% of
        /// margin, so the icon itself spans about 62% of the tile.
        /// </summary>
        public const float FieldIconBox = 0.66f;

        /// <summary>
        /// The share of a sticker tile's side that its detailed icon picture's box takes: the pictures keep about 5% of
        /// margin, so the icon itself spans about 70% of the tile.
        /// </summary>
        public const float StickerIconBox = 0.76f;

        /// <summary>
        /// Where a tile's icon picture goes on the box the tile's picture is drawn in (<paramref name="tile"/>, top-down; a
        /// square, or a pressed tile's shorter box): a square of <see cref="FieldIconBox"/> or <see cref="StickerIconBox"/>
        /// of its width, centered across and on the face's middle (the picture's top part above its lip,
        /// <see cref="UiRaster.TileLipShare"/>).
        /// </summary>
        public static Box TileIconBox(Box tile, TileStyle style)
        {
            float face = tile.Height * (1f - UiRaster.TileLipShare(style));
            float side = tile.Width * (style == TileStyle.Sticker ? StickerIconBox : FieldIconBox);
            return Box.FromCenter(tile.CenterX, tile.Top + (face / 2f), side, side);
        }

        /// <summary>
        /// How opaque a tile's icon picture is over its face: a queued pod's (<see cref="TileState.Dimmed"/>) 0.55, so it
        /// mixes with the face dimmed 45% toward <c>parchment.bottom</c> as the drawn symbol does; a stuck slot's
        /// (<see cref="TileState.Grey"/>) grey copy (<see cref="GreyPixels"/>) 0.8 over the grey face; else 1.
        /// </summary>
        public static float TileIconAlpha(TileState state) => state == TileState.Dimmed ? 0.55f : state == TileState.Grey ? 0.8f : 1f;

        /// <summary>
        /// Turns RGBA bytes (straight or premultiplied alpha: the luma is linear) grey in place, with the luma of
        /// <see cref="Rgba.Grey"/>: the stuck tile's icon picture (the hosts make the grey copy once per picture).
        /// </summary>
        public static void GreyPixels(byte[] rgba)
        {
            for (int i = 0; i + 3 < rgba.Length; i += 4)
            {
                int l = ((299 * rgba[i]) + (587 * rgba[i + 1]) + (114 * rgba[i + 2]) + 500) / 1000;
                byte g = (byte)(l > 255 ? 255 : l);
                rgba[i] = g;
                rgba[i + 1] = g;
                rgba[i + 2] = g;
            }
        }
    }
}
