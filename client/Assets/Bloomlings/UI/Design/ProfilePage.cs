using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The profile page (spec 005 FR-037, after the reference game's profile, in the garden look), both builds: the page
    /// header (<see cref="Header"/>: back, the "Profile" banner, the Petals pill), the parchment panel (<see cref="Panel"/>)
    /// to the bottom of the screen, and in it, top to bottom:
    /// <list type="bullet">
    /// <item><description>the player's card (<see cref="Card"/>, a cream row): the round avatar in its frame
    /// (<see cref="Avatar"/>; a tap opens the edit card), the name (<see cref="Name"/>) with the pencil
    /// (<see cref="Edit"/>), the short ID (<see cref="Id"/>), the joining month (<see cref="Joined"/>) and the wooden
    /// "Level N" plaque (<see cref="Plaque"/>);</description></item>
    /// <item><description>three stat cells (<see cref="Stats"/>: levels won, pictures collected, milestones);</description></item>
    /// <item><description>the Achievements title (<see cref="AchievementsTitle"/>), its tiles
    /// (<see cref="Achievements"/>: a well with the trophy and the count, the name under it) and their note
    /// (<see cref="AchievementsNote"/>).</description></item>
    /// </list>
    /// The page has no bottom menu (it is not a menu place: Home's avatar opens it, back returns Home). Everything is in
    /// shares of the safe width W; on a page too short for it all, the content below the header shrinks evenly to fit.
    /// Engine-free.
    /// </summary>
    public sealed record ReferenceProfileRegions(
        Box Safe,
        float W,
        PageHeader Header,
        Box Panel,
        Box Card,
        Box Avatar,
        Box Name,
        Box Edit,
        Box Id,
        Box Joined,
        Box Plaque,
        IReadOnlyList<Box> Stats,
        Box AchievementsTitle,
        IReadOnlyList<Box> Achievements,
        Box AchievementsNote)
    {
        /// <summary>The number of achievement tiles (<c>Achievements.All</c>).</summary>
        public const int AchievementCount = 3;

        /// <summary>The panel's corner radius (a card's, <c>radius.card</c> of its width, at least <c>radius.card_min</c>).</summary>
        public float PanelRadius(float scale) => Math.Max(DesignTokens.Radius.CardMin * scale, Panel.Width * DesignTokens.Radius.Card);

        /// <summary>A stat cell's value line (its upper 58%).</summary>
        public static Box StatValue(Box cell) => new Box(cell.Left, cell.Top + (cell.Height * 0.08f), cell.Right, cell.Top + (cell.Height * 0.6f));

        /// <summary>A stat cell's label line (its lower part).</summary>
        public static Box StatLabel(Box cell) => new Box(cell.Left + (cell.Width * 0.05f), cell.Top + (cell.Height * 0.6f), cell.Right - (cell.Width * 0.05f), cell.Bottom - (cell.Height * 0.08f));

        /// <summary>An achievement tile's well (the square above its label).</summary>
        public static Box AchievementWell(Box tile) => new Box(tile.Left, tile.Top, tile.Right, tile.Top + tile.Width);

        /// <summary>An achievement well's trophy: half the well, its middle 42% down.</summary>
        public static Box AchievementTrophy(Box well) => Box.FromCenter(well.CenterX, well.Top + (well.Height * 0.42f), well.Width * 0.5f, well.Height * 0.5f);

        /// <summary>An achievement well's count line, along its bottom (under the trophy, left of the badge's middle).</summary>
        public static Box AchievementProgress(Box well) => new Box(well.Left + (well.Width * 0.06f), well.Top + (well.Height * 0.7f), well.Right - (well.Width * 0.2f), well.Bottom - (well.Height * 0.04f));

        /// <summary>An achievement tile's label (under its well).</summary>
        public static Box AchievementLabel(Box tile) => new Box(tile.Left - (tile.Width * 0.1f), tile.Top + tile.Width, tile.Right + (tile.Width * 0.1f), tile.Bottom);
    }

    /// <summary>The achievements' look (spec 005 FR-037 as amended on 2026-10-06; contracts/look.md §6.11).</summary>
    public static class AchievementLook
    {
        /// <summary>A tier's trophy color: the medals' bronze, silver and gold; null before the first tier.</summary>
        public static Rgba? TierColor(int tier) => tier switch
        {
            <= 0 => null,
            1 => DesignTokens.Colors.MedalBronze,
            2 => DesignTokens.Colors.MedalSilver,
            _ => DesignTokens.Colors.MedalGold,
        };
    }

    /// <summary>
    /// The "Edit profile" card (spec 005 FR-037), both builds: a centered card (<see cref="ScreenLayout.Card"/>) with the
    /// title and close, then the preview (<see cref="Preview"/>: the picked avatar in the picked frame and badge, the
    /// name beside it in <see cref="PreviewName"/>), the tabs (<see cref="Tabs"/>: Avatar, Frame, Badge, Name), the tab's
    /// grid (<see cref="Grid"/>, <see cref="Columns"/> cells a row: <see cref="Cell"/>) and the main button
    /// (<see cref="Button"/>: "Save", or "Buy" with the price). The Name tab shows the name's field
    /// (<see cref="NameField"/>) and its "Change name" button (<see cref="NameButton"/>) in the grid's place, and an
    /// empty or locked tab shows its note in <see cref="Note"/>. Engine-free.
    /// </summary>
    public sealed record ProfileEditRegions(CardRegions Card, Box Preview, Box PreviewName, Box Tabs, Box Grid, Box Button, float CellSize, float Gap)
    {
        /// <summary>Cells in a grid row.</summary>
        public const int Columns = 4;

        /// <summary>The rows the grid holds: the 14 avatars.</summary>
        public const int Rows = 4;

        /// <summary>The card's content height in reference units (title and close excluded), for <see cref="ScreenLayout.Card"/>.</summary>
        public const float ContentUnits = TabsUnits + GapUnits + PreviewUnits + GapUnits + (Rows * CellUnits) + ((Rows - 1) * CellGapUnits) + ButtonGapUnits + DesignTokens.Size.CardPrimaryHeight;

        internal const float TabsUnits = 104f;
        internal const float PreviewUnits = 190f;
        internal const float GapUnits = 22f;
        internal const float CellUnits = 196f;
        internal const float CellGapUnits = 14f;
        internal const float ButtonGapUnits = 30f;

        /// <summary>The cell of <paramref name="index"/> in the grid, rows from the top, centered across.</summary>
        public Box Cell(int index)
        {
            int row = index / Columns;
            int column = index % Columns;
            float width = (Columns * CellSize) + ((Columns - 1) * Gap);
            float left = Grid.CenterX - (width / 2f) + (column * (CellSize + Gap));
            float top = Grid.Top + (row * (CellSize + Gap));
            return new Box(left, top, left + CellSize, top + CellSize);
        }

        /// <summary>The round picture in a cell (its upper 84%, centered across).</summary>
        public static Box CellPicture(Box cell) => Box.FromCenter(cell.CenterX, cell.Top + (cell.Height * 0.43f), cell.Width * 0.8f, cell.Width * 0.8f);

        /// <summary>A cell's price pill, over the picture's foot.</summary>
        public static Box CellPrice(Box cell) => Box.FromCenter(cell.CenterX, cell.Bottom - (cell.Height * 0.12f), cell.Width * 0.84f, cell.Height * 0.24f);

        /// <summary>A cell's check badge (the picked one), on the picture's upper right.</summary>
        public static Box CellCheck(Box cell) => Box.FromCenter(cell.Right - (cell.Width * 0.16f), cell.Top + (cell.Height * 0.14f), cell.Width * 0.3f, cell.Width * 0.3f);

        /// <summary>The Name tab's field: a parchment well across the grid's top.</summary>
        public Box NameField => new Box(Grid.Left + (Grid.Width * 0.04f), Grid.Top + (CellSize * 0.2f), Grid.Right - (Grid.Width * 0.04f), Grid.Top + (CellSize * 0.75f));

        /// <summary>The Name tab's "Change name" button, under the field.</summary>
        public Box NameButton => Box.FromCenter(Grid.CenterX, NameField.Bottom + (CellSize * 0.6f), Grid.Width * 0.6f, CellSize * 0.7f);

        /// <summary>The Name tab's hint (the rules: up to 16 letters and digits), under the button.</summary>
        public Box NameHint => new Box(Grid.Left, NameButton.Bottom + (CellSize * 0.14f), Grid.Right, NameButton.Bottom + (CellSize * 0.44f));

        /// <summary>An empty or locked tab's note, in the grid's middle.</summary>
        public Box Note => Box.FromCenter(Grid.CenterX, Grid.Top + (Grid.Height * 0.3f), Grid.Width * 0.92f, CellSize * 0.5f);
    }

    /// <content>The profile page and its edit card (spec 005 FR-037).</content>
    public static partial class ScreenLayout
    {
        /// <summary>
        /// The profile page (<see cref="ReferenceProfileRegions"/>), in shares of the safe width W: the Store page's
        /// header and panel (<see cref="ReferenceStore"/>), then, from 0.045 W under the panel's top, 0.88 W wide: the card
        /// 0.55 W tall (the avatar 0.3 W at 0.05 W in; beside it, 0.05 W on, the name 0.09 W tall with the pencil 0.11 W
        /// at its end, the ID and the joining month 0.06 W each; the plaque 0.5 W × 0.11 W, 0.04 W under the avatar), the
        /// stat cells 0.2 W tall 0.04 W lower (three, 0.03 W apart), the Achievements title 0.08 W tall 0.05 W lower, the
        /// tiles 0.03 W lower (three squares 0.2 W with a 0.06 W label, 0.07 W apart) and their note 0.06 W tall 0.02 W
        /// lower. The content ends at least 0.04 W over the safe bottom; it shrinks evenly when it would not.
        /// </summary>
        public static ReferenceProfileRegions ReferenceProfile(float width, float height, Insets insets)
        {
            ReferenceStoreRegions store = ReferenceStore(width, height, insets, hasCosmetics: false, hasStatus: false);
            Box safe = store.Safe;
            float w = store.W;
            float top = store.Panel.Top + (0.045f * w);
            const float content = 0.55f + 0.04f + 0.2f + 0.05f + 0.08f + 0.03f + 0.26f + 0.02f + 0.06f;
            float room = safe.Bottom - (0.04f * w) - top;
            float k = Math.Min(1f, room / (content * w));
            float u = w * k;
            float left = safe.CenterX - (0.44f * u);
            float right = safe.CenterX + (0.44f * u);

            var card = new Box(left, top, right, top + (0.55f * u));
            var avatar = new Box(card.Left + (0.05f * u), card.Top + (0.05f * u), card.Left + (0.35f * u), card.Top + (0.35f * u));
            float column = avatar.Right + (0.05f * u);
            float edit = 0.11f * u;
            var name = new Box(column, avatar.Top + (0.02f * u), card.Right - (0.04f * u) - edit - (0.02f * u), avatar.Top + (0.11f * u));
            Box editBox = Box.FromCenter(card.Right - (0.04f * u) - (edit / 2f), name.CenterY, edit, edit);
            var id = new Box(column, name.Bottom + (0.03f * u), card.Right - (0.04f * u), name.Bottom + (0.09f * u));
            var joined = new Box(column, id.Bottom + (0.01f * u), card.Right - (0.04f * u), id.Bottom + (0.07f * u));
            Box plaque = Box.FromCenter(card.CenterX, avatar.Bottom + (0.04f * u) + (0.055f * u), 0.5f * u, 0.11f * u);

            float statsTop = card.Bottom + (0.04f * u);
            float cell = ((right - left) - (2f * 0.03f * u)) / 3f;
            var stats = new Box[3];
            for (int i = 0; i < 3; i++)
            {
                float x = left + (i * (cell + (0.03f * u)));
                stats[i] = new Box(x, statsTop, x + cell, statsTop + (0.2f * u));
            }

            var title = new Box(left, stats[0].Bottom + (0.05f * u), right, stats[0].Bottom + (0.13f * u));
            float tile = 0.2f * u;
            float tileGap = 0.07f * u;
            float tilesWidth = (ReferenceProfileRegions.AchievementCount * tile) + ((ReferenceProfileRegions.AchievementCount - 1) * tileGap);
            float tilesTop = title.Bottom + (0.03f * u);
            var tiles = new Box[ReferenceProfileRegions.AchievementCount];
            for (int i = 0; i < tiles.Length; i++)
            {
                float x = safe.CenterX - (tilesWidth / 2f) + (i * (tile + tileGap));
                tiles[i] = new Box(x, tilesTop, x + tile, tilesTop + tile + (0.06f * u));
            }

            var note = new Box(left, tiles[0].Bottom + (0.02f * u), right, tiles[0].Bottom + (0.08f * u));
            return new ReferenceProfileRegions(safe, w, store.Header, store.Panel, card, avatar, name, editBox, id, joined, plaque, stats, title, tiles, note);
        }

        /// <summary>
        /// The edit card (<see cref="ProfileEditRegions"/>), in reference units under the card's title: the tabs
        /// <c>104</c> tall, the preview <c>190</c> tall <c>22</c> lower (the avatar a circle of its height at the body's
        /// left, the name beside it), the grid <c>22</c> lower with <see cref="ProfileEditRegions.Rows"/> rows of
        /// <see cref="ProfileEditRegions.Columns"/> cells (<c>196</c> square, <c>14</c> apart; narrower to fit the
        /// body's width, shorter to fit a capped card), and the main button (<c>card.primary_height</c>) <c>30</c> under
        /// the grid at the body's bottom.
        /// </summary>
        public static ProfileEditRegions ProfileEdit(float width, float height, Insets insets)
        {
            CardRegions card = Card(width, height, insets, ProfileEditRegions.ContentUnits);
            float u = DesignTokens.ScaleFor(width, height);
            Box body = card.Body;
            var tabs = new Box(body.Left, body.Top, body.Right, body.Top + (ProfileEditRegions.TabsUnits * u));
            float previewTop = tabs.Bottom + (ProfileEditRegions.GapUnits * u);
            float previewSize = ProfileEditRegions.PreviewUnits * u;
            var preview = new Box(body.Left + (body.Width * 0.06f), previewTop, body.Left + (body.Width * 0.06f) + previewSize, previewTop + previewSize);
            var previewName = new Box(preview.Right + (body.Width * 0.06f), preview.Top + (previewSize * 0.2f), body.Right - (body.Width * 0.04f), preview.Bottom - (previewSize * 0.2f));
            Box button = CardButton(body, body.Bottom - (DesignTokens.Size.CardPrimaryHeight * u), true, u);
            float gridTop = preview.Bottom + (ProfileEditRegions.GapUnits * u);
            var grid = new Box(body.Left, gridTop, body.Right, Math.Max(gridTop + 1f, button.Top - (ProfileEditRegions.ButtonGapUnits * u)));
            float gap = ProfileEditRegions.CellGapUnits * u;
            float byWidth = (grid.Width - ((ProfileEditRegions.Columns - 1) * gap)) / ProfileEditRegions.Columns;
            float byHeight = (grid.Height - ((ProfileEditRegions.Rows - 1) * gap)) / ProfileEditRegions.Rows;
            float cell = Math.Min(ProfileEditRegions.CellUnits * u, Math.Min(byWidth, byHeight));
            return new ProfileEditRegions(card, preview, previewName, tabs, grid, button, cell, gap);
        }
    }
}
