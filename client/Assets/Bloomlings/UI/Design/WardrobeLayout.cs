using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The Wardrobe screen (spec 001 FR-063; spec 005 contracts/look.md §4.6, the reference's Wardrobe), top to bottom:
    /// the back button, the wooden banner and the Petals pill; the hero on its stone pedestal between the ‹ › arrows; the
    /// parchment name card with its name tab; the tabs of the four families and the profile; and the lighter panel with
    /// the kind chips, the outfit cards (three to a row, one or two rows) and the footer between the page arrows. Sizes in
    /// screen pixels, y down. Engine-free.
    /// </summary>
    public sealed record WardrobeRegions(
        Box Safe,
        Box Back,
        Box Banner,
        Box Petals,
        Box Stage,
        Box Pedestal,
        Box Hero,
        Box Previous,
        Box Next,
        Box NameCard,
        Box NameTab,
        Box About,
        Box Tabs,
        Box Panel,
        Box Chips,
        Box Grid,
        int Rows,
        Box Footer,
        Box PagePrevious,
        Box PageNext)
    {
        /// <summary>The outfit cards to a row.</summary>
        public const int Columns = 3;

        /// <summary>The outfit cards on one page.</summary>
        public int PerPage => Columns * Rows;

        /// <summary>The bands in their screen order (the layout tests check this order and no overlap).</summary>
        public IReadOnlyList<(string Name, Box Box)> Ordered => new[]
        {
            ("Back", Back), ("Stage", Stage), ("NameCard", NameCard), ("Tabs", Tabs), ("Panel", Panel),
        };

        /// <summary>Everything a finger can press.</summary>
        public IReadOnlyList<(string Name, Box Box)> Buttons => new[]
        {
            ("Back", Back), ("Previous", Previous), ("Next", Next), ("PagePrevious", PagePrevious), ("PageNext", PageNext),
        };

        /// <summary>The box of outfit card <paramref name="slot"/> on a page (row by row).</summary>
        public Box Card(int slot)
        {
            float gap = Grid.Width * 0.024f;
            float width = (Grid.Width - (gap * (Columns - 1))) / Columns;
            float height = (Grid.Height - (gap * (Rows - 1))) / Math.Max(1, Rows);
            int column = slot % Columns;
            int row = slot / Columns;
            float x = Grid.Left + (column * (width + gap));
            float y = Grid.Top + (row * (height + gap));
            return new Box(x, y, x + width, y + height);
        }
    }

    /// <summary>The Wardrobe's layout (<see cref="WardrobeRegions"/>).</summary>
    public static class WardrobeLayout
    {
        /// <summary>The smallest stage (units) for the hero on its pedestal before the cards drop to one row.</summary>
        public const float StageMin = 460f;

        public static WardrobeRegions Wardrobe(float width, float height, Insets insets)
        {
            Box safe = ScreenLayout.SafeArea(width, height, insets);
            float u = DesignTokens.ScaleFor(width, height);
            float side = 30f * u;
            float left = safe.Left + side;
            float right = safe.Right - side;

            // The top bar: the back button at the left, the Petals pill at the right, the banner between them.
            float bar = DesignTokens.Size.IconButton * u;
            float top = safe.Top + (20f * u);
            var back = new Box(left, top, left + bar, top + bar);
            var petals = new Box(right - (330f * u), top + (bar * 0.14f), right, top + (bar * 0.86f));
            float bannerLeft = back.Right + (20f * u);
            float bannerRight = petals.Left - (10f * u);
            float bannerWidth = Math.Min(bannerRight - bannerLeft, 480f * u);
            Box banner = Box.FromCenter((bannerLeft + bannerRight) / 2f, top + (bar / 2f), bannerWidth, 118f * u);

            // From the bottom up: the panel (chips, cards, footer), the tabs, the name card; the stage takes the rest.
            float bottom = safe.Bottom - (16f * u);
            float pad = 22f * u;
            float chips = 96f * u;
            float footer = DesignTokens.Size.TouchMin * u;
            float gridWidth = (right - left) - (2f * pad);
            float cardWidth = (gridWidth - (gridWidth * 0.024f * (WardrobeRegions.Columns - 1))) / WardrobeRegions.Columns;
            float panelFixed = pad + chips + (20f * u) + (10f * u) + footer + (8f * u);
            float tabs = 220f * u;
            float name = 280f * u;
            float stageTop = back.Bottom + (8f * u);
            float twoRows = (cardWidth * 1.2f * 2f) + (gridWidth * 0.024f);
            int rows = bottom - stageTop - panelFixed - tabs - name - twoRows >= StageMin * u ? 2 : 1;
            float grid = rows == 2 ? twoRows : Math.Min(cardWidth * 1.35f, Math.Max(cardWidth * 1.1f, bottom - stageTop - panelFixed - tabs - name - (StageMin * u)));

            var panel = new Box(left, bottom - panelFixed - grid, right, bottom);
            var chipRow = new Box(panel.Left + pad, panel.Top + pad, panel.Right - pad, panel.Top + pad + chips);
            var gridBox = new Box(panel.Left + pad, chipRow.Bottom + (20f * u), panel.Right - pad, chipRow.Bottom + (20f * u) + grid);
            var footerBox = new Box(panel.Left + pad, panel.Bottom - (8f * u) - footer, panel.Right - pad, panel.Bottom - (8f * u));
            float arrow = 104f * u;
            Box pagePrevious = Box.FromCenter(footerBox.Left + (footer / 2f), footerBox.CenterY, footer, footer);
            Box pageNext = Box.FromCenter(footerBox.Right - (footer / 2f), footerBox.CenterY, footer, footer);

            var tabRow = new Box(left + (6f * u), panel.Top - tabs, right - (6f * u), panel.Top);
            var card = new Box(left - (side * 0.5f), tabRow.Top - (12f * u) - name, right + (side * 0.5f), tabRow.Top - (12f * u));
            Box tab = Box.FromCenter(card.CenterX, card.Top + (36f * u), Math.Min(card.Width * 0.6f, 560f * u), 128f * u);
            var about = new Box(card.Left + (60f * u), tab.Bottom + (6f * u), card.Right - (60f * u), card.Bottom - (14f * u));

            // The hero on the pedestal, as large as the stage allows (the pedestal's foot reaches a little behind the name
            // tab, as in the reference), the arrows at its sides.
            var stage = new Box(safe.Left, stageTop, safe.Right, card.Top);
            (Box pedestal, Box hero) = HomeStage.HeroOnPedestal(new Box(stage.Left, stage.Top, stage.Right, stage.Bottom + (18f * u)));
            float arrowY = (hero.Top + pedestal.Top) / 2f;
            Box previous = Box.FromCenter(left + (arrow * 0.7f), arrowY, footer, footer);
            Box next = Box.FromCenter(right - (arrow * 0.7f), arrowY, footer, footer);
            return new WardrobeRegions(safe, back, banner, petals, stage, pedestal, hero, previous, next, card, tab, about, tabRow, panel, chipRow, gridBox, rows, footerBox, pagePrevious, pageNext);
        }
    }
}
