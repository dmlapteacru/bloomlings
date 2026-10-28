using System;

namespace Bloomlings.Client.UI.Design
{
    /// <content>The round level screen (spec 006 FR-049, FR-064; the owner's level screen of 2026-10-28).</content>
    public static partial class ScreenLayout
    {
        /// <summary>
        /// The round level screen's regions (spec 006 FR-049, FR-064, contracts/presentation.md) at the owner's sizes of
        /// <see cref="RoundScreenLook"/>: the reference top bar (Pause, the sign, the speed pill, the badge under them), then
        /// top down the fixed board box (<see cref="ReferenceGameplayRegions.RoundBoard"/>), the gap, the places' bar as wide
        /// as the box, the booster row, and the pods' columns of cubes (<see cref="RoundScreenLook.PodRows"/> rows of square
        /// cubes one after another with a small gap, never overlapping: <see cref="ReferenceGameplayRegions.Pod"/>);
        /// <paramref name="stackCount"/> columns centred <see cref="RoundScreenLook.ColumnGap"/> cubes apart, their cubes
        /// narrower only where they would not fit across. Before the boosters unlock the pods follow the places at once. All of
        /// it shrinks by one factor where the safe area is shorter than the owner's stack (<see cref="RoundFit"/>). The board box
        /// and the places stand where the fullest layout (a badge and the booster row) puts them, so the box is the same on
        /// every level of the screen.
        /// </summary>
        public static ReferenceGameplayRegions RoundGameplay(
            float width,
            float height,
            Insets insets,
            int stackCount,
            int slotCount,
            bool hasBoosters = true,
            bool hasBadge = false)
        {
            Box safe = SafeArea(width, height, insets);
            float w = safe.Width;
            float k = ReferenceScale(safe);

            // The top bar under the top inset, as on every gameplay screen (§6.1).
            float barTop = safe.Top + (0.012f * w);
            var bar = new Box(safe.Left, barTop, safe.Right, barTop + (0.13f * w));
            var pause = new Box(safe.Left + (0.04f * w), bar.Top, safe.Left + (0.17f * w), bar.Bottom);
            Box sign = Box.FromCenter(safe.CenterX, bar.CenterY, 0.42f * w, 0.115f * w);
            Box speed = Box.FromCenter(safe.Right - (0.04f * w) - (0.1f * w), bar.CenterY, 0.2f * w, 0.115f * w);
            var badgeBox = new Box(safe.CenterX - (0.18f * w), bar.Bottom + (0.004f * w), safe.CenterX + (0.18f * w), bar.Bottom + (0.056f * w));
            Box badge = hasBadge ? badgeBox : new Box(safe.CenterX, bar.Bottom, safe.CenterX, bar.Bottom);

            (float top, float s) = RoundFit(safe, badgeBox.Bottom);
            float h = RoundReferenceHeight(w);

            // The board box: the owner's width and height, both by the fit's factor, centred.
            float boxWidth = RoundScreenLook.BoardWidthShare * w * s;
            float boxHeight = RoundScreenLook.BoardHeightShare * h * s;
            var roundBoard = new Box(safe.CenterX - (boxWidth / 2f), top, safe.CenterX + (boxWidth / 2f), top + boxHeight);

            // The places' bar (its cream panel is the row's box: GameplaySkin.SlotPanel adds 0.012 W round the plates' row).
            float pad = 0.012f * w;
            float slotTop = roundBoard.Bottom + (RoundScreenLook.GapAfterBoardShare * h * s);
            float slotBottom = slotTop + (RoundScreenLook.SlotsRowShare * h * s);
            var slotRow = new Box(roundBoard.Left + (0.02f * w), slotTop + pad, roundBoard.Right - (0.02f * w), slotBottom - pad);

            // The booster row right under the places, its tiles BoosterTileShare of it.
            float boosterHeight = RoundScreenLook.BoostersRowShare * h * s;
            float tile = boosterHeight * RoundScreenLook.BoosterTileShare;
            Box boosterRow = hasBoosters
                ? new Box(roundBoard.Left, slotBottom + ((boosterHeight - tile) / 2f), roundBoard.Right, slotBottom + ((boosterHeight + tile) / 2f))
                : new Box(safe.CenterX, slotBottom, safe.CenterX, slotBottom);

            // The pods: 1.5 gaps under the boosters (or the places), one pod row of three full cubes whatever the columns.
            int stacks = Math.Max(1, stackCount);
            int rows = RoundScreenLook.PodRows;
            float fullCube = RoundScreenLook.PodCubeShare * w * s;
            float podGap = RoundScreenLook.PodGapShare * w * s;
            float cube = Math.Min(fullCube, RoundScreenLook.ColumnsShare * w / (stacks + ((stacks - 1) * RoundScreenLook.ColumnGap)));
            float podTop = (hasBoosters ? slotBottom + boosterHeight : slotBottom) + (1.5f * podGap);
            var podRow = new Box(safe.Left + (0.02f * w), podTop, safe.Right - (0.02f * w), podTop + (rows * fullCube) + ((rows - 1) * podGap));

            float lineHeight = Math.Max(2f, 0.005f * w);
            Box separatorTop = hasBoosters ? Box.FromCenter(safe.CenterX, slotBottom, 0.92f * w, lineHeight) : new Box(safe.CenterX, slotBottom, safe.CenterX, slotBottom);
            Box separatorBottom = hasBoosters ? Box.FromCenter(safe.CenterX, podTop - (0.75f * podGap), 0.92f * w, lineHeight) : new Box(safe.CenterX, podTop, safe.CenterX, podTop);
            var tray = new Box(0f, slotTop, width, height);
            var trayContent = new Box(safe.Left + (0.035f * w), slotRow.Top, safe.Right - (0.035f * w), Math.Max(slotRow.Bottom, podRow.Bottom));
            var entryStrip = new Box(safe.Left, roundBoard.Bottom, safe.Right, slotTop);
            var board = new Box(safe.Left, top, safe.Right, roundBoard.Bottom);

            int slots = Math.Max(1, slotCount);
            Box[] slotBoxes = Spread(slotRow, slots, slotRow.Width, 0.012f * w, 0.012f * w);
            Box[] boosterBoxes = hasBoosters
                ? Spread(Box.FromCenter(safe.CenterX, boosterRow.CenterY, boxWidth, tile), 4, tile, 0.014f * w)
                : Array.Empty<Box>();

            // The columns: a cube wide, ColumnGap cubes apart, centred; the cubes from the pod row's top.
            float columnGap = cube * RoundScreenLook.ColumnGap;
            Box[] columns = stackCount <= 0
                ? Array.Empty<Box>()
                : Spread(new Box(safe.CenterX - (RoundScreenLook.ColumnsShare * w / 2f), podRow.Top, safe.CenterX + (RoundScreenLook.ColumnsShare * w / 2f), podRow.Bottom), stacks, cube, columnGap, columnGap);

            return new ReferenceGameplayRegions(
                safe, w, k, bar, pause, sign, speed, badge, board, entryStrip, tray, 0.06f * w, trayContent,
                slotRow, slotBoxes, separatorTop, boosterRow, boosterBoxes, separatorBottom, podRow, columns, rows,
                cube, cube, podGap, roundBoard);
        }

        /// <summary>The reference phone's height for a safe width <paramref name="w"/>: shares of H are of this (<see cref="RoundScreenLook.ReferenceHeight"/>).</summary>
        public static float RoundReferenceHeight(float w) => w * RoundScreenLook.ReferenceHeight / RoundScreenLook.ReferenceWidth;

        /// <summary>
        /// Where the round board box starts and the factor the owner's stack shrinks by on this screen: the box from
        /// <see cref="RoundScreenLook.BoardTopShare"/> of H below the screen's top, but never above
        /// <see cref="RoundScreenLook.BarGapShare"/> under <paramref name="underBar"/> (the Hard badge's bottom under the top
        /// bar); then the box, the gap, the places, the booster row and three pod cubes with their gaps at their owner's sizes,
        /// shrunk together by one factor (at most 1) until they end above the bottom inset.
        /// </summary>
        public static (float Top, float Scale) RoundFit(Box safe, float underBar)
        {
            float w = safe.Width;
            float h = RoundReferenceHeight(w);
            float top = Math.Max(RoundScreenLook.BoardTopShare * h, underBar + (RoundScreenLook.BarGapShare * w));
            float stack = ((RoundScreenLook.BoardHeightShare + RoundScreenLook.GapAfterBoardShare + RoundScreenLook.SlotsRowShare + RoundScreenLook.BoostersRowShare) * h)
                + (((RoundScreenLook.PodRows * RoundScreenLook.PodCubeShare) + ((RoundScreenLook.PodRows - 1 + 1.5f) * RoundScreenLook.PodGapShare)) * w);
            float room = safe.Bottom - (RoundScreenLook.BottomShare * w) - top;
            return (top, Math.Max(0.1f, Math.Min(1f, room / Math.Max(1f, stack))));
        }

        /// <summary>
        /// The round screen's controls aligned to the board box <paramref name="outer"/> (the owner's places bar, 2026-10-28):
        /// the places spread across the bar as wide as the box (each plate as wide as fits, at most as tall as the row), the
        /// four boosters across the box's width.
        /// </summary>
        public static ReferenceGameplayRegions AlignRound(ReferenceGameplayRegions r, Box outer)
        {
            float gap = r.W * 0.012f;
            int n = r.Slots.Count;
            var slots = new Box[n];
            float plate = (r.SlotRow.Width - (gap * Math.Max(0, n - 1))) / Math.Max(1, n);
            for (int i = 0; i < n; i++)
            {
                slots[i] = Box.FromCenter(r.SlotRow.Left + (plate * 0.5f) + (i * (plate + gap)), r.SlotRow.CenterY, plate, Math.Min(plate, r.SlotRow.Height));
            }

            var boosts = new Box[r.Boosters.Count];
            float side = Math.Min(r.BoosterRow.Height, (outer.Width - (r.W * 0.014f * 3)) / 4);
            float spread = (outer.Width - (side * 4)) / 3;
            for (int i = 0; i < boosts.Length; i++)
            {
                boosts[i] = Box.FromCenter(outer.Left + (side * 0.5f) + (i * (side + spread)), r.BoosterRow.CenterY, side, side);
            }

            return r with { Slots = slots, Boosters = boosts };
        }
    }
}
