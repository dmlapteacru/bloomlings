using System;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Playtest.Design
{
    /// <summary>How a variant and a family look: color, symbol and body from the core's catalog and the shape library.</summary>
    public static class Visuals
    {
        public static Rgba ColorOf(VariantId variant) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? Rgba.FromHex(info.ColorHex) : C.StateStuck;

        public static string SymbolOf(VariantId variant) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? ShapeLibrary.SymbolId(info.IconId) : ShapeLibrary.Fallback;

        public static Family FamilyOf(VariantId variant) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? info.Family : Family.Sprig;

        /// <summary>The variant's icon id (its crest and belly symbol): <c>leaf</c>, <c>bud</c>, …</summary>
        public static string? IconOf(VariantId variant) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? info.IconId : null;

        /// <summary>
        /// A kawaii Bloomling (spec 003 FR-032): the figure from <see cref="BloomlingArt"/> in the variant color, and the
        /// variant symbol on its white belly badge (pods, slots, walkers; spec 001 FR-012 prominence: symbol, color,
        /// then silhouette). Without an icon id it has no badge (Home, the leaderboard). Walkers get the white halo.
        /// </summary>
        public static void Bloomling(IPainter p, Box box, Family family, Rgba color, string? iconId, BloomlingMood mood = BloomlingMood.Happy, bool halo = false)
        {
            var look = new BloomlingLook(family, color, iconId, mood, Badge: true, Halo: halo);
            p.Mark(ShapeLibrary.SilhouetteId(family));
            if (mood != BloomlingMood.None)
            {
                p.Mark("char.face");
            }

            p.Picture(look.Key, size => BloomlingArt.Render(look, size, topDown: true, premultiplied: true), box);
            if (look.ShowsBadge)
            {
                p.Mark("char.accent");
                p.Shape(ShapeLibrary.SymbolId(iconId!), BloomlingArt.SymbolBox(box), BloomlingArt.SymbolColor(color));
            }
        }

        /// <summary>A soft flat shadow under the feet of a Bloomling drawn into <paramref name="box"/>.</summary>
        public static void GroundShadow(IPainter p, Box box, float alpha = 0.12f)
        {
            float feet = box.CenterY - (BloomlingArt.FeetY * BloomlingArt.Fit / ShapeRaster.Margin * box.Height / 2f);
            Box shadow = Box.FromCenter(box.CenterX, feet, box.Width * 0.56f, box.Height * 0.09f);
            p.FillRound(shadow, shadow.Height / 2f, C.GardenShadow.WithAlpha(alpha));
        }

        /// <summary>A variant tile as in demos and legends: a raised rounded tile with its symbol.</summary>
        public static void VariantTile(IPainter p, Box box, VariantId variant)
        {
            Rgba color = ColorOf(variant);
            Box face = Kit.Block(p, box, color, DesignTokens.TileEdge(color), box.Width * DesignTokens.Radius.Tile, Kit.CellLip(p, box.Height), DesignTokens.Garden.CellHighlightAlpha, top: DesignTokens.TileTop(color));
            float s = face.Width * 0.62f;
            p.Shape(SymbolOf(variant), Box.FromCenter(face.CenterX, face.CenterY, s, s), color.Ink);
        }

        /// <summary>The Hard/Super Hard badge texts and colors.</summary>
        public static (string Text, Rgba Color, string Slot)? DifficultyBadge(Core.Definitions.DifficultyClass difficulty) => difficulty switch
        {
            Core.Definitions.DifficultyClass.Hard => (PlaytestText.T("difficulty.hard"), C.BadgeHard, "ui.badge.hard"),
            Core.Definitions.DifficultyClass.SuperHard => (PlaytestText.T("difficulty.super_hard"), C.BadgeSuperHard, "ui.badge.super_hard"),
            _ => null,
        };

        public static float Clamp01(float v) => Math.Max(0f, Math.Min(1f, v));
    }
}
