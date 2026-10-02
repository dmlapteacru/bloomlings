using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The board of frames 7–9 in the reference look (spec 005 contracts/look.md §3.1, §3.6, §4.1; research D2, D12, D14).
    /// <list type="bullet">
    /// <item><description>The board lies on the lawn inside a border of sandy stone blocks (<see cref="BoardLayout"/>).</description></item>
    /// <item><description>Target tiles are candy tiles (board style) that nearly touch, parted by the dark board gap.</description></item>
    /// <item><description>Restored ground shows the finished picture as pale flat cells.</description></item>
    /// <item><description>Stones are stone blocks; specials are candy-like blocks with their white glyph and counter;
    /// keys, the next-layer peek and mystery tiles keep their meaning in the same style.</description></item>
    /// <item><description>Each Garden Entry is a stone arch on its side of the board, where the Bloomlings come out
    /// and walk their routes.</description></item>
    /// </list>
    /// </summary>
    public static class BoardPainter
    {
        /// <summary>How much a target tile is inset into its cell (each side, in cells): the tiles nearly touch.</summary>
        private const float TileInset = 0.008f;

        /// <summary>How much a restored cell is inset into its cell (each side, in cells).</summary>
        private const float GroundInset = 0.025f;

        /// <summary>The layout each level screen was last drawn with (the walkers' doors depend on it).</summary>
        private static readonly ConditionalWeakTable<LevelScreen, BoardLayout> Layouts = new ConditionalWeakTable<LevelScreen, BoardLayout>();

        /// <summary>The board fitted into <paramref name="area"/> (<see cref="BoardLayout.Fit"/>).</summary>
        public static void Draw(IPainter p, Box area, LevelScreen s)
        {
            LevelView view = s.Session.View;
            Draw(p, BoardLayout.Fit(area, view.Width, view.Height, view.Entries), s);
        }

        /// <summary>
        /// The board in <paramref name="layout"/>: on the gameplay screen the reference regions' fit
        /// (<see cref="ReferenceGameplayRegions.FitBoard"/>: the stone border at most 0.86 of the safe width, a bottom
        /// entry's arch in the entry strip; spec 005 FR-020, contracts/look.md §6.1).
        /// </summary>
        public static void Draw(IPainter p, BoardLayout layout, LevelScreen s)
        {
            LevelView view = s.Session.View;
            int w = view.Width;
            int h = view.Height;
            Layouts.AddOrUpdate(s, layout);
            float cell = layout.Cell;
            s.Board = (layout.Grid.Left, layout.Grid.Top, cell, h);

            // The entries' stone arches on the lawn, then the stone border with the dark gap the tiles lie in.
            for (int i = 0; i < layout.Arches.Count; i++)
            {
                Arch(p, layout.Arches[i]);
            }

            Kit.StoneBorder(p, layout.Grid, cell);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var pos = new CellPos(x, y);
                    CellInfo info = s.Animator.Cell(pos);
                    Box full = CellBox(s, pos);
                    switch (info.Kind)
                    {
                        case CellKind.Open:
                            if (IsGrass(s.Session.Definition, s.Session.Picture, x, y))
                            {
                                // The picture's background reads as garden (spec 005 FR-020).
                                Kit.GrassCell(p, full, UiRaster.GrassSeed(x, y));
                            }
                            else
                            {
                                Ground(p, full, cell, PictureColor(s, x, y));
                            }

                            break;
                        case CellKind.Stone:
                            Stone(p, full, cell, PictureColor(s, x, y), x, y);
                            break;
                        case CellKind.Special:
                            Special(p, s, info, full, cell);
                            break;
                        default:
                            Tile(p, info, full, cell, 1f, 1f);
                            if (s.Targeting == Recovery.BloomBurst && info.Visible.HasValue && !info.MysteryHidden)
                            {
                                // Bloom Burst targeting: a ring on every candidate tile, which takes the tap.
                                p.Mark("fx.burst");
                                VariantId variant = info.Visible.Value;
                                float pulse = 0.75f + (0.25f * (float)Math.Sin(p.Now * 6.0));
                                p.StrokeRound(full.Inset(cell * 0.05f), cell * 0.12f, Math.Max(p.U(4f), cell * 0.05f), C.BoosterBloomBurst.WithAlpha(0.85f * pulse));
                                p.Hit(full, () => s.UseBooster(BoosterKind.BloomBurst, new UseBloomBurst(variant)));
                            }

                            break;
                    }
                }
            }

            // Restored tiles shrink away; the picture shows beneath.
            foreach (Fade fade in s.Animator.Fades)
            {
                float k = Visuals.Clamp01((s.Animator.Now - fade.Start) / LevelAnimator.FadeSeconds);
                if (fade.Look.Kind == CellKind.Target)
                {
                    Box at = CellBox(s, fade.Cell);
                    Tile(p, fade.Look, at, cell, 1f - (0.5f * k), 1f - k);

                    // A sparkle as the tile is restored; droplets for the Drop family.
                    p.PushAlpha(1f - k);
                    float spark = cell * (0.35f + (0.4f * k));
                    bool drop = fade.Look.Visible.HasValue && Visuals.FamilyOf(fade.Look.Visible.Value) == Family.Drop;
                    if (drop)
                    {
                        Rgba splash = Visuals.ColorOf(fade.Look.Visible!.Value).Lighten(0.72f);
                        p.Shape("fx.droplet", Box.FromCenter(at.CenterX - (cell * 0.2f), at.CenterY - (cell * 0.1f) - (cell * 0.3f * k), spark * 0.6f, spark * 0.6f), splash);
                        p.Shape("fx.droplet", Box.FromCenter(at.CenterX + (cell * 0.22f), at.CenterY - (cell * 0.2f) - (cell * 0.3f * k), spark * 0.5f, spark * 0.5f), splash);
                    }
                    else
                    {
                        p.Shape("fx.sparkle", Box.FromCenter(at.CenterX + (cell * 0.18f), at.CenterY - (cell * 0.18f), spark, spark), Rgba.White);
                    }

                    p.PopAlpha();
                }
            }

            // Bloom Burst: a ring of light spreads over the board.
            if (s.LastBooster.HasValue && s.LastBooster.Value.Kind == BoosterKind.BloomBurst && s.Animator.Now - s.LastBooster.Value.At < 0.6f)
            {
                p.Mark("fx.burst");
                Box board = layout.Grid;
                float k = (s.Animator.Now - s.LastBooster.Value.At) / 0.6f;
                p.PushAlpha(1f - k);
                p.StrokeCircle(board.CenterX, board.CenterY, board.Width * (0.1f + (0.6f * k)), p.U(24f), C.PetalCenter);
                p.PopAlpha();
            }

            DrawWalkers(p, s, cell);
        }

        public static Box CellBox(LevelScreen s, CellPos pos)
        {
            (float ox, float oy, float cell, int height) = s.Board;
            float left = ox + (pos.X * cell);
            float top = oy + ((height - 1 - pos.Y) * cell);
            return new Box(left, top, left + cell, top + cell);
        }

        /// <summary>Where Bloomlings come in through an entry: the door of its stone arch.</summary>
        public static (float X, float Y) EntryPoint(LevelScreen s, EntryDef entry)
        {
            IReadOnlyList<EntryDef> entries = s.Session.View.Entries;
            if (Layouts.TryGetValue(s, out BoardLayout? layout))
            {
                for (int i = 0; i < entries.Count && i < layout.Arches.Count; i++)
                {
                    if (entries[i] == entry)
                    {
                        return layout.Arches[i].Door;
                    }
                }
            }

            // Before the first frame: just outside the entry cell.
            Box c = CellBox(s, entry.Cell);
            float d = s.Board.Cell * 0.78f;
            return entry.Side switch
            {
                EntrySide.Top => (c.CenterX, c.CenterY - d),
                EntrySide.Left => (c.CenterX - d, c.CenterY),
                EntrySide.Right => (c.CenterX + d, c.CenterY),
                _ => (c.CenterX, c.CenterY + d),
            };
        }

        /// <summary>A Garden Entry: its stone arch (<c>board.arch</c>) on its piers on its side of the board, over a soft ground shadow.</summary>
        private static void Arch(IPainter p, EntryArch arch)
        {
            p.Mark("tile.entry");
            float r = arch.Radius;
            (float sx, float sy) = arch.Side switch
            {
                EntrySide.Top => (0f, -r * 0.5f),
                EntrySide.Left => (-r * 0.5f, 0f),
                EntrySide.Right => (r * 0.5f, 0f),
                _ => (0f, r * 0.5f),
            };
            bool across = arch.Side == EntrySide.Left || arch.Side == EntrySide.Right;
            Box shadow = Box.FromCenter(arch.BaseX - (sx * 0.86f) + (r * 0.04f), arch.BaseY - (sy * 0.86f) + (r * 0.08f), across ? r * 1.16f : r * 2.14f, across ? r * 2.14f : r * 1.16f);
            Kit.SoftShadow(p, shadow, r, 0.18f, 0.02f);
            Kit.StoneArch(p, arch);
        }

        /// <summary>
        /// A target tile (§3.1): a candy tile in the board style, nearly filling its cell. A hidden mystery tile is the
        /// lilac "?" tile; the next layer peeks from a small chip in the top-right corner and a key waiting under the tile
        /// shows on a cream disc in the top-left corner, both kept off the symbol.
        /// </summary>
        public static void Tile(IPainter p, CellInfo info, Box full, float cell, float scale, float alpha)
        {
            p.Mark("tile.base");
            // Drawn at its rest size and shrunk with the canvas (a fading tile), so its picture is the one the board
            // already has, not a new size every frame.
            Box box = full.Inset(cell * TileInset);
            bool shrinking = scale != 1f;
            if (shrinking)
            {
                p.PushTransform(0f, 0f, scale, box.CenterX, box.CenterY);
            }

            p.PushAlpha(alpha);
            if (info.MysteryHidden || !info.Visible.HasValue)
            {
                Kit.CandyTile(p, box, (VariantId?)null, TileStyle.Board);
            }
            else
            {
                Kit.CandyTile(p, box, info.Visible.Value, TileStyle.Board);

                if (info.RemainingLayers > 1 && info.Next.HasValue)
                {
                    LayerPeek(p, box, info.Next.Value);
                }

                if (info.KeyId != null)
                {
                    KeyCorner(p, box);
                }
            }

            p.PopAlpha();
            if (shrinking)
            {
                p.PopTransform();
            }
        }

        /// <summary>
        /// The next layer (<c>tile.layer_peek</c>): a small candy tile of its variant in a cream ring with a thin dark rim,
        /// tucked into the tile's top-right corner like a chip stacked on it.
        /// </summary>
        private static void LayerPeek(IPainter p, Box tile, VariantId next)
        {
            p.Mark("tile.layer_peek");
            float s = tile.Width;
            float chip = s * 0.4f;
            float m = s * 0.03f;
            var box = new Box(tile.Right - m - chip, tile.Top + m, tile.Right - m, tile.Top + m + chip);
            float ring = Math.Max(1f, chip * 0.1f);
            float line = Math.Max(1f, chip * 0.045f);
            Kit.SoftShadow(p, box, chip * 0.26f, 0.3f, 0.06f);
            p.FillRound(box.Inset(-line), (chip * 0.24f) + line, GardenLook.BoardGap);
            p.FillRoundGradient(box, chip * 0.24f, C.CreamTop, C.CreamFace);
            Kit.CandyTile(p, box.Inset(ring), next, TileStyle.Board);
        }

        /// <summary>A key waiting under a tile (<c>tile.key</c>): the gold key on a cream disc in the tile's top-left corner.</summary>
        private static void KeyCorner(IPainter p, Box tile)
        {
            float s = tile.Width;
            float d = s * 0.38f;
            float m = s * 0.03f;
            Box disc = new Box(tile.Left + m, tile.Top + m, tile.Left + m + d, tile.Top + m + d);
            float line = Math.Max(1f, d * 0.05f);
            Kit.SoftShadow(p, disc, d / 2f, 0.3f, 0.06f);
            p.FillCircle(disc.CenterX, disc.CenterY, (d / 2f) + line, C.CreamLine);
            p.FillRoundGradient(disc, d / 2f, C.CreamTop, C.CreamFace);
            Box key = disc.Inset(d * 0.14f);
            Func<float, float, float> sdf = ShapeLibrary.Get("tile.key");
            p.ShapeOf("tile.key/line", (x, y) => sdf(x, y) - 0.09f, key, C.InkBrown);
            p.Shape("tile.key", key, C.MedalGold);
        }

        /// <summary>
        /// Restored ground (<c>tile.ground</c>, §4.1): a pale flat cell of the finished picture with a small radius and a
        /// faint inner shadow along its top, over a slightly deeper shade of itself, so the picture reads as one calm
        /// mosaic next to the saturated tiles.
        /// </summary>
        private static void Ground(IPainter p, Box full, float cell, Rgba color)
        {
            p.Mark("tile.ground");
            p.FillRect(full, color.Darken(0.12f));
            Box box = full.Inset(cell * GroundInset);
            float r = box.Width * 0.1f;
            p.FillRound(box, r, color);
            p.PushClip(box);
            p.FillRoundGradient(new Box(box.Left, box.Top, box.Right, box.Top + (box.Height * 0.24f)), r, C.GardenShadow.WithAlpha(0.12f), C.GardenShadow.WithAlpha(0f));
            p.PopClip();
        }

        /// <summary>
        /// A stone obstacle (<c>tile.stone</c>, §4.1): a raised block of the border's sandy stone (<c>mat.stone</c>) over a
        /// soft shadow, sitting on the restored ground, with a crack across it, so it reads as part of the garden's
        /// stonework, a fixed obstacle among the candy tiles. The crack and the mottling follow the cell, so neighboring
        /// stones differ.
        /// </summary>
        private static void Stone(IPainter p, Box full, float cell, Rgba ground, int x, int y)
        {
            p.Mark("tile.stone");
            Ground(p, full, cell, ground);
            Box box = full.Inset(cell * 0.06f);
            Kit.SoftShadow(p, box, box.Width * 0.24f, 0.36f, 0.08f);
            Kit.StoneBlock(p, box, 21 + (((x * 5) + (y * 3)) % 6), 0.24f);

            // The crack: a jagged groove from the upper edge toward the middle with one branch, over a light lower lip.
            bool flip = ((x + y) % 2) == 1;
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
                    p.Line(line[i].X + dx, line[i].Y + dy, line[i + 1].X + dx, line[i + 1].Y + dy, groove, C.StoneTop.Lighten(0.5f).WithAlpha(0.9f));
                    p.Line(line[i].X, line[i].Y, line[i + 1].X, line[i + 1].Y, groove, C.StoneLine.WithAlpha(0.72f));
                }
            }
        }

        /// <summary>
        /// A special (§4.1): a candy-like raised block in the special's color (a lighter top, a darker lip and a crisp
        /// outline) with its white glyph outlined in a darker shade, and, until it opens, its counter on a count badge.
        /// </summary>
        private static void Special(IPainter p, LevelScreen s, CellInfo info, Box full, float cell)
        {
            (int progress, int total, bool triggered) = info.SpecialId != null ? s.Animator.Special(info.SpecialId) : (0, 0, true);
            SpecialType type = SpecialType.Gate;
            foreach (SpecialInfo special in s.Session.View.Specials)
            {
                if (special.Id == info.SpecialId)
                {
                    type = special.Type;
                }
            }

            (Rgba color, string shape) = type switch
            {
                SpecialType.Fountain => (C.SpecialFountain, "special.fountain"),
                SpecialType.Chest => (C.SpecialChest, "special.chest"),
                SpecialType.Statue => (C.SpecialStatue, "special.statue"),
                SpecialType.Bridge => (C.SpecialBridge, triggered ? "special.bridge" : "special.bridge_broken"),
                _ => (C.SpecialGate, "special.gate"),
            };
            Box box = full.Inset(cell * TileInset);
            color = GardenLook.SpecialFace(color);
            Box face = CandyBlock(p, box, color);
            bool counter = !triggered && total > 1;
            float glyph = Math.Min(face.Width, face.Height) * (counter ? 0.56f : 0.66f);
            Box g = Box.FromCenter(face.CenterX, face.CenterY - (face.Height * (counter ? 0.1f : 0f)), glyph, glyph);
            Func<float, float, float> sdf = ShapeLibrary.Get(shape);
            p.ShapeOf(shape + "/line", (x, y) => sdf(x, y) - 0.08f, g, color.Darken(0.45f));
            p.Shape(shape, g, Rgba.White);
            if (counter)
            {
                // The counter on a count badge over the block's bottom edge, big enough to read at a glance.
                float badge = cell * 0.36f;
                Kit.CountBadge(p, face.CenterX, face.Bottom - (badge * 0.12f), badge, progress + "/" + total);
            }
        }

        /// <summary>
        /// A raised block in the candy tile's manner (§3.1) for the specials: a crisp darker outline, a darker lip along
        /// the bottom, a face lighter at the top, a faint gloss band and a thin light bevel. Returns the face.
        /// </summary>
        private static Box CandyBlock(IPainter p, Box box, Rgba color)
        {
            float s = Math.Min(box.Width, box.Height);
            float r = Math.Max(3f, s * 0.1f);
            float line = Math.Max(1f, s * 0.022f);
            float lip = s * 0.07f;
            p.FillRound(box, r, color.Darken(0.45f));
            Box inner = box.Inset(line);
            float ri = Math.Max(1f, r - line);
            p.FillRound(inner, ri, color.Darken(0.28f));
            var face = new Box(inner.Left, inner.Top, inner.Right, inner.Bottom - lip);
            p.FillRoundGradient(face, ri, color.Lighten(0.28f), color);
            p.StrokeRound(face.Inset(line * 0.6f), ri, line * 0.8f, color.Lighten(0.45f).WithAlpha(0.45f));
            var band = new Box(face.Left + (face.Width * 0.1f), face.Top + (face.Height * 0.06f), face.Right - (face.Width * 0.1f), face.Top + (face.Height * 0.26f));
            p.FillRoundGradient(band, band.Height / 2f, Rgba.White.WithAlpha(0.16f), Rgba.White.WithAlpha(0f));
            return face;
        }

        /// <summary>Bloomlings on their way: small figures of their family in the variant color, hopping along the route.</summary>
        private static void DrawWalkers(IPainter p, LevelScreen s, float cell)
        {
            LevelView view = s.Session.View;
            foreach (Walker walker in s.Animator.Walkers)
            {
                if (walker.Route.Count == 0)
                {
                    continue;
                }

                var points = new List<(float X, float Y)>();
                foreach (EntryDef candidate in view.Entries)
                {
                    if (candidate.Cell == walker.Route[0])
                    {
                        points.Add(EntryPoint(s, candidate));
                        break;
                    }
                }

                foreach (CellPos pos in walker.Route)
                {
                    Box b = CellBox(s, pos);
                    points.Add((b.CenterX, b.CenterY));
                }

                float progress = Visuals.Clamp01(s.Animator.WaveTime / Math.Max(0.05f, walker.Arrival));
                if (progress >= 1f && s.Animator.WaveTime > walker.Arrival + 0.12f)
                {
                    continue;
                }

                float t = progress * (points.Count - 1);
                int i = Math.Min((int)t, Math.Max(0, points.Count - 2));
                float f = points.Count == 1 ? 0f : t - i;
                float x = points.Count == 1 ? points[0].X : points[i].X + ((points[i + 1].X - points[i].X) * f);
                float y = points.Count == 1 ? points[0].Y : points[i].Y + ((points[i + 1].Y - points[i].Y) * f);
                y -= Math.Abs((float)Math.Sin(f * Math.PI)) * cell * 0.2f;
                float size = cell * 0.82f;
                Box body = Box.FromCenter(x, y - (size * 0.1f), size, size);
                Visuals.GroundShadow(p, Box.FromCenter(x, y - (size * 0.04f), size, size));
                Visuals.Character(p, body, walker.Variant, CharacterMood.Happy);
            }
        }

        /// <summary>
        /// A light variant-tinted block with the variant's character on it (spec 004 FR-012, FR-013): since spec 005 board
        /// tiles are candy tiles; the Bloomlings sheet (frame 24) still shows each character on its spec 004 tile.
        /// Returns the face.
        /// </summary>
        public static Box CharacterBlock(IPainter p, Box box, VariantId variant)
        {
            Rgba tint = DesignTokens.CharacterTile(Visuals.ColorOf(variant));
            Box face = Kit.Block(p, box, tint, DesignTokens.TileEdge(tint), box.Width * DesignTokens.Radius.Tile, Kit.CellLip(p, box.Height), DesignTokens.Garden.CellHighlightAlpha, top: DesignTokens.TileTop(tint));
            Visuals.Character(p, CharacterArt.OnTile(face), variant, CharacterMood.Happy);
            return face;
        }

        /// <summary>The finished picture under a restored cell: a pale version of its role's variant color (§4.1).</summary>
        public static Rgba PictureColor(LevelScreen s, int x, int y) => PictureColor(s.Session.Definition, s.Session.Picture, x, y);

        public static Rgba PictureColor(LevelDefinition definition, BasePicture picture, int x, int y)
        {
            (VariantId? variant, int value) = PictureCell(definition, picture, x, y);
            if (variant.HasValue)
            {
                return Visuals.ColorOf(variant.Value).Lighten(0.55f);
            }

            return value == BasePicture.Stone ? C.StoneFace.Lighten(0.35f) : C.TileGround;
        }

        /// <summary>Whether a picture cell is the picture's background (no role, no stone): it shows as grass (<c>tile.grass</c>).</summary>
        public static bool IsGrass(LevelDefinition definition, BasePicture picture, int x, int y)
        {
            (VariantId? variant, int value) = PictureCell(definition, picture, x, y);
            return !variant.HasValue && value != BasePicture.Stone;
        }

        /// <summary>The variant a picture cell's role maps to (null for ground and stones) and the picture's raw value.</summary>
        private static (VariantId? Variant, int Value) PictureCell(LevelDefinition definition, BasePicture picture, int x, int y)
        {
            int px = definition.Picture.Mirror == Mirror.Horizontal ? picture.Width - 1 - x : x;
            if (px < 0 || px >= picture.Width || y < 0 || y >= picture.Height)
            {
                return (null, BasePicture.Empty);
            }

            int value = picture.CellAt(px, y);
            if (value >= 0 && definition.Mapping.TryGetValue(picture.Roles[value].RoleId, out VariantId variant))
            {
                return (variant, value);
            }

            return (null, value);
        }

        /// <summary>
        /// A finished picture (the win card and the Collection; research D14, §4.4): each cell a flat full-color candy tile
        /// of its role's variant (no lip, a small gloss, the board-style symbol), stones as stone blocks and ground as
        /// cream cells, inside a thin stone border when the cells are big enough for one.
        /// </summary>
        public static void Picture(IPainter p, Box box, LevelDefinition definition, BasePicture picture)
        {
            p.Mark("tile.picture");
            int w = Math.Max(1, picture.Width);
            int h = Math.Max(1, picture.Height);
            const float thin = 0.3f;
            float rim = BoardLayout.Gap + thin;
            float cell = Math.Min(box.Width / (w + (2f * rim)), box.Height / (h + (2f * rim)));
            bool framed = cell >= p.U(16f);
            if (!framed)
            {
                cell = Math.Min(box.Width / w, box.Height / h);
            }

            float ox = box.CenterX - (cell * w / 2f);
            float oy = box.CenterY - (cell * h / 2f);
            var grid = new Box(ox, oy, ox + (cell * w), oy + (cell * h));
            if (framed)
            {
                Kit.StoneBorder(p, grid, cell, thin);
            }
            else
            {
                p.FillRound(grid, cell * 0.2f, GardenLook.BoardGap);
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float left = ox + (x * cell);
                    float top = oy + ((h - 1 - y) * cell);
                    var full = new Box(left, top, left + cell, top + cell);
                    (VariantId? variant, int value) = PictureCell(definition, picture, x, y);
                    if (variant.HasValue)
                    {
                        Kit.CandyTile(p, full.Inset(cell * TileInset), variant.Value, TileStyle.Flat);
                    }
                    else if (value == BasePicture.Stone)
                    {
                        Kit.StoneBlock(p, full.Inset(cell * 0.04f), 21 + (((x * 5) + (y * 3)) % 6), 0.26f);
                    }
                    else
                    {
                        Kit.GrassCell(p, full, UiRaster.GrassSeed(x, y));
                    }
                }
            }
        }
    }
}
