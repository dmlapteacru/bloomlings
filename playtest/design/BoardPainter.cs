using System;
using System.Collections.Generic;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The board of frames 7–9 (spec 002 FR-011, FR-015).
    /// <list type="bullet">
    /// <item><description>Raised rounded tiles in the variant colors carry the variant symbol, never a face.</description></item>
    /// <item><description>Restored ground shows the finished picture in light colors.</description></item>
    /// <item><description>Stones, keys, locks, layers, mystery tiles and specials are garden objects that never hide a
    /// tile's symbol.</description></item>
    /// <item><description>The Garden Entry sits below the board.</description></item>
    /// <item><description>Bloomlings walk their routes.</description></item>
    /// </list>
    /// </summary>
    public static class BoardPainter
    {
        public static void Draw(IPainter p, Box area, LevelScreen s)
        {
            LevelView view = s.Session.View;
            int w = view.Width;
            int h = view.Height;
            float cell = Math.Min(area.Width / w, area.Height / (h + 0.7f));
            float ox = area.CenterX - (cell * w / 2f);
            float oy = area.Top + ((area.Height - (cell * (h + 0.7f))) / 2f);
            s.Board = (ox, oy, cell, h);

            // A soft light panel behind the board keeps its contrast over the garden (FR-008).
            Box panel = new Box(ox - (cell * 0.18f), oy - (cell * 0.18f), ox + (cell * w) + (cell * 0.18f), oy + (cell * h) + (cell * 0.18f));
            p.FillRound(panel.Offset(0f, p.U(6f)), cell * 0.4f, Rgba.Black.WithAlpha(0.06f));
            p.FillRound(panel, cell * 0.4f, C.SurfacePanel.WithAlpha(0.72f));

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
                            p.Mark("tile.ground");
                            p.FillRound(full.Inset(cell * 0.02f), cell * 0.12f, PictureColor(s, x, y));
                            break;
                        case CellKind.Stone:
                            Stone(p, full, cell);
                            break;
                        case CellKind.Special:
                            Special(p, s, info, full, cell);
                            break;
                        default:
                            Tile(p, info, full, cell, 1f, 1f);
                            if (s.Targeting == Recovery.BloomBurst && info.Visible.HasValue && !info.MysteryHidden)
                            {
                                VariantId variant = info.Visible.Value;
                                p.StrokeRound(full.Inset(cell * 0.04f), cell * 0.2f, p.U(4f), C.BoosterBloomBurst.WithAlpha(0.7f));
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

                    // A sparkle as the tile is restored.
                    p.PushAlpha(1f - k);
                    float spark = cell * (0.35f + (0.4f * k));
                    p.Shape("fx.sparkle", Box.FromCenter(at.CenterX + (cell * 0.18f), at.CenterY - (cell * 0.18f), spark, spark), Rgba.White);
                    p.PopAlpha();
                }
            }

            // Bloom Burst: a ring of light spreads over the board.
            if (s.LastBooster.HasValue && s.LastBooster.Value.Kind == BoosterKind.BloomBurst && s.Animator.Now - s.LastBooster.Value.At < 0.6f)
            {
                p.Mark("fx.burst");
                float k = (s.Animator.Now - s.LastBooster.Value.At) / 0.6f;
                p.PushAlpha(1f - k);
                p.StrokeCircle(panel.CenterX, panel.CenterY, panel.Width * (0.1f + (0.6f * k)), p.U(24f), C.PetalCenter);
                p.PopAlpha();
            }

            // The Garden Entry markers below the board.
            foreach (EntryDef entry in view.Entries)
            {
                p.Mark("tile.entry");
                (float ex, float ey) = EntryPoint(s, entry);
                Box marker = Box.FromCenter(ex, ey, cell * 0.8f, cell * 0.36f);
                p.FillRound(marker, marker.Height / 2f, C.PetalCenter);
                p.StrokeRound(marker, marker.Height / 2f, p.U(3f), C.PetalCenter.Darken(0.25f));
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

        public static (float X, float Y) EntryPoint(LevelScreen s, EntryDef entry)
        {
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

        /// <summary>A target tile: raised, rounded, in its variant color, with its symbol in ink (research R7).</summary>
        public static void Tile(IPainter p, CellInfo info, Box full, float cell, float scale, float alpha)
        {
            p.Mark("tile.base");
            Box box = full.Inset(cell * 0.05f).Scale(scale, scale);
            float radius = box.Width * DesignTokens.Radius.Tile;
            p.PushAlpha(alpha);
            if (info.MysteryHidden || !info.Visible.HasValue)
            {
                Rgba mystery = Rgba.FromHex("#B8AFCB");
                Kit.Raised(p, box, mystery, mystery.Darken(0.25f), radius, top: mystery.Lighten(0.15f));
                p.Shape("tile.mystery", Box.FromCenter(box.CenterX, box.CenterY - (box.Height * 0.03f), box.Width * 0.6f, box.Width * 0.6f), Rgba.White);
                p.PopAlpha();
                return;
            }

            VariantId variant = info.Visible.Value;
            Rgba color = Visuals.ColorOf(variant);
            Box face = Kit.Raised(p, box, color, DesignTokens.TileEdge(color), radius, top: DesignTokens.TileTop(color));
            float symbol = face.Width * 0.6f;
            p.Shape(Visuals.SymbolOf(variant), Box.FromCenter(face.CenterX, face.CenterY, symbol, symbol), color.Ink);

            // The next layer peeks in the top-right corner (never over the symbol).
            if (info.RemainingLayers > 1 && info.Next.HasValue)
            {
                p.Mark("tile.layer_peek");
                Rgba next = Visuals.ColorOf(info.Next.Value);
                float c = face.Width * 0.36f;
                Box corner = new Box(face.Right - c, face.Top - (c * 0.15f), face.Right + (c * 0.15f), face.Top + c);
                p.FillRound(corner, c * 0.3f, Rgba.White);
                p.FillRound(corner.Inset(p.U(3f)), c * 0.26f, next);
                p.Shape(Visuals.SymbolOf(info.Next.Value), corner.Inset(corner.Width * 0.22f), next.Ink);
            }

            // A key waiting under the tile shows in the top-left corner.
            if (info.KeyId != null)
            {
                float k = face.Width * 0.4f;
                Box key = new Box(face.Left - (k * 0.15f), face.Top - (k * 0.15f), face.Left + k, face.Top + k);
                p.FillCircle(key.CenterX, key.CenterY, k * 0.52f, Rgba.White);
                p.Shape("tile.key", key.Inset(k * 0.08f), C.MedalGold.Darken(0.2f));
            }

            p.PopAlpha();
        }

        private static void Stone(IPainter p, Box full, float cell)
        {
            Box box = full.Inset(cell * 0.06f);
            p.Shape("tile.stone", box.Offset(0f, cell * 0.05f), C.TileStoneEdge);
            p.Shape("tile.stone", box, C.TileStone);
            p.Shape("tile.stone", box.Scale(0.7f, 0.55f).Offset(-cell * 0.06f, -cell * 0.12f), C.TileStone.Lighten(0.25f));
        }

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
                SpecialType.Fountain => (Rgba.FromHex("#8FB4D6"), "special.fountain"),
                SpecialType.Chest => (Rgba.FromHex("#C08A57"), "special.chest"),
                SpecialType.Statue => (Rgba.FromHex("#A7A9BA"), "special.statue"),
                SpecialType.Bridge => (Rgba.FromHex("#A57C58"), triggered ? "special.bridge" : "special.bridge_broken"),
                _ => (Rgba.FromHex("#6E9A5B"), "special.gate"),
            };
            Box box = full.Inset(cell * 0.05f);
            Box face = Kit.Raised(p, box, color, color.Darken(0.28f), box.Width * 0.18f, top: color.Lighten(0.12f));
            p.Shape(shape, face.Inset(face.Width * 0.16f), Rgba.White.WithAlpha(0.95f));
            if (!triggered && total > 1)
            {
                Box count = Box.FromCenter(face.CenterX, face.Bottom - (face.Height * 0.12f), face.Width * 0.9f, face.Height * 0.34f);
                p.FillRound(count, count.Height / 2f, C.BadgeCount.WithAlpha(0.85f));
                p.Text(progress + "/" + total, count.CenterX, count.CenterY, T.Badge, C.TextOnColor, count.Width * 0.9f, sizeScale: cell / p.U(120f));
            }
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
                float size = cell * 0.78f;
                Box body = Box.FromCenter(x, y - (size * 0.1f), size, size);
                p.FillCircle(x, y + (size * 0.42f), size * 0.28f, Rgba.Black.WithAlpha(0.12f));
                p.Shape(Client.UI.Design.ShapeLibrary.SilhouetteId(Visuals.FamilyOf(walker.Variant)), body.Inset(-p.U(3f)), Rgba.White);
                Visuals.Bloomling(p, body, Visuals.FamilyOf(walker.Variant), Visuals.ColorOf(walker.Variant), Visuals.SymbolOf(walker.Variant), face: true, symbolScale: 0.42f);
            }
        }

        /// <summary>The finished picture under a restored cell: a light version of its role's variant color.</summary>
        public static Rgba PictureColor(LevelScreen s, int x, int y) => PictureColor(s.Session.Definition, s.Session.Picture, x, y);

        public static Rgba PictureColor(LevelDefinition definition, BasePicture picture, int x, int y)
        {
            int px = definition.Picture.Mirror == Mirror.Horizontal ? picture.Width - 1 - x : x;
            if (px < 0 || px >= picture.Width || y < 0 || y >= picture.Height)
            {
                return C.TileGround;
            }

            int value = picture.CellAt(px, y);
            if (value >= 0 && definition.Mapping.TryGetValue(picture.Roles[value].RoleId, out VariantId variant))
            {
                return Visuals.ColorOf(variant).Lighten(0.45f);
            }

            return value == BasePicture.Stone ? C.TileStone.Lighten(0.2f) : C.TileGround;
        }

        /// <summary>A small finished picture (win card, Collection): every cell in its light picture color.</summary>
        public static void Picture(IPainter p, Box box, LevelDefinition definition, BasePicture picture)
        {
            p.Mark("tile.picture");
            int w = Math.Max(1, picture.Width);
            int h = Math.Max(1, picture.Height);
            float cell = Math.Min(box.Width / w, box.Height / h);
            float ox = box.CenterX - (cell * w / 2f);
            float oy = box.CenterY - (cell * h / 2f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float left = ox + (x * cell);
                    float top = oy + ((h - 1 - y) * cell);
                    p.FillRound(new Box(left, top, left + cell, top + cell).Inset(cell * 0.03f), cell * 0.15f, PictureColor(definition, picture, x, y));
                }
            }
        }
    }
}
