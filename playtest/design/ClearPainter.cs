using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The board's clearing style in the playtest (spec 005 FR-038): each frame it turns the animator's walkers and
    /// just-cleared tiles into the kit's list (<see cref="ClearLook"/>, in cell units), and draws its items over the
    /// tiles (<see cref="FxLayer.Board"/>, from <see cref="BoardPainter"/>) and over the tray and slots
    /// (<see cref="FxLayer.Over"/>, from <see cref="LevelScreen"/>). The Unity client's <c>ClearFxView</c> draws the
    /// same list. The Store's previews draw theirs with <see cref="DrawItems"/> too.
    /// </summary>
    public static class ClearPainter
    {
        /// <summary>
        /// Builds this frame's list for <paramref name="s"/>: <paramref name="slots"/> are the slot plates, where the tiles
        /// go. Call once the board's origin and cell (<see cref="LevelScreen.Board"/>) are known.
        /// </summary>
        public static void Build(LevelScreen s, IReadOnlyList<Box> slots)
        {
            FxList list = s.Fx;
            list.Clear();
            s.Held.Clear();
            s.ClearFades.Clear();
            ClearStyle style = s.Animator.Style;
            float now = s.Animator.Now;
            foreach (Fade fade in s.Animator.Fades)
            {
                if (fade.Look.Kind != CellKind.Target || !fade.Look.Visible.HasValue)
                {
                    continue;
                }

                s.ClearFades.Add(new ClearFade(Center(s, fade.Cell), fade.Look.Visible.Value, fade.Start, Seat(s, slots, fade.PodId)));
            }

            foreach (Walker walker in s.Animator.Walkers)
            {
                if (walker.Route.Count == 0)
                {
                    continue;
                }

                var walk = new ClearWalk(Points(s, walker.Route), walker.Variant, walker.Start, walker.Arrival, Seat(s, slots, walker.PodId));
                if (ClearLook.Holds(style, walk, now))
                {
                    s.Held.Add(walker.Route[walker.Route.Count - 1]);
                }

                ClearLook.Walker(list, style, walk, now);
            }

            foreach (ClearFade fade in s.ClearFades)
            {
                ClearLook.Restore(list, style, fade, now);
            }
        }

        /// <summary>How many degrees the tile at <paramref name="pos"/> sways beside a just-opened flower (Blossom).</summary>
        public static float Sway(LevelScreen s, CellPos pos) =>
            s.ClearFades.Count == 0 ? 0f : ClearLook.Sway(s.Animator.Style, s.ClearFades, Center(s, pos), s.Animator.Now);

        /// <summary>Draws the built list's items of <paramref name="layer"/> on the board's grid.</summary>
        public static void Draw(IPainter p, LevelScreen s, FxLayer layer)
        {
            (float ox, float oy, float cell, int _) = s.Board;
            DrawItems(p, s.Fx.Items, layer, ox, oy, cell);
        }

        /// <summary>Draws <paramref name="items"/> of <paramref name="layer"/>, cell (0, 0)'s top left at (ox, oy), a cell <paramref name="cell"/> wide.</summary>
        public static void DrawItems(IPainter p, IReadOnlyList<FxItem> items, FxLayer layer, float ox, float oy, float cell)
        {
            string? marked = null;
            foreach (FxItem item in items)
            {
                if (item.Layer != layer || Math.Abs(item.Sx) < 1e-3f || Math.Abs(item.Sy) < 1e-3f)
                {
                    continue;
                }

                if (item.Slot != marked)
                {
                    p.Mark(item.Slot);
                    marked = item.Slot;
                }

                float x = ox + (item.X * cell);
                float y = oy + (item.Y * cell);
                float w = item.W * cell;
                float h = item.H * cell;
                bool turned = Math.Abs(item.Turn) > 0.01f;
                bool squashed = Math.Abs(item.Sx - 1f) > 1e-3f || Math.Abs(item.Sy - 1f) > 1e-3f;
                p.PushAlpha(item.Alpha);
                if (turned)
                {
                    p.PushRotate(item.Turn, x, y);
                }

                if (squashed)
                {
                    p.PushSquash(item.Sx, item.Sy, x, y);
                }

                Box box = Box.FromCenter(x, y, w, h);
                switch (item.Kind)
                {
                    case FxKind.Character:
                        Visuals.Character(p, box, item.Variant, item.Mood);
                        break;
                    case FxKind.Tile:
                        Kit.CandyTile(p, box, item.Variant, TileStyle.Board);
                        break;
                    case FxKind.Circle:
                        if (Math.Abs(w - h) < 0.5f)
                        {
                            p.FillCircle(x, y, w / 2f, item.Color);
                        }
                        else
                        {
                            p.PushSquash(1f, h / Math.Max(0.01f, w), x, y);
                            p.FillCircle(x, y, w / 2f, item.Color);
                            p.PopTransform();
                        }

                        break;
                    case FxKind.Ring:
                        p.StrokeCircle(x, y, w / 2f, Math.Max(1f, item.Line * cell), item.Color);
                        break;
                    case FxKind.Round:
                        p.FillRound(box, item.Radius * cell, item.Color);
                        break;
                    case FxKind.RoundRing:
                        p.StrokeRound(box, item.Radius * cell, Math.Max(1f, item.Line * cell), item.Color);
                        break;
                    case FxKind.Shape:
                        p.Shape(item.Shape ?? ShapeLibrary.Fallback, box, item.Color);
                        break;
                }

                if (squashed)
                {
                    p.PopTransform();
                }

                if (turned)
                {
                    p.PopTransform();
                }

                p.PopAlpha();
            }
        }

        // A cell's center in cell units (y down, the grid's top left at 0, 0).
        private static (float X, float Y) Center(LevelScreen s, CellPos pos)
        {
            (float _, float _, float _, int height) = s.Board;
            return (pos.X + 0.5f, height - 1 - pos.Y + 0.5f);
        }

        // A walker's way: its arch's door, then its route's cells.
        private static List<(float X, float Y)> Points(LevelScreen s, IReadOnlyList<CellPos> route)
        {
            (float ox, float oy, float cell, int _) = s.Board;
            var points = new List<(float X, float Y)>(route.Count + 1);
            foreach (EntryDef entry in s.Session.View.Entries)
            {
                if (entry.Cell == route[0])
                {
                    (float dx, float dy) = BoardPainter.EntryPoint(s, entry);
                    points.Add(((dx - ox) / cell, (dy - oy) / cell));
                    break;
                }
            }

            foreach (CellPos pos in route)
            {
                points.Add(Center(s, pos));
            }

            return points;
        }

        // Where a pod's tiles go in its slot (the plate's tile), in cell units; null while it shows in none.
        private static Box? Seat(LevelScreen s, IReadOnlyList<Box> slots, string? podId)
        {
            int place = podId == null ? -1 : s.Animator.PlaceOf(podId);
            if (place < 0 || place >= slots.Count)
            {
                return null;
            }

            (float ox, float oy, float cell, int _) = s.Board;
            Box tile = ReferenceGameplayRegions.SlotTile(slots[place]);
            return new Box((tile.Left - ox) / cell, (tile.Top - oy) / cell, (tile.Right - ox) / cell, (tile.Bottom - oy) / cell);
        }
    }
}
