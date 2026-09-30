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

        /// <summary>
        /// A Bloomling: the family body in the variant color with a small face, carrying the variant symbol in ink
        /// (pods, slots, walkers, Home; spec 001 FR-012 prominence: symbol, color, then silhouette).
        /// </summary>
        public static void Bloomling(IPainter p, Box box, Family family, Rgba color, string? symbolId, bool face = true, float symbolScale = 0.5f)
        {
            p.Shape(ShapeLibrary.SilhouetteId(family), box, color);
            Rgba ink = color.Ink;
            if (face && symbolId == null)
            {
                // A whole face on figures without a symbol (Home, the splash).
                Box faceBox = Box.FromCenter(box.CenterX, box.Top + (box.Height * FaceY(family)), box.Width * 0.34f, box.Height * 0.34f);
                p.Shape("char.face", faceBox, ink.WithAlpha(0.85f));
            }
            else if (face)
            {
                // Just the eyes above the symbol, so the symbol stays clear (FR-012 prominence).
                p.Mark("char.face");
                float eyeY = box.Top + (box.Height * (FaceY(family) - 0.06f));
                float r = box.Width * 0.045f;
                p.FillCircle(box.CenterX - (box.Width * 0.11f), eyeY, r, ink.WithAlpha(0.85f));
                p.FillCircle(box.CenterX + (box.Width * 0.11f), eyeY, r, ink.WithAlpha(0.85f));
            }

            if (symbolId != null)
            {
                p.Mark("char.accent");
                float s = box.Width * symbolScale;
                p.Shape(symbolId, Box.FromCenter(box.CenterX, box.Top + (box.Height * 0.68f), s, s), ink);
            }
        }

        /// <summary>Where a family's eyes sit, as a share of the body box from its top.</summary>
        private static float FaceY(Family family) => family switch
        {
            Family.Twig => 0.36f,
            Family.Drop => 0.5f,
            _ => 0.5f,
        };

        /// <summary>A variant tile as in demos and legends: a raised rounded tile with its symbol.</summary>
        public static void VariantTile(IPainter p, Box box, VariantId variant)
        {
            Rgba color = ColorOf(variant);
            Box face = Kit.Raised(p, box, color, DesignTokens.TileEdge(color), box.Width * DesignTokens.Radius.Tile, top: DesignTokens.TileTop(color));
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
