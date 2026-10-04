using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The reference gameplay screen (spec 005 FR-020, FR-021; contracts/look.md §6.1), top to bottom: the top bar (Pause,
    /// the wooden level sign, the speed pill), the board in its stone border on the lawn, the entry strip (a thin strip of
    /// lawn under the board), and one parchment
    /// tray from there to the bottom of the screen holding the Waiting Slots, the four booster boxes and the Source stacks,
    /// parted by two thin lines. Each stack is a column of pods one after another, never on each other (the owner's
    /// gameplay rule, 2026-10-03): the exposed pod in the top row and the next ones below it, <see cref="PodRows"/> rows
    /// in all (<see cref="Pod"/>, <see cref="Chip"/>). <see cref="W"/> is the safe width in pixels and <see cref="K"/> the factor the tray
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
        IReadOnlyList<Box> Columns,
        int PodRows,
        float FrontHeight,
        float QueueHeight,
        float PodGap)
    {
        /// <summary>The fewest pods of a stack the tray shows (the exposed one and the next two), on shorter screens.</summary>
        public const int MinPodRows = 3;

        /// <summary>The most pods of a stack the tray shows (the exposed one and the next three), from <see cref="FourRowsAspect"/>.</summary>
        public const int MaxPodRows = 4;

        /// <summary>The safe height to width ratio from which the tray shows <see cref="MaxPodRows"/> rows (19.5:9 phones and taller).</summary>
        public const float FourRowsAspect = 1.95f;

        /// <summary>The widest a pod gets, as a share of <see cref="W"/> (times <see cref="K"/>).</summary>
        public const float PodMaxShare = 0.24f;

        /// <summary>The exposed pod's height and a waiting pod's height, as shares of <see cref="W"/> (times <see cref="K"/>).</summary>
        public const float FrontShare = 0.13f;

        public const float QueueShare = 0.1f;

        /// <summary>The gap between two pods of a column, and between two columns, as shares of <see cref="W"/>.</summary>
        public const float PodGapShare = 0.01f;

        public const float ColumnGapShare = 0.016f;
        /// <summary>The widest the stone border's outer box may be, as a share of <see cref="W"/>.</summary>
        public const float MaxBoardShare = 0.86f;

        /// <summary>The entry strip's height, the lawn between the board region and the tray, as a share of <see cref="W"/> (times <see cref="K"/>).</summary>
        public const float EntryStripShare = 0.04f;

        /// <summary>
        /// The lawn from the board's top to the entry strip's bottom across the safe width: the area the board is fitted
        /// into (<see cref="FitBoard"/>).
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

        /// <summary>Whether the tray shows the pod at <paramref name="depth"/> of a stack (0 = exposed).</summary>
        public bool Shows(int depth) => depth >= 0 && depth < PodRows;

        /// <summary>
        /// The box of the pod at <paramref name="depth"/> (0 = exposed) of Source stack <paramref name="stack"/>: the
        /// exposed pod at the column's top, <see cref="FrontHeight"/> tall, and each next one under the one before it,
        /// <see cref="QueueHeight"/> tall, <see cref="PodGap"/> apart, never overlapping. Depths from
        /// <see cref="PodRows"/> on fall under the pod row (the tray does not show them).
        /// </summary>
        public Box Pod(int stack, int depth)
        {
            Box column = Columns[stack];
            if (depth <= 0)
            {
                return new Box(column.Left, column.Top, column.Right, column.Top + FrontHeight);
            }

            float top = column.Top + FrontHeight + PodGap + ((depth - 1) * (QueueHeight + PodGap));
            return new Box(column.Left, top, column.Right, top + QueueHeight);
        }

        /// <summary>The parts of the pod at <paramref name="depth"/> of stack <paramref name="stack"/> (<see cref="PodChip"/>).</summary>
        public PodChip Chip(int stack, int depth) => PodChip.In(Pod(stack, depth));

        /// <summary>A Waiting Slot plate's lip under its face, as a share of the plate's shorter side.</summary>
        public const float SlotLipShare = 0.055f;

        /// <summary>
        /// The sticker tile in a Waiting Slot's <paramref name="plate"/> (both builds; z-tray): the face is the plate less
        /// its lip; the tile is <c>min(0.74 face width, 0.66 face height)</c>, centered across, 8% of the face below its
        /// top, and the count takes the room under it.
        /// </summary>
        public static Box SlotTile(Box plate)
        {
            float lip = Math.Min(plate.Width, plate.Height) * SlotLipShare;
            var face = new Box(plate.Left, plate.Top, plate.Right, plate.Bottom - lip);
            float tile = Math.Min(face.Width * 0.74f, face.Height * 0.66f);
            return Box.FromCenter(face.CenterX, face.Top + (face.Height * 0.08f) + (tile / 2f), tile, tile);
        }

        /// <summary>
        /// The board's layout in these regions: <see cref="BoardLayout.Fit"/> over <see cref="BoardArea"/> (less 0.02 W at
        /// each side), the stone border's outer box at most <see cref="MaxBoardShare"/> × <see cref="W"/> wide. The Garden
        /// Entries take no room: their Bloomlings set off from the border (<see cref="BoardLayout.Door"/>).
        /// </summary>
        public BoardLayout FitBoard(int width, int height) =>
            BoardLayout.Fit(BoardArea.Inset(W * 0.02f, 0f), width, height, W * MaxBoardShare);
    }

    /// <summary>
    /// One pod of the tray's grid (spec 005 FR-021, contracts/look.md §6.1; the owner's choice "E" of 2026-10-03: the icon
    /// first, the count small in a corner): a wooden frame a little wider than tall (<see cref="Aspect"/> of its height,
    /// centered in its place in the column), its panel inside a border of <see cref="Border"/> of its height, the
    /// variant's icon over the panel's middle (<see cref="Icon"/>; the candy tile in <see cref="Tile"/> for a mystery or
    /// locked pod, or without the owner's picture), the count's small outlined digits over the panel's bottom right corner
    /// (<see cref="Count"/>), and the "+N" disc of the pods deeper than the tray shows over the frame's top left corner
    /// (<see cref="Badge"/>). Engine-free.
    /// </summary>
    public sealed record PodChip(Box Frame, Box Inner, Box Tile, Box Icon, Box Count, Box Badge)
    {
        /// <summary>The frame's border, as a share of the pod's height.</summary>
        public const float Border = 0.09f;

        /// <summary>The frame's width, as a multiple of its height (a place wider than that centers it).</summary>
        public const float Aspect = 1.3f;

        /// <summary>The narrowest a pod's place in its column is, as a multiple of its height (the layout keeps it so): the frame and a little air beside it.</summary>
        public const float MinAspect = 1.45f;

        /// <summary>The candy tile's side, as a share of the panel's height (a little over it, into the border).</summary>
        public const float TileShare = 1.04f;

        /// <summary>How far the owner's icon reaches beyond the tile's square on each side, as a share of the pod's height.</summary>
        public const float IconGrow = 0.04f;

        /// <summary>The count's type size, as a share of the pod's height.</summary>
        public const float CountShare = 0.36f;

        /// <summary>How far the count's middle stands in from the panel's bottom right corner, as a share of its type size.</summary>
        public const float CountInset = 0.42f;

        /// <summary>The white outline round the count's digits, in fractions of its type size (it reads over the icon).</summary>
        public const float CountOutlineEm = 0.14f;

        /// <summary>The "+N" disc's size, as a share of the pod's height.</summary>
        public const float BadgeShare = 0.44f;

        /// <summary>How opaque the icon of a waiting pod is (its veiled panel shows through a little).</summary>
        public const float WaitingIconAlpha = 0.7f;

        /// <summary>The parts of a pod in its <paramref name="place"/> (<see cref="ReferenceGameplayRegions.Pod"/>).</summary>
        public static PodChip In(Box place)
        {
            float h = place.Height;
            Box frame = Box.FromCenter(place.CenterX, place.CenterY, Math.Min(place.Width, h * Aspect), h);
            Box inner = frame.Inset(h * Border);
            float side = inner.Height * TileShare;
            Box tile = Box.FromCenter(inner.CenterX, inner.CenterY, side, side);
            Box icon = tile.Inset(-h * IconGrow);
            float size = h * CountShare;
            Box count = Box.FromCenter(inner.Right - (size * CountInset), inner.Bottom - (size * CountInset), inner.Width * 0.5f, size);
            float badge = h * BadgeShare;
            Box badgeBox = Box.FromCenter(frame.Left + (h * 0.12f), frame.Top + (h * 0.12f), badge, badge);
            return new PodChip(frame, inner, tile, icon, count, badgeBox);
        }

        /// <summary>
        /// The count's look (§6.1): <c>ink.brown</c> digits, softer (<c>ink.brown_soft</c> mixed 30% toward
        /// <c>parchment.bottom</c>) on a waiting or locked pod (<paramref name="dim"/>), in a white outline of
        /// <see cref="CountOutlineEm"/>.
        /// </summary>
        public static TextLook CountLook(bool dim)
        {
            Rgba ink = dim ? DesignTokens.Colors.InkBrownSoft.Mix(DesignTokens.Colors.ParchmentBottom, 0.3f) : DesignTokens.Colors.InkBrown;
            return new TextLook(ink, ink, Rgba.White, CountOutlineEm, 0f, 0f);
        }
    }

    /// <summary>
    /// The Petals balance pill's parts (spec 005 §3.4; the owner's note of 2026-10-03: a short amount must not float in
    /// the middle of the pill, and the lotus must not hang off its left end), in both builds (<c>Kit.PetalsPill</c>,
    /// <c>UiKit.PetalsPill</c>): a cream pill as tall as its layout box that fits its content, at most the box's width and
    /// placed in it by <c>align</c> (1: its right end on the box's, as on Home and in the Wardrobe; 0.5: centered); its face
    /// over the lip; the lotus fully inside its left end; the amount left-aligned right after the lotus
    /// (<see cref="Amount"/>, digits <see cref="AmountSize"/> tall); and, while the Store is open, the round green "+" over
    /// its right end (<see cref="Plus"/>, empty without). Engine-free.
    /// </summary>
    public sealed record PetalsPillParts(Box Pill, Box Face, Box Lotus, Box Amount, float AmountSize, Box Plus)
    {
        /// <summary>The cream lip under the face, as a share of the pill's height.</summary>
        public const float LipShare = 0.09f;

        /// <summary>The lotus picture's side, as a share of the pill's height (the owner's picture fills about 94% × 73% of it).</summary>
        public const float LotusShare = 0.92f;

        /// <summary>From the pill's left end to the lotus picture's box, as a share of the pill's height.</summary>
        public const float LotusInset = 0.07f;

        /// <summary>Between the lotus picture's box and the amount, as a share of the pill's height.</summary>
        public const float AmountGap = 0.07f;

        /// <summary>The amount's type size, as a share of the pill's height.</summary>
        public const float AmountShare = 0.5f;

        /// <summary>The cream after the amount when there is no "+", as a share of the pill's height.</summary>
        public const float EndShare = 0.38f;

        /// <summary>The "+" disc's diameter, as a share of the pill's height.</summary>
        public const float PlusShare = 1f;

        /// <summary>How far the "+" reaches beyond the pill's right end, as a share of the pill's height.</summary>
        public const float PlusOut = 0.2f;

        /// <summary>Between the amount and the "+", as a share of the pill's height.</summary>
        public const float PlusGap = 0.1f;

        /// <summary>
        /// The parts in the layout <paramref name="box"/> for an amount <paramref name="amountWidth"/> wide at
        /// <see cref="AmountShare"/> of the box's height (0 when the text engine cannot tell yet: the pill takes the whole
        /// box), with or without the "+". The pill and the part of the "+" beyond its right end stay inside the box,
        /// placed by <paramref name="align"/> (0 left, 0.5 centered, 1 right); an amount too long for that takes the whole
        /// box, the "+" reaching a little beyond it, before its digits would shrink.
        /// </summary>
        public static PetalsPillParts Fit(Box box, float amountWidth, bool plus, float align = 1f)
        {
            float h = box.Height;
            float start = h * (LotusInset + LotusShare + AmountGap);
            float end = h * (plus ? PlusShare - PlusOut + PlusGap : EndShare);
            float reach = plus ? h * PlusOut : 0f;
            float room = Math.Max(0f, box.Width - reach);
            float width = amountWidth > 0f ? Math.Min(box.Width, start + amountWidth + end) : room;
            float left = box.Left + (Math.Max(0f, room - width) * Math.Max(0f, Math.Min(1f, align)));
            var pill = new Box(left, box.Top, left + width, box.Bottom);
            var face = new Box(pill.Left, pill.Top, pill.Right, pill.Bottom - (h * LipShare));
            float lotus = h * LotusShare;
            Box lotusBox = Box.FromCenter(pill.Left + (h * LotusInset) + (lotus / 2f), face.CenterY - (h * 0.01f), lotus, lotus);
            float size = h * AmountShare;
            float amountLeft = pill.Left + start;
            var amount = new Box(amountLeft, face.CenterY - (size / 2f), Math.Max(amountLeft, pill.Right - end), face.CenterY + (size / 2f));
            float disc = h * PlusShare;
            Box plusBox = plus
                ? Box.FromCenter(pill.Right + (h * PlusOut) - (disc / 2f), box.CenterY, disc, disc)
                : new Box(pill.Right, box.CenterY, pill.Right, box.CenterY);
            return new PetalsPillParts(pill, face, lotusBox, amount, size, plusBox);
        }

        /// <summary>
        /// The text the pill's width is measured with: the grouped amount with every digit a zero, so a counting amount
        /// keeps its pill (it changes only with the number of digits).
        /// </summary>
        public static string WidthText(string amount)
        {
            var text = new char[amount.Length];
            for (int i = 0; i < amount.Length; i++)
            {
                text[i] = amount[i] >= '0' && amount[i] <= '9' ? '0' : amount[i];
            }

            return new string(text);
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
    /// Home in the reference layout (spec 005 FR-024, FR-030, contracts/look.md §6.4): Settings at the top left, the Petals
    /// pill at the top right, the logo across the top, the diorama in the middle, the Daily Challenge side button at the
    /// right (more with <see cref="SideButton"/>), the wooden level plaque, the big Play button and the milestone teaser
    /// with the free booster offer beside it, all above the bottom menu (<see cref="NavTop"/>, its top; the Store, the
    /// Wardrobe, the Leaderboard and the Collection are its places since the owner's request of 2026-10-04, so Home has no
    /// side button or rank pill of theirs). Every box is laid out; screens draw the ones unlocked. Engine-free.
    /// </summary>
    public sealed record ReferenceHomeRegions(
        Box Safe,
        float W,
        Box Settings,
        Box Petals,
        Box Logo,
        Box Diorama,
        Box Daily,
        Box Plaque,
        Box Play,
        Box Teaser,
        Box FreeBooster,
        float NavTop)
    {
        /// <summary>The side buttons' size, as a share of <see cref="W"/>.</summary>
        public const float SideButtonShare = 0.13f;

        /// <summary>The gap between two side buttons of a column, as a share of <see cref="W"/>.</summary>
        public const float SideGapShare = 0.03f;

        /// <summary>The size of Play's label as a share of its button's height (the reference's big "PLAY"), both builds.</summary>
        public const float PlayLabelShare = 0.5f;

        /// <summary>Play's height, as a share of the safe height: the reference's, and the least it shrinks to.</summary>
        public const float PlayShare = 0.15f;

        public const float PlayMinShare = 0.11f;

        /// <summary>The level plaque's height and the gap between it and Play, as shares of the safe height.</summary>
        public const float PlaqueShare = 0.085f;

        public const float PlaqueGapShare = 0.01f;

        /// <summary>
        /// The highest the plaque's top goes before Play shrinks, as a share of the safe height (it stood at 0.64 before
        /// the bottom menu): the heroes on the fountain stay in view above it.
        /// </summary>
        public const float PlaqueFloorShare = 0.6f;

        /// <summary>The owner's logo picture's width, as a share of <see cref="W"/> (its letters span about 0.8 W).</summary>
        public const float LogoPictureShare = 0.82f;

        /// <summary>
        /// The box of the owner's logo picture (pictures.md C1, <paramref name="width"/> × <paramref name="height"/> with
        /// transparent margins of about a tenth of its height): sized by width, <see cref="LogoPictureShare"/> of W,
        /// centered on <see cref="Logo"/>, its top no higher than Settings' bottom less its top margin, so the letters
        /// fill the reference's 10% to 20.5% of the height.
        /// </summary>
        public Box LogoPicture(int width = 1200, int height = 440)
        {
            float w = LogoPictureShare * W;
            float h = w * height / Math.Max(1f, width);
            Box box = Box.FromCenter(Logo.CenterX, Logo.CenterY, w, h);
            float top = Settings.Bottom - (0.1f * h);
            return box.Top < top ? box.Offset(0f, top - box.Top) : box;
        }

        /// <summary>The bands in their screen order.</summary>
        public IReadOnlyList<(string Name, Box Box)> Ordered => new[]
        {
            ("Petals", Petals), ("Logo", Logo), ("Plaque", Plaque), ("Play", Play), ("Teaser", Teaser),
        };

        /// <summary>Everything a finger can press (the bottom menu's places are its own, <see cref="BottomNavRegions.Buttons"/>).</summary>
        public IReadOnlyList<(string Name, Box Box)> Buttons => new[]
        {
            ("Settings", Settings), ("Petals", Petals), ("Daily", Daily), ("Play", Play), ("FreeBooster", FreeBooster),
        };

        /// <summary>
        /// The cream round side button <paramref name="index"/> (0 at the top) of the left or the right column: 0.13 W,
        /// 0.04 W from the edge, stacked from 24% of the height with 0.03 W between them (the Daily Challenge is the right
        /// column's first).
        /// </summary>
        public Box SideButton(bool right, int index)
        {
            float size = W * SideButtonShare;
            float x = right ? Safe.Right - (W * 0.04f) - size : Safe.Left + (W * 0.04f);
            float y = Daily.Top + (index * (size + (W * SideGapShare)));
            return new Box(x, y, x + size, y + size);
        }
    }

    /// <summary>
    /// The Wardrobe in the reference layout (spec 005 FR-025, contracts/look.md §6.5), both builds: the back button, the
    /// wooden banner and the Petals pill; the hero on its stone pedestal between the ‹ › arrows; the parchment name card
    /// with its name tab, the role line and the description; the family tabs (<see cref="Tab"/>); and the parchment panel
    /// to the bottom of the screen (behind the bottom menu, FR-030) with the optional kind chips, one row of three outfit
    /// cards (<see cref="Card"/>) and the footer between the page arrows, all above the bottom menu's top
    /// (<see cref="NavTop"/>). Engine-free.
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
        Box PageNext,
        float NavTop)
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

        /// <summary>The header row: the back button, the banner and the Petals pill on one line (<see cref="PageHeader"/>).</summary>
        public PageHeader Header => new PageHeader(Back, Banner, Petals);
    }

    /// <summary>
    /// The header row of a full-screen page (contracts/look.md §6.5, §6.6; the owner's note of 2026-10-04: "the elements
    /// there are not on one line"), shared by the Wardrobe and the Store in both builds (<see cref="ScreenLayout.PageHeader"/>):
    /// the cream round back button at the left, the wooden banner with ivy in the middle and the Petals pill at the right,
    /// all three centered on one line, the back button's middle (<see cref="CenterY"/>). The banner is
    /// <see cref="BannerShare"/> of W tall, so with its ivy clusters it stands about as tall as the back button, and it
    /// spans the room between the back button and the Petals pill's box less its clusters' reach
    /// (<see cref="GardenLook.IvyReach"/>) and <see cref="GapShare"/> on each side, so its leaves touch neither
    /// (<see cref="BannerExtent"/>). Engine-free.
    /// </summary>
    public sealed record PageHeader(Box Back, Box Banner, Box Petals)
    {
        /// <summary>The back button's side, as a share of W (0.04 W from the left).</summary>
        public const float BackShare = 0.12f;

        /// <summary>The back button's top, as a share of the safe height.</summary>
        public const float TopShare = 0.025f;

        /// <summary>The banner's plank height, as a share of W (its ivy clusters are <see cref="GardenLook.IvyShare"/> of it).</summary>
        public const float BannerShare = 0.1f;

        /// <summary>The Petals pill's box, as shares of W: its width (to 0.02 W from the right) and its height.</summary>
        public const float PetalsWidthShare = 0.28f;

        public const float PetalsHeightShare = 0.08f;

        /// <summary>The room kept between the banner's leaves and the back button or the Petals pill, as a share of W.</summary>
        public const float GapShare = 0.005f;

        /// <summary>The one line the three share: the back button's middle.</summary>
        public float CenterY => Back.CenterY;

        /// <summary>The banner with its ivy clusters (<see cref="GardenLook.SignExtent"/>).</summary>
        public Box BannerExtent => GardenLook.SignExtent(Banner, SignDecor.Ivy);

        /// <summary>The whole row: the three and the banner's leaves.</summary>
        public Box Row
        {
            get
            {
                Box extent = BannerExtent;
                return new Box(
                    Math.Min(Back.Left, extent.Left),
                    Math.Min(Math.Min(Back.Top, extent.Top), Petals.Top),
                    Math.Max(Petals.Right, extent.Right),
                    Math.Max(Math.Max(Back.Bottom, extent.Bottom), Petals.Bottom));
            }
        }
    }

    /// <summary>
    /// The Store as a full-screen page (contracts/look.md §6.6; the owner's note of 2026-10-04: "the Store must be a separate
    /// page, not a popup"), both builds, over the Wardrobe's garden: the page header (<see cref="Header"/>: back, the
    /// "Store" banner, the Petals pill); a parchment panel (<see cref="Panel"/>) from under the header to the bottom of
    /// the screen (behind the bottom menu, FR-030; the list ends above its top, <see cref="NavTop"/>) holding the Shop /
    /// Cosmetics tabs (<see cref="Tabs"/>, empty before the cosmetics open), Unity's offline
    /// line (<see cref="Status"/>, empty without it) and the list (<see cref="List"/>): on the Shop tab one row per item
    /// (<see cref="Row"/>, <see cref="RowsPerPage"/>; the rows grow to fill a taller page), on the Cosmetics tab the family tabs over the lighter panel with the
    /// outfit cards (<see cref="OutfitCard"/>, <see cref="OutfitsPerPage"/>); and the footer line between the page arrows at
    /// the list's bottom when the items take more than one page. Engine-free.
    /// </summary>
    public sealed record ReferenceStoreRegions(
        Box Safe,
        float W,
        PageHeader Header,
        Box Panel,
        Box Tabs,
        Box Status,
        Box List,
        Box Footer,
        Box PagePrevious,
        Box PageNext,
        float NavTop)
    {
        /// <summary>
        /// A Shop row's height, as a share of W: at least <see cref="RowShare"/> (how many fit a page; 0.15 W before the
        /// bottom menu, which took the room of a row on 16:9 phones), at most <see cref="RowMaxShare"/> (rows grow to fill a
        /// taller page); and the gap between two rows.
        /// </summary>
        public const float RowShare = 0.135f;

        /// <summary>The row height the row's type sizes are drawn for, as a share of W (taller rows grow their names, shorter ones shrink them).</summary>
        public const float RowTypeShare = 0.15f;

        public const float RowMaxShare = 0.19f;

        public const float RowGapShare = 0.025f;

        /// <summary>The Cosmetics tab's family tabs' height, as a share of W.</summary>
        public const float FamilyTabsShare = 0.21f;

        /// <summary>The outfit cards to a row, and the gap between two cards as a share of W.</summary>
        public const int OutfitColumns = 3;

        public const float OutfitGapShare = 0.02f;

        /// <summary>An outfit card's height (its cost pill's room included) to its width: at least, and at most.</summary>
        public const float OutfitMinAspect = 1.15f;

        public const float OutfitMaxAspect = 1.45f;

        /// <summary>The back button, the banner and the Petals pill (<see cref="PageHeader"/>).</summary>
        public Box Back => Header.Back;

        public Box Banner => Header.Banner;

        public Box Petals => Header.Petals;

        /// <summary>The panel's corner radius (a card's, <c>radius.card</c> of its width, at least <c>radius.card_min</c>).</summary>
        public float PanelRadius(float scale) => Math.Max(DesignTokens.Radius.CardMin * scale, Panel.Width * DesignTokens.Radius.Card);

        /// <summary>How many Shop rows fit in the list, above the footer when <paramref name="footer"/> (at least one).</summary>
        public int RowsFitting(bool footer)
        {
            float row = W * RowShare;
            float gap = W * RowGapShare;
            float bottom = footer ? Footer.Top - gap : List.Bottom;
            return Math.Max(1, (int)Math.Floor((bottom - List.Top + gap + 0.5f) / (row + gap)));
        }

        /// <summary>The Shop rows a page shows for <paramref name="count"/> items: all of them when they fit, else as many as fit above the footer.</summary>
        public int RowsPerPage(int count) => count <= RowsFitting(false) ? Math.Max(1, count) : RowsFitting(true);

        /// <summary>
        /// The Shop rows' height for <paramref name="count"/> items: a page of rows (<see cref="RowsPerPage"/>) filling
        /// the list (above the footer when there are more pages), from <see cref="RowShare"/> to <see cref="RowMaxShare"/> of W.
        /// </summary>
        public float RowHeight(int count)
        {
            int rows = RowsPerPage(count);
            float gap = W * RowGapShare;
            float bottom = count > rows ? Footer.Top - gap : List.Bottom;
            float fill = (bottom - List.Top - (gap * (rows - 1))) / rows;
            return Math.Max(W * RowShare, Math.Min(W * RowMaxShare, fill));
        }

        /// <summary>Shop row <paramref name="slot"/> of a page of <paramref name="count"/> items: <see cref="RowHeight"/> tall across the list, from its top.</summary>
        public Box Row(int slot, int count)
        {
            float row = RowHeight(count);
            float top = List.Top + (slot * (row + (W * RowGapShare)));
            return new Box(List.Left, top, List.Right, top + row);
        }

        /// <summary>The Cosmetics tab's family tabs across the list's top.</summary>
        public Box FamilyTabs => new Box(List.Left, List.Top, List.Right, List.Top + (W * FamilyTabsShare));

        /// <summary>The lighter panel under the family tabs, to the list's bottom (the footer inside it).</summary>
        public Box OutfitPanel => new Box(List.Left, FamilyTabs.Bottom, List.Right, List.Bottom);

        /// <summary>The outfit cards' area: the lighter panel less 0.02 W, above the footer.</summary>
        public Box OutfitGrid
        {
            get
            {
                float inset = W * OutfitGapShare;
                Box panel = OutfitPanel;
                return new Box(panel.Left + inset, panel.Top + inset, panel.Right - inset, Math.Max(panel.Top + inset, Footer.Top - inset));
            }
        }

        /// <summary>The rows of outfit cards a page shows: as many as fit at <see cref="OutfitMinAspect"/> (at least one).</summary>
        public int OutfitRows
        {
            get
            {
                Box grid = OutfitGrid;
                float gap = W * OutfitGapShare;
                float width = OutfitCardWidth;
                return Math.Max(1, (int)Math.Floor((grid.Height + gap) / ((width * OutfitMinAspect) + gap)));
            }
        }

        /// <summary>The outfit cards a page shows.</summary>
        public int OutfitsPerPage => OutfitRows * OutfitColumns;

        private float OutfitCardWidth => (OutfitGrid.Width - (W * OutfitGapShare * (OutfitColumns - 1))) / OutfitColumns;

        /// <summary>
        /// Outfit card <paramref name="slot"/> of the page (its cost pill's room included): three to a row across the grid,
        /// <see cref="OutfitRows"/> rows from its top, each card at most <see cref="OutfitMaxAspect"/> times as tall as wide.
        /// </summary>
        public Box OutfitCard(int slot)
        {
            Box grid = OutfitGrid;
            float gap = W * OutfitGapShare;
            float width = OutfitCardWidth;
            int rows = OutfitRows;
            float height = Math.Min(width * OutfitMaxAspect, (grid.Height - (gap * (rows - 1))) / rows);
            int column = slot % OutfitColumns;
            int row = slot / OutfitColumns;
            float x = grid.Left + (column * (width + gap));
            float y = grid.Top + (row * (height + gap));
            return new Box(x, y, x + width, y + height);
        }

        /// <summary>The bands in their screen order.</summary>
        public IReadOnlyList<(string Name, Box Box)> Ordered
        {
            get
            {
                var bands = new List<(string, Box)> { ("Header", Header.Row) };
                if (!Tabs.IsEmpty)
                {
                    bands.Add(("Tabs", Tabs));
                }

                if (!Status.IsEmpty)
                {
                    bands.Add(("Status", Status));
                }

                bands.Add(("List", new Box(List.Left, List.Top, List.Right, Footer.Top)));
                bands.Add(("Footer", Footer));
                return bands;
            }
        }

        /// <summary>Everything a finger can press (the tabs, rows and cards excepted).</summary>
        public IReadOnlyList<(string Name, Box Box)> Buttons => new[]
        {
            ("Back", Back), ("Petals", Petals), ("PagePrevious", PagePrevious), ("PageNext", PageNext),
        };
    }

    /// <content>The reference layouts of spec 005 (contracts/look.md §6).</content>
    public static partial class ScreenLayout
    {
        /// <summary>The safe height to width ratio the reference's screens were measured at: a 19.5:9 phone less its insets.</summary>
        public const float ReferenceAspect = 2.0f;

        /// <summary>
        /// How much the gameplay tray rows shrink on a screen of this safe shape: <c>clamp((H / W) / 2.0, 0.8, 1)</c>.
        /// </summary>
        public static float ReferenceScale(Box safe) =>
            Math.Max(0.8f, Math.Min(1f, safe.Height / Math.Max(1f, safe.Width) / ReferenceAspect));

        /// <summary>
        /// The reference gameplay layout (contracts/look.md §6.1). The board's lawn ends in the entry strip, 0.04 W of lawn
        /// over the tray whatever the level's Garden Entries (they have no arch since the owner's note of 2026-10-03, so a
        /// bottom entry no longer takes a 0.17 W strip); <paramref name="stackCount"/> Source stacks become columns of pods
        /// (<see cref="ReferenceGameplayRegions.Pod"/>):
        /// four rows from <see cref="ReferenceGameplayRegions.FourRowsAspect"/>, three on shorter screens;
        /// <paramref name="slotCount"/> Waiting Slots, the extra slot included, share the slot row. Without
        /// boosters (before they unlock) the booster row and its line collapse and the board takes the room; a Hard or
        /// Super Hard badge (<paramref name="hasBadge"/>) sits under the sign and pushes the board down.
        /// </summary>
        public static ReferenceGameplayRegions ReferenceGameplay(
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
            // the slots and 0.025 W of padding above them. The pods: one column per stack, the exposed pod on top and the
            // next ones under it, never overlapping (the owner, 2026-10-03), each pod's place at least PodChip.MinAspect
            // times as wide as tall, so its frame (PodChip.Aspect of its height) has its whole width and air beside it.
            int stacks = Math.Max(1, stackCount);
            int podRows = safe.Height / Math.Max(1f, w) >= ReferenceGameplayRegions.FourRowsAspect ? ReferenceGameplayRegions.MaxPodRows : ReferenceGameplayRegions.MinPodRows;
            float columnGap = ReferenceGameplayRegions.ColumnGapShare * w;
            float podWidth = Math.Min(ReferenceGameplayRegions.PodMaxShare * w * k, ((0.96f * w) - (columnGap * (stacks - 1))) / stacks);
            float frontHeight = Math.Min(ReferenceGameplayRegions.FrontShare * w * k, podWidth / PodChip.MinAspect);
            float queueHeight = Math.Min(ReferenceGameplayRegions.QueueShare * w * k, podWidth / PodChip.MinAspect);
            float podGap = ReferenceGameplayRegions.PodGapShare * w * k;
            float podRowHeight = frontHeight + ((podRows - 1) * (queueHeight + podGap));
            float contentBottom = safe.Bottom - (0.02f * w);
            var podRow = new Box(safe.Left + (0.02f * w), contentBottom - podRowHeight, safe.Right - (0.02f * w), contentBottom);
            float separator = 0.03f * w * k;
            float lineHeight = Math.Max(2f, 0.005f * w);
            Box SeparatorAbove(Box row) => Box.FromCenter(safe.CenterX, row.Top - (separator / 2f), 0.92f * w, lineHeight);
            Box separatorBottom = hasBoosters ? SeparatorAbove(podRow) : new Box(safe.CenterX, podRow.Top, safe.CenterX, podRow.Top);
            float boosterBottom = podRow.Top - separator;
            Box boosterRow = hasBoosters
                ? new Box(safe.Left + (0.05f * w), boosterBottom - (0.18f * w * k), safe.Right - (0.05f * w), boosterBottom)
                : new Box(safe.CenterX, podRow.Top, safe.CenterX, podRow.Top);
            Box above = hasBoosters ? boosterRow : podRow;
            Box separatorTop = SeparatorAbove(above);
            float slotBottom = above.Top - separator;
            var slotRow = new Box(safe.Left + (0.04f * w), slotBottom - (0.16f * w * k), safe.Right - (0.04f * w), slotBottom);
            float trayTop = slotRow.Top - (0.025f * w);
            var tray = new Box(0f, trayTop, width, height);
            var trayContent = new Box(safe.Left + (0.035f * w), slotRow.Top, safe.Right - (0.035f * w), contentBottom);

            float strip = ReferenceGameplayRegions.EntryStripShare * w * k;
            var entryStrip = new Box(safe.Left, trayTop - strip, safe.Right, trayTop);
            float boardWidth = ReferenceGameplayRegions.MaxBoardShare * w;
            var board = new Box(safe.CenterX - (boardWidth / 2f), boardTop, safe.CenterX + (boardWidth / 2f), Math.Max(boardTop, entryStrip.Top));

            // The slots: portrait plates 0.14 W wide spread evenly across 0.92 W (an extra slot narrows them).
            int slots = Math.Max(1, slotCount);
            Box[] slotBoxes = Spread(Box.FromCenter(safe.CenterX, slotRow.CenterY, 0.92f * w, slotRow.Height), slots, 0.14f * w * k, 0.0238f * w);

            // The boosters: four squircles 0.16 W spread evenly across 0.9 W.
            Box[] boosterBoxes = hasBoosters
                ? Spread(Box.FromCenter(safe.CenterX, boosterRow.CenterY, 0.9f * w, 0.16f * w * k), 4, 0.16f * w * k, 0.03f * w)
                : Array.Empty<Box>();

            // The columns: spread evenly across 0.96 W, at most 0.04 W apart, centered.
            Box[] columns = stackCount <= 0
                ? Array.Empty<Box>()
                : Spread(new Box(safe.CenterX - (0.48f * w), podRow.Top, safe.CenterX + (0.48f * w), podRow.Bottom), stacks, podWidth, columnGap, 0.04f * w);

            return new ReferenceGameplayRegions(
                safe, w, k, bar, pause, sign, speed, badge, board, entryStrip, tray, 0.06f * w, trayContent,
                slotRow, slotBoxes, separatorTop, boosterRow, boosterBoxes, separatorBottom, podRow, columns, podRows,
                frontHeight, queueHeight, podGap);
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
        /// of H (an 8:9 solo picture's box, as wide as the group needs at most 0.8 W); the pedestal 0.8 W wide from 70.5% to
        /// 81.5% of H (its top ellipse about 73.8%, under the hero's feet at 73.4%); the rays around the hero with a radius of 0.6 W; the reward pill
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
            var pedestal = new Box(safe.CenterX - (0.4f * w), Y(0.705f), safe.CenterX + (0.4f * w), Y(0.815f));
            var reward = new Box(safe.CenterX - (0.235f * w), Y(0.76f), safe.CenterX + (0.235f * w), Y(0.85f));
            float side = Math.Min(0.21f * w, (safe.Right - (0.02f * w)) - (reward.Right + (0.02f * w)));
            float sideHeight = Math.Min(reward.Height, 0.13f * w);
            Box @double = Box.FromCenter(reward.Right + (0.02f * w) + (side / 2f), reward.CenterY, side, sideHeight);
            Box drop = Box.FromCenter(reward.Left - (0.02f * w) - (side / 2f), reward.CenterY, side, sideHeight);
            var next = new Box(safe.CenterX - (0.42f * w), Y(0.86f), safe.CenterX + (0.42f * w), Y(0.96f));
            return new WinRegions(safe, w, sign, picture, hero, pedestal, hero.CenterX, hero.CenterY, 0.6f * w, reward, @double, drop, next);
        }

        /// <summary>
        /// Home in the reference layout (contracts/look.md §6.4), in fractions of the safe height H and width W: Settings
        /// 0.13 W at 0.04 W from the left, the Petals pill 0.38 W × 0.095 W at 0.02 W from the right, both from 2.5% of H;
        /// the logo 0.8 W wide from 10% to 20.5%; the diorama from 22% to 0.06 H under the plaque's top; the Daily
        /// Challenge side button from 24%; then, bottom up from the bottom menu's top (<see cref="BottomNavTop"/>) less
        /// 0.015 W and <paramref name="bottomReserve"/> (the playtest's dev row): the teaser row (the teaser pill 0.5 W ×
        /// 0.04 H with the free booster pill at its right) whose touch boxes end there, Play 0.85 W wide ending 1 px over
        /// them, 0.15 H tall unless the plaque would rise above 60% of H (then shorter, at least 0.11 H and the touch
        /// minimum), and the plaque 0.5 W × 0.085 H 0.01 H over Play.
        /// </summary>
        public static ReferenceHomeRegions ReferenceHome(float width, float height, Insets insets, float bottomReserve = 0f)
        {
            Box safe = SafeArea(width, height, insets);
            float w = safe.Width;
            float h = Math.Max(1f, safe.Height);
            float Y(float share) => safe.Top + (h * share);

            float button = ReferenceHomeRegions.SideButtonShare * w;
            var settings = new Box(safe.Left + (0.04f * w), Y(0.025f), safe.Left + (0.04f * w) + button, Y(0.025f) + button);
            var petals = new Box(safe.Right - (0.02f * w) - (0.38f * w), settings.CenterY - (0.0475f * w), safe.Right - (0.02f * w), settings.CenterY + (0.0475f * w));
            var logo = new Box(safe.CenterX - (0.4f * w), Math.Max(Y(0.1f), settings.Bottom + (0.01f * w)), safe.CenterX + (0.4f * w), Y(0.205f));
            float sideTop = Math.Max(Y(0.24f), logo.Bottom + (0.02f * w));
            var daily = new Box(safe.Right - (0.04f * w) - button, sideTop, safe.Right - (0.04f * w), sideTop + button);

            // Bottom up from the menu's top: the teaser row, whose touch boxes (the free booster's) end at the limit; Play
            // over them; the plaque over Play. Play shrinks before the plaque rises above PlaqueFloorShare of H.
            float navTop = BottomNavTop(width, height, insets);
            float limit = navTop - (Design.BottomNav.GapShare * w) - Math.Max(0f, bottomReserve);
            float touch = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(width, height);
            float rowHeight = 0.04f * h;
            float rowCenter = limit - (touch / 2f);
            var teaser = new Box(safe.CenterX - (0.25f * w), rowCenter - (rowHeight / 2f), safe.CenterX + (0.25f * w), rowCenter + (rowHeight / 2f));
            var free = new Box(teaser.Right + (0.02f * w), teaser.Top, safe.Right - (0.02f * w), teaser.Bottom);
            float playBottom = rowCenter - (touch / 2f) - 1f;
            float plaqueHeight = ReferenceHomeRegions.PlaqueShare * h;
            float gap = ReferenceHomeRegions.PlaqueGapShare * h;
            float room = playBottom - gap - plaqueHeight - Y(ReferenceHomeRegions.PlaqueFloorShare);
            float playHeight = Math.Min(ReferenceHomeRegions.PlayShare * h, Math.Max(Math.Max(ReferenceHomeRegions.PlayMinShare * h, touch), room));
            var play = new Box(safe.CenterX - (0.425f * w), playBottom - playHeight, safe.CenterX + (0.425f * w), playBottom);
            var plaque = new Box(safe.CenterX - (0.25f * w), play.Top - gap - plaqueHeight, safe.CenterX + (0.25f * w), play.Top - gap);
            var diorama = new Box(safe.Left, Y(0.22f), safe.Right, Y(0.7f));
            return new ReferenceHomeRegions(safe, w, settings, petals, logo, diorama, daily, plaque, play, teaser, free, navTop);
        }

        /// <summary>
        /// The header row of a full-screen page (contracts/look.md §6.5, §6.6; <see cref="Design.PageHeader"/>), in
        /// fractions of the safe height H and width W: the back button 0.12 W at 0.04 W from the left, its top at 2.5% of H;
        /// on its middle line the Petals pill's box 0.28 W × 0.08 W at 0.02 W from the right (the pill fits its amount at
        /// the box's right end) and the banner 0.1 W tall, from the back button's right plus 0.005 W and its left ivy
        /// cluster's reach (0.665 of its height) to the Petals box's left less the same (on every phone from about 23% to
        /// 63% of W).
        /// </summary>
        public static PageHeader PageHeader(Box safe)
        {
            float w = safe.Width;
            float side = Design.PageHeader.BackShare * w;
            float cy = safe.Top + (safe.Height * Design.PageHeader.TopShare) + (side / 2f);
            Box back = Box.FromCenter(safe.Left + (0.04f * w) + (side / 2f), cy, side, side);
            float pill = Design.PageHeader.PetalsHeightShare * w;
            var petals = new Box(safe.Right - (0.02f * w) - (Design.PageHeader.PetalsWidthShare * w), cy - (pill / 2f), safe.Right - (0.02f * w), cy + (pill / 2f));
            float plank = Design.PageHeader.BannerShare * w;
            float room = (Design.PageHeader.GapShare * w) + (GardenLook.IvyReach * plank);
            var banner = new Box(back.Right + room, cy - (plank / 2f), petals.Left - room, cy + (plank / 2f));
            return new PageHeader(back, banner, petals);
        }

        /// <summary>
        /// The Wardrobe in the reference layout (contracts/look.md §6.5), in fractions of the page's height H and the safe
        /// width W: the page header (<see cref="PageHeader(Box)"/>: back, banner and Petals pill on the back button's middle
        /// line, from the safe area's own height); the hero (an 8:9 box) from 11% to 37% on
        /// the pedestal 0.6 W wide from 35% to 43%; the ‹ › arrows 0.09 W at 8% and 92% of W, 28% of H; the name card
        /// 0.92 W from 42% to 57% with its tab 0.5 W; the tabs from 56% to 68.5%; the panel from 67% to the bottom of the
        /// screen with the kind chips (when <paramref name="hasChips"/>), one row of cards 0.29 W wide and the footer at
        /// 93% between the page arrows. H is the safe height shortened so the page arrows' touch boxes (centered on the
        /// footer, the touch minimum tall) end on the bottom menu's top (<see cref="BottomNavTop"/>, FR-030).
        /// </summary>
        public static ReferenceWardrobeRegions ReferenceWardrobe(float width, float height, Insets insets, bool hasChips = false)
        {
            Box safe = SafeArea(width, height, insets);
            float w = safe.Width;
            float navTop = BottomNavTop(width, height, insets);
            float touch = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(width, height);
            float h = Math.Max(1f, Math.Min(safe.Height, (navTop - (touch / 2f) - safe.Top) / 0.93f));
            float Y(float share) => safe.Top + (h * share);
            float X(float share) => safe.Left + (w * share);

            PageHeader header = PageHeader(safe);
            Box back = header.Back;
            Box banner = header.Banner;
            Box petals = header.Petals;
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
            return new ReferenceWardrobeRegions(safe, w, back, banner, petals, hero, pedestal, previous, next, nameCard, nameTab, role, about, tabs, panel, chips, grid, footer, pagePrevious, pageNext, navTop);
        }

        /// <summary>
        /// The Store as a page (contracts/look.md §6.6), in fractions of the safe width W: the page header
        /// (<see cref="PageHeader(Box)"/>); the parchment panel 0.96 W wide from 0.03 W under the header row to the bottom
        /// of the screen; in it, from 0.045 W under its top, the Shop / Cosmetics tabs 0.8 W × 0.1 W (when
        /// <paramref name="hasCosmetics"/>), Unity's offline line 0.05 W tall 0.02 W under them (when
        /// <paramref name="hasStatus"/>), then the list 0.88 W wide from 0.04 W under the tabs (0.02 W under the line) to
        /// 0.04 W over the safe bottom, with the footer line (0.84 W, 0.12 W or the touch minimum tall) 0.02 W over the
        /// list's bottom and the page arrows 0.09 W at its ends. The list ends 0.02 W over the bottom menu's top
        /// (<see cref="BottomNavTop"/>, FR-030; 0.04 W over the safe bottom before it).
        /// </summary>
        public static ReferenceStoreRegions ReferenceStore(float width, float height, Insets insets, bool hasCosmetics = true, bool hasStatus = false)
        {
            Box safe = SafeArea(width, height, insets);
            float w = safe.Width;
            float X(float share) => safe.Left + (w * share);

            PageHeader header = PageHeader(safe);
            var panel = new Box(X(0.02f), header.Row.Bottom + (0.03f * w), X(0.98f), height);
            float y = panel.Top + (0.045f * w);
            var tabs = new Box(safe.CenterX, y, safe.CenterX, y);
            if (hasCosmetics)
            {
                tabs = new Box(X(0.1f), y, X(0.9f), y + (0.1f * w));
                y = tabs.Bottom + (0.04f * w);
            }

            var status = new Box(safe.CenterX, y, safe.CenterX, y);
            if (hasStatus)
            {
                status = new Box(X(0.06f), y - (0.02f * w), X(0.94f), y + (0.03f * w));
                y = status.Bottom + (0.02f * w);
            }

            float navTop = BottomNavTop(width, height, insets);
            var list = new Box(X(0.06f), y, X(0.94f), Math.Max(y + 1f, navTop - (0.02f * w)));
            float touch = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(width, height);
            float line = Math.Max(0.12f * w, touch);
            var footer = new Box(X(0.08f), list.Bottom - (0.02f * w) - line, X(0.92f), list.Bottom - (0.02f * w));
            float arrow = 0.09f * w;
            Box pagePrevious = Box.FromCenter(footer.Left + (line / 2f), footer.CenterY, arrow, arrow);
            Box pageNext = Box.FromCenter(footer.Right - (line / 2f), footer.CenterY, arrow, arrow);
            return new ReferenceStoreRegions(safe, w, header, panel, tabs, status, list, footer, pagePrevious, pageNext, navTop);
        }
    }
}
