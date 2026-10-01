using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>A rectangle in screen pixels, y down (the playtest's canvas; Unity converts with <see cref="Normalized"/>).</summary>
    public readonly struct Box
    {
        public Box(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = Math.Max(left, right);
            Bottom = Math.Max(top, bottom);
        }

        public float Left { get; }

        public float Top { get; }

        public float Right { get; }

        public float Bottom { get; }

        public float Width => Right - Left;

        public float Height => Bottom - Top;

        public float CenterX => (Left + Right) / 2f;

        public float CenterY => (Top + Bottom) / 2f;

        public bool IsEmpty => Width <= 0f || Height <= 0f;

        public static Box FromCenter(float cx, float cy, float width, float height) =>
            new Box(cx - (width / 2f), cy - (height / 2f), cx + (width / 2f), cy + (height / 2f));

        public Box Inset(float by) => new Box(Left + by, Top + by, Right - by, Bottom - by);

        public Box Inset(float dx, float dy) => new Box(Left + dx, Top + dy, Right - dx, Bottom - dy);

        public Box Offset(float dx, float dy) => new Box(Left + dx, Top + dy, Right + dx, Bottom + dy);

        /// <summary>Scaled about its center.</summary>
        public Box Scale(float sx, float sy) => FromCenter(CenterX, CenterY, Width * sx, Height * sy);

        public bool Contains(float x, float y) => x >= Left && x <= Right && y >= Top && y <= Bottom;

        /// <summary>Whether this box lies inside another, within a tolerance of half a pixel.</summary>
        public bool Within(Box outer) => Left >= outer.Left - 0.5f && Top >= outer.Top - 0.5f && Right <= outer.Right + 0.5f && Bottom <= outer.Bottom + 0.5f;

        /// <summary>Whether the two boxes share an area (touching edges do not count).</summary>
        public bool Overlaps(Box other) =>
            !IsEmpty && !other.IsEmpty && Left < other.Right - 0.5f && other.Left < Right - 0.5f && Top < other.Bottom - 0.5f && other.Top < Bottom - 0.5f;

        /// <summary>Unity anchors: (xMin, yMin, xMax, yMax) in 0–1 of the screen, y up.</summary>
        public (float XMin, float YMin, float XMax, float YMax) Normalized(float screenWidth, float screenHeight) =>
            (Left / screenWidth, 1f - (Bottom / screenHeight), Right / screenWidth, 1f - (Top / screenHeight));

        public override string ToString() => string.Format(System.Globalization.CultureInfo.InvariantCulture, "[{0:0},{1:0} {2:0}x{3:0}]", Left, Top, Width, Height);
    }

    /// <summary>The safe-area insets of a screen, in pixels (notches, rounded corners, navigation bars).</summary>
    public readonly struct Insets
    {
        public Insets(float top, float bottom, float left = 0f, float right = 0f)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }

        public float Top { get; }

        public float Bottom { get; }

        public float Left { get; }

        public float Right { get; }

        public static Insets None => default;
    }

    /// <summary>The gameplay screen of frames 7–9, top to bottom (FR-009, spec 001 FR-068).</summary>
    public sealed record GameplayRegions(Box Safe, Box TopBar, Box Badge, Box Board, Box Slots, Box Tray, Box Boosters)
    {
        /// <summary>The regions in their screen order (the layout tests check this order and no overlap).</summary>
        public IReadOnlyList<(string Name, Box Box)> Ordered => new[]
        {
            ("TopBar", TopBar), ("Badge", Badge), ("Board", Board), ("Slots", Slots), ("Tray", Tray), ("Boosters", Boosters),
        };
    }

    /// <summary>Home of frames 2 and 3, top to bottom (FR-017). Collapsed regions are empty.</summary>
    public sealed record HomeRegions(Box Safe, Box TopBar, Box Hero, Box Features, Box Level, Box Teaser, Box Play, Box Rank, Box Daily, Box Extra)
    {
        public IReadOnlyList<(string Name, Box Box)> Ordered => new[]
        {
            ("TopBar", TopBar), ("Hero", Hero), ("Level", Level), ("Teaser", Teaser), ("Play", Play), ("Rank", Rank), ("Daily", Daily), ("Extra", Extra),
        };
    }

    /// <summary>A centered popup card (frames 4, 5, 6, 11, 16, 17; FR-007).</summary>
    public sealed record CardRegions(Box Card, Box Title, Box Close, Box Body);

    /// <summary>The jam bottom sheet (frame 10, FR-019). It keeps the board visible above it (spec 001 FR-027).</summary>
    public sealed record SheetRegions(Box Sheet, Box Grip, Box Title, Box Subtitle, Box Body);

    /// <summary>
    /// The regions of every screen, from the screen size and its safe insets (research R5): one rule for both clients,
    /// tested for 16:9 to 21:9 (SC-007). Sizes come from <see cref="DesignTokens"/> in reference units scaled by
    /// <see cref="DesignTokens.ScaleFor"/>. On tall phones the spare height goes to the board (gameplay) or the hero
    /// (Home); on short phones those shrink first, and the bands keep at least 80% of their size so counts stay
    /// readable. Engine-free.
    /// </summary>
    public static class ScreenLayout
    {
        /// <summary>The smallest share of the safe height the board keeps (data-model rule 3).</summary>
        public const float BoardMinShare = 0.45f;

        public static Box SafeArea(float width, float height, Insets insets) =>
            new Box(insets.Left, insets.Top, width - insets.Right, height - insets.Bottom);

        /// <param name="hasBadge">A Hard or Super Hard level: the badge sits under the level pill.</param>
        /// <param name="hasBoosters">At least one booster is unlocked; before L3 the bar is hidden (frame 14).</param>
        public static GameplayRegions Gameplay(float width, float height, Insets insets, bool hasBadge, bool hasBoosters)
        {
            Box safe = SafeArea(width, height, insets);
            float u = DesignTokens.ScaleFor(width, height);
            float margin = 24f * u;

            // Band heights (reference units): top bar, badge, slots, tray, boosters; and the gaps between them.
            float topPad = 16f;
            float topBar = DesignTokens.Size.IconButton;
            float badge = hasBadge ? 46f : 0f;
            float gap = 20f;
            float slots = 170f;
            float tray = 340f;
            float boosters = hasBoosters ? DesignTokens.Size.BoosterButton : 0f;
            float bottomPad = 20f;
            float fixedBands = (slots + tray + boosters) * u;
            float fixedRest = (topPad + topBar + badge + (gap * 4f) + bottomPad) * u;

            // Short screens: shrink the bands (to 80% at most) before the board drops below its share.
            float k = 1f;
            float boardMin = safe.Height * BoardMinShare;
            if (safe.Height - fixedRest - fixedBands < boardMin && fixedBands > 0f)
            {
                k = Math.Max(0.8f, (safe.Height - fixedRest - boardMin) / fixedBands);
            }

            float y = safe.Top + (topPad * u);
            var topBox = new Box(safe.Left + margin, y, safe.Right - margin, y + (topBar * u));
            y = topBox.Bottom;
            Box badgeBox = hasBadge ? Box.FromCenter(safe.CenterX, y + (badge * u / 2f), 300f * u, badge * u) : new Box(safe.CenterX, y, safe.CenterX, y);
            y += (badge + gap) * u;
            float boardTop = y;

            float bottom = safe.Bottom - (bottomPad * u);
            var boosterBox = new Box(safe.Left + margin, bottom - (boosters * u * k), safe.Right - margin, bottom);
            bottom = boosterBox.Top - (hasBoosters ? gap * u : 0f);
            var trayBox = new Box(safe.Left + margin, bottom - (tray * u * k), safe.Right - margin, bottom);
            bottom = trayBox.Top - (gap * u);
            var slotBox = new Box(safe.Left + margin, bottom - (slots * u * k), safe.Right - margin, bottom);
            var boardBox = new Box(safe.Left + margin, boardTop, safe.Right - margin, slotBox.Top - (gap * u));
            if (!hasBoosters)
            {
                boosterBox = new Box(safe.Left + margin, safe.Bottom - (bottomPad * u), safe.Right - margin, safe.Bottom - (bottomPad * u));
            }

            return new GameplayRegions(safe, topBox, badgeBox, boardBox, slotBox, trayBox, boosterBox);
        }

        /// <param name="bottomReserve">Height kept free at the bottom (the playtest's dev row), in pixels.</param>
        public static HomeRegions Home(float width, float height, Insets insets, HomeLook look, float bottomReserve = 0f)
        {
            Box safe = SafeArea(width, height, insets);
            float u = DesignTokens.ScaleFor(width, height);
            float margin = DesignTokens.Size.Margin * u;
            float left = safe.Left + margin;
            float right = safe.Right - margin;

            float y = safe.Top + (20f * u);
            var top = new Box(left, y, right, y + (DesignTokens.Size.IconButton * u));

            // From the bottom up: Extra (free booster offer), Daily card, Rank, Play, Teaser, Level.
            float bottom = safe.Bottom - bottomReserve - (36f * u);
            Box Take(float refHeight, bool shown, float gapAfter = 18f)
            {
                if (!shown)
                {
                    return new Box(left, bottom, right, bottom);
                }

                var b = new Box(left, bottom - (refHeight * u), right, bottom);
                bottom = b.Top - (gapAfter * u);
                return b;
            }

            Box extra = Take(96f, look.FreeBoosterOffer);
            Box daily = Take(150f, look.DailyChallenge, 22f);
            Box rank = Take(96f, look.Rank, 22f);
            // PLAY is shorter and taller (spec 003 FR-011); its leaves reach above and below it, so it keeps more room.
            bottom -= 20f * u;
            Box playRow = Take(DesignTokens.Size.PlayHeight, true, 48f);
            Box teaser = Take(78f, look.Teaser, 20f);
            Box level = Take(110f, true, 10f);

            float playWidth = Math.Min(playRow.Width, DesignTokens.Size.PlayWidth * u);
            Box play = Box.FromCenter(playRow.CenterX, playRow.CenterY, playWidth, playRow.Height);
            Box teaserPill = look.Teaser ? Box.FromCenter(teaser.CenterX, teaser.CenterY, Math.Min(teaser.Width, 640f * u), teaser.Height) : teaser;
            Box rankRow = look.Rank ? Box.FromCenter(rank.CenterX, rank.CenterY, Math.Min(rank.Width, 520f * u), rank.Height) : rank;

            var hero = new Box(safe.Left, top.Bottom + (12f * u), safe.Right, level.Top - (12f * u));
            float featureWidth = look.Wardrobe || look.Collection ? DesignTokens.Size.IconButton * u : 0f;
            var features = new Box(left, hero.Top + (40f * u), left + featureWidth, hero.Bottom - (40f * u));
            return new HomeRegions(safe, top, hero, features, level, teaserPill, play, rankRow, daily, extra);
        }

        /// <summary>
        /// A card's button centered in its body from <paramref name="top"/> (spec 003 FR-011): the primary one at
        /// <c>size.card_primary</c>, the secondary ones at <c>size.card_secondary_width</c> and <c>size.secondary_height</c>.
        /// </summary>
        public static Box CardButton(Box body, float top, bool primary, float scale)
        {
            float width = Math.Min(body.Width, (primary ? DesignTokens.Size.CardPrimaryWidth : DesignTokens.Size.CardSecondaryWidth) * scale);
            float height = (primary ? DesignTokens.Size.CardPrimaryHeight : DesignTokens.Size.SecondaryHeight) * scale;
            return new Box(body.CenterX - (width / 2f), top, body.CenterX + (width / 2f), top + height);
        }

        /// <param name="contentHeight">The card's content height in reference units (title and close excluded).</param>
        public static CardRegions Card(float width, float height, Insets insets, float contentHeight)
        {
            Box safe = SafeArea(width, height, insets);
            float u = DesignTokens.ScaleFor(width, height);
            float cardWidth = Math.Min(safe.Width * 0.84f, 920f * u);
            float titleHeight = 130f * u;
            float padding = DesignTokens.Space.M * u;
            float cardHeight = Math.Min(safe.Height - (80f * u), titleHeight + (contentHeight * u) + padding);
            Box card = Box.FromCenter(safe.CenterX, safe.CenterY, cardWidth, cardHeight);
            var title = new Box(card.Left + (120f * u), card.Top + (20f * u), card.Right - (120f * u), card.Top + titleHeight);
            float close = 96f * u;
            var closeBox = new Box(card.Right - close - (22f * u), card.Top + (22f * u), card.Right - (22f * u), card.Top + (22f * u) + close);
            var body = new Box(card.Left + padding, card.Top + titleHeight, card.Right - padding, card.Bottom - padding);
            return new CardRegions(card, title, closeBox, body);
        }

        /// <param name="contentHeight">The sheet's body height in reference units (under the title and subtitle).</param>
        public static SheetRegions Sheet(float width, float height, Insets insets, float contentHeight)
        {
            Box safe = SafeArea(width, height, insets);
            float u = DesignTokens.ScaleFor(width, height);
            float side = 24f * u;
            float grip = 40f * u;
            float title = 84f * u;
            float subtitle = 56f * u;
            float padding = DesignTokens.Space.M * u;
            float sheetHeight = Math.Min(safe.Height * 0.6f, grip + title + subtitle + (contentHeight * u) + padding + insets.Bottom);
            var sheet = new Box(side, height - sheetHeight, width - side, height + (40f * u));
            var gripBox = Box.FromCenter(sheet.CenterX, sheet.Top + (grip / 2f), 110f * u, 12f * u);
            var titleBox = new Box(sheet.Left + padding, sheet.Top + grip, sheet.Right - padding, sheet.Top + grip + title);
            var subtitleBox = new Box(sheet.Left + padding, titleBox.Bottom, sheet.Right - padding, titleBox.Bottom + subtitle);
            var body = new Box(sheet.Left + padding, subtitleBox.Bottom + (10f * u), sheet.Right - padding, height - insets.Bottom - padding);
            return new SheetRegions(sheet, gripBox, titleBox, subtitleBox, body);
        }

        /// <summary>
        /// Splits a row into <paramref name="count"/> equal cells with a gap, each at most <paramref name="maxSize"/>
        /// wide and square when <paramref name="square"/>, centered in the row (slots, boosters, option tiles).
        /// </summary>
        public static Box[] Row(Box row, int count, float gap, float maxSize, bool square)
        {
            var cells = new Box[Math.Max(0, count)];
            if (count <= 0)
            {
                return cells;
            }

            float size = Math.Min(maxSize, (row.Width - (gap * (count - 1))) / count);
            float cellHeight = square ? Math.Min(size, row.Height) : row.Height;
            float cellWidth = square ? cellHeight : size;
            float total = (cellWidth * count) + (gap * (count - 1));
            float x = row.CenterX - (total / 2f);
            for (int i = 0; i < count; i++)
            {
                cells[i] = new Box(x, row.CenterY - (cellHeight / 2f), x + cellWidth, row.CenterY + (cellHeight / 2f));
                x += cellWidth + gap;
            }

            return cells;
        }

        /// <summary>Splits a column into rows of a fixed height with a gap, from the top (card lists).</summary>
        public static Box[] Column(Box column, int count, float rowHeight, float gap)
        {
            var rows = new Box[Math.Max(0, count)];
            float y = column.Top;
            for (int i = 0; i < count; i++)
            {
                rows[i] = new Box(column.Left, y, column.Right, y + rowHeight);
                y += rowHeight + gap;
            }

            return rows;
        }
    }
}
