using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Shared popup heading and settings-row geometry. Header decoration is optional; there is no lotus crown.</summary>
    public static class CardLook
    {
        /// <summary>The title sign's height in reference units.</summary>
        public const float SignUnits = 118f;

        /// <summary>The lotus behind the sign, as a share of the sign's height (its side).</summary>
        public const float LotusShare = 2.3f;

        /// <summary>
        /// The title sign over the frame's top edge (<paramref name="title"/> is the card's title region): centered, 30 units
        /// above the title's middle, as wide as the title in <paramref name="titleWidth"/> plus 1.4 of its height, at most
        /// 78% of the card.
        /// </summary>
        public static Box Sign(Box card, Box title, float titleWidth, float unit)
        {
            float h = SignUnits * unit;
            float width = Math.Min(card.Width * 0.80f, Math.Max(card.Width*.62f,titleWidth + (h * 1.4f)));
            return Box.FromCenter(card.CenterX, title.CenterY - (30f * unit), width, h);
        }

        /// <summary>The lotus behind the sign: a square <see cref="LotusShare"/> of its height, its middle just above the sign's top, so its petals rise above it.</summary>
        public static Box Lotus(Box sign)
        {
            float side = sign.Height * LotusShare;
            return Box.FromCenter(sign.CenterX, sign.Top - (sign.Height * 0.05f), side, side);
        }

        /// <summary>The card's corner radius for its <paramref name="card"/> box (<c>radius.card</c> of its width, at least <c>radius.card_min</c>).</summary>
        public static float Radius(Box card, float unit) => Math.Max(DesignTokens.Radius.CardMin * unit, card.Width * DesignTokens.Radius.Card);

        /// <summary>
        /// Settings symbols: speaker for Sound, vibrating phone for Haptics, a heart, a diamond and a triangle for the symbols
        /// mode (spec 006 FR-047), with drawn fallbacks.
        /// </summary>
        public static SettingsIcon SettingsIconOf(string key) => key switch
        {
            "settings.sound" => new SettingsIcon(PopupDesign.SoundIcon,null,"ui.sound"),
            "settings.haptics" => new SettingsIcon(PopupDesign.HapticsIcon,null,"ui.haptics"),
            "settings.music" => new SettingsIcon(OwnerPictures.VariantIcon("bud"), null, ShapeLibrary.SymbolId("bud")),
            "settings.symbols" => new SettingsIcon(null, "ui.symbols", "ui.symbols"),
            _ => new SettingsIcon(null, "ui.fast", "ui.fast"),
        };

        /// <summary>A Settings row's icon box: a square 62% of the row's height, its middle 74 units in from its left end.</summary>
        public static Box SettingsIconBox(Box row, float unit) => Box.FromCenter(row.Left + (74f * unit), row.CenterY, row.Height * 0.62f, row.Height * 0.62f);

        /// <summary>Where a Settings row's label starts, after its icon.</summary>
        public static float SettingsLabelLeft(Box row, float unit) => row.Left + (134f * unit);

        /// <summary>A Settings row's label size, as a share of <c>type.button_secondary</c> (the owner's mockup's large letters).</summary>
        public const float SettingsLabelScale = 1.04f;

        /// <summary>
        /// A Settings row's switch (spec 005 FR-048, the owner's mockup): 160 × 82 units, about two thirds of the row's
        /// height as in the mockup, its middle 116 units in from the row's right end.
        /// </summary>
        public static Box SettingsToggleBox(Box row, float unit) => Box.FromCenter(row.Right - (116f * unit), row.CenterY, 160f * unit, 82f * unit);

        /// <summary>Where a Settings row's label must end, clear of its switch.</summary>
        public static float SettingsLabelRight(Box row, float unit) => row.Right - (214f * unit);

        /// <summary>
        /// A section title's wooden sign (spec 005 FR-047, the profile's "Achievements" after the owner's references of
        /// 2026-10-08; plain since FR-049): centered in <paramref name="title"/>, 1.1 times its height, as wide as the text in
        /// <paramref name="textWidth"/> plus twice the sign's height (a sign's height of wood at each end), at most 80% of the
        /// title.
        /// </summary>
        public static Box TitleSign(Box title, float textWidth)
        {
            float h = title.Height * 1.1f;
            return Box.FromCenter(title.CenterX, title.CenterY, Math.Min(title.Width * 0.8f, textWidth + (h * 2f)), h);
        }

        /// <summary>
        /// The top of a tile's face in its rim (spec 005 FR-047, <see cref="GardenLook.TileRimShare"/>): the raised face's
        /// box less its front side, where the tile's content goes.
        /// </summary>
        public static Box TileTop(Box tile)
        {
            Box face = UiRaster.RaisedFaceBox(tile, GardenLook.TileRimShare);
            return new Box(face.Left, face.Top, face.Right, face.Bottom - (Math.Min(tile.Width, tile.Height) * UiRaster.FaceSide));
        }

        /// <summary>The flowers over the pages' frame top corners (spec 005 FR-047), their side in reference units.</summary>
        public const float PageFlowerUnits = 170f;

        /// <summary>The flowers on the bottom menu's ends under a page's frame (spec 005 FR-047), their side in reference units.</summary>
        public const float PageFootFlowerUnits = 150f;

        /// <summary>
        /// The owner's flowers (<c>flowers</c>, pictures.md D6) over a corner of <paramref name="target"/> (spec 005 FR-047, the
        /// owner's references of 2026-10-08): a square of <paramref name="side"/> whose middle is
        /// <paramref name="over"/> of it inside the corner. <see cref="Mirrored"/> and <see cref="Turned"/> say how the
        /// picture is flipped there.
        /// </summary>
        public static Box CornerBox(Box target, Corner corner, float side, float over = 0.22f)
        {
            bool right = corner == Corner.TopRight || corner == Corner.BottomRight;
            bool bottom = corner == Corner.BottomLeft || corner == Corner.BottomRight;
            float cx = right ? target.Right - (side * over) : target.Left + (side * over);
            float cy = bottom ? target.Bottom - (side * over) : target.Top + (side * over);
            return Box.FromCenter(cx, cy, side, side);
        }

        /// <summary>Whether the corner's picture is mirrored left to right (the right corners).</summary>
        public static bool Mirrored(Corner corner) => corner == Corner.TopRight || corner == Corner.BottomRight;

        /// <summary>Whether the corner's picture is turned upside down (the bottom corners).</summary>
        public static bool Turned(Corner corner) => corner == Corner.BottomLeft || corner == Corner.BottomRight;

        /// <summary>
        /// The flowers over a page's frame (spec 005 FR-047, the owner's references of 2026-10-08), clear of the page's
        /// content: over its top corners (<see cref="PageFlowerUnits"/>, their middles 12% of a side inside the corners) and
        /// on the bottom menu's top edge where the frame goes under it at <paramref name="bottom"/> (the panel's own bottom on
        /// a page without one; <see cref="PageFootFlowerUnits"/>, their middles 20% of a side in from the frame's sides and
        /// 15% below the edge). Hosts draw them after the bottom menu.
        /// </summary>
        public static IReadOnlyList<(Corner Corner, Box Box)> PageFlowers(Box panel, float bottom, float unit)
        {
            float side = PageFlowerUnits * unit;
            float foot = PageFootFlowerUnits * unit;
            var frame = new Box(panel.Left, panel.Top, panel.Right, bottom);
            float footY = bottom + (foot * 0.15f);
            return new[]
            {
                (Corner.TopLeft, CornerBox(frame, Corner.TopLeft, side, 0.12f)),
                (Corner.TopRight, CornerBox(frame, Corner.TopRight, side, 0.12f)),
                (Corner.BottomLeft, Box.FromCenter(frame.Left + (foot * 0.2f), footY, foot, foot)),
                (Corner.BottomRight, Box.FromCenter(frame.Right - (foot * 0.2f), footY, foot, foot)),
            };
        }

        /// <summary>The cream panel inside the frame (<see cref="UiRaster.CardFrameBorder"/> in from each side, the frame's front side below).</summary>
        public static Box Panel(Box card)
        {
            float border = UiRaster.CardFrameBorder(card.Width);
            return new Box(card.Left + border, card.Top + border, card.Right - border, card.Bottom - (border * 1.3f));
        }
    }

    /// <summary>A corner of a box (spec 005 FR-047: where the flowers go).</summary>
    public enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
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
