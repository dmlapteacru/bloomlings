namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The round level screen's sizes and its pods' queue (spec 006 FR-049, FR-064; contracts/presentation.md; the owner's
    /// level screen of 2026-10-28: "a bigger board and a bigger picture; the pods without their extra box, volumetric like
    /// cubes, in exactly 3 rows one after another, no indicator of how many more; as in Colony Flow"). Every size is the
    /// owner's value from the board constructor (<c>tools/board-constructor</c>, its "copy the values" block of 2026-10-28),
    /// one named constant each under the constructor's key, so new values plug in without touching the layout
    /// (<c>ScreenLayout.RoundGameplay</c>).
    /// <list type="bullet">
    /// <item><description>The constructor draws a 1080 × 2340 phone (<see cref="ReferenceWidth"/>, <see cref="ReferenceHeight"/>):
    /// widths and cubes are shares of the screen's width W, heights shares of its height H. The game takes H as the
    /// reference phone's (W × 2340 / 1080), so the owner's proportions hold on every phone.</description></item>
    /// <item><description>Where the safe area is shorter than that stack (a 16:9 phone, or the insets of a notch), the board
    /// box, the rows and the pod cubes all shrink by one factor until they fit (<c>ScreenLayout.RoundFit</c>); a taller
    /// phone keeps the owner's sizes and the room is left at the bottom.</description></item>
    /// <item><description>The board box is one fixed box on every level of a screen, from the fullest layout (the Hard badge
    /// and the booster row).</description></item>
    /// </list>
    /// No dependency beyond the language: the level tester links this file alone for its minimal look.
    /// </summary>
    public static class RoundScreenLook
    {
        /// <summary>The constructor's phone width in pixels (<c>screen</c>).</summary>
        public const float ReferenceWidth = 1080f;

        /// <summary>The constructor's phone height in pixels: shares of H are of W × this / <see cref="ReferenceWidth"/>.</summary>
        public const float ReferenceHeight = 2340f;

        // ---- The board box ----

        /// <summary>The board box's width, as a share of W (constructor <c>boardWidthShare</c>; 0.86 before 2026-10-28).</summary>
        public const float BoardWidthShare = 0.953f;

        /// <summary>The board box's height, as a share of H (constructor <c>boardHeightShare</c>); not capped to a square.</summary>
        public const float BoardHeightShare = 0.44f;

        /// <summary>
        /// Where the board box starts, from the screen's top, as a share of H (constructor <c>boardTopShare</c>); never
        /// higher than <see cref="BarGapShare"/> under the top bar's buttons and the Hard badge, which stand under the
        /// phone's top inset.
        /// </summary>
        public const float BoardTopShare = 0.1f;

        /// <summary>
        /// The top bar's band (Pause, the level's sign, the speed pill), as a share of H (constructor <c>topBarShare</c>): the
        /// constructor's picture of it. The game keeps its buttons' own sizes under the top inset; the board box keeps clear of
        /// them by <see cref="BarGapShare"/>.
        /// </summary>
        public const float TopBarShare = 0.105f;

        /// <summary>The least air between the top bar's buttons (or the Hard badge under them) and the board box, as a share of W.</summary>
        public const float BarGapShare = 0.015f;

        /// <summary>
        /// The stone frame's thickness, as a share of the box's shorter side (constructor <c>frameShare</c>), the same on every
        /// level; the frame's unit is <c>RoundBoardLayout.FrameUnits</c> = <c>BoardLayout.Rim</c> / this.
        /// </summary>
        public const float FrameShare = 0.042f;

        /// <summary>
        /// The walkway ring round the picture, in cubes (constructor <c>padCells</c>): the level's rows carry one open ring
        /// inside their wall, so 1 fits the rows as they are; each cube more leaves one more cell of surface on each side.
        /// </summary>
        public const int PadCells = 1;

        /// <summary>
        /// A board cube's body as a share of its column (constructor <c>boardCubeFill</c>, the owner's 0.98 of 2026-10-28): the
        /// cubes touch, a hairline at most between neighbours.
        /// </summary>
        public const float BoardCubeFill = 0.98f;

        /// <summary>
        /// The board's oblique cubes (the owner's "like Colony Flow, somehow at an angle", 2026-10-28; flat 2D, no vanishing
        /// point, constitution VII): a row's pitch as a share of the column pitch, so each cube's top is wider than tall
        /// (Colony Flow's 30 px rows on 34 px columns).
        /// </summary>
        public const float BoardRowShare = 0.88f;

        /// <summary>
        /// A board cube's front face under its top, as a share of the column pitch: drawn far rows first, the nearer cube hides
        /// it, so it shows only on the picture's bottom edge, at steps of the silhouette and where a cube was eaten.
        /// </summary>
        public const float BoardFrontShare = 0.38f;

        /// <summary>The rounded bevel along a cube top's lower edge, as a share of the column pitch: all that stays of a hidden front.</summary>
        public const float BoardBevelShare = 0.12f;

        /// <summary>
        /// A cube's side face at the board's outermost column, as a share of the cube's width (constitution 1.2.0 VII, the
        /// owner's "with side faces" of 2026-10-28; Colony Flow's 0.10 at 34 px cells): a cube left of the centre shows its
        /// left side, right of it its right side, linear in the distance from the centre and 0 there, and only where the
        /// neighbouring cell on that side is empty. Pods and places alike, by their place across the screen.
        /// </summary>
        public const float SideShare = 0.10f;

        /// <summary>How many widths a side face takes on each side of the centre (the side pictures a colour has).</summary>
        public const int SideSteps = 5;

        /// <summary>A cube's corner radius, as a share of its width (Colony Flow's soft corners).</summary>
        public const float CubeCornerShare = 0.12f;

        /// <summary>
        /// A pod's front face (its thicker base) under its top, as a share of the pod's height (constructor
        /// <c>cubeDepthShare</c>, the owner's 0.26 of 2026-10-28: "the cubes must look more like cubes, taller"); the cubes in
        /// the places alike.
        /// </summary>
        public const float CubeDepthShare = 0.26f;

        /// <summary>A waiting pod's flatter base (rows 2 and 3), as a share of its height.</summary>
        public const float WaitingDepthShare = 0.18f;

        /// <summary>The white outline round a top-row pod (the one that takes the tap), as a share of its width.</summary>
        public const float PodRingShare = 0.05f;

        /// <summary>How opaque a waiting pod's number is (Colony Flow's half-clear numbers under the top row).</summary>
        public const float WaitingCountAlpha = 0.6f;

        /// <summary>
        /// The largest a board cube's side may be, in pixels of a 1080-wide screen (scaled with W; constructor
        /// <c>boardCubeMaxPx</c>); 0 (the owner's null) puts no cap, so a small picture fills the box.
        /// </summary>
        public const float BoardCubeMax = 0f;

        // ---- The tray ----

        /// <summary>The gap between the board box and the places' row, as a share of H (constructor <c>gapAfterBoardShare</c>).</summary>
        public const float GapAfterBoardShare = 0.03f;

        /// <summary>
        /// The places' row, as a share of H (constructor <c>slotsRowShare</c>): one cream bar as wide as the board box holding
        /// the places (five, six with Extra Slot).
        /// </summary>
        public const float SlotsRowShare = 0.085f;

        /// <summary>The booster row, as a share of H (constructor <c>boostersRowShare</c>), right under the places.</summary>
        public const float BoostersRowShare = 0.075f;

        /// <summary>A booster tile's side, as a share of the booster row's height (the constructor's 0.86).</summary>
        public const float BoosterTileShare = 0.86f;

        /// <summary>A pod cube's side, as a share of W (constructor <c>podCubeShare</c>); narrower only where the columns need it.</summary>
        public const float PodCubeShare = 0.15f;

        /// <summary>The gap between two cubes of a column, as a share of W (constructor <c>podGapShare</c>); 1.5 of it above the pods.</summary>
        public const float PodGapShare = 0.02f;

        /// <summary>The gap between two columns, in cubes (the constructor's 0.7 of a cube).</summary>
        public const float ColumnGap = 0.7f;

        /// <summary>The widest the columns may spread, as a share of W: six columns of cubes and their gaps fit it.</summary>
        public const float ColumnsShare = 0.96f;

        /// <summary>
        /// How many pods of a column the tray shows (constructor <c>podRows</c>; the owner, 2026-10-28): the top pod and the
        /// next two, one after another, never under each other, and no "+N" for the rest.
        /// </summary>
        public const int PodRows = 3;

        /// <summary>The space under the pods, above the bottom inset, as a share of W.</summary>
        public const float BottomShare = 0.02f;

        // ---- The pods' look ----

        /// <summary>
        /// How far a waiting pod's colour (rows 2 and 3) is lightened toward white (constructor <c>inactiveLighten</c>): Colony
        /// Flow barely lightens, the waiting cue being the half-clear number and the flatter base; the owner's 0.46 of
        /// 2026-10-28 is the most allowed.
        /// </summary>
        public const float InactiveLighten = 0.12f;

        /// <summary>
        /// The most a waiting pod's colour may be lightened while every waiting colour stays at least the palette's readability
        /// bound (CIEDE2000 10) from every other material's colour, top row or waiting (FR-047's measure): above it, a waiting
        /// water reads as sky and a waiting petal as blush. <see cref="InactiveLighten"/> above this is the owner's choice.
        /// </summary>
        public const float ReadableInactiveLighten = 0.18f;

        /// <summary>The count's type size on a pod cube, as a share of the cube's side.</summary>
        public const float CountShare = 0.42f;

        /// <summary>The count's type size on a pod cube in the symbols mode (the symbol takes the top's middle), as a share of the side.</summary>
        public const float SymbolCountShare = 0.3f;

        /// <summary>The white count's dark outline, in fractions of its type size.</summary>
        public const float CountOutlineEm = 0.11f;
    }
}
