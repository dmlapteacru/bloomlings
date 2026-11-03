namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The round board's cube look as one set of numbers (spec 006 FR-064, <c>contracts/presentation.md</c> "Oblique cubes"):
    /// the cube's proportions on the grid and the shading of its oblique picture (<see cref="UiRaster.BoardCube(int, int, Rgba, string?, BoardCubeLook)"/>),
    /// read by <see cref="RoundBoardLayout"/> (row pitch, picture box, gap) and by the hosts' cube pictures. All shares
    /// are of the column pitch (the cell's width). <see cref="Default"/> is the game's look, built from the
    /// <see cref="RoundScreenLook"/> constants.
    /// <para>
    /// The owner's look-dev of 2026-11-03 ("in the middle of a big picture the cubes look flat, a drawn grid"): the variants
    /// of the review sheet (<c>product/previews/board-cubes-2026-11-03/</c>, the preview's <c>--board-cubes</c>) are values of
    /// this struct (<see cref="Variants"/>); the owner chose "E", whose numbers are the <see cref="RoundScreenLook"/> board
    /// constants and so <see cref="Default"/>. The pods keep the look before (<see cref="Previous"/>'s shading).
    /// </para>
    /// <para>
    /// Geometry, flat 2D from one fixed tilted viewpoint (constitution VII): each cube's picture is a column wide and
    /// <see cref="TopShare"/> + <see cref="FrontShare"/> + <see cref="ShadowShare"/> tall, starting at its cell's top; rows
    /// stand <see cref="RowShare"/> apart and are drawn far to near, so the next row's top hides the part of a front below
    /// <see cref="RowShare"/>. With <see cref="TopShare"/> = <see cref="RowShare"/> (the look before) a front shows only where
    /// the cell in front is empty; with a shorter top (the game's look) every cube shows a band <see cref="FrontPeek"/> of its front. The hit
    /// box stays the cell (<see cref="RoundBoardLayout.CellBox(int)"/>, a row's pitch: the top and its visible band).
    /// </para>
    /// </summary>
    public readonly struct BoardCubeLook
    {
        public BoardCubeLook(string key, float fill, float rowShare, float topShare, float frontShare, float bevelShare, float gloss, float cornerShare,
            float rim = 1f, float topLift = 0.1f, float bevelDark = 0.6f, float frontLight = 0.12f, float frontDeep = 0.32f, float seam = 0.45f,
            float glossSize = 0.085f, float shadowShare = 0f, float shadowAlpha = 0f)
        {
            Key = key;
            Fill = fill;
            RowShare = rowShare;
            TopShare = topShare;
            FrontShare = frontShare;
            BevelShare = bevelShare;
            Gloss = gloss;
            CornerShare = cornerShare;
            Rim = rim;
            TopLift = topLift;
            BevelDark = bevelDark;
            FrontLight = frontLight;
            FrontDeep = frontDeep;
            Seam = seam;
            GlossSize = glossSize;
            ShadowShare = shadowShare;
            ShadowAlpha = shadowAlpha;
        }

        /// <summary>The cache key's suffix of the cube pictures in this look: empty for <see cref="Default"/>, so its keys stay the game's.</summary>
        public string Key { get; }

        /// <summary>A cube's body as a share of its column (<see cref="RoundScreenLook.BoardCubeFill"/>): the rest is the gap at its sides and top.</summary>
        public float Fill { get; }

        /// <summary>A row's pitch (<see cref="RoundScreenLook.BoardRowShare"/>).</summary>
        public float RowShare { get; }

        /// <summary>A cube's top face, from its cell's top to the front's upper edge (the game's look: <see cref="RowShare"/>).</summary>
        public float TopShare { get; }

        /// <summary>A cube's front face under its top (<see cref="RoundScreenLook.BoardFrontShare"/>).</summary>
        public float FrontShare { get; }

        /// <summary>The rounded bevel along the top's lower edge (<see cref="RoundScreenLook.BoardBevelShare"/>).</summary>
        public float BevelShare { get; }

        /// <summary>The soft light spot's strongest alpha (<see cref="RoundScreenLook.CubeGloss"/>).</summary>
        public float Gloss { get; }

        /// <summary>The corner radius as a share of the width (<see cref="RoundScreenLook.CubeCornerShare"/>).</summary>
        public float CornerShare { get; }

        /// <summary>How strong the light rim along the top's upper and left edges is (1: the game's).</summary>
        public float Rim { get; }

        /// <summary>How much lighter than the hue the top face is (0.1: the game's).</summary>
        public float TopLift { get; }

        /// <summary>How dark the bevel turning down gets (0.6: the game's).</summary>
        public float BevelDark { get; }

        /// <summary>The front's darkening at its upper edge (0.12: the game's).</summary>
        public float FrontLight { get; }

        /// <summary>The front's darkening at its foot (0.32: the game's).</summary>
        public float FrontDeep { get; }

        /// <summary>The dark seam round the top (0.45: the game's).</summary>
        public float Seam { get; }

        /// <summary>The light spot's radius as a share of the width (0.085: the game's).</summary>
        public float GlossSize { get; }

        /// <summary>A soft shadow under the cube onto the board's surface, how far below the foot (0: none).</summary>
        public float ShadowShare { get; }

        /// <summary>The shadow's strongest alpha (0: none).</summary>
        public float ShadowAlpha { get; }

        /// <summary>How far a cube's picture reaches below its cell (its front, beyond the row, and its shadow).</summary>
        public float Overhang => (TopShare - RowShare) + FrontShare + ShadowShare;

        /// <summary>A cube picture's height as a share of its width.</summary>
        public float PictureShare => TopShare + FrontShare + ShadowShare;

        /// <summary>How much of each front shows under a filled cell in front of it (0 in the game's look).</summary>
        public float FrontPeek => System.Math.Max(0f, System.Math.Min(FrontShare, RowShare - TopShare));

        /// <summary>The top face's share of the cube's body (top and front, no shadow): where a side face turns from light to dark.</summary>
        public float TopOfBody => TopShare / (TopShare + FrontShare);

        /// <summary>The game's look (the <see cref="RoundScreenLook"/> constants): the owner's look "E" of 2026-11-03.</summary>
        public static readonly BoardCubeLook Default = new BoardCubeLook(string.Empty, RoundScreenLook.BoardCubeFill, RoundScreenLook.BoardRowShare,
            RoundScreenLook.BoardCubeTopShare, RoundScreenLook.BoardFrontShare, RoundScreenLook.BoardBevelShare, RoundScreenLook.CubeGloss, RoundScreenLook.CubeCornerShare,
            rim: RoundScreenLook.BoardRim, topLift: RoundScreenLook.BoardTopLift, bevelDark: RoundScreenLook.BoardBevelDark, frontLight: RoundScreenLook.BoardFrontLight,
            frontDeep: RoundScreenLook.BoardFrontDeep, seam: RoundScreenLook.BoardSeam, glossSize: RoundScreenLook.BoardGlossSize,
            shadowShare: RoundScreenLook.BoardShadowShare, shadowAlpha: RoundScreenLook.BoardShadowAlpha);

        /// <summary>
        /// The board's look before 2026-11-03 (variant A of the review sheet), and the shading the pods, the places and the square
        /// cube tiles keep: the cubes touching, the top a whole row tall, a front only where the cell in front is empty.
        /// </summary>
        public static readonly BoardCubeLook Previous = new BoardCubeLook("lookA", 0.98f, 0.88f, 0.88f, 0.38f, 0.12f, RoundScreenLook.TileGloss, RoundScreenLook.PodCornerShare);

        /// <summary>The cube picture box of a cell whose top is <paramref name="cellTop"/>, <paramref name="cell"/> px wide: from its top, <see cref="PictureShare"/> of the width tall.</summary>
        public Box PictureBox(Box cellTop, float cell) => new Box(cellTop.Left, cellTop.Top, cellTop.Right, cellTop.Top + (cell * PictureShare));

        /// <summary>
        /// The look the board layouts take when they are built (<see cref="RoundBoardLayout.Fit(Box, RoundLevel, float)"/>):
        /// <see cref="Default"/>. Only the preview's look-dev sheet sets it; the game never does.
        /// </summary>
        public static BoardCubeLook Current { get; set; } = Default;

        /// <summary>The owner's review variants of 2026-11-03 (A the look before, <see cref="Previous"/>; E the owner's choice, now <see cref="Default"/>), with their sheet labels.</summary>
        public static readonly (string Id, string Label, BoardCubeLook Look)[] Variants =
        {
            ("A", "A — было", Previous),

            // B: only a gap between the cubes; a hairline of the far cube's front shows over each top.
            ("B", "B — зазор", new BoardCubeLook("lookB", 0.91f, 0.88f, 0.88f, 0.38f, 0.12f, 0.32f, 0.12f)),

            // C: the top shorter than the row, so 0.18 of each 0.44 front (41 %) peeks under every cube; a slight gap.
            ("C", "C — полоска спереди у каждого", new BoardCubeLook("lookC", 0.92f, 0.88f, 0.70f, 0.44f, 0.10f, 0.32f, 0.12f)),

            // D: a taller front (0.50, 0.24 of it showing), a stronger rim and bevel, a darker foot; a slight gap.
            ("D", "D — выше и объёмнее", new BoardCubeLook("lookD", 0.92f, 0.90f, 0.66f, 0.50f, 0.14f, 0.36f, 0.12f,
                rim: 1.5f, topLift: 0.14f, bevelDark: 0.8f, frontLight: 0.16f, frontDeep: 0.48f, seam: 0.5f)),

            // E: the reference game's packed cubes: softer rounder tops, a big soft gloss, a darker band on every cube, thin
            // gaps and a soft shadow on the surface.
            // E: the reference game's packed cubes: softer rounder tops, a big soft gloss, a darker band on every cube, thin
            // gaps and a soft shadow on the surface. The owner's choice of 2026-11-03: the game's look.
            ("E", "E — как у референса (выбран)", Default),
        };
    }
}
