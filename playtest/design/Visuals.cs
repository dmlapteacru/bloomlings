using System;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// How a variant and a family look: color and symbol from the core's catalog, the generated 2D characters and 3D
    /// heroes of spec 004, and the shape library's family bodies when a picture is missing.
    /// </summary>
    public static class Visuals
    {
        public static Rgba ColorOf(VariantId variant) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? Rgba.FromHex(info.ColorHex) : C.StateStuck;

        public static string SymbolOf(VariantId variant) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? ShapeLibrary.SymbolId(info.IconId) : ShapeLibrary.Fallback;

        public static Family FamilyOf(VariantId variant) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? info.Family : Family.Sprig;

        /// <summary>
        /// A variant's 2D character (spec 004 FR-005 to FR-007): the generated picture whose whole shape is the variant's
        /// symbol, with a face in <paramref name="mood"/>. Without the picture it draws the spec 002 figure: the family
        /// body in the variant color with the symbol in ink (FR-021).
        /// </summary>
        public static void Character(IPainter p, Box box, VariantId variant, CharacterMood mood)
        {
            Family family = FamilyOf(variant);
            p.Mark(ShapeLibrary.SilhouetteId(family));
            if (VariantCatalog.Default.TryGet(variant, out VariantInfo info))
            {
                p.Mark(CharacterArt.Slot2D(info.IconId));
                string name = CharacterArt.Picture2D(info.IconId, mood);
                if (p.HasSprite(name))
                {
                    p.Sprite(name, box);
                    return;
                }
            }

            Rgba color = mood switch
            {
                CharacterMood.Asleep => DesignTokens.PodQueued(ColorOf(variant)),
                CharacterMood.Worried => ColorOf(variant).Grey().Mix(C.StateStuck, 0.35f),
                _ => ColorOf(variant),
            };
            p.Shape(ShapeLibrary.SilhouetteId(family), box, color);
            float s = box.Width * 0.46f;
            p.Shape(SymbolOf(variant), Box.FromCenter(box.CenterX, box.Top + (box.Height * 0.62f), s, s), color.Ink);
        }

        /// <summary>A soft flat shadow under a character standing in <paramref name="box"/> (part of the character's look).</summary>
        public static void GroundShadow(IPainter p, Box box)
        {
            float cx = box.CenterX;
            float cy = box.Bottom - (box.Height * 0.05f);
            p.PushSquash(1f, 0.3f, cx, cy);
            p.FillCircle(cx, cy, box.Width * 0.3f, Rgba.Black.WithAlpha(0.13f));
            p.PopTransform();
        }

        /// <summary>
        /// A family's 3D hero (meta screens only, FR-016 and FR-017) in its outfit: the skin masked by the picture, the
        /// hat on top, and a worn expression over the blank face. Without the pictures it draws the family body.
        /// </summary>
        public static void Hero(IPainter p, Box box, Family family, Outfit? outfit)
        {
            p.Mark(CharacterArt.HeroSlot(family));
            bool expression = outfit?.Expression != null;
            string name = CharacterArt.Hero(family, blank: expression);
            if (!p.HasSprite(name))
            {
                p.Mark(ShapeLibrary.SilhouetteId(family));
                p.Shape(ShapeLibrary.SilhouetteId(family), box.Inset(box.Width * 0.12f), ColorOf(HeroVariant(family)));
                return;
            }

            Box picture = PainterBase.Fit(box, CharacterArt.HeroWidth, CharacterArt.HeroHeight);
            if (outfit?.Trail != null)
            {
                p.Shape(ShapeLibrary.CosmeticId(outfit.Trail.Shape), CharacterArt.TrailBox(picture), Tint(outfit.Trail));
            }

            p.Sprite(name, picture);
            if (outfit?.Skin != null)
            {
                p.Mark(ShapeLibrary.CosmeticId(outfit.Skin.Shape));
                p.SpriteSkin(name, picture, outfit.Skin.Shape, Rgba.White.WithAlpha(CosmeticCatalog.SkinOpacity));
            }

            if (outfit?.Expression != null)
            {
                (float x, float y) = CharacterArt.FaceCenterHero(family);
                p.Shape(ShapeLibrary.CosmeticId(outfit.Expression.Shape), CharacterArt.ExpressionBox(picture, (x, y)), C.TextPrimary);
            }

            if (outfit?.Hat != null)
            {
                p.Shape(ShapeLibrary.CosmeticId(outfit.Hat.Shape), CharacterArt.HatBox(picture), Tint(outfit.Hat));
            }
        }

        /// <summary>The four 3D heroes on the stone pedestal (splash, Home early, win and milestone cards).</summary>
        public static void Group(IPainter p, Box box)
        {
            p.Mark(CharacterArt.GroupSlot);
            if (p.HasSprite(CharacterArt.Group))
            {
                p.Sprite(CharacterArt.Group, box);
                return;
            }

            // Fallback: the four family bodies in a row.
            Box fitted = PainterBase.Fit(box, CharacterArt.GroupWidth, CharacterArt.GroupHeight);
            float size = fitted.Width / 5f;
            for (int i = 0; i < CharacterArt.Families.Count; i++)
            {
                Family family = CharacterArt.Families[i];
                p.Mark(ShapeLibrary.SilhouetteId(family));
                float x = fitted.CenterX + ((i - 1.5f) * size * 1.1f);
                p.Shape(ShapeLibrary.SilhouetteId(family), Box.FromCenter(x, fitted.CenterY, size, size), ColorOf(HeroVariant(family)));
            }
        }

        /// <summary>The Leafling experiment, a guest on Home (spec 004 research R17); nothing when its picture is missing.</summary>
        public static void Guest(IPainter p, Box box)
        {
            p.Mark(CharacterArt.LeaflingSlot);
            if (p.HasSprite(CharacterArt.Leafling))
            {
                p.Sprite(CharacterArt.Leafling, box);
            }
        }

        /// <summary>
        /// An owner background (spec 005 pictures.md B, <see cref="OwnerPictures"/>) cover-fitted into
        /// <paramref name="box"/> and clipped to it, or <paramref name="fallback"/> (the code-drawn backdrop) while the
        /// picture is missing. Callers mark the background's slot.
        /// </summary>
        public static void Background(IPainter p, Box box, string picture, Action fallback)
        {
            string name = PainterBase.BackgroundPrefix + picture;
            (int Width, int Height)? size = p.HasSprite(name) ? p.SpriteSize(name) : null;
            if (!size.HasValue || size.Value.Width <= 0 || size.Value.Height <= 0)
            {
                fallback();
                return;
            }

            float scale = Math.Max(box.Width / size.Value.Width, box.Height / size.Value.Height);
            p.PushClip(box);
            p.Sprite(name, Box.FromCenter(box.CenterX, box.CenterY, size.Value.Width * scale, size.Value.Height * scale));
            p.PopClip();
        }

        /// <summary>
        /// The owner's logo picture (spec 005 pictures.md C1) fitted into <paramref name="box"/>, or
        /// <paramref name="fallback"/> (the wooden letters, <see cref="Kit.WoodLogo"/>) while it is missing. It marks
        /// <c>brand.wordmark</c>.
        /// </summary>
        public static void Logo(IPainter p, Box box, Action fallback)
        {
            p.Mark("brand.wordmark");
            string name = PainterBase.BrandPrefix + OwnerPictures.Logo;
            if (p.HasSprite(name))
            {
                p.Sprite(name, box);
                return;
            }

            fallback();
        }

        /// <summary>The variant whose color a family's fallback hero body takes.</summary>
        private static VariantId HeroVariant(Family family) => family switch
        {
            Family.Bloom => VariantId.Flower,
            Family.Drop => VariantId.Water,
            Family.Twig => VariantId.Wood,
            _ => VariantId.Leaf,
        };

        /// <summary>A cosmetic's tint: its own color, or gold.</summary>
        public static Rgba Tint(CosmeticItem item) => item.Tint.StartsWith("#", StringComparison.Ordinal) ? Rgba.FromHex(item.Tint) : C.MedalGold;

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
