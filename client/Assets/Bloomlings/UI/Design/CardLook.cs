using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// A popup card's pieces (spec 005 FR-045, the owner's mockup of 2026-10-08; contracts/look.md §6.19), shared by the
    /// playtest's <c>Kit.Card</c> and Unity's <c>UiKit.Card</c>: the wooden frame round the cream panel
    /// (<see cref="UiRaster.CardFrame"/>), the title on a wooden sign over the frame's top edge, a pink lotus behind the
    /// sign, the leaves and flower over two corners, and the close button over the top-right corner. Engine-free.
    /// </summary>
    public static class CardLook
    {
        /// <summary>The title sign's height in reference units.</summary>
        public const float SignUnits = 118f;

        /// <summary>The lotus behind the sign, as a share of the sign's height (its side).</summary>
        public const float LotusShare = 2.3f;

        /// <summary>The leaves and flower over the card's corners, their side in reference units.</summary>
        public const float DecorationUnits = 170f;

        /// <summary>
        /// The title sign over the frame's top edge (<paramref name="title"/> is the card's title region): centered, 30 units
        /// above the title's middle, as wide as the title in <paramref name="titleWidth"/> plus 1.4 of its height, at most
        /// 78% of the card.
        /// </summary>
        public static Box Sign(Box card, Box title, float titleWidth, float unit)
        {
            float h = SignUnits * unit;
            float width = Math.Min(card.Width * 0.78f, titleWidth + (h * 1.4f));
            return Box.FromCenter(card.CenterX, title.CenterY - (30f * unit), width, h);
        }

        /// <summary>The lotus behind the sign: a square <see cref="LotusShare"/> of its height, its middle just above the sign's top, so its petals rise above it.</summary>
        public static Box Lotus(Box sign)
        {
            float side = sign.Height * LotusShare;
            return Box.FromCenter(sign.CenterX, sign.Top - (sign.Height * 0.05f), side, side);
        }

        /// <summary>The leaves and flower over the card's top-left and bottom-right corners (the main buttons' sprig, the second turned half way).</summary>
        public static (Box TopLeft, Box BottomRight) Decoration(Box card, float unit)
        {
            float side = DecorationUnits * unit;
            float over = side * 0.28f;
            var topLeft = new Box(card.Left - over, card.Top - over, card.Left - over + side, card.Top - over + side);
            var bottomRight = new Box(card.Right + over - side, card.Bottom + over - side, card.Right + over, card.Bottom + over);
            return (topLeft, bottomRight);
        }

        /// <summary>The card's corner radius for its <paramref name="card"/> box (<c>radius.card</c> of its width, at least <c>radius.card_min</c>).</summary>
        public static float Radius(Box card, float unit) => Math.Max(DesignTokens.Radius.CardMin * unit, card.Width * DesignTokens.Radius.Card);

        /// <summary>
        /// The icon of a Settings row (the owner's mockup of 2026-10-08): the owner's flower for Sound, the leaf for Haptics,
        /// the violet bud for Music (Unity's row, not in the mockup), or, for Fast forward, the raised <c>ui.fast</c> glyph in
        /// <c>medal.gold</c>. <see cref="SettingsIcon.Picture"/> is
        /// an icon picture's name (<see cref="OwnerPictures.VariantIcon"/>), else <see cref="SettingsIcon.Glyph"/> the shape.
        /// </summary>
        public static SettingsIcon SettingsIconOf(string key) => key switch
        {
            "settings.sound" => new SettingsIcon(OwnerPictures.VariantIcon("flower"), null, ShapeLibrary.SymbolId("flower")),
            "settings.haptics" => new SettingsIcon(OwnerPictures.VariantIcon("leaf"), null, ShapeLibrary.SymbolId("leaf")),
            "settings.music" => new SettingsIcon(OwnerPictures.VariantIcon("bud"), null, ShapeLibrary.SymbolId("bud")),
            _ => new SettingsIcon(null, "ui.fast", "ui.fast"),
        };

        /// <summary>A Settings row's icon box: a square 62% of the row's height, its middle 74 units in from its left end.</summary>
        public static Box SettingsIconBox(Box row, float unit) => Box.FromCenter(row.Left + (74f * unit), row.CenterY, row.Height * 0.62f, row.Height * 0.62f);

        /// <summary>Where a Settings row's label starts, after its icon.</summary>
        public static float SettingsLabelLeft(Box row, float unit) => row.Left + (134f * unit);

        /// <summary>A Settings row's label size, as a share of <c>type.button_secondary</c> (the owner's mockup's large letters).</summary>
        public const float SettingsLabelScale = 1.04f;

        /// <summary>The cream panel inside the frame (<see cref="UiRaster.CardFrameBorder"/> in from each side, the frame's front side below).</summary>
        public static Box Panel(Box card)
        {
            float border = UiRaster.CardFrameBorder(card.Width);
            return new Box(card.Left + border, card.Top + border, card.Right - border, card.Bottom - (border * 1.3f));
        }
    }

    /// <summary>A Settings row's icon: the owner's picture, or the raised glyph, and the drawn shape that stands in for the picture.</summary>
    public readonly struct SettingsIcon
    {
        public SettingsIcon(string? picture, string? glyph, string standIn)
        {
            Picture = picture;
            Glyph = glyph;
            StandIn = standIn;
        }

        public string? Picture { get; }

        public string? Glyph { get; }

        public string StandIn { get; }
    }
}
