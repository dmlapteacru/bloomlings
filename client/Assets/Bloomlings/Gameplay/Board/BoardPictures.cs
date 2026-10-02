using System;
using System.Collections.Generic;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// The board's pictures in the reference look (spec 005 contracts/look.md §3.6, §4.1, §4.4; research D14), the
    /// Unity twins of the playtest's <c>BoardPainter</c> recipes, as engine-free RGBA bytes (straight alpha, rows from the
    /// top, the same arguments giving the same bytes, as <see cref="UiRaster"/>):
    /// <list type="bullet">
    /// <item><description><see cref="Ground"/>: the restored ground under the tiles, each cell a pale flat cell of the
    /// finished picture with a small radius and a faint inner shadow along its top, over a deeper shade of itself, and
    /// the picture's background as grass (<see cref="UiRaster.Grass"/>);</description></item>
    /// <item><description><see cref="Finished"/>: the finished picture in full color (the win, the Collection), each cell a
    /// flat candy tile of its role's variant (<see cref="TileStyle.Flat"/>), stones as stone blocks and the background as
    /// grass, inside a thin stone border;</description></item>
    /// <item><description><see cref="StoneObstacle"/>: a stone obstacle, a raised block of the border's sandy stone over a
    /// soft shadow with a jagged crack.</description></item>
    /// </list>
    /// Colors are the core catalog's variant colors, so the ground matches the candy tiles over it.
    /// </summary>
    public static class BoardPictures
    {
        /// <summary>How much a target tile is inset into its cell (each side, in cells): the tiles nearly touch.</summary>
        public const float TileInset = 0.008f;

        /// <summary>How much a restored cell is inset into its cell (each side, in cells).</summary>
        public const float GroundInset = 0.025f;

        /// <summary>The finished picture's stone border, in cells (thinner than the board's).</summary>
        public const float PictureBorder = 0.3f;

        /// <summary>Room around the framed finished picture for the border's shadow, in cells.</summary>
        public const float PictureMargin = 0.08f;

        /// <summary>Room around a stone obstacle's cell for its shadow, in cells (each side).</summary>
        public const float ObstacleMargin = 0.12f;

        /// <summary>
        /// The most pixels per cell (at least 8, <see cref="Finished"/>'s smallest) at which the finished picture of a
        /// <paramref name="width"/> × <paramref name="height"/> board, framed or not, fits into <paramref name="maxSide"/>
        /// pixels each way.
        /// </summary>
        public static int CellPixelsToFit(int width, int height, int maxSide, bool framed)
        {
            float rim = framed ? BoardLayout.Gap + PictureBorder + PictureMargin : 0f;
            float cells = Math.Max(1, Math.Max(width, height)) + (2f * rim);
            return Math.Max(8, (int)Math.Floor(maxSide / cells));
        }

        /// <summary>A variant's color from the core catalog (the candy tiles' color).</summary>
        public static Rgba ColorOf(VariantId variant) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? Rgba.FromHex(info.ColorHex) : C.StateStuck;

        /// <summary>The variant a picture cell's role maps to (null for ground and stones) and the picture's raw value; the mirror applied.</summary>
        public static (VariantId? Variant, int Value) PictureCell(LevelDefinition definition, BasePicture picture, int x, int y)
        {
            int px = definition.Picture.Mirror == Mirror.Horizontal ? picture.Width - 1 - x : x;
            if (px < 0 || px >= picture.Width || y < 0 || y >= picture.Height)
            {
                return (null, BasePicture.Empty);
            }

            int value = picture.CellAt(px, y);
            if (value >= 0 && value < picture.Roles.Count && definition.Mapping.TryGetValue(picture.Roles[value].RoleId, out VariantId variant))
            {
                return (variant, value);
            }

            return (null, value);
        }

        /// <summary>Whether a picture cell is the picture's background (no role, no stone): it shows as grass (<c>tile.grass</c>).</summary>
        public static bool IsGrass(LevelDefinition definition, BasePicture picture, int x, int y)
        {
            (VariantId? variant, int value) = PictureCell(definition, picture, x, y);
            return !variant.HasValue && value != BasePicture.Stone;
        }

        /// <summary>The restored ground's color under a cell (§4.1): its role's variant color lightened 0.55, stone or ground.</summary>
        public static Rgba GroundColor(LevelDefinition definition, BasePicture picture, int x, int y)
        {
            (VariantId? variant, int value) = PictureCell(definition, picture, x, y);
            if (variant.HasValue)
            {
                return ColorOf(variant.Value).Lighten(0.55f);
            }

            return value == BasePicture.Stone ? C.StoneFace.Lighten(0.35f) : C.TileGround;
        }

        /// <summary>
        /// The restored ground of a <paramref name="width"/> × <paramref name="height"/> board (<c>tile.ground</c>), each cell
        /// <paramref name="cellPixels"/> square, the board's top row first. A cell is its color darkened 0.12, with the
        /// cell's pale face inset by <see cref="GroundInset"/> (radius 10% of it) and a faint shadow along the face's top
        /// quarter.
        /// </summary>
        public static byte[] Ground(LevelDefinition definition, BasePicture picture, int width, int height, int cellPixels)
        {
            int w = Math.Max(1, width);
            int h = Math.Max(1, height);
            int c = Math.Max(4, cellPixels);
            int pw = w * c;
            int ph = h * c;
            var pixels = new byte[pw * ph * 4];
            var grass = new byte[UiRaster.GrassVariants][];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int left = x * c;
                    int top = (h - 1 - y) * c;
                    if (IsGrass(definition, picture, x, y))
                    {
                        // The picture's background reads as garden (spec 005 FR-020, tile.grass).
                        int seed = UiRaster.GrassSeed(x, y);
                        grass[seed] ??= UiRaster.Grass(c, seed);
                        Blit(pixels, pw, ph, grass[seed], c, c, left, top);
                        continue;
                    }

                    Rgba color = GroundColor(definition, picture, x, y);
                    FillRect(pixels, pw, left, top, c, c, color.Darken(0.12f));
                    var full = new Box(left, top, left + c, top + c);
                    Box face = full.Inset(c * GroundInset);
                    float r = face.Width * 0.1f;
                    FillRound(pixels, pw, ph, face, r, color, color);
                    var shade = new Box(face.Left, face.Top, face.Right, face.Top + (face.Height * 0.24f));
                    FillRound(pixels, pw, ph, shade, r, C.GardenShadow.WithAlpha(0.12f), C.GardenShadow.WithAlpha(0f), face);
                }
            }

            return pixels;
        }

        /// <summary>
        /// The finished picture in full color (<c>tile.picture</c>, research D14, §4.4), each cell
        /// <paramref name="cellPixels"/> square: every picture cell a flat candy tile of its role's variant (no lip, a small
        /// gloss, the board-style symbol), stones as stone blocks, ground as cream cells. <paramref name="framed"/> puts it
        /// in a thin stone border (<see cref="PictureBorder"/> cells thick, with room for the border's shadow); otherwise it
        /// lies on the board's dark gap. <paramref name="icons"/> gives the pixels of an owner icon picture by its name
        /// (straight-alpha RGBA, rows from the top) or null: with a variant's field icon (spec 005 pictures.md G17–G24) its
        /// tiles are the flat face with the icon baked over it (<see cref="FlatTile"/>). Returns the bytes and their size.
        /// </summary>
        public static byte[] Finished(LevelDefinition definition, BasePicture picture, int cellPixels, bool framed, out int width, out int height, Func<string, (byte[] Rgba, int Width, int Height)?>? icons = null)
        {
            int w = Math.Max(1, picture.Width);
            int h = Math.Max(1, picture.Height);
            float c = Math.Max(8, cellPixels);
            float rim = framed ? BoardLayout.Gap + PictureBorder + PictureMargin : 0f;
            width = (int)Math.Ceiling((w + (2f * rim)) * c);
            height = (int)Math.Ceiling((h + (2f * rim)) * c);
            var pixels = new byte[width * height * 4];
            var grid = new Box(rim * c, rim * c, (rim * c) + (w * c), (rim * c) + (h * c));
            if (framed)
            {
                StoneBorder(pixels, width, height, grid, c, PictureBorder);
            }
            else
            {
                FillRound(pixels, width, height, grid, c * 0.2f, GardenLook.BoardGap, GardenLook.BoardGap);
            }

            var tiles = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            int tileSize = Math.Max(1, (int)Math.Round(c * (1f - (2f * TileInset))));
            int grassSize = Math.Max(1, (int)Math.Round(c));
            var grass = new byte[UiRaster.GrassVariants][];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float left = grid.Left + (x * c);
                    float top = grid.Top + ((h - 1 - y) * c);
                    var full = new Box(left, top, left + c, top + c);
                    (VariantId? variant, int value) = PictureCell(definition, picture, x, y);
                    if (variant.HasValue)
                    {
                        Rgba color = ColorOf(variant.Value);
                        string icon = VariantCatalog.Default.TryGet(variant.Value, out VariantInfo info) ? info.IconId : "mystery";
                        string key = icon + "/" + color.Hex;
                        if (!tiles.TryGetValue(key, out byte[]? tile))
                        {
                            tile = FlatTile(tileSize, color, icon, icons);
                            tiles[key] = tile;
                        }

                        Blit(pixels, width, height, tile, tileSize, tileSize, (int)Math.Round(full.CenterX - (tileSize / 2f)), (int)Math.Round(full.CenterY - (tileSize / 2f)));
                    }
                    else if (value == BasePicture.Stone)
                    {
                        StoneBlock(pixels, width, height, full.Inset(c * 0.04f), 21 + (((x * 5) + (y * 3)) % 6), 0.26f);
                    }
                    else
                    {
                        // The picture's background as grass (tile.grass), as on the board.
                        int seed = UiRaster.GrassSeed(x, y);
                        grass[seed] ??= UiRaster.Grass(grassSize, seed);
                        Blit(pixels, width, height, grass[seed], grassSize, grassSize, (int)Math.Round(full.CenterX - (grassSize / 2f)), (int)Math.Round(full.CenterY - (grassSize / 2f)));
                    }
                }
            }

            return pixels;
        }

        /// <summary>
        /// A flat candy tile of the finished picture (<see cref="TileStyle.Flat"/>) of side <paramref name="size"/>: with the
        /// owner's field icon of <paramref name="iconId"/> from <paramref name="icons"/> (pictures.md G17–G24), the flat face
        /// (<see cref="UiRaster.TileFace"/>) with the icon drawn over its middle (<see cref="OwnerPictures.TileIconBox"/>,
        /// <see cref="UiRaster.DrawOver"/>), as the playtest's <c>Kit.CandyTile</c> draws it; otherwise the drawn tile.
        /// </summary>
        public static byte[] FlatTile(int size, Rgba color, string iconId, Func<string, (byte[] Rgba, int Width, int Height)?>? icons)
        {
            string? name = OwnerPictures.TileIcon(iconId, TileStyle.Flat, TileState.Normal);
            (byte[] Rgba, int Width, int Height)? art = name != null && icons != null ? icons(name) : null;
            if (!art.HasValue)
            {
                return UiRaster.Tile(size, color, iconId, TileStyle.Flat);
            }

            byte[] tile = UiRaster.TileFace(size, color, TileStyle.Flat);
            UiRaster.DrawOver(tile, size, size, art.Value.Rgba, art.Value.Width, art.Value.Height, OwnerPictures.TileIconBox(new Box(0f, 0f, size, size), TileStyle.Flat));
            return tile;
        }

        /// <summary>
        /// A stone obstacle (<c>tile.stone</c>, §4.1) as a square picture of side <paramref name="size"/>: the cell in its
        /// middle with <see cref="ObstacleMargin"/> of it around for the shadow, and in the cell a block of the border's
        /// stone inset 6% (radius 24%, <paramref name="seed"/> picks its mottling) over a soft shadow, with a jagged crack
        /// from its upper edge toward the middle and one branch, a dark groove over a light lower lip; <paramref name="flip"/>
        /// mirrors the crack so neighboring stones differ.
        /// </summary>
        public static byte[] StoneObstacle(int size, int seed, bool flip)
        {
            size = Math.Max(8, size);
            float cell = size / (1f + (2f * ObstacleMargin));
            float m = (size - cell) / 2f;
            var pixels = new byte[size * size * 4];
            var full = new Box(m, m, m + cell, m + cell);
            Box box = full.Inset(cell * 0.06f);
            SoftShadow(pixels, size, size, box, box.Width * 0.24f, 0.36f, 0.08f);
            StoneBlock(pixels, size, size, box, seed, 0.24f);

            float w = box.Width;
            float X(float k) => flip ? box.Right - (k * w) : box.Left + (k * w);
            float Y(float k) => box.Top + (k * box.Height);
            (float X, float Y)[] crack = { (X(0.36f), Y(0.08f)), (X(0.44f), Y(0.28f)), (X(0.37f), Y(0.45f)), (X(0.52f), Y(0.63f)) };
            (float X, float Y)[] branch = { (X(0.44f), Y(0.28f)), (X(0.62f), Y(0.35f)) };
            float groove = Math.Max(1.5f, w * 0.035f);
            foreach ((float X, float Y)[] line in new[] { crack, branch })
            {
                for (int i = 0; i + 1 < line.Length; i++)
                {
                    float dx = groove * 0.45f;
                    float dy = groove * 0.6f;
                    Line(pixels, size, size, line[i].X + dx, line[i].Y + dy, line[i + 1].X + dx, line[i + 1].Y + dy, groove, C.StoneTop.Lighten(0.5f).WithAlpha(0.9f));
                    Line(pixels, size, size, line[i].X, line[i].Y, line[i + 1].X, line[i + 1].Y, groove, C.StoneLine.WithAlpha(0.72f));
                }
            }

            return pixels;
        }

        /// <summary>
        /// The stone border (§3.6, the playtest's <c>Kit.StoneBorder</c>) around <paramref name="grid"/>: its soft shadow, the
        /// dark joints, the board gap and the blocks of <see cref="StoneBorderView.Blocks"/>.
        /// </summary>
        private static void StoneBorder(byte[] pixels, int width, int height, Box grid, float cell, float thickness)
        {
            float gap = cell * 0.04f;
            float t = cell * thickness;
            float joint = cell * 0.04f;
            Box inner = grid.Inset(-gap);
            Box outer = inner.Inset(-t);
            Rgba shadow = C.GardenShadow.WithAlpha(0.22f);
            FillRound(pixels, width, height, outer.Offset(0f, t * 0.22f).Inset(-t * 0.05f, 0f), t * 0.5f, shadow, shadow);
            Rgba joints = C.StoneLine.WithAlpha(0.45f);
            FillRound(pixels, width, height, outer.Inset(joint * 0.5f), t * 0.4f, joints, joints);
            FillRound(pixels, width, height, inner, gap * 2f, GardenLook.BoardGap, GardenLook.BoardGap);
            foreach ((Box box, int seed, float share) in StoneBorderView.Blocks(grid, cell, thickness))
            {
                StoneBlock(pixels, width, height, box, seed, share);
            }
        }

        /// <summary>A stone block (<see cref="UiRaster.Stone"/>) at a box, rounded to whole pixels; the kit's <c>StoneBlock</c> recipe.</summary>
        private static void StoneBlock(byte[] pixels, int width, int height, Box box, int seed, float radiusShare)
        {
            int left = (int)Math.Round(box.Left);
            int top = (int)Math.Round(box.Top);
            int w = Math.Max(1, (int)Math.Round(box.Right) - left);
            int h = Math.Max(1, (int)Math.Round(box.Bottom) - top);
            float side = Math.Min(w, h);
            byte[] stone = UiRaster.Stone(w, h, side * radiusShare, Math.Max(1f, side * 0.035f), seed);
            Blit(pixels, width, height, stone, w, h, left, top);
        }

        /// <summary>The kit's soft shadow: four <c>garden.shadow</c> layers, each grown by 1.5% of the shorter side at a quarter of the alpha.</summary>
        private static void SoftShadow(byte[] pixels, int width, int height, Box box, float radius, float alpha, float offsetShare)
        {
            float s = Math.Min(box.Width, box.Height);
            Box shadow = box.Offset(0f, s * offsetShare);
            Rgba color = C.GardenShadow.WithAlpha(alpha / 4f);
            for (int k = 0; k < 4; k++)
            {
                float grow = s * 0.015f * k;
                FillRound(pixels, width, height, shadow.Inset(-grow), Math.Max(0f, radius) + grow, color, color);
            }
        }

        // ---- Raster helpers (straight alpha, source over) ----

        private static void FillRect(byte[] pixels, int width, int left, int top, int w, int h, Rgba color)
        {
            for (int y = top; y < top + h; y++)
            {
                for (int x = left; x < left + w; x++)
                {
                    int i = ((y * width) + x) * 4;
                    pixels[i] = color.R;
                    pixels[i + 1] = color.G;
                    pixels[i + 2] = color.B;
                    pixels[i + 3] = color.A;
                }
            }
        }

        /// <summary>
        /// A rounded rectangle with a vertical gradient from <paramref name="top"/> to <paramref name="bottom"/>, anti-aliased
        /// over one pixel, blended over the pixels; <paramref name="clip"/> keeps only the pixels whose centers lie in it.
        /// </summary>
        private static void FillRound(byte[] pixels, int width, int height, Box box, float radius, Rgba top, Rgba bottom, Box? clip = null)
        {
            if (box.Width <= 0f || box.Height <= 0f)
            {
                return;
            }

            float r = Math.Max(0f, Math.Min(radius, Math.Min(box.Width, box.Height) / 2f));
            int x0 = Math.Max(0, (int)Math.Floor(box.Left) - 1);
            int x1 = Math.Min(width, (int)Math.Ceiling(box.Right) + 1);
            int y0 = Math.Max(0, (int)Math.Floor(box.Top) - 1);
            int y1 = Math.Min(height, (int)Math.Ceiling(box.Bottom) + 1);
            for (int py = y0; py < y1; py++)
            {
                float y = py + 0.5f;
                if (clip.HasValue && (y < clip.Value.Top || y > clip.Value.Bottom))
                {
                    continue;
                }

                Rgba color = top.Mix(bottom, (y - box.Top) / box.Height);
                for (int px = x0; px < x1; px++)
                {
                    float x = px + 0.5f;
                    if (clip.HasValue && (x < clip.Value.Left || x > clip.Value.Right))
                    {
                        continue;
                    }

                    float cover = Cover(RoundRect(x, y, box, r));
                    if (cover > 0f)
                    {
                        Blend(pixels, ((py * width) + px) * 4, color, cover);
                    }
                }
            }
        }

        /// <summary>A line with round caps, <paramref name="lineWidth"/> wide, blended over the pixels.</summary>
        private static void Line(byte[] pixels, int width, int height, float ax, float ay, float bx, float by, float lineWidth, Rgba color)
        {
            float half = lineWidth / 2f;
            int x0 = Math.Max(0, (int)Math.Floor(Math.Min(ax, bx) - half) - 1);
            int x1 = Math.Min(width, (int)Math.Ceiling(Math.Max(ax, bx) + half) + 1);
            int y0 = Math.Max(0, (int)Math.Floor(Math.Min(ay, by) - half) - 1);
            int y1 = Math.Min(height, (int)Math.Ceiling(Math.Max(ay, by) + half) + 1);
            float dx = bx - ax;
            float dy = by - ay;
            float length = Math.Max(1e-6f, (dx * dx) + (dy * dy));
            for (int py = y0; py < y1; py++)
            {
                float y = py + 0.5f;
                for (int px = x0; px < x1; px++)
                {
                    float x = px + 0.5f;
                    float t = Math.Max(0f, Math.Min(1f, (((x - ax) * dx) + ((y - ay) * dy)) / length));
                    float ex = x - (ax + (t * dx));
                    float ey = y - (ay + (t * dy));
                    float cover = Cover((float)Math.Sqrt((ex * ex) + (ey * ey)) - half);
                    if (cover > 0f)
                    {
                        Blend(pixels, ((py * width) + px) * 4, color, cover);
                    }
                }
            }
        }

        /// <summary>Blends a picture over the pixels with its top-left corner at (<paramref name="left"/>, <paramref name="top"/>).</summary>
        private static void Blit(byte[] pixels, int width, int height, byte[] source, int sourceWidth, int sourceHeight, int left, int top)
        {
            for (int sy = 0; sy < sourceHeight; sy++)
            {
                int y = top + sy;
                if (y < 0 || y >= height)
                {
                    continue;
                }

                for (int sx = 0; sx < sourceWidth; sx++)
                {
                    int x = left + sx;
                    if (x < 0 || x >= width)
                    {
                        continue;
                    }

                    int s = ((sy * sourceWidth) + sx) * 4;
                    byte a = source[s + 3];
                    if (a == 0)
                    {
                        continue;
                    }

                    Blend(pixels, ((y * width) + x) * 4, new Rgba(source[s], source[s + 1], source[s + 2], a), 1f);
                }
            }
        }

        /// <summary>Straight-alpha source over: <paramref name="color"/> at <paramref name="cover"/> over the pixel at <paramref name="i"/>.</summary>
        private static void Blend(byte[] pixels, int i, Rgba color, float cover)
        {
            float a = color.A / 255f * cover;
            if (a <= 0f)
            {
                return;
            }

            float da = pixels[i + 3] / 255f;
            float keep = da * (1f - a);
            float oa = a + keep;
            pixels[i] = Byte(((color.R * a) + (pixels[i] * keep)) / oa);
            pixels[i + 1] = Byte(((color.G * a) + (pixels[i + 1] * keep)) / oa);
            pixels[i + 2] = Byte(((color.B * a) + (pixels[i + 2] * keep)) / oa);
            pixels[i + 3] = Byte(oa * 255f);
        }

        private static byte Byte(float v) => (byte)Math.Max(0, Math.Min(255, (int)Math.Round(v)));

        private static float Cover(float distance) => Math.Max(0f, Math.Min(1f, 0.5f - distance));

        /// <summary>The signed distance from (<paramref name="x"/>, <paramref name="y"/>) to a rounded rectangle (negative inside).</summary>
        private static float RoundRect(float x, float y, Box box, float radius)
        {
            float hw = box.Width / 2f;
            float hh = box.Height / 2f;
            float qx = Math.Abs(x - box.CenterX) - (hw - radius);
            float qy = Math.Abs(y - box.CenterY) - (hh - radius);
            float ox = Math.Max(qx, 0f);
            float oy = Math.Max(qy, 0f);
            return (float)Math.Sqrt((ox * ox) + (oy * oy)) + Math.Min(Math.Max(qx, qy), 0f) - radius;
        }
    }
}
