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
        /// A family's 3D hero (meta screens only, FR-016 and FR-017) in its outfit: the skin masked by the picture in the
        /// skin's tint, the hat on top, and a worn expression over the blank face when the blank twin is the same character
        /// (<see cref="CharacterArt.HasMatchingBlank"/>), else as a badge beside the face. Without the pictures it draws the
        /// family body.
        /// </summary>
        public static void Hero(IPainter p, Box box, Family family, Outfit? outfit)
        {
            p.Mark(CharacterArt.HeroSlot(family));
            bool expression = outfit?.Expression != null;
            bool blank = expression && CharacterArt.HasMatchingBlank(family);
            string name = CharacterArt.Hero(family, blank: blank);
            if (blank && !p.HasSprite(name))
            {
                blank = false;
                name = CharacterArt.Hero(family);
            }

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
                p.SpriteSkin(name, picture, outfit.Skin.Shape, Tint(outfit.Skin).WithAlpha(CosmeticCatalog.SkinOpacity));
            }

            if (outfit?.Expression != null)
            {
                string glyph = ShapeLibrary.CosmeticId(outfit.Expression.Shape);
                if (blank)
                {
                    (float x, float y) = CharacterArt.FaceCenterHero(family);
                    p.Shape(glyph, CharacterArt.ExpressionBox(picture, (x, y)), C.TextPrimary);
                }
                else
                {
                    // The hero keeps its drawn face; the expression shows on a cream badge beside it (pictures.md A5).
                    ExpressionBadge(p, CharacterArt.ExpressionBadge(picture), glyph);
                }
            }

            if (outfit?.Hat != null)
            {
                Hat(p, CharacterArt.HatOnHero(picture, family, outfit.Hat.Shape), outfit.Hat);
            }
        }

        /// <summary>
        /// Whether a family's animated hero can show (spec 005 FR-028): its clips were baked (<see cref="HeroMotion.Has"/>)
        /// and their frames are embedded (else the hosts keep the still pictures).
        /// </summary>
        public static bool HasMotion(IPainter p, Family family) =>
            HeroMotion.Has(family)
            && p.HasSprite(MotionFrame(HeroMotion.Frame(family, MotionClip.Idle, 0)))
            && p.HasSprite(MotionFrame(HeroMotion.Frame(family, MotionClip.Idle, -1)))
            && p.HasSprite(MotionFrame(HeroMotion.Frame(family, MotionClip.React, -1)));

        /// <summary>A hero frame's picture name for the painter (<see cref="PainterBase.HeroMotionPrefix"/>).</summary>
        public static string MotionFrame(HeroFrame frame) => PainterBase.HeroMotionPrefix + frame.Name;

        /// <summary>
        /// A family's animated hero (spec 005 FR-028; meta screens only, constitution VII: flat pre-rendered frames) in its
        /// frame cell <paramref name="cell"/> (<see cref="HeroMotion.Cell"/>, <see cref="HomeLayers.HeroCell"/>) at
        /// <paramref name="pose"/>: the pose's frame, and over it the idle frame it cross-fades from at the pose's alpha. In
        /// <paramref name="outfit"/>: the trail behind (<see cref="CharacterArt.TrailBox"/> of the cell), the skin through
        /// each frame's own alpha, the expression on its cream badge (frames have no blank twin) and the hat on the head
        /// turned with it (<see cref="HeroMotion.Hat"/>, between the two frames' while they cross-fade). Callers check
        /// <see cref="HasMotion"/> first.
        /// </summary>
        public static void MotionHero(IPainter p, Box cell, Family family, HeroPose pose, Outfit? outfit)
        {
            p.Mark(HeroMotion.Slot(family));
            HeroFrame frame = HeroMotion.Frame(family, pose.Clip, pose.Index);
            HeroFrame? from = pose.FromIdle >= 0 && pose.FromAlpha > 0f ? HeroMotion.Frame(family, pose.FromClip, pose.FromIdle) : (HeroFrame?)null;
            if (outfit?.Trail != null)
            {
                p.Shape(ShapeLibrary.CosmeticId(outfit.Trail.Shape), CharacterArt.TrailBox(cell), Tint(outfit.Trail));
            }

            MotionFrameIn(p, cell, frame, outfit);
            if (from.HasValue)
            {
                p.PushAlpha(pose.FromAlpha);
                MotionFrameIn(p, cell, from.Value, outfit);
                p.PopAlpha();
            }

            if (outfit?.Expression != null)
            {
                ExpressionBadge(p, CharacterArt.ExpressionBadge(cell), ShapeLibrary.CosmeticId(outfit.Expression.Shape));
            }

            if (outfit?.Hat != null)
            {
                (Box box, float degrees) = HeroMotion.Hat(cell, family, frame, outfit.Hat.Shape);
                if (from.HasValue)
                {
                    (Box fromBox, float fromDegrees) = HeroMotion.Hat(cell, family, from.Value, outfit.Hat.Shape);
                    float k = pose.FromAlpha;
                    box = Box.FromCenter(box.CenterX + ((fromBox.CenterX - box.CenterX) * k), box.CenterY + ((fromBox.CenterY - box.CenterY) * k), box.Width, box.Height);
                    degrees += (fromDegrees - degrees) * k;
                }

                p.PushRotate(degrees, box.CenterX, box.CenterY);
                Hat(p, box, outfit.Hat);
                p.PopTransform();
            }
        }

        /// <summary>One hero frame in its cell, with the worn skin through the frame's own alpha.</summary>
        private static void MotionFrameIn(IPainter p, Box cell, HeroFrame frame, Outfit? outfit)
        {
            string name = MotionFrame(frame);
            Box box = HeroMotion.PictureBox(cell, frame);
            p.Sprite(name, box);
            if (outfit?.Skin != null)
            {
                p.Mark(ShapeLibrary.CosmeticId(outfit.Skin.Shape));
                p.SpriteSkin(name, box, outfit.Skin.Shape, Tint(outfit.Skin).WithAlpha(CosmeticCatalog.SkinOpacity));
            }
        }

        /// <summary>A worn expression on a cream badge beside a hero's drawn face (pictures.md A5).</summary>
        private static void ExpressionBadge(IPainter p, Box badge, string glyph)
        {
            float line = Math.Max(1f, badge.Width * 0.06f);
            p.FillCircle(badge.CenterX, badge.CenterY + (line * 0.6f), badge.Width / 2f, C.InkBrown.WithAlpha(0.25f));
            p.FillCircle(badge.CenterX, badge.CenterY, badge.Width / 2f, C.CreamTop);
            p.StrokeCircle(badge.CenterX, badge.CenterY, (badge.Width / 2f) - (line / 2f), line, C.CreamLine);
            p.Shape(glyph, Box.FromCenter(badge.CenterX, badge.CenterY, badge.Width * 0.7f, badge.Width * 0.47f), C.InkBrown);
        }

        /// <summary>A worn hat in full color: a darker outline of its own tint, the fill, a light top-left.</summary>
        private static void Hat(IPainter p, Box hatBox, CosmeticItem item)
        {
            string hat = ShapeLibrary.CosmeticId(item.Shape);
            Rgba tint = Tint(item);
            if (ShapeLibrary.Has(hat))
            {
                Func<float, float, float> sdf = ShapeLibrary.Get(hat);
                p.ShapeOf(hat + "/line", (x, y) => sdf(x, y) - 0.06f, hatBox, tint.Darken(0.45f));
                p.Shape(hat, hatBox, tint);
                p.ShapeOf(hat + "/light", (x, y) => Math.Max(sdf(x + 0.05f, y - 0.06f) + 0.07f, sdf(x, y) + 0.03f), hatBox, tint.Lighten(0.35f).WithAlpha(0.5f));
            }
            else
            {
                p.Shape(hat, hatBox, tint);
            }
        }

        /// <summary>The four 3D heroes on the stone pedestal (the win and milestone cards).</summary>
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

        /// <summary>
        /// An owner background (spec 005 pictures.md B, <see cref="OwnerPictures"/>) cover-fitted into
        /// <paramref name="box"/> (or placed by <paramref name="place"/> from the picture's size, as the win's
        /// <see cref="OwnerPictures.TopAnchored"/>) and clipped to it, or <paramref name="fallback"/> (the code-drawn
        /// backdrop) while the picture is missing. Callers mark the background's slot.
        /// </summary>
        public static void Background(IPainter p, Box box, string picture, Action fallback, Func<int, int, Box>? place = null)
        {
            string name = PainterBase.BackgroundPrefix + picture;
            (int Width, int Height)? size = p.HasSprite(name) ? p.SpriteSize(name) : null;
            if (!size.HasValue || size.Value.Width <= 0 || size.Value.Height <= 0)
            {
                fallback();
                return;
            }

            float scale = Math.Max(box.Width / size.Value.Width, box.Height / size.Value.Height);
            Box at = place?.Invoke(size.Value.Width, size.Value.Height)
                ?? Box.FromCenter(box.CenterX, box.CenterY, size.Value.Width * scale, size.Value.Height * scale);
            p.PushClip(box);
            p.Sprite(name, at);
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
