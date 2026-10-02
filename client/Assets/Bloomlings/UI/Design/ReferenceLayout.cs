using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The reference gameplay screen (spec 005 FR-020, FR-021; contracts/look.md §6.1), top to bottom: the top bar (Pause,
    /// the wooden level sign, the speed pill), the board in its stone border on the lawn, the entry strip, and one parchment
    /// tray from there to the bottom of the screen holding the Waiting Slots, the four booster boxes and one deck per Source
    /// stack, parted by two thin lines. <see cref="W"/> is the safe width in pixels and <see cref="K"/> the factor the tray
    /// rows (and the entry strip) shrink by on screens shorter than 19.5:9. Collapsed regions (no badge, no boosters) are
    /// empty. Screen pixels, y down. Engine-free.
    /// </summary>
    public sealed record ReferenceGameplayRegions(
        Box Safe,
        float W,
        float K,
        Box TopBar,
        Box Pause,
        Box Sign,
        Box Speed,
        Box Badge,
        Box Board,
        Box EntryStrip,
        Box Tray,
        float TrayRadius,
        Box TrayContent,
        Box SlotRow,
        IReadOnlyList<Box> Slots,
        Box SeparatorTop,
        Box BoosterRow,
        IReadOnlyList<Box> Boosters,
        Box SeparatorBottom,
        Box PodRow,
        IReadOnlyList<Box> Decks,
        int PodRows)
    {
        /// <summary>The widest the stone border's outer box may be, as a share of <see cref="W"/>.</summary>
        public const float MaxBoardShare = 0.86f;

        /// <summary>
        /// The lawn from the board's top to the entry strip's bottom across the safe width: the area the board and its
        /// arches share (<see cref="FitBoard"/>).
        /// </summary>
        public Box BoardArea => new Box(Safe.Left, Board.Top, Safe.Right, EntryStrip.Bottom);

        /// <summary>The bands in their screen order (the layout tests check this order and no overlap).</summary>
        public IReadOnlyList<(string Name, Box Box)> Ordered
        {
            get
            {
                var bands = new List<(string, Box)> { ("TopBar", TopBar) };
                if (!Badge.IsEmpty)
                {
                    bands.Add(("Badge", Badge));
                }

                bands.Add(("Board", Board));
                bands.Add(("EntryStrip", EntryStrip));
                bands.Add(("SlotRow", SlotRow));
                if (!BoosterRow.IsEmpty)
                {
                    bands.Add(("BoosterRow", BoosterRow));
                }

                bands.Add(("PodRow", PodRow));
                return bands;
            }
        }

        /// <summary>
        /// The count badge of booster box <paramref name="index"/>: a disc 0.34 of the box over its lower right corner,
        /// its center 0.55 of its size inside the corner (as <c>Kit.BoosterTile</c> draws it).
        /// </summary>
        public Box BoosterBadge(int index)
        {
            Box box = Boosters[index];
            float size = Math.Min(box.Width, box.Height) * 0.34f;
            return Box.FromCenter(box.Right - (size * 0.55f), box.Bottom - (size * 0.55f), size, size);
        }

        /// <summary>The parts of the deck of Source stack <paramref name="index"/> (<see cref="PodDeck"/>).</summary>
        public PodDeck Deck(int index) => PodDeck.In(Decks[index]);

        /// <summary>
        /// The board's layout in these regions: <see cref="BoardLayout.Fit"/> over <see cref="BoardArea"/> (less 0.02 W at
        /// each side), narrowed so the stone border's outer box is at most <see cref="MaxBoardShare"/> × <see cref="W"/>
        /// wide. A bottom entry's arch then stands in the entry strip under the board; side and top arches stand beside
        /// it, as <see cref="BoardLayout"/> places them.
        /// </summary>
        public BoardLayout FitBoard(int width, int height, IReadOnlyList<EntryDef> entries)
        {
            Box area = BoardArea.Inset(W * 0.02f, 0f);
            BoardLayout layout = BoardLayout.Fit(area, width, height, entries);
            float max = W * MaxBoardShare;
            if (layout.Outer.Width <= max + 0.5f)
            {
                return layout;
            }

            // The widest area whose border stays within the cap (the cells grow with the area's width until its height
            // binds them, so a bisection finds it).
            float low = 0f;
            float high = area.Width;
            BoardLayout best = BoardLayout.Fit(Box.FromCenter(area.CenterX, area.CenterY, area.Width * max / layout.Outer.Width, area.Height), width, height, entries);
            for (int i = 0; i < 18; i++)
            {
                float mid = (low + high) / 2f;
                BoardLayout trial = BoardLayout.Fit(Box.FromCenter(area.CenterX, area.CenterY, mid, area.Height), width, height, entries);
                if (trial.Outer.Width <= max + 0.5f)
                {
                    low = mid;
                    best = trial;
                }
                else
                {
                    high = mid;
                }
            }

            return best;
        }
    }

    /// <summary>
    /// One Source stack drawn as a deck (spec 005 FR-021, contracts/look.md §6.1): the exposed pod in front filling the
    /// bottom <see cref="FrontShare"/> of the deck box, and up to two buried pods as frames of the same size behind it,
    /// each raised by <see cref="Raise"/> of the deck's height over the one in front, so a band of each shows above it.
    /// <see cref="Inner"/> is the front frame's panel (inside its border of 11% of the width), <see cref="Tile"/> the
    /// sticker tile (62% of the frame's width, 3% of the panel below its top), <see cref="Count"/> the room for the
    /// count under it, and <see cref="Badge"/> the "+N" disc on the deck's top right. Engine-free.
    /// </summary>
    public sealed record PodDeck(Box Deck, Box Front, Box Buried1, Box Buried2, Box Inner, Box Tile, Box Count, Box Badge)
    {
        /// <summary>The share of the deck's height the front pod fills.</summary>
        public const float FrontShare = 0.78f;

        /// <summary>How far each buried pod rises over the one in front of it, as a share of the deck's height.</summary>
        public const float Raise = 0.09f;

        /// <summary>The frame's border, as a share of the pod's width (<c>Kit.PodFrame</c>).</summary>
        public const float Border = 0.11f;

        /// <summary>The sticker tile's side, as a share of the frame's width.</summary>
        public const float TileShare = 0.62f;

        /// <summary>The parts of a deck in <paramref name="deck"/>.</summary>
        public static PodDeck In(Box deck)
        {
            float h = deck.Height;
            float w = deck.Width;
            var front = new Box(deck.Left, deck.Bottom - (h * FrontShare), deck.Right, deck.Bottom);
            Box buried1 = front.Offset(0f, -h * Raise);
            Box buried2 = front.Offset(0f, -2f * h * Raise);
            Box inner = front.Inset(w * Border);
            float tile = Math.Min(w * TileShare, inner.Height * 0.78f);
            float tileTop = inner.Top + (inner.Height * 0.03f);
            var tileBox = new Box(inner.CenterX - (tile / 2f), tileTop, inner.CenterX + (tile / 2f), tileTop + tile);
            var count = new Box(inner.Left, tileBox.Bottom, inner.Right, inner.Bottom);
            float badge = w * 0.26f;
            Box badgeBox = Box.FromCenter(deck.Right - (badge * 0.32f), deck.Top + (badge * 0.32f), badge, badge);
            return new PodDeck(deck, front, buried1, buried2, inner, tileBox, count, badgeBox);
        }

        /// <summary>The frame of buried pod <paramref name="depth"/> (1 or 2).</summary>
        public Box Buried(int depth) => depth <= 1 ? Buried1 : Buried2;

        /// <summary>
        /// The visible band of buried pod <paramref name="depth"/> (1 or 2): from its top to the top of the pod in front of
        /// it. The band shows the frame's top edge and a strip of the pod's variant color with its small symbol.
        /// </summary>
        public Box Band(int depth)
        {
            Box pod = Buried(depth);
            Box over = depth <= 1 ? Front : Buried1;
            return new Box(pod.Left, pod.Top, pod.Right, over.Top);
        }
    }

    /// <summary>
    /// The jam card (spec 005 FR-022, contracts/look.md §6.2): a centered modal card over the dimmed gameplay, top to
    /// bottom: the title, the subtitle (up to two lines), the well with the slot contents, the choices as a two-column
    /// grid of big colored buttons, each with its cost pill hanging under its bottom edge, and Restart; the cream round
    /// close button over the top-right corner (empty when the sheet may not be closed). <see cref="Scale"/> is the factor
    /// every height shrank by to fit a short screen (1 when it fits). Engine-free.
    /// </summary>
    public sealed record JamCardRegions(
        Box Safe,
        float W,
        float Scale,
        Box Card,
        Box Close,
        Box Title,
        Box Subtitle,
        Box Well,
        IReadOnlyList<Box> Choices,
        IReadOnlyList<Box> CostPills,
        Box Restart)
    {
        /// <summary>The bands in their order (the tests check this order and no overlap).</summary>
        public IReadOnlyList<(string Name, Box Box)> Ordered
        {
            get
            {
                var bands = new List<(string, Box)> { ("Title", Title), ("Subtitle", Subtitle), ("Well", Well) };
                for (int i = 0; i < Choices.Count; i += 2)
                {
                    bands.Add(("Choices" + (i / 2), new Box(Card.Left, Choices[i].Top, Card.Right, CostPills[i].Bottom)));
                }

                bands.Add(("Restart", Restart));
                return bands;
            }
        }

        /// <summary>
        /// The tile and the count of Waiting Slot <paramref name="index"/> of <paramref name="count"/> in the well: the
        /// well split into equal cells, each with a tile of 0.11 W (scaled) near its top and the count under it.
        /// </summary>
        public (Box Tile, Box Count) WellCell(int index, int count)
        {
            int n = Math.Max(1, count);
            float cell = Well.Width / n;
            float cx = Well.Left + ((index + 0.5f) * cell);
            float tile = Math.Min(cell * 0.86f, Math.Min(W * 0.11f * Scale, Well.Height * 0.6f));
            float top = Well.Top + (Well.Height * 0.1f);
            var tileBox = new Box(cx - (tile / 2f), top, cx + (tile / 2f), top + tile);
            var countBox = new Box(cx - (cell / 2f), tileBox.Bottom, cx + (cell / 2f), Well.Bottom - (Well.Height * 0.04f));
            return (tileBox, countBox);
        }
    }

    /// <summary>
    /// The full-screen win celebration (spec 005 FR-023, contracts/look.md §6.3), no top bar: the wooden sign with
    /// flowers, the finished picture large in its stone frame (fit inside <see cref="Picture"/>), the celebrating hero
    /// (or the group) standing on the stone pedestal and overlapping the picture's foot, the light rays behind it, the
    /// reward pill on the pedestal's front with the ×2 offer (<see cref="Double"/>) at its right and a dropped booster
    /// (<see cref="Drop"/>) at its left, and the big Next button at the bottom. Engine-free.
    /// </summary>
    public sealed record WinRegions(
        Box Safe,
        float W,
        Box Sign,
        Box Picture,
        Box Hero,
        Box Pedestal,
        float RaysX,
        float RaysY,
        float RaysRadius,
        Box Reward,
        Box Double,
        Box Drop,
        Box Next)
    {
        /// <summary>Everything a finger can press.</summary>
        public IReadOnlyList<(string Name, Box Box)> Buttons => new[] { ("Pause", Pause), ("Double", Double), ("Next", Next) };

        /// <summary>
        /// The cream Pause button in the top-left corner, beside the sign's end: 0.11 W square, 0.03 W from the left and
        /// 0.015 W under the top inset. The win shows no top bar (FR-023), but Pause stays usable over it (spec 005
        /// FR-016), so Home, Restart and Settings stay reachable.
        /// </summary>
        public Box Pause => new Box(Safe.Left + (0.03f * W), Safe.Top + (0.015f * W), Safe.Left + (0.14f * W), Safe.Top + (0.125f * W));
    }

    /// <summary>
    /// Home in the reference layout (spec 005 FR-024, contracts/look.md §6.4): Settings at the top left, the Petals pill
    /// at the top right, the logo across the top, the diorama in the middle, the side buttons (Wardrobe and Collection at
    /// the left, Daily Challenge and Store at the right, more with <see cref="SideButton"/>) and the rank pill under the
    /// right column, the wooden level plaque, the big Play button, and the milestone teaser with the free booster offer
    /// beside it at the bottom. Every box is laid out; screens draw the ones unlocked. Engine-free.
    /// </summary>
    public sealed record ReferenceHomeRegions(
        Box Safe,
        float W,
        Box Settings,
        Box Petals,
        Box Logo,
        Box Diorama,
        Box Wardrobe,
        Box Collection,
        Box Daily,
        Box Store,
        Box Rank,
        Box Plaque,
        Box Play,
        Box Teaser,
        Box FreeBooster)
    {
        /// <summary>The side buttons' size, as a share of <see cref="W"/>.</summary>
        public const float SideButtonShare = 0.13f;

        /// <summary>The gap between two side buttons of a column, as a share of <see cref="W"/>.</summary>
        public const float SideGapShare = 0.03f;

        /// <summary>The bands in their screen order.</summary>
        public IReadOnlyList<(string Name, Box Box)> Ordered => new[]
        {
            ("Petals", Petals), ("Logo", Logo), ("Plaque", Plaque), ("Play", Play), ("Teaser", Teaser),
        };

        /// <summary>Everything a finger can press.</summary>
        public IReadOnlyList<(string Name, Box Box)> Buttons => new[]
        {
            ("Settings", Settings), ("Petals", Petals), ("Wardrobe", Wardrobe), ("Collection", Collection), ("Daily", Daily),
            ("Store", Store), ("Rank", Rank), ("Play", Play), ("FreeBooster", FreeBooster),
        };

        /// <summary>
        /// The cream round side button <paramref name="index"/> (0 at the top) of the left or the right column: 0.13 W,
        /// 0.04 W from the edge, stacked from 24% of the height with 0.03 W between them.
        /// </summary>
        public Box SideButton(bool right, int index)
        {
            float size = W * SideButtonShare;
            float x = right ? Safe.Right - (W * 0.04f) - size : Safe.Left + (W * 0.04f);
            float y = Wardrobe.Top + (index * (size + (W * SideGapShare)));
            return new Box(x, y, x + size, y + size);
        }
    }

    /// <summary>
    /// The Wardrobe in the reference layout (spec 005 FR-025, contracts/look.md §6.5), both builds: the back button, the
    /// wooden banner and the Petals pill; the hero on its stone pedestal between the ‹ › arrows; the parchment name card
    /// with its name tab, the role line and the description; the family tabs (<see cref="Tab"/>); and the parchment panel
    /// to the bottom of the screen with the optional kind chips, one row of three outfit cards (<see cref="Card"/>) and
    /// the footer between the page arrows. Engine-free.
    /// </summary>
    public sealed record ReferenceWardrobeRegions(
        Box Safe,
        float W,
        Box Back,
        Box Banner,
        Box Petals,
        Box Hero,
        Box Pedestal,
        Box Previous,
        Box Next,
        Box NameCard,
        Box NameTab,
        Box Role,
        Box About,
        Box Tabs,
        Box Panel,
        Box Chips,
        Box Grid,
        Box Footer,
        Box PagePrevious,
        Box PageNext)
    {
        /// <summary>The outfit cards to a row (one row a page).</summary>
        public const int Columns = 3;

        /// <summary>The bands in their screen order.</summary>
        public IReadOnlyList<(string Name, Box Box)> Ordered
        {
            get
            {
                var bands = new List<(string, Box)> { ("Back", Back), ("Hero", Hero), ("NameTab", NameTab), ("Role", Role), ("About", About), ("Tabs", Tabs) };
                if (!Chips.IsEmpty)
                {
                    bands.Add(("Chips", Chips));
                }

                bands.Add(("Grid", Grid));
                bands.Add(("Footer", Footer));
                return bands;
            }
        }

        /// <summary>Everything a finger can press (the tabs and cards excepted).</summary>
        public IReadOnlyList<(string Name, Box Box)> Buttons => new[]
        {
            ("Back", Back), ("Petals", Petals), ("Previous", Previous), ("Next", Next), ("PagePrevious", PagePrevious), ("PageNext", PageNext),
        };

        /// <summary>
        /// Family tab <paramref name="index"/> of <paramref name="count"/> (four families, five with the profile): 0.24 W
        /// wide at most, spread evenly across 0.96 W with 0.01 W between them.
        /// </summary>
        public Box Tab(int index, int count)
        {
            int n = Math.Max(1, count);
            float gap = W * 0.01f;
            float width = Math.Min(W * 0.24f, (Tabs.Width - (gap * (n - 1))) / n);
            float total = (width * n) + (gap * (n - 1));
            float x = Tabs.CenterX - (total / 2f) + (index * (width + gap));
            return new Box(x, Tabs.Top, x + width, Tabs.Bottom);
        }

        /// <summary>Outfit card <paramref name="slot"/> (0–2) of the page: 0.29 W wide, spread evenly across the grid.</summary>
        public Box Card(int slot)
        {
            float width = Math.Min(W * 0.29f, Grid.Width / Columns);
            float gap = (Grid.Width - (width * Columns)) / (Columns - 1);
            float x = Grid.Left + (slot * (width + gap));
            return new Box(x, Grid.Top, x + width, Grid.Bottom);
        }
    }

    /// <content>The reference layouts of spec 005 (contracts/look.md §6).</content>
    public static partial class ScreenLayout
    {
        /// <summary>The safe height to width ratio the reference's screens were measured at (19.5:9 without insets).</summary>
        public const float ReferenceAspect = 2.17f;

        /// <summary>
        /// How much the gameplay tray rows shrink on a screen of this safe shape: <c>clamp((H / W) / 2.17, 0.8, 1)</c>.
        /// </summary>
        public static float ReferenceScale(Box safe) =>
            Math.Max(0.8f, Math.Min(1f, safe.Height / Math.Max(1f, safe.Width) / ReferenceAspect));

        /// <summary>
        /// The reference gameplay layout (contracts/look.md §6.1). <paramref name="entrySides"/> are the level's Garden
        /// Entry sides (a bottom entry takes a 0.17 W strip under the board for its arch, else the strip is 0.04 W);
        /// <paramref name="stackCount"/> Source stacks become one row of decks (two rows when a deck would be narrower than
        /// 0.17 W); <paramref name="slotCount"/> Waiting Slots, the extra slot included, share the slot row. Without
        /// boosters (before they unlock) the booster row and its line collapse and the board takes the room; a Hard or
        /// Super Hard badge (<paramref name="hasBadge"/>) sits under the sign and pushes the board down.
        /// </summary>
        public static ReferenceGameplayRegions ReferenceGameplay(
            float width,
            float height,
            Insets insets,
            IReadOnlyCollection<EntrySide> entrySides,
            int stackCount,
            int slotCount,
            bool hasBoosters = true,
            bool hasBadge = false)
        {
            Box safe = SafeArea(width, height, insets);
            float w = safe.Width;
            float k = ReferenceScale(safe);

            // The top bar under the top inset: Pause at the left, the speed pill at the right, the sign between them.
            float barTop = safe.Top + (0.012f * w);
            var bar = new Box(safe.Left, barTop, safe.Right, barTop + (0.13f * w));
            var pause = new Box(safe.Left + (0.04f * w), bar.Top, safe.Left + (0.17f * w), bar.Bottom);
            Box sign = Box.FromCenter(safe.CenterX, bar.CenterY, 0.42f * w, 0.115f * w);
            Box speed = Box.FromCenter(safe.Right - (0.04f * w) - (0.1f * w), bar.CenterY, 0.2f * w, 0.115f * w);
            Box badge = hasBadge
                ? new Box(safe.CenterX - (0.18f * w), bar.Bottom + (0.004f * w), safe.CenterX + (0.18f * w), bar.Bottom + (0.056f * w))
                : new Box(safe.CenterX, bar.Bottom, safe.CenterX, bar.Bottom);
            float boardTop = (hasBadge ? badge.Bottom : bar.Bottom) + (0.02f * w);

            // The tray, bottom up: its content ends 0.02 W above the bottom inset; the pods, a line, the boosters, a line,
            // the slots and 0.025 W of padding above them.
            int stacks = Math.Max(1, stackCount);
            float gapX = 0.04f * w / 3f;
            float deckWidth = Math.Min(0.23f * w * k, ((0.96f * w) - (gapX * (stacks - 1))) / stacks);
            int podRows = 1;
            int perRow = stacks;
            if (deckWidth < 0.17f * w)
            {
                podRows = 2;
                perRow = (stacks + 1) / 2;
                deckWidth = Math.Min(0.23f * w * k, ((0.96f * w) - (gapX * (perRow - 1))) / perRow);
            }

            float rowGap = 0.015f * w;
            float deckHeight = podRows == 1 ? 0.31f * w * k : 0.26f * w * k;
            float podRowHeight = (deckHeight * podRows) + (rowGap * (podRows - 1));
            float contentBottom = safe.Bottom - (0.02f * w);
            var podRow = new Box(safe.Left + (0.02f * w), contentBottom - podRowHeight, safe.Right - (0.02f * w), contentBottom);
            float separator = 0.04f * w * k;
            float lineHeight = Math.Max(2f, 0.005f * w);
            Box SeparatorAbove(Box row) => Box.FromCenter(safe.CenterX, row.Top - (separator / 2f), 0.92f * w, lineHeight);
            Box separatorBottom = hasBoosters ? SeparatorAbove(podRow) : new Box(safe.CenterX, podRow.Top, safe.CenterX, podRow.Top);
            float boosterBottom = podRow.Top - separator;
            Box boosterRow = hasBoosters
                ? new Box(safe.Left + (0.05f * w), boosterBottom - (0.23f * w * k), safe.Right - (0.05f * w), boosterBottom)
                : new Box(safe.CenterX, podRow.Top, safe.CenterX, podRow.Top);
            Box above = hasBoosters ? boosterRow : podRow;
            Box separatorTop = SeparatorAbove(above);
            float slotBottom = above.Top - separator;
            var slotRow = new Box(safe.Left + (0.04f * w), slotBottom - (0.19f * w * k), safe.Right - (0.04f * w), slotBottom);
            float trayTop = slotRow.Top - (0.025f * w);
            var tray = new Box(0f, trayTop, width, height);
            var trayContent = new Box(safe.Left + (0.035f * w), slotRow.Top, safe.Right - (0.035f * w), contentBottom);

            bool bottomEntry = false;
            foreach (EntrySide side in entrySides)
            {
                bottomEntry |= side == EntrySide.Bottom;
            }

            float strip = (bottomEntry ? 0.17f : 0.04f) * w * k;
            var entryStrip = new Box(safe.Left, trayTop - strip, safe.Right, trayTop);
            float boardWidth = ReferenceGameplayRegions.MaxBoardShare * w;
            var board = new Box(safe.CenterX - (boardWidth / 2f), boardTop, safe.CenterX + (boardWidth / 2f), Math.Max(boardTop, entryStrip.Top));

            // The slots: portrait plates 0.165 W wide spread evenly across 0.92 W (an extra slot narrows them).
            int slots = Math.Max(1, slotCount);
            Box[] slotBoxes = Spread(Box.FromCenter(safe.CenterX, slotRow.CenterY, 0.92f * w, slotRow.Height), slots, 0.165f * w * k, 0.0238f * w);

            // The boosters: four squircles 0.195 W spread evenly across 0.9 W.
            Box[] boosterBoxes = hasBoosters
                ? Spread(Box.FromCenter(safe.CenterX, boosterRow.CenterY, 0.9f * w, 0.195f * w * k), 4, 0.195f * w * k, 0.03f * w)
                : Array.Empty<Box>();

            // The decks: one row (two when narrow), each row spread evenly across 0.96 W, at most 0.06 W apart, centered.
            var decks = new Box[stackCount <= 0 ? 0 : stacks];
            for (int row = 0, i = 0; row < podRows; row++)
            {
                int inRow = Math.Min(perRow, stacks - i);
                float top = podRow.Top + (row * (deckHeight + rowGap));
                Box[] cells = Spread(new Box(safe.CenterX - (0.48f * w), top, safe.CenterX + (0.48f * w), top + deckHeight), inRow, deckWidth, gapX, 0.06f * w);
                for (int c = 0; c < inRow && i < decks.Length; c++, i++)
                {
                    decks[i] = cells[c];
                }
            }

            return new ReferenceGameplayRegions(
                safe, w, k, bar, pause, sign, speed, badge, board, entryStrip, tray, 0.06f * w, trayContent,
                slotRow, slotBoxes, separatorTop, boosterRow, boosterBoxes, separatorBottom, podRow, decks, podRows);
        }

        /// <summary>
        /// <paramref name="count"/> cells of <paramref name="size"/> (narrowed to fit with at least
        /// <paramref name="minGap"/> between them) spread evenly across <paramref name="row"/>: the first and the last
        /// touch the row's ends and the gaps are equal, unless a gap would exceed <paramref name="maxGap"/>; then the cells
        /// sit that far apart, centered. Cells are as tall as the row.
        /// </summary>
        private static Box[] Spread(Box row, int count, float size, float minGap, float maxGap = float.MaxValue)
        {
            var cells = new Box[Math.Max(0, count)];
            if (count <= 0)
            {
                return cells;
            }

            float width = Math.Min(size, (row.Width - (minGap * (count - 1))) / count);
            float gap = count == 1 ? 0f : Math.Max(minGap, Math.Min(maxGap, (row.Width - (width * count)) / (count - 1)));
            float total = (width * count) + (gap * (count - 1));
            float x = row.CenterX - (total / 2f);
            for (int i = 0; i < count; i++)
            {
                cells[i] = new Box(x, row.Top, x + width, row.Bottom);
                x += width + gap;
            }

            return cells;
        }

        /// <summary>
        /// The jam card (contracts/look.md §6.2): 0.92 W wide, centered at 51% of the safe height, its height from its
        /// content; <paramref name="choiceCount"/> choices (the free rescue included) in two columns 0.4 W wide with 0.05 W
        /// between them (an odd last one centered), each a 0.205 W button with its cost pill (0.09 W tall) hanging
        /// 0.075 W under it; the close button only when <paramref name="hasClose"/>. A short screen shrinks every height
        /// and gap together (<see cref="JamCardRegions.Scale"/>).
        /// </summary>
        public static JamCardRegions JamCard(float width, float height, Insets insets, int choiceCount, bool hasClose)
        {
            Box safe = SafeArea(width, height, insets);
            float w = safe.Width;
            int choices = Math.Max(0, choiceCount);
            int rows = (choices + 1) / 2;
            const float top = 0.06f;
            const float title = 0.1f;
            const float subtitle = 0.11f;
            const float well = 0.21f;
            const float button = 0.205f;
            const float hang = 0.075f;
            const float rowGap = 0.03f;
            const float restart = 0.14f;
            float content = top + title + 0.02f + subtitle + 0.035f + well + 0.04f
                + (rows * (button + hang)) + (Math.Max(0, rows - 1) * rowGap) + (rows > 0 ? 0.045f : 0f) + restart + top;
            float room = safe.Height - (0.1f * w);
            float scale = Math.Min(1f, room / (content * w));
            float s = w * scale;

            float cardHeight = content * s;
            float cy = Math.Min(Math.Max(safe.Top + (safe.Height * 0.51f), safe.Top + (0.05f * w) + (cardHeight / 2f)), safe.Bottom - (0.05f * w) - (cardHeight / 2f));
            Box card = Box.FromCenter(safe.CenterX, cy, 0.92f * w, cardHeight);
            float closeSize = 0.13f * w * Math.Max(0.85f, scale);
            Box close = hasClose
                ? Box.FromCenter(card.Right - (0.06f * w), Math.Max(safe.Top + (closeSize / 2f), card.Top + (0.025f * w)), closeSize, closeSize)
                : new Box(card.Right, card.Top, card.Right, card.Top);

            float y = card.Top + (top * s);
            var titleBox = new Box(card.Left + (0.16f * w), y, card.Right - (0.16f * w), y + (title * s));
            y = titleBox.Bottom + (0.02f * s);
            var subtitleBox = new Box(card.Left + (0.06f * w), y, card.Right - (0.06f * w), y + (subtitle * s));
            y = subtitleBox.Bottom + (0.035f * s);
            Box wellBox = new Box(safe.CenterX - (0.4f * w), y, safe.CenterX + (0.4f * w), y + (well * s));
            y = wellBox.Bottom + (0.04f * s);

            var choiceBoxes = new Box[choices];
            var pills = new Box[choices];
            float column = 0.4f * w;
            float gap = 0.05f * w;
            for (int i = 0; i < choices; i++)
            {
                int row = i / 2;
                bool alone = i == choices - 1 && choices % 2 == 1;
                float cx = alone ? safe.CenterX : safe.CenterX + ((i % 2 == 0 ? -1f : 1f) * ((column + gap) / 2f));
                float rowTop = y + (row * (button + hang + rowGap) * s);
                choiceBoxes[i] = new Box(cx - (column / 2f), rowTop, cx + (column / 2f), rowTop + (button * s));
                float pill = 0.09f * s;
                float pillBottom = choiceBoxes[i].Bottom + (hang * s);
                pills[i] = new Box(cx - (0.3f * w / 2f), pillBottom - pill, cx + (0.3f * w / 2f), pillBottom);
            }

            if (rows > 0)
            {
                y += ((rows * (button + hang)) + ((rows - 1) * rowGap) + 0.045f) * s;
            }

            Box restartBox = new Box(safe.CenterX - (0.35f * w), y, safe.CenterX + (0.35f * w), y + (restart * s));
            return new JamCardRegions(safe, w, scale, card, close, titleBox, subtitleBox, wellBox, choiceBoxes, pills, restartBox);
        }

        /// <summary>
        /// The full-screen win (contracts/look.md §6.3), in fractions of the safe height H and width W: the sign
        /// 0.66 W × 0.13 H from 7.5% of H; the picture at most 0.8 W wide from 21.5% to 58% of H; the hero from 50% to 76%
        /// of H (an 8:9 solo picture's box, as wide as the group needs at most 0.8 W); the pedestal 0.8 W wide from 72% to
        /// 83% of H (its top ellipse about 74%); the rays around the hero with a radius of 0.6 W; the reward pill
        /// 0.47 W × 0.09 H from 76% to 85% of H with the ×2 offer at its right and the dropped booster at its left; Next
        /// 0.84 W wide from 86% to 96% of H.
        /// </summary>
        public static WinRegions WinScreen(float width, float height, Insets insets)
        {
            Box safe = SafeArea(width, height, insets);
            float w = safe.Width;
            float h = safe.Height;
            float Y(float share) => safe.Top + (h * share);

            var sign = new Box(safe.CenterX - (0.33f * w), Y(0.075f), safe.CenterX + (0.33f * w), Y(0.205f));
            var picture = new Box(safe.CenterX - (0.4f * w), Y(0.215f), safe.CenterX + (0.4f * w), Y(0.58f));
            float heroHeight = Y(0.76f) - Y(0.5f);
            float heroWidth = Math.Min(0.8f * w, heroHeight * CharacterArt.HeroWidth / CharacterArt.HeroHeight);
            var hero = new Box(safe.CenterX - (heroWidth / 2f), Y(0.5f), safe.CenterX + (heroWidth / 2f), Y(0.76f));
            var pedestal = new Box(safe.CenterX - (0.4f * w), Y(0.72f), safe.CenterX + (0.4f * w), Y(0.83f));
            var reward = new Box(safe.CenterX - (0.235f * w), Y(0.76f), safe.CenterX + (0.235f * w), Y(0.85f));
            float side = Math.Min(0.21f * w, (safe.Right - (0.02f * w)) - (reward.Right + (0.02f * w)));
            float sideHeight = Math.Min(reward.Height, 0.13f * w);
            Box @double = Box.FromCenter(reward.Right + (0.02f * w) + (side / 2f), reward.CenterY, side, sideHeight);
            Box drop = Box.FromCenter(reward.Left - (0.02f * w) - (side / 2f), reward.CenterY, side, sideHeight);
            var next = new Box(safe.CenterX - (0.42f * w), Y(0.86f), safe.CenterX + (0.42f * w), Y(0.96f));
            return new WinRegions(safe, w, sign, picture, hero, pedestal, hero.CenterX, hero.CenterY, 0.6f * w, reward, @double, drop, next);
        }

        /// <summary>
        /// Home in the reference layout (contracts/look.md §6.4), in fractions of the safe height (less
        /// <paramref name="bottomReserve"/>, the playtest's dev row) and width W: Settings 0.13 W at 0.04 W from the left,
        /// the Petals pill 0.38 W × 0.095 W at 0.02 W from the right, both from 2.5% of H; the logo 0.8 W wide from 10% to
        /// 20.5%; the diorama from 22% to 70%; the side buttons from 24%; the rank pill (0.3 W × 0.075 W) under the right
        /// column; the plaque 0.5 W × 0.085 H from 64% to 72.5%; Play 0.85 W wide from 73.5% to 88.5%; the teaser pill
        /// 0.5 W wide from 89.5% to 93.5% with the free booster pill at its right.
        /// </summary>
        public static ReferenceHomeRegions ReferenceHome(float width, float height, Insets insets, float bottomReserve = 0f)
        {
            Box safe = SafeArea(width, height, insets);
            float w = safe.Width;
            float h = Math.Max(1f, safe.Height - Math.Max(0f, bottomReserve));
            float Y(float share) => safe.Top + (h * share);

            float button = ReferenceHomeRegions.SideButtonShare * w;
            var settings = new Box(safe.Left + (0.04f * w), Y(0.025f), safe.Left + (0.04f * w) + button, Y(0.025f) + button);
            var petals = new Box(safe.Right - (0.02f * w) - (0.38f * w), settings.CenterY - (0.0475f * w), safe.Right - (0.02f * w), settings.CenterY + (0.0475f * w));
            var logo = new Box(safe.CenterX - (0.4f * w), Math.Max(Y(0.1f), settings.Bottom + (0.01f * w)), safe.CenterX + (0.4f * w), Y(0.205f));
            var diorama = new Box(safe.Left, Y(0.22f), safe.Right, Y(0.7f));
            float gap = ReferenceHomeRegions.SideGapShare * w;
            Box Side(bool right, int index)
            {
                float x = right ? safe.Right - (0.04f * w) - button : safe.Left + (0.04f * w);
                float y = Math.Max(Y(0.24f), logo.Bottom + (0.02f * w)) + (index * (button + gap));
                return new Box(x, y, x + button, y + button);
            }

            Box wardrobe = Side(false, 0);
            Box collection = Side(false, 1);
            Box daily = Side(true, 0);
            Box store = Side(true, 1);
            var rank = new Box(safe.Right - (0.04f * w) - (0.3f * w), store.Bottom + gap, safe.Right - (0.04f * w), store.Bottom + gap + (0.075f * w));
            var plaque = new Box(safe.CenterX - (0.25f * w), Y(0.64f), safe.CenterX + (0.25f * w), Y(0.725f));
            var play = new Box(safe.CenterX - (0.425f * w), Y(0.735f), safe.CenterX + (0.425f * w), Y(0.885f));
            // The teaser row 0.04 H tall from 89.5%, lowered when needed so the free booster's touch box clears Play.
            float rowHeight = 0.04f * h;
            float touch = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(width, height);
            float rowCenter = Math.Max(Y(0.915f), play.Bottom + (touch / 2f) + 1f);
            var teaser = new Box(safe.CenterX - (0.25f * w), rowCenter - (rowHeight / 2f), safe.CenterX + (0.25f * w), rowCenter + (rowHeight / 2f));
            var free = new Box(teaser.Right + (0.02f * w), teaser.Top, safe.Right - (0.02f * w), teaser.Bottom);
            return new ReferenceHomeRegions(safe, w, settings, petals, logo, diorama, wardrobe, collection, daily, store, rank, plaque, play, teaser, free);
        }

        /// <summary>
        /// The Wardrobe in the reference layout (contracts/look.md §6.5), in fractions of the safe height H and width W:
        /// back 0.12 W at 0.04 W from the left from 2.5% of H; the banner from 24% to 70% of W, from 4.5% to 10% of H;
        /// the Petals pill 0.32 W × 0.08 W at 0.02 W from the right from 3.5%; the hero (an 8:9 box) from 11% to 37% on
        /// the pedestal 0.6 W wide from 35% to 43%; the ‹ › arrows 0.09 W at 8% and 92% of W, 28% of H; the name card
        /// 0.92 W from 42% to 57% with its tab 0.5 W; the tabs from 56% to 68.5%; the panel from 67% to the bottom of the
        /// screen with the kind chips (when <paramref name="hasChips"/>), one row of cards 0.29 W wide and the footer at
        /// 93% between the page arrows.
        /// </summary>
        public static ReferenceWardrobeRegions ReferenceWardrobe(float width, float height, Insets insets, bool hasChips = false)
        {
            Box safe = SafeArea(width, height, insets);
            float w = safe.Width;
            float h = safe.Height;
            float Y(float share) => safe.Top + (h * share);
            float X(float share) => safe.Left + (w * share);

            var back = new Box(X(0.04f), Y(0.025f), X(0.16f), Y(0.025f) + (0.12f * w));
            var banner = new Box(X(0.24f), Y(0.045f), X(0.7f), Y(0.1f));
            var petals = new Box(X(0.66f), Y(0.035f), X(0.98f), Y(0.035f) + (0.08f * w));
            float heroHeight = Y(0.37f) - Y(0.11f);
            float heroWidth = Math.Min(0.8f * w, heroHeight * CharacterArt.HeroWidth / CharacterArt.HeroHeight);
            var hero = new Box(safe.CenterX - (heroWidth / 2f), Y(0.11f), safe.CenterX + (heroWidth / 2f), Y(0.37f));
            var pedestal = new Box(X(0.2f), Y(0.35f), X(0.8f), Y(0.43f));
            float arrow = 0.09f * w;
            Box previous = Box.FromCenter(X(0.08f), Y(0.28f), arrow, arrow);
            Box next = Box.FromCenter(X(0.92f), Y(0.28f), arrow, arrow);
            var nameCard = new Box(X(0.04f), Y(0.42f), X(0.96f), Y(0.57f));
            var nameTab = new Box(X(0.25f), Y(0.42f), X(0.75f), Y(0.475f));
            var role = new Box(X(0.1f), nameTab.Bottom, X(0.9f), Y(0.505f));
            var about = new Box(X(0.1f), role.Bottom, X(0.9f), Y(0.56f));
            var tabs = new Box(X(0.02f), Y(0.56f), X(0.98f), Y(0.685f));
            var panel = new Box(X(0.02f), Y(0.67f), X(0.98f), height);
            Box chips = hasChips ? new Box(X(0.06f), Y(0.695f), X(0.94f), Y(0.74f)) : new Box(safe.CenterX, Y(0.695f), safe.CenterX, Y(0.695f));
            var grid = new Box(X(0.04f), hasChips ? Y(0.75f) : Y(0.7f), X(0.96f), hasChips ? Y(0.895f) : Y(0.88f));
            var footer = new Box(X(0.15f), Y(0.91f), X(0.85f), Y(0.95f));
            Box pagePrevious = Box.FromCenter(X(0.08f), footer.CenterY, arrow, arrow);
            Box pageNext = Box.FromCenter(X(0.92f), footer.CenterY, arrow, arrow);
            return new ReferenceWardrobeRegions(safe, w, back, banner, petals, hero, pedestal, previous, next, nameCard, nameTab, role, about, tabs, panel, chips, grid, footer, pagePrevious, pageNext);
        }
    }
}
