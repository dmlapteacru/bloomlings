using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The parts of one Leaderboard row (contracts/look.md §6.8, <see cref="ReferenceLeaderboardRegions.Parts"/>), left to
    /// right: the rank (<see cref="Rank"/>, its medal <see cref="Medal"/> for ranks 1–3), the round portrait, the name,
    /// the player's marker and badge (their own row only) and the score (the highest completed level). Positions are
    /// shares of the row's width, as on the Leaderboard card they replace; sizes are shares of its height. Engine-free.
    /// </summary>
    public sealed record LeaderboardRowParts(Box Rank, Box Medal, Box Portrait, Box Name, Box Marker, Box Badge, Box Score)
    {
        /// <summary>The parts in their order from the left (the tests check that none overlaps the next).</summary>
        public IReadOnlyList<(string Name, Box Box)> Ordered => new[]
        {
            ("Rank", Rank), ("Portrait", Portrait), ("Name", Name), ("Marker", Marker), ("Badge", Badge), ("Score", Score),
        };
    }

    /// <summary>
    /// The Leaderboard as a full-screen page (contracts/look.md §6.8; the owner's request of 2026-10-04: "All the menu's
    /// places must be a separate page. Not popups."), both builds, on the Store page's frame (<see cref="ScreenLayout.LockedPage"/>,
    /// so the four pages line up): the page header (<see cref="Header"/>: back, the "Leaderboard" banner, the Petals pill),
    /// the parchment panel (<see cref="Panel"/>) from under the header to the bottom of the screen, and in it the page's
    /// area (<see cref="Area"/>, the Store page's list box, above the bottom menu's top <see cref="NavTop"/>) holding, top
    /// to bottom, the rank rows (<see cref="Rows"/>, <see cref="Row"/>: as many lines as the data has and the box fits,
    /// <see cref="LinesFitting"/>, the player's own row among them), the status line (<see cref="Status"/>: offline; an
    /// empty board's line shows in <see cref="Empty"/> instead) and the Refresh button (<see cref="Refresh"/>). Before L10
    /// the panel holds the locked notice in <see cref="Area"/> instead. Engine-free.
    /// </summary>
    public sealed record ReferenceLeaderboardRegions(
        Box Safe,
        float W,
        PageHeader Header,
        Box Panel,
        Box Area,
        Box Rows,
        Box Status,
        Box Refresh,
        float NavTop)
    {
        /// <summary>
        /// A row's height, as a share of W: at least <see cref="RowShare"/> (eight lines fit on every phone from 16:9),
        /// at most <see cref="RowMaxShare"/> (the rows grow on a taller page, up to what their parts leave room for); and
        /// the gap between two rows.
        /// </summary>
        public const float RowShare = 0.11f;

        public const float RowMaxShare = 0.13f;

        public const float RowGapShare = 0.02f;

        /// <summary>The row height the rows' type sizes are drawn for, as a share of W (taller rows grow their letters).</summary>
        public const float RowTypeShare = 0.11f;

        /// <summary>The fewest lines the rows' box holds on any phone (the data's: the player and three neighbours a side).</summary>
        public const int MinLines = 8;

        /// <summary>The status line's height, as a share of W.</summary>
        public const float StatusShare = 0.06f;

        /// <summary>The Refresh button's width, as a share of W (its height the Store's footer line's).</summary>
        public const float RefreshWidthShare = 0.54f;

        /// <summary>The room between the rows, the status line and the Refresh button, as a share of W.</summary>
        public const float GapShare = 0.02f;

        /// <summary>The medal's side, as a share of the row's height (the card's 72 of 96 units).</summary>
        public const float MedalShare = 0.75f;

        /// <summary>The portrait's side, as a share of the row's height (the card's 68 of 96 units).</summary>
        public const float PortraitShare = 0.7f;

        /// <summary>The player's marker and badge, as a share of the row's height.</summary>
        public const float MarkShare = 0.55f;

        /// <summary>The back button, the banner and the Petals pill (<see cref="PageHeader"/>).</summary>
        public Box Back => Header.Back;

        public Box Banner => Header.Banner;

        public Box Petals => Header.Petals;

        /// <summary>The panel's corner radius (a card's, <c>radius.card</c> of its width, at least <c>radius.card_min</c>).</summary>
        public float PanelRadius(float scale) => Math.Max(DesignTokens.Radius.CardMin * scale, Panel.Width * DesignTokens.Radius.Card);

        /// <summary>The line an empty board's message takes (no ranks yet, or offline before the first read): the rows' middle.</summary>
        public Box Empty => Box.FromCenter(Rows.CenterX, Rows.CenterY, Rows.Width, StatusShare * W);

        /// <summary>How many lines (ranks and the gap marker between the top and the player's neighbours) fit at <see cref="RowShare"/>.</summary>
        public int LinesFitting
        {
            get
            {
                float row = W * RowShare;
                float gap = W * RowGapShare;
                return Math.Max(1, (int)Math.Floor((Rows.Height + gap + 0.5f) / (row + gap)));
            }
        }

        /// <summary>The lines a page shows of <paramref name="lines"/>: all of them when they fit, else as many as fit.</summary>
        public int LinesShown(int lines) => Math.Max(1, Math.Min(lines, LinesFitting));

        /// <summary>
        /// The rows' height for <paramref name="lines"/> lines: the shown lines (<see cref="LinesShown"/>) filling the rows'
        /// box from its top, from <see cref="RowShare"/> to <see cref="RowMaxShare"/> of W.
        /// </summary>
        public float RowHeight(int lines)
        {
            int shown = LinesShown(lines);
            float gap = W * RowGapShare;
            float fill = (Rows.Height - (gap * (shown - 1))) / shown;
            return Math.Max(W * RowShare, Math.Min(W * RowMaxShare, fill));
        }

        /// <summary>Line <paramref name="line"/> of <paramref name="lines"/>: <see cref="RowHeight"/> tall across the rows' box, from its top.</summary>
        public Box Row(int line, int lines)
        {
            float row = RowHeight(lines);
            float top = Rows.Top + (line * (row + (W * RowGapShare)));
            return new Box(Rows.Left, top, Rows.Right, top + row);
        }

        /// <summary>
        /// The first of the <paramref name="shown"/> lines a page shows of <paramref name="lines"/>, keeping line
        /// <paramref name="focus"/> (the player's own row; −1 for none) in view: 0 when they all fit, else the window
        /// around it as near its middle as the ends allow.
        /// </summary>
        public static int FirstLine(int lines, int shown, int focus)
        {
            if (lines <= shown || focus < 0)
            {
                return 0;
            }

            return Math.Max(0, Math.Min(lines - shown, focus - (shown / 2)));
        }

        /// <summary>
        /// A row's parts (<see cref="LeaderboardRowParts"/>), the card's recipe on the page's row: the rank on its middle at
        /// 8.2% of the row's width (the medal <see cref="MedalShare"/> of its height), the portrait at 20%
        /// (<see cref="PortraitShare"/>), the name from 27% to 60%, the marker at 65.5% and the badge at 74.5%
        /// (<see cref="MarkShare"/>), and the score on its middle 10.6% of the width from the row's right end, 18.8% wide.
        /// </summary>
        public static LeaderboardRowParts Parts(Box row)
        {
            float w = row.Width;
            float h = row.Height;
            float cy = row.CenterY;
            float X(float share) => row.Left + (w * share);
            Box rank = Box.FromCenter(X(0.082f), cy, w * 0.13f, h);
            Box medal = Box.FromCenter(rank.CenterX, cy, h * MedalShare, h * MedalShare);
            Box portrait = Box.FromCenter(X(0.2f), cy, h * PortraitShare, h * PortraitShare);
            var name = new Box(X(0.27f), row.Top, X(0.6f), row.Bottom);
            Box marker = Box.FromCenter(X(0.655f), cy, h * MarkShare, h * MarkShare);
            Box badge = Box.FromCenter(X(0.745f), cy, h * MarkShare, h * MarkShare);
            Box score = Box.FromCenter(row.Right - (w * 0.106f), cy, w * 0.188f, h);
            return new LeaderboardRowParts(rank, medal, portrait, name, marker, badge, score);
        }

        /// <summary>The bands in their screen order.</summary>
        public IReadOnlyList<(string Name, Box Box)> Ordered => new[]
        {
            ("Header", Header.Row), ("Rows", Rows), ("Status", Status), ("Refresh", Refresh),
        };

        /// <summary>Everything a finger can press (the bottom menu's places are its own, <see cref="BottomNavRegions.Buttons"/>).</summary>
        public IReadOnlyList<(string Name, Box Box)> Buttons => new[]
        {
            ("Back", Back), ("Petals", Petals), ("Refresh", Refresh),
        };
    }

    /// <summary>
    /// The Collection as a full-screen page (contracts/look.md §6.9; the owner's request of 2026-10-04: "All the menu's
    /// places must be a separate page. Not popups."), both builds, on the Store page's frame (<see cref="ScreenLayout.LockedPage"/>):
    /// the page header (back, the "Collection" banner, the Petals pill), the parchment panel and, in the page's area
    /// (<see cref="Area"/>, above the bottom menu's top <see cref="NavTop"/>), the count line (<see cref="Count"/>) and the
    /// grid of framed finished pictures (<see cref="Grid"/>, <see cref="Cell"/>: three to a row, as large as fit, as many
    /// rows as the grid holds, <see cref="PerPage"/>) with the footer line between the page arrows at the area's bottom
    /// when the pictures take more than one page (<see cref="Footer"/>, <see cref="PagePrevious"/>, <see cref="PageNext"/>).
    /// A picture's detail takes the area instead: the picture large in its frame (<see cref="Picture"/>), its name
    /// (<see cref="Name"/>) and "Completed at Level N" (<see cref="Level"/>), centered. Before the first picture the panel
    /// holds the locked notice in <see cref="Area"/>. It is never a level selector. Engine-free.
    /// </summary>
    public sealed record ReferenceCollectionRegions(
        Box Safe,
        float W,
        PageHeader Header,
        Box Panel,
        Box Area,
        Box Count,
        Box Grid,
        float Side,
        Box Footer,
        Box PagePrevious,
        Box PageNext,
        Box Picture,
        Box Name,
        Box Level,
        float NavTop)
    {
        /// <summary>The pictures to a row.</summary>
        public const int Columns = 3;

        /// <summary>The gap between two frames, as a share of W.</summary>
        public const float CellGapShare = 0.03f;

        /// <summary>
        /// How much smaller than the grid's full width allows a frame may be, so one more row fits a page (the owner's
        /// choice of 2026-10-04: as many rows as fit, the page arrows right under them).
        /// </summary>
        public const float MinSideShare = 0.84f;

        /// <summary>The count line's height, and the room under it, as shares of W.</summary>
        public const float CountShare = 0.06f;

        public const float CountGapShare = 0.02f;

        /// <summary>The detail's picture at most, its name's and its level's lines, as shares of W.</summary>
        public const float PictureShare = 0.8f;

        public const float NameShare = 0.1f;

        public const float LevelShare = 0.07f;

        /// <summary>The gaps under the detail's picture and under its name, as shares of W.</summary>
        public const float PictureGapShare = 0.04f;

        public const float NameGapShare = 0.01f;

        /// <summary>The back button, the banner and the Petals pill (<see cref="PageHeader"/>).</summary>
        public Box Back => Header.Back;

        public Box Banner => Header.Banner;

        public Box Petals => Header.Petals;

        /// <summary>The panel's corner radius (a card's, <c>radius.card</c> of its width, at least <c>radius.card_min</c>).</summary>
        public float PanelRadius(float scale) => Math.Max(DesignTokens.Radius.CardMin * scale, Panel.Width * DesignTokens.Radius.Card);

        /// <summary>
        /// A frame's side (<see cref="Side"/>): three across the grid with <see cref="CellGapShare"/> between them, or a
        /// little less (down to <see cref="MinSideShare"/> of that) when one more row then fits a page.
        /// </summary>
        public float CellSize => Side;

        /// <summary>How many rows of frames fit in the grid, above the footer when <paramref name="footer"/> (at least one).</summary>
        public int RowsFitting(bool footer)
        {
            float gap = W * CellGapShare;
            float bottom = footer ? Footer.Top - gap : Grid.Bottom;
            return Math.Max(1, (int)Math.Floor((bottom - Grid.Top + gap + 0.5f) / (CellSize + gap)));
        }

        /// <summary>The pictures a page shows of <paramref name="count"/>: all of them when they fit, else as many rows as fit above the footer.</summary>
        public int PerPage(int count) => count <= RowsFitting(false) * Columns ? Math.Max(1, count) : RowsFitting(true) * Columns;

        /// <summary>The pages <paramref name="count"/> pictures take (at least one).</summary>
        public int Pages(int count) => Math.Max(1, (count + PerPage(count) - 1) / PerPage(count));

        /// <summary>Frame <paramref name="slot"/> of a page: square, <see cref="CellSize"/>, three to a row from the grid's top, centered across it.</summary>
        public Box Cell(int slot)
        {
            float gap = W * CellGapShare;
            float side = CellSize;
            float left = Grid.CenterX - (((side * Columns) + (gap * (Columns - 1))) / 2f);
            float x = left + ((slot % Columns) * (side + gap));
            float y = Grid.Top + ((slot / Columns) * (side + gap));
            return new Box(x, y, x + side, y + side);
        }

        /// <summary>The detail's parts in their screen order.</summary>
        public IReadOnlyList<(string Name, Box Box)> DetailOrdered => new[] { ("Picture", Picture), ("Name", Name), ("Level", Level) };

        /// <summary>The grid's bands in their screen order.</summary>
        public IReadOnlyList<(string Name, Box Box)> Ordered => new[]
        {
            ("Header", Header.Row), ("Count", Count), ("Grid", new Box(Grid.Left, Grid.Top, Grid.Right, Footer.Top)), ("Footer", Footer),
        };

        /// <summary>Everything a finger can press (the frames excepted; the bottom menu's places are its own).</summary>
        public IReadOnlyList<(string Name, Box Box)> Buttons => new[]
        {
            ("Back", Back), ("Petals", Petals), ("PagePrevious", PagePrevious), ("PageNext", PageNext),
        };
    }

    /// <content>The Leaderboard and Collection pages (contracts/look.md §6.8, §6.9).</content>
    public static partial class ScreenLayout
    {
        /// <summary>
        /// The Leaderboard page (contracts/look.md §6.8; <see cref="ReferenceLeaderboardRegions"/>), on the Store page's
        /// frame (<see cref="LockedPage"/>: the page header, the panel and the area <c>0.88W</c> wide from <c>0.045W</c>
        /// under the panel's top to <c>0.02W</c> over the bottom menu's top), in fractions of the safe width W: the Refresh
        /// button <c>0.54W</c> wide and <c>max(0.12W, size.touch_min)</c> tall, centered, <c>0.02W</c> over the area's
        /// bottom (where the Store page's footer is); the status line <c>0.06W</c> tall across the area, <c>0.02W</c> over
        /// Refresh; the rows' box from the area's top to <c>0.02W</c> over the status line.
        /// </summary>
        public static ReferenceLeaderboardRegions ReferenceLeaderboard(float width, float height, Insets insets)
        {
            LockedPageRegions page = LockedPage(width, height, insets);
            float w = page.W;
            Box area = page.Notice;
            float gap = ReferenceLeaderboardRegions.GapShare * w;
            float touch = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(width, height);
            float line = Math.Max(0.12f * w, touch);
            float refreshWidth = ReferenceLeaderboardRegions.RefreshWidthShare * w;
            var refresh = new Box(area.CenterX - (refreshWidth / 2f), area.Bottom - gap - line, area.CenterX + (refreshWidth / 2f), area.Bottom - gap);
            var status = new Box(area.Left, refresh.Top - gap - (ReferenceLeaderboardRegions.StatusShare * w), area.Right, refresh.Top - gap);
            var rows = new Box(area.Left, area.Top, area.Right, Math.Max(area.Top + 1f, status.Top - gap));
            return new ReferenceLeaderboardRegions(page.Safe, w, page.Header, page.Panel, area, rows, status, refresh, page.NavTop);
        }

        /// <summary>
        /// The Collection page (contracts/look.md §6.9; <see cref="ReferenceCollectionRegions"/>), on the Store page's frame
        /// (<see cref="LockedPage"/>), in fractions of the safe width W: the count line <c>0.06W</c> tall at the area's top;
        /// the grid from <c>0.02W</c> under it to the area's bottom, its frames square, three to a row <c>0.03W</c> apart
        /// (about <c>0.273W</c> each, or down to <see cref="ReferenceCollectionRegions.MinSideShare"/> of that when one more
        /// row then fits above the footer, the frames centered across the grid); the footer line as the Store page's
        /// (<c>0.84W</c> wide, <c>max(0.12W, size.touch_min)</c> tall) right under a page's last row, <c>0.03W</c> lower,
        /// at most <c>0.02W</c> over the area's bottom, with the page arrows <c>0.09W</c> at its ends; and
        /// the detail centered in the area: the picture square, <c>0.8W</c> at most (less on a short area), then
        /// <c>0.04W</c> lower its name's line <c>0.88W × 0.1W</c> and <c>0.01W</c> lower its level's line <c>0.07W</c>.
        /// </summary>
        public static ReferenceCollectionRegions ReferenceCollection(float width, float height, Insets insets)
        {
            LockedPageRegions page = LockedPage(width, height, insets);
            Box safe = page.Safe;
            float w = page.W;
            float X(float share) => safe.Left + (w * share);
            Box area = page.Notice;
            var count = new Box(area.Left, area.Top, area.Right, area.Top + (ReferenceCollectionRegions.CountShare * w));
            float gridTop = count.Bottom + (ReferenceCollectionRegions.CountGapShare * w);
            var grid = new Box(area.Left, gridTop, area.Right, Math.Max(gridTop + 1f, area.Bottom));

            // The frames: as many rows as fit above the footer, one more when the frames may shrink a little for it; the
            // footer and its page arrows (the Store page's) right under a page's last row.
            float touch = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(width, height);
            float line = Math.Max(0.12f * w, touch);
            float cellGap = ReferenceCollectionRegions.CellGapShare * w;
            float full = (grid.Width - (cellGap * (ReferenceCollectionRegions.Columns - 1))) / ReferenceCollectionRegions.Columns;
            float room = Math.Max(1f, area.Bottom - (0.02f * w) - line - cellGap - grid.Top);
            int rows = Math.Max(1, (int)Math.Floor((room + cellGap + 0.5f) / (full + cellGap)));
            float side = full;
            float more = (room - (cellGap * rows)) / (rows + 1);
            if (more >= full * ReferenceCollectionRegions.MinSideShare)
            {
                rows++;
                side = more;
            }

            float footerTop = Math.Min(grid.Top + (rows * (side + cellGap)), area.Bottom - (0.02f * w) - line);
            var footer = new Box(X(0.08f), footerTop, X(0.92f), footerTop + line);
            float arrow = 0.09f * w;
            Box pagePrevious = Box.FromCenter(footer.Left + (line / 2f), footer.CenterY, arrow, arrow);
            Box pageNext = Box.FromCenter(footer.Right - (line / 2f), footer.CenterY, arrow, arrow);

            // The detail: the picture, its name and its level, centered in the area together.
            float text = (ReferenceCollectionRegions.PictureGapShare + ReferenceCollectionRegions.NameShare + ReferenceCollectionRegions.NameGapShare + ReferenceCollectionRegions.LevelShare) * w;
            float pictureSide = Math.Max(1f, Math.Min(ReferenceCollectionRegions.PictureShare * w, area.Height - text));
            float top = area.CenterY - ((pictureSide + text) / 2f);
            Box picture = Box.FromCenter(area.CenterX, top + (pictureSide / 2f), pictureSide, pictureSide);
            float nameTop = picture.Bottom + (ReferenceCollectionRegions.PictureGapShare * w);
            var name = new Box(area.Left, nameTop, area.Right, nameTop + (ReferenceCollectionRegions.NameShare * w));
            float levelTop = name.Bottom + (ReferenceCollectionRegions.NameGapShare * w);
            var level = new Box(area.Left, levelTop, area.Right, levelTop + (ReferenceCollectionRegions.LevelShare * w));
            return new ReferenceCollectionRegions(safe, w, page.Header, page.Panel, area, count, grid, side, footer, pagePrevious, pageNext, picture, name, level, page.NavTop);
        }
    }
}
