using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// One Bloomling's trip as a clearing style draws it, in cell units with y down (<see cref="FxItem"/>): its
    /// <see cref="Points"/> are its arch's door, then its route's cell centers, the target last; it sets off at
    /// <see cref="Start"/> on the timeline clock and its tile clears <see cref="Arrival"/> seconds later (the whole trip,
    /// <see cref="ClearStyles.TripSeconds"/>, perhaps scaled by its wave). <see cref="Seat"/> is where its tile goes in
    /// its pod's slot, when the host knows it.
    /// </summary>
    public readonly struct ClearWalk
    {
        public ClearWalk(IReadOnlyList<(float X, float Y)> points, VariantId variant, float start, float arrival, Box? seat)
        {
            Points = points;
            Variant = variant;
            Start = start;
            Arrival = arrival;
            Seat = seat;
        }

        public IReadOnlyList<(float X, float Y)> Points { get; }

        public VariantId Variant { get; }

        public float Start { get; }

        public float Arrival { get; }

        public Box? Seat { get; }

        /// <summary>The route's cells (the door is not one).</summary>
        public int Cells => Math.Max(1, Points.Count - 1);

        public (float X, float Y) Target => Points[Points.Count - 1];
    }

    /// <summary>A tile the rules just cleared at <see cref="Start"/>: its cell's center, its variant and its slot's seat.</summary>
    public readonly struct ClearFade
    {
        public ClearFade((float X, float Y) center, VariantId variant, float start, Box? seat)
        {
            Center = center;
            Variant = variant;
            Start = start;
            Seat = seat;
        }

        public (float X, float Y) Center { get; }

        public VariantId Variant { get; }

        public float Start { get; }

        public Box? Seat { get; }
    }

    /// <summary>
    /// The clearing styles' look (spec 005 FR-038, contracts/look.md §6.12): for each walker and each just-cleared tile,
    /// the items to draw this frame (<see cref="FxList"/>), so that both builds and the Store's previews draw the same
    /// thing. The walkers walk a little hop every two cells (<see cref="HopsPerCell"/>) and lean with each hop; each style then acts at the tile, may walk
    /// back, and sends the tile on its last leg to the slot. A walker's tile is the look's from its act on
    /// (<see cref="Holds"/>: the host draws the cell's ground under it). Tokens and registered slots only
    /// (<see cref="SlotOf"/>). Engine-free.
    /// </summary>
    public static class ClearLook
    {
        /// <summary>A walker's size, in cells (the Parade's are smaller).</summary>
        public const float WalkerSize = 0.74f;

        /// <summary>
        /// A walker's hops a cell: half a hop, about four and a half hops a second at the reference game's pace of 9 cells a
        /// second (<see cref="ClearStyles.PerCell"/>; it was two a cell at the calm 1.4 cells a second, under three a second).
        /// </summary>
        public const float HopsPerCell = 0.5f;

        private const float ParadeSize = 0.66f;

        // A tile as the board draws it, in cells.
        private const float TileSide = 0.88f;

        private enum Leg
        {
            Before,
            Out,
            Act,
            Back,
            Fin,
            Done,
        }

        /// <summary>The registered slot a style's items stand for (<c>fx.clear.blossom</c>, …).</summary>
        public static string SlotOf(ClearStyle style) => "fx.clear." + ClearStyles.Id(style).Substring("clear.".Length);

        /// <summary>Whether the walker's tile is the look's now (from its act to its clear): the host leaves it off the board.</summary>
        public static bool Holds(ClearStyle style, in ClearWalk walk, float now)
        {
            Spot spot = Locate(style, walk, now);
            return spot.Leg == Leg.Act || spot.Leg == Leg.Back || spot.Leg == Leg.Fin;
        }

        /// <summary>The walker's items this frame (none before it sets off or after its clear).</summary>
        public static void Walker(FxList list, ClearStyle style, in ClearWalk walk, float now)
        {
            Spot spot = Locate(style, walk, now);
            if (spot.Leg == Leg.Before || spot.Leg == Leg.Done || walk.Points.Count == 0)
            {
                return;
            }

            list.Slot = SlotOf(style);
            switch (style)
            {
                case ClearStyle.Munchers:
                    Munchers(list, walk, spot);
                    break;
                case ClearStyle.Fireflies:
                    Fireflies(list, walk, spot, now);
                    break;
                case ClearStyle.Bubbles:
                    Bubbles(list, walk, spot, now);
                    break;
                case ClearStyle.Pushers:
                    Pushers(list, walk, spot);
                    break;
                case ClearStyle.Fireworks:
                    Fireworks(list, walk, spot);
                    break;
                case ClearStyle.Parade:
                    Parade(list, walk, spot, now);
                    break;
                default:
                    Blossom(list, walk, spot);
                    break;
            }

            list.Layer = FxLayer.Board;
        }

        /// <summary>A just-cleared tile's items: Blossom's flower fading into the picture, the Parade's confetti, the slot's ring.</summary>
        public static void Restore(FxList list, ClearStyle style, in ClearFade fade, float now)
        {
            float t = now - fade.Start;
            if (t < 0f || t >= ClearStyles.RestoreSeconds)
            {
                return;
            }

            list.Slot = SlotOf(style);
            list.Layer = FxLayer.Board;
            (float x, float y) = fade.Center;
            Rgba color = ColorOf(fade.Variant);
            if (style == ClearStyle.Blossom)
            {
                float k = t / 0.9f;
                if (k < 1f)
                {
                    float fadeOut = Smooth((k - 0.3f) / 0.7f);
                    list.PushAlpha(1f - fadeOut);
                    Flower(list, x, y, 1.15f * (1f - (0.35f * fadeOut)), 40f + (6f * (float)Math.Sin(k * Math.PI * 3f)), color);
                    list.Pop();
                    float sp = Clamp01(k / 0.8f);
                    if (IsDrop(fade.Variant))
                    {
                        // The Drop family's tiles splash two droplets instead (fx.droplet).
                        Rgba splash = color.Lighten(0.72f).WithAlpha(1f - sp);
                        float size = 0.35f + (0.4f * sp);
                        list.Shape("fx.droplet", Box.FromCenter(x - 0.2f, y - 0.1f - (0.3f * sp), size * 0.6f, size * 0.6f), splash);
                        list.Shape("fx.droplet", Box.FromCenter(x + 0.22f, y - 0.2f - (0.3f * sp), size * 0.5f, size * 0.5f), splash);
                    }
                    else
                    {
                        list.Shape("fx.sparkle", Box.FromCenter(x + 0.3f, y - 0.45f - (0.25f * sp), 0.4f, 0.4f), Rgba.White.WithAlpha(1f - sp));
                    }
                }
            }
            else if (style == ClearStyle.Parade)
            {
                Confetti(list, x, y, t / 1f, color, (int)((x * 31f) + (y * 17f)));
            }

            if (fade.Seat.HasValue)
            {
                Landed(list, fade.Seat.Value, t);
            }

            list.Layer = FxLayer.Board;
        }

        /// <summary>Blossom: how many degrees a tile near a just-opened flower sways (0 for none and in the other styles).</summary>
        public static float Sway(ClearStyle style, IReadOnlyList<ClearFade> fades, (float X, float Y) cell, float now)
        {
            if (style != ClearStyle.Blossom)
            {
                return 0f;
            }

            float sum = 0f;
            foreach (ClearFade fade in fades)
            {
                float d = Math.Abs(fade.Center.X - cell.X) + Math.Abs(fade.Center.Y - cell.Y);
                if (d < 0.5f || d > 2.5f)
                {
                    continue;
                }

                float t = (now - fade.Start - (0.14f * d)) / 0.9f;
                if (t <= 0f || t >= 1f)
                {
                    continue;
                }

                sum += (float)Math.Sin(t * Math.PI * 2f) * (1f - t) / d;
            }

            return 7f * Math.Max(-1f, Math.Min(1f, sum));
        }

        // ---- The styles ----

        // Blossom: it pats the tile, which sinks turning, and a flower opens where it was.
        private static void Blossom(FxList list, in ClearWalk walk, Spot spot)
        {
            if (spot.Leg == Leg.Out)
            {
                Walking(list, walk, spot, WalkerSize);
                return;
            }

            float u = spot.U;
            Box tile = TileAt(walk.Target);
            float sink = Clamp01(u / 0.45f);
            if (sink < 1f)
            {
                list.PushTurn(18f * sink, tile.CenterX, tile.CenterY);
                list.PushScale(1f - (0.5f * sink), tile.CenterX, tile.CenterY);
                list.PushAlpha(1f - sink);
                list.Tile(tile, walk.Variant);
                list.Pop();
                list.Pop();
                list.Pop();
            }

            float g = Clamp01((u - 0.3f) / 0.6f);
            if (g > 0f)
            {
                Flower(list, tile.CenterX, tile.CenterY, 1.15f * Back(g), g * 40f, ColorOf(walk.Variant));
            }

            // The Bloomling hops once on its tile and sinks into it.
            float hop = (float)Math.Sin(Clamp01(u / 0.3f) * Math.PI) * 0.12f;
            float gone = Smooth((u - 0.2f) / 0.25f);
            if (gone < 1f)
            {
                Body(list, spot.X, spot.Y, WalkerSize * (1f - (0.6f * gone)), hop, 0f, 1f, 1f, 1f - gone, walk.Variant);
            }
        }

        // Munchers: it climbs onto the tile, eats it from the top in three bites, and waddles home full.
        private static void Munchers(FxList list, in ClearWalk walk, Spot spot)
        {
            float swing = Swing(spot);
            switch (spot.Leg)
            {
                case Leg.Out:
                    Walking(list, walk, spot, WalkerSize);
                    return;
                case Leg.Act:
                {
                    float u = spot.U;
                    Box tile = TileAt(walk.Target);
                    float chew = u > 0.15f && u < 0.9f ? (float)Math.Sin(u * Math.PI * 14f) : 0f;
                    float gulp = u > 0.88f ? (float)Math.Sin((u - 0.88f) / 0.12f * Math.PI * 0.5f) : 0f;
                    float bites = Bites(u);
                    float climb = Smooth(u / 0.15f);
                    float top = tile.Bottom - (tile.Height * bites);
                    float y = Lerp(spot.Y, top - (WalkerSize * 0.3f), climb);
                    if (bites > 0.01f)
                    {
                        list.PushSquash(1f, bites, tile.CenterX, tile.Bottom);
                        list.Tile(tile, walk.Variant);
                        list.Pop();
                    }

                    Body(list, spot.X, y, WalkerSize, 0f, 0f, 1f + (0.08f * chew) + (0.16f * gulp), 1f - (0.08f * chew) - (0.06f * gulp), 1f, walk.Variant);
                    Crumbs(list, tile, u, walk.Variant);
                    return;
                }

                case Leg.Back:
                    // Round and full: wider, lower, a slow waddle.
                    Body(list, spot.X, spot.Y, WalkerSize, Math.Abs(swing) * 0.04f, swing * 11f, 1.16f, 0.94f, 1f, walk.Variant);
                    return;
                default:
                {
                    // Home: a little hop into the arch.
                    float u = spot.U;
                    float sink = 1f - (0.35f * u);
                    Body(list, spot.X, spot.Y, WalkerSize, (float)Math.Sin(u * Math.PI) * 0.16f, 0f, (1.16f - (0.16f * u)) * sink, (0.94f + (0.06f * u)) * sink, 1f - u, walk.Variant);
                    return;
                }
            }
        }

        // Fireflies: the tile glows under it and breaks into fireflies that hover and swarm to the slot.
        private static void Fireflies(FxList list, in ClearWalk walk, Spot spot, float now)
        {
            Box tile = TileAt(walk.Target);
            switch (spot.Leg)
            {
                case Leg.Out:
                    Walking(list, walk, spot, WalkerSize);
                    return;
                case Leg.Act:
                {
                    float g = spot.U;
                    list.PushScale(1f + (0.05f * (float)Math.Sin(g * Math.PI * 4f)), tile.CenterX, tile.CenterY);
                    list.Tile(tile, walk.Variant);
                    list.Round(tile.Inset(0.02f), 0.16f, DesignTokens.Colors.FxFireflyGlow.WithAlpha(0.7f * g));
                    list.Pop();
                    list.Circle(tile.CenterX, tile.CenterY, 0.75f * g, DesignTokens.Colors.FxFireflyGlow.WithAlpha(0.25f * g));
                    Body(list, spot.X, spot.Y, WalkerSize, Math.Abs((float)Math.Sin(g * Math.PI * 2f)) * 0.08f, 0f, 1f, 1f, 1f, walk.Variant);
                    return;
                }

                default:
                {
                    // It claps as its light flies off, then is gone.
                    float u = Clamp01(spot.U / 0.25f);
                    if (u < 1f)
                    {
                        float clap = 0.12f * (float)Math.Sin(spot.U * Math.PI * 16f) * (1f - u);
                        Body(list, spot.X, spot.Y, WalkerSize, 0f, 0f, 1f + clap, 1f - clap, 1f - u, walk.Variant);
                    }

                    list.Layer = FxLayer.Over;
                    Swarm(list, walk, tile, spot.U, now);
                    return;
                }
            }
        }

        // Bubbles: it steps back and blows a bubble round the tile, which floats on a lazy wave to the slot and pops.
        private static void Bubbles(FxList list, in ClearWalk walk, Spot spot, float now)
        {
            Box tile = TileAt(walk.Target);
            (float tx, float ty) = Toward(walk.Points, walk.Points.Count - 2);
            switch (spot.Leg)
            {
                case Leg.Out:
                    Walking(list, walk, spot, WalkerSize);
                    return;
                case Leg.Act:
                {
                    float u = spot.U;
                    float back = Smooth(u / 0.25f);
                    float g = Smooth((u - 0.25f) / 0.75f);
                    float lift = 0.08f * g;
                    float side = 1f - (0.3f * g);
                    list.Tile(Box.FromCenter(tile.CenterX, tile.CenterY - lift, tile.Width * side, tile.Height * side), walk.Variant);
                    Bubble(list, tile.CenterX, tile.CenterY - lift, 0.62f * g, now);
                    float puff = 0.08f * (float)Math.Sin(u * Math.PI * 6f) * (1f - g);
                    Body(list, spot.X - (tx * 0.5f * back), spot.Y - (ty * 0.5f * back), WalkerSize, 0f, 0f, 1f + puff, 1f - puff, 1f, walk.Variant);
                    return;
                }

                default:
                {
                    float t = spot.U;
                    float wave = Clamp01(t / 0.3f);
                    if (wave < 1f)
                    {
                        // It waves the bubble off.
                        Body(list, spot.X - (tx * 0.5f), spot.Y - (ty * 0.5f), WalkerSize, 0f, (float)Math.Sin(t * Math.PI * 10f) * 12f, 1f, 1f, 1f - wave, walk.Variant);
                    }

                    list.Layer = FxLayer.Over;
                    float fromY = tile.CenterY - 0.08f;
                    (float sx, float sy, float seat) = walk.Seat.HasValue
                        ? (walk.Seat.Value.CenterX, walk.Seat.Value.CenterY, walk.Seat.Value.Width)
                        : (tile.CenterX, tile.CenterY - 1.6f, tile.Width * 0.7f);
                    float e = Smooth(t);
                    float x = Lerp(tile.CenterX, sx, e) + ((float)Math.Sin(t * Math.PI * 2.2f) * 0.55f * (1f - e));
                    float y = Lerp(fromY, sy, e) - ((float)Math.Sin(t * Math.PI) * 1.3f);
                    float inner = Lerp(tile.Width * 0.7f, seat, e);
                    float r = Lerp(0.62f, seat * 0.75f, e);
                    if (t < 0.93f)
                    {
                        list.PushTurn((float)Math.Sin(now * 2.1f) * 8f, x, y);
                        list.Tile(Box.FromCenter(x, y, inner, inner), walk.Variant);
                        list.Pop();
                        Bubble(list, x, y, r, now);
                    }
                    else
                    {
                        list.Tile(Box.FromCenter(x, y, inner, inner), walk.Variant);
                        Pop(list, x, y, r, (t - 0.93f) / 0.07f);
                    }

                    return;
                }
            }
        }

        // Pushers: it steps behind the tile and pushes it home, the tile tumbling a cell at a time; from the arch it hops into the slot.
        private static void Pushers(FxList list, in ClearWalk walk, Spot spot)
        {
            Box tile = TileAt(walk.Target);
            int segments = Math.Max(1, walk.Points.Count - 1);
            switch (spot.Leg)
            {
                case Leg.Out:
                    Walking(list, walk, spot, WalkerSize);
                    return;
                case Leg.Act:
                {
                    // It steps round behind the tile and leans in; the tile wobbles loose.
                    (float tx, float ty) = Toward(walk.Points, segments - 1);
                    float u = Smooth(spot.U / 0.7f);
                    list.PushTurn((float)Math.Sin(spot.U * Math.PI * 4f) * 4f * spot.U, tile.CenterX, tile.Bottom);
                    list.Tile(tile, walk.Variant);
                    list.Pop();
                    Body(list, spot.X + (tx * 0.62f * u), spot.Y + (ty * 0.62f * u), WalkerSize, Math.Abs((float)Math.Sin(spot.U * Math.PI * 2f)) * 0.06f, 0f, 1f, 1f, 1f, walk.Variant);
                    return;
                }

                case Leg.Back:
                {
                    (float rx, float ry, float turn, float lift, float squash, int segment) = Roll(walk.Points, spot.U);
                    (float dx, float dy) = Toward(walk.Points, segment);
                    float hops = spot.U * segments * 2f;
                    float swing = (float)Math.Sin(hops * Math.PI);
                    Body(list, rx + (dx * 0.62f), ry + (dy * 0.62f), WalkerSize, Math.Abs(swing) * 0.05f, (dx < 0f ? -1f : 1f) * -8f, 1f, 1f, 1f, walk.Variant);
                    float cy = ry - (lift * 0.16f);
                    float s = tile.Width;
                    list.Ellipse(Box.FromCenter(rx, ry + (s * 0.42f), s * (0.85f - (0.15f * lift)), s * 0.22f), Rgba.Black.WithAlpha(0.13f));
                    list.PushTurn(turn * (dx > 0f ? -1f : 1f), rx, cy);
                    list.PushSquash(1f + (0.1f * squash), 1f - (0.12f * squash), rx, cy + (s / 2f));
                    list.Tile(Box.FromCenter(rx, cy, s, s), walk.Variant);
                    list.Pop();
                    list.Pop();
                    return;
                }

                default:
                {
                    // At the arch: a little cheer as the tile hops into the slot, then home.
                    (float dx, float dy) = Toward(walk.Points, 0);
                    float u = Clamp01(spot.U / 0.6f);
                    if (u < 1f)
                    {
                        Body(list, spot.X + (dx * 0.62f), spot.Y + (dy * 0.62f), WalkerSize, (float)Math.Sin(u * Math.PI) * 0.14f, 0f, 1f, 1f, 1f - u, walk.Variant);
                    }

                    list.Layer = FxLayer.Over;
                    Toss(list, walk, spot.X, spot.Y, tile.Width, spot.U, -90f);
                    return;
                }
            }
        }

        // Fireworks: the tile swells white under it and pops; its sparkles fly in arcs to the slot.
        private static void Fireworks(FxList list, in ClearWalk walk, Spot spot)
        {
            Box tile = TileAt(walk.Target);
            Rgba color = ColorOf(walk.Variant);
            switch (spot.Leg)
            {
                case Leg.Out:
                    Walking(list, walk, spot, WalkerSize);
                    return;
                case Leg.Act:
                {
                    float u = spot.U;
                    list.PushScale(1f + (0.22f * Ease(u)), tile.CenterX, tile.CenterY);
                    list.Tile(tile, walk.Variant);
                    list.Round(tile.Inset(0.08f), 0.2f, Rgba.White.WithAlpha(0.65f * u));
                    list.Pop();
                    float gone = Smooth((u - 0.7f) / 0.3f);
                    if (gone < 1f)
                    {
                        Body(list, spot.X, spot.Y, WalkerSize * (1f - (0.5f * gone)), Math.Abs((float)Math.Sin(u * Math.PI * 3f)) * 0.06f, 0f, 1f, 1f, 1f - gone, walk.Variant);
                    }

                    return;
                }

                default:
                {
                    float t = spot.U;
                    float burst = Clamp01(t / 0.4f);
                    if (burst < 1f)
                    {
                        list.PushAlpha(1f - burst);
                        list.Circle(tile.CenterX, tile.CenterY, 0.45f + (0.35f * burst), Rgba.White.WithAlpha(0.55f));
                        for (int i = 0; i < 10; i++)
                        {
                            double a = i * Math.PI * 2 / 10;
                            float d = 0.35f + (0.75f * Ease(burst));
                            list.Circle(tile.CenterX + ((float)Math.Cos(a) * d), tile.CenterY + ((float)Math.Sin(a) * d), 0.07f * (1f - burst), i % 2 == 0 ? Rgba.White : color.Lighten(0.35f));
                        }

                        list.Pop();
                    }

                    list.Layer = FxLayer.Over;
                    (float sx, float sy) = walk.Seat.HasValue ? (walk.Seat.Value.CenterX, walk.Seat.Value.CenterY) : (tile.CenterX, tile.CenterY - 1.8f);
                    for (int i = 0; i < 4; i++)
                    {
                        float k = Clamp01((t - 0.1f - (i * 0.08f)) / 0.62f);
                        if (k <= 0f || k >= 1f)
                        {
                            continue;
                        }

                        float side = (i % 2 == 0 ? -1f : 1f) * (0.8f + (0.4f * i));
                        float e = k * k;
                        float x = tile.CenterX + ((sx - tile.CenterX) * e) + (side * (float)Math.Sin(k * Math.PI));
                        float y = tile.CenterY + ((sy - tile.CenterY) * e) - (0.9f * (float)Math.Sin(k * Math.PI));
                        float size = 1.15f - (0.4f * k);
                        Rgba glow = i % 2 == 0 ? color : DesignTokens.Colors.GardenGlow;
                        list.Circle(x, y, size * 0.32f, glow.WithAlpha(0.45f));
                        list.PushTurn(k * 200f, x, y);
                        list.Shape("fx.sparkle", Box.FromCenter(x, y, size, size), Rgba.White);
                        list.Pop();
                        list.Circle(x, y, size * 0.13f, glow);
                    }

                    return;
                }
            }
        }

        // Confetti Parade: a line of small Bloomlings with a trail of dots; each dances on its tile, which squashes, stretches and pops.
        private static void Parade(FxList list, in ClearWalk walk, Spot spot, float now)
        {
            Box tile = TileAt(walk.Target);
            if (spot.Leg == Leg.Out)
            {
                Rgba tint = ColorOf(walk.Variant).Lighten(0.3f);
                for (int d = 1; d <= 3; d++)
                {
                    Spot back = Locate(ClearStyle.Parade, walk, now - (d * 0.16f));
                    if (back.Leg != Leg.Out)
                    {
                        break;
                    }

                    list.Circle(back.X, back.Y + 0.18f, 0.08f - (d * 0.013f), tint.WithAlpha(0.5f - (d * 0.12f)));
                }

                Spot waddle = spot;
                Walking(list, walk, waddle, ParadeSize, waddle: 9f, hopHeight: 0.08f);
                return;
            }

            float u = spot.U;
            float sx = 1f;
            float sy = 1f;
            float hop = 0f;
            if (u < 0.55f)
            {
                // Two little hops; the tile bounces as it lands.
                float h = (u / 0.55f) * 2f;
                float f = h - (float)Math.Floor(h);
                hop = (float)Math.Sin(f * Math.PI) * 0.16f;
                float land = f < 0.2f ? (float)Math.Sin((f / 0.2f) * Math.PI) : 0f;
                sx = 1f + (0.08f * land);
                sy = 1f - (0.1f * land);
            }
            else
            {
                // The squash and the stretch before the pop.
                float t = (u - 0.55f) / 0.45f;
                sx = t < 0.45f ? 1f + (0.22f * (t / 0.45f)) : 1.22f - (0.42f * ((t - 0.45f) / 0.55f));
                sy = t < 0.45f ? 1f - (0.2f * (t / 0.45f)) : 0.8f + (0.55f * ((t - 0.45f) / 0.55f));
            }

            list.PushSquash(sx, sy, tile.CenterX, tile.Bottom);
            list.Tile(tile, walk.Variant);
            list.Pop();
            float gone = Smooth((u - 0.5f) / 0.25f);
            if (gone < 1f)
            {
                Body(list, spot.X, spot.Y - ((1f - sy) * 0.4f), ParadeSize * (1f - (0.5f * gone)), hop, 0f, 1f, 1f, 1f - gone, walk.Variant);
            }
        }

        // ---- The pieces ----

        private static void Walking(FxList list, in ClearWalk walk, Spot spot, float size, float waddle = 7f, float hopHeight = 0.085f)
        {
            float swing = Swing(spot);
            float hop = spot.Walking ? Math.Abs(swing) * hopHeight : 0f;
            Body(list, spot.X, spot.Y, size, hop, spot.Walking ? swing * waddle : 0f, 1f, 1f, 1f, walk.Variant);
        }

        /// <summary>A Bloomling standing at (x, y): its shadow, then its character, leaning and squashed about its feet.</summary>
        private static void Body(FxList list, float x, float y, float size, float hop, float lean, float sx, float sy, float alpha, VariantId variant)
        {
            if (alpha <= 0f || size <= 0f)
            {
                return;
            }

            list.PushAlpha(alpha);
            Box body = Box.FromCenter(x, y - (size * 0.1f) - hop, size, size);
            Box ground = Box.FromCenter(x, y - (size * 0.1f), size * sx * (1f - hop), size);
            list.Shadow(ground);
            list.PushTurn(lean, x, body.Bottom);
            list.PushSquash(sx, sy, x, body.Bottom);
            list.Character(body, variant);
            list.Pop();
            list.Pop();
            list.Pop();
        }

        private static void Flower(FxList list, float x, float y, float size, float turn, Rgba color)
        {
            list.PushTurn(turn, x, y);
            Rgba petal = color.Lighten(0.12f);
            for (int i = 0; i < 5; i++)
            {
                double a = (i * Math.PI * 2 / 5) - (Math.PI / 2);
                float px = x + ((float)Math.Cos(a) * size * 0.26f);
                float py = y + ((float)Math.Sin(a) * size * 0.26f);
                list.Circle(px, py, size * 0.23f, Rgba.White);
                list.Circle(px, py, size * 0.19f, petal);
            }

            list.Circle(x, y, size * 0.17f, DesignTokens.Colors.GardenFlowerCenter);
            list.Ring(x, y, size * 0.17f, 0.025f, DesignTokens.Colors.GardenFlowerCenterLine);
            list.Pop();
        }

        // Munchers: how much of the tile is left after the bites so far.
        private static float Bites(float u)
        {
            float left = 1f;
            left = Lerp(left, 0.68f, Smooth((u - 0.32f) / 0.07f));
            left = Lerp(left, 0.38f, Smooth((u - 0.56f) / 0.07f));
            return Lerp(left, 0f, Smooth((u - 0.8f) / 0.07f));
        }

        private static void Crumbs(FxList list, Box tile, float u, VariantId variant)
        {
            Rgba color = ColorOf(variant);
            float[] at = { 0.32f, 0.56f, 0.8f };
            for (int b = 0; b < at.Length; b++)
            {
                float t = (u - at[b]) / 0.3f;
                if (t <= 0f || t >= 1f)
                {
                    continue;
                }

                for (int i = 0; i < 7; i++)
                {
                    float dir = ((i - 3f) * 0.42f) + (b == 1 ? 0.3f : b == 2 ? -0.3f : 0f);
                    float vy = 0.35f + (0.3f * (((i * 7) + b) % 4) / 3f);
                    float x = tile.CenterX + (dir * 0.7f * t);
                    float y = tile.Bottom - (tile.Height * Bites(at[b])) + ((((i * 5) % 3) - 1f) * 0.06f) - (vy * 1.6f * t) + (1.2f * t * t);
                    float r = 0.1f * (1f - (0.4f * t));
                    list.PushAlpha(1f - (t * t));
                    list.PushTurn(t * 300f * (i % 2 == 0 ? 1f : -1f), x, y);
                    list.Round(Box.FromCenter(x, y, r * 2.4f, r * 2.4f), r * 0.6f, Rgba.White);
                    list.Round(Box.FromCenter(x, y, r * 2f, r * 2f), r * 0.5f, color);
                    list.Pop();
                    list.Pop();
                }
            }
        }

        // Fireflies: the light breaks into twelve fireflies with trails; they hover, then drift on curves to the slot.
        private static void Swarm(FxList list, in ClearWalk walk, Box tile, float t, float now)
        {
            if (t < 0.12f)
            {
                list.Circle(tile.CenterX, tile.CenterY, 0.5f + (0.4f * (t / 0.12f)), DesignTokens.Colors.FxFireflyGlow.WithAlpha(0.5f * (1f - (t / 0.12f))));
            }

            (float sx, float sy) = walk.Seat.HasValue ? (walk.Seat.Value.CenterX, walk.Seat.Value.CenterY) : (tile.CenterX, tile.CenterY - 2f);
            int seed = (int)((walk.Target.X * 7f) + (walk.Target.Y * 13f));
            Rgba gold = DesignTokens.Colors.FxFirefly;
            Rgba glow = DesignTokens.Colors.FxFireflyGlow;
            for (int i = 0; i < 12; i++)
            {
                (float x, float y, float k) = Firefly(tile, sx, sy, seed, i, t, now);
                float fade = k > 0.97f ? 1f - ((k - 0.97f) / 0.03f) : 1f;
                float twinkle = 0.7f + (0.3f * (float)Math.Sin((now * 9f) + (i * 1.7f)));
                for (int j = 3; j >= 1; j--)
                {
                    (float px, float py, float _) = Firefly(tile, sx, sy, seed, i, t - (j * 0.012f), now - (j * 0.03f));
                    list.Circle(px, py, 0.07f - (j * 0.012f), gold.WithAlpha((0.5f - (j * 0.12f)) * fade));
                }

                list.Circle(x, y, 0.3f, gold.WithAlpha(0.22f * twinkle * fade));
                list.Circle(x, y, 0.17f, glow.WithAlpha(0.55f * twinkle * fade));
                list.Circle(x, y, 0.095f, gold.WithAlpha(fade));
                list.Circle(x, y, 0.05f, Rgba.White.WithAlpha(fade));
            }
        }

        private static (float X, float Y, float K) Firefly(Box tile, float sx, float sy, int seed, int i, float t, float clock)
        {
            float a = (float)((((seed + (i * 37)) % 100 + 100) % 100) / 100.0 * Math.PI * 2);
            float r0 = 0.15f + (0.25f * ((((seed + (i * 53)) % 10) + 10) % 10 / 10f));
            float spread = 1f + (2.2f * Smooth(t / 0.3f));
            float hx = tile.CenterX + ((float)Math.Cos(a) * r0 * spread);
            float hy = tile.CenterY + ((float)Math.Sin(a) * r0 * spread) - (0.5f * Smooth(t / 0.3f));
            float delay = 0.25f + (0.3f * ((((seed + (i * 29)) % 10) + 10) % 10 / 10f));
            float k = Smooth((t - delay) / (1f - delay - 0.02f));
            float bend = (i % 2 == 0 ? 1f : -1f) * (1.2f + (0.12f * i));
            float mx = ((hx + sx) / 2f) + bend;
            float my = ((hy + sy) / 2f) - 0.8f;
            float x = ((1 - k) * (1 - k) * hx) + (2 * (1 - k) * k * mx) + (k * k * sx);
            float y = ((1 - k) * (1 - k) * hy) + (2 * (1 - k) * k * my) + (k * k * sy);
            x += (float)Math.Sin((clock * 3.1f) + (i * 1.3f)) * 0.14f * (1f - k);
            y += (float)Math.Cos((clock * 2.7f) + (i * 0.9f)) * 0.12f * (1f - k);
            return (x, y, k);
        }

        // A soap bubble of radius r: its glass, a rainbow rim, a white rim and highlights, wobbling gently.
        private static void Bubble(FxList list, float x, float y, float r, float now)
        {
            if (r < 0.02f)
            {
                return;
            }

            float wob = (float)Math.Sin(now * 5.3f) * 0.05f;
            list.PushSquash(1f + wob, 1f - wob, x, y);
            list.Circle(x, y, r, DesignTokens.Colors.FxGlass.WithAlpha(0.22f));
            list.Ring(x, y, r, Math.Max(0.02f, r * 0.07f), DesignTokens.Colors.FxGlassRim.WithAlpha(0.55f));
            list.Ring(x, y, r * 0.96f, Math.Max(0.015f, r * 0.05f), Rgba.White.WithAlpha(0.85f));
            list.Circle(x - (r * 0.38f), y - (r * 0.42f), r * 0.16f, Rgba.White.WithAlpha(0.9f));
            list.Circle(x - (r * 0.15f), y - (r * 0.58f), r * 0.07f, Rgba.White.WithAlpha(0.8f));
            list.Pop();
        }

        private static void Pop(FxList list, float x, float y, float r, float t)
        {
            list.PushAlpha(1f - Clamp01(t));
            list.Ring(x, y, r * (1f + (0.5f * t)), Math.Max(0.02f, r * 0.08f), Rgba.White);
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI * 2 / 8;
                float d = r * (1f + (0.8f * t));
                list.Circle(x + ((float)Math.Cos(a) * d), y + ((float)Math.Sin(a) * d), r * 0.09f, DesignTokens.Colors.FxGlass);
            }

            list.Pop();
        }

        // The tile's hop from (x, y) down into the slot, turning, with a squash as it lands.
        private static void Toss(FxList list, in ClearWalk walk, float x0, float y0, float side0, float t, float turn)
        {
            float y1 = y0 - 0.74f;
            (float sx, float sy, float seat) = walk.Seat.HasValue
                ? (walk.Seat.Value.CenterX, walk.Seat.Value.CenterY, walk.Seat.Value.Width)
                : (x0, y1 - 1.5f, side0);
            float e = Smooth(t);
            float side = Lerp(side0, seat, e);
            float x = Lerp(x0, sx, e);
            float y = Lerp(y1, sy, e) - (1.4f * (float)Math.Sin(t * Math.PI));
            float squash = t > 0.86f ? (float)Math.Sin((t - 0.86f) / 0.14f * Math.PI) : 0f;
            list.PushTurn(e * turn, x, y);
            list.PushSquash(1f + (0.16f * squash), 1f - (0.16f * squash), x, y + (side / 2f));
            list.Tile(Box.FromCenter(x, y, side, side), walk.Variant);
            list.Pop();
            list.Pop();
        }

        private static void Confetti(FxList list, float x, float y, float k, Rgba color, int seed)
        {
            if (k <= 0f || k >= 1f)
            {
                return;
            }

            list.PushAlpha(1f - k);
            list.Ring(x, y - 0.15f, 0.3f + (0.8f * Ease(k)), Math.Max(0.01f, 0.12f * (1f - k)), Rgba.White);
            for (int i = 0; i < 10; i++)
            {
                double angle = (i * Math.PI * 2 / 10) + ((((seed % 7) + 7) % 7) * 0.2);
                float speed = 0.9f + (0.35f * ((((seed + (i * 13)) % 5) + 5) % 5) / 4f);
                float px = x + ((float)Math.Cos(angle) * speed * Ease(k));
                float py = y - 0.15f - ((float)Math.Sin(angle) * speed * Ease(k)) + (1.1f * k * k);
                float size = 0.26f * (1f - (0.35f * k));
                Rgba c = i % 3 == 0 ? DesignTokens.Colors.GardenGlow : i % 3 == 1 ? color : DesignTokens.Colors.FxConfettiPink;
                list.PushTurn(k * 400f * (i % 2 == 0 ? 1f : -1f), px, py);
                list.Round(Box.FromCenter(px, py, size + 0.05f, (size * 0.62f) + 0.05f), size * 0.2f, Rgba.White);
                list.Round(Box.FromCenter(px, py, size, size * 0.62f), size * 0.15f, c);
                list.Pop();
            }

            list.Pop();
        }

        // The slot taking what came: a soft ring and a sparkle as its count goes down.
        private static void Landed(FxList list, Box seat, float t)
        {
            float g = Clamp01(t / 0.45f);
            if (g >= 1f)
            {
                return;
            }

            list.Layer = FxLayer.Over;
            Box slot = seat.Inset(-seat.Width * 0.25f);
            list.PushAlpha(0.8f * (1f - g));
            list.RoundRing(slot.Inset(-0.18f * Ease(g)), slot.Width * 0.22f, Math.Max(0.01f, 0.08f * (1f - g)), Rgba.White);
            list.Shape("fx.sparkle", Box.FromCenter(slot.Right - (slot.Width * 0.18f), slot.Top + (slot.Height * 0.12f) - (0.25f * g), 0.45f, 0.45f), Rgba.White);
            list.Pop();
        }

        // ---- The trip ----

        private struct Spot
        {
            public Leg Leg;
            public float U;
            public float X;
            public float Y;
            public float Hops;
            public bool Walking;
        }

        private static Spot Locate(ClearStyle style, in ClearWalk walk, float now)
        {
            ClearLegs legs = ClearStyles.LegsOf(style, walk.Cells);
            float total = legs.Total;
            float t = (now - walk.Start) * total / Math.Max(0.05f, walk.Arrival);
            int segments = Math.Max(1, walk.Points.Count - 1);
            var spot = new Spot();
            (float X, float Y) at;
            if (t < 0f)
            {
                spot.Leg = Leg.Before;
                at = Along(walk.Points, 0f);
            }
            else if (t <= legs.Out)
            {
                float progress = legs.Out <= 0f ? 1f : Clamp01(t / legs.Out);
                at = Along(walk.Points, progress);
                spot.Leg = Leg.Out;
                spot.U = progress;
                spot.Hops = progress * segments * HopsPerCell;
                spot.Walking = progress < 1f;
            }
            else if (t <= legs.Out + legs.Act)
            {
                at = Along(walk.Points, 1f);
                spot.Leg = Leg.Act;
                spot.U = (t - legs.Out) / Math.Max(0.001f, legs.Act);
                spot.Hops = segments * HopsPerCell;
            }
            else if (t <= legs.Out + legs.Act + legs.Back)
            {
                spot.Leg = Leg.Back;
                spot.U = (t - legs.Out - legs.Act) / Math.Max(0.001f, legs.Back);
                at = Along(walk.Points, 1f - spot.U);
                spot.Hops = (segments * HopsPerCell) + (spot.U * segments * HopsPerCell);
                spot.Walking = true;
            }
            else if (t < total)
            {
                at = Along(walk.Points, legs.Back > 0f ? 0f : 1f);
                spot.Leg = Leg.Fin;
                spot.U = (t - legs.Out - legs.Act - legs.Back) / Math.Max(0.001f, legs.Fin);
                spot.Hops = segments * 2f * HopsPerCell;
            }
            else
            {
                at = Along(walk.Points, legs.Back > 0f ? 0f : 1f);
                spot.Leg = Leg.Done;
                spot.U = 1f;
            }

            spot.X = at.X;
            spot.Y = at.Y;
            return spot;
        }

        private static float Swing(Spot spot) => (float)Math.Sin(spot.Hops * Math.PI);

        private static (float X, float Y) Along(IReadOnlyList<(float X, float Y)> points, float progress)
        {
            if (points.Count == 0)
            {
                return (0f, 0f);
            }

            if (points.Count == 1)
            {
                return points[0];
            }

            float t = Clamp01(progress) * (points.Count - 1);
            int i = Math.Min((int)t, points.Count - 2);
            float f = t - i;
            return (Lerp(points[i].X, points[i + 1].X, f), Lerp(points[i].Y, points[i + 1].Y, f));
        }

        // The unit step from point i towards point i + 1 (towards the tile).
        private static (float X, float Y) Toward(IReadOnlyList<(float X, float Y)> points, int i)
        {
            if (points.Count < 2)
            {
                return (0f, -1f);
            }

            i = Math.Max(0, Math.Min(points.Count - 2, i));
            float dx = points[i + 1].X - points[i].X;
            float dy = points[i + 1].Y - points[i].Y;
            float len = Math.Max(0.001f, (float)Math.Sqrt((dx * dx) + (dy * dy)));
            return (dx / len, dy / len);
        }

        // Pushers: the tile's place on its way home (it rests a moment after each tumble), its turn, lift, landing squash and segment.
        private static (float X, float Y, float Turn, float Lift, float Squash, int Segment) Roll(IReadOnlyList<(float X, float Y)> points, float u)
        {
            int segments = Math.Max(1, points.Count - 1);
            float steps = Clamp01(u) * segments;
            int s = Math.Min(segments - 1, (int)steps);
            float f = steps - s;
            float m = Smooth(f / 0.72f);
            float param = 1f - ((s + m) / segments);
            (float x, float y) = Along(points, param);
            float squash = f > 0.72f && f < 0.9f ? (float)Math.Sin((f - 0.72f) / 0.18f * Math.PI) : 0f;
            int segment = Math.Min(points.Count - 2, Math.Max(0, (int)Math.Floor((param * segments) - 0.0001f)));
            return (x, y, (s + m) * 90f, (float)Math.Sin(m * Math.PI), squash, segment);
        }

        private static Box TileAt((float X, float Y) center) => Box.FromCenter(center.X, center.Y, TileSide, TileSide);

        private static bool IsDrop(VariantId variant) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) && info.Family == Family.Drop;

        private static Rgba ColorOf(VariantId variant) =>
            VariantCatalog.Default.TryGet(variant, out VariantInfo info) ? Rgba.FromHex(info.ColorHex) : DesignTokens.Colors.InkBrown;

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        private static float Lerp(float a, float b, float t) => a + ((b - a) * t);

        private static float Ease(float t) => 1f - ((1f - t) * (1f - t));

        private static float Smooth(float t)
        {
            t = Clamp01(t);
            return t * t * (3f - (2f * t));
        }

        private static float Back(float t)
        {
            const float c = 1.9f;
            float u = t - 1f;
            return 1f + ((c + 1f) * u * u * u) + (c * u * u);
        }
    }
}
