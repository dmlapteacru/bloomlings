using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// The walker mock's "in hands" act (<see cref="WalkerLook.Hands"/>; the owner's idea of 2026-11-03, review material, not the
    /// game's look): the walker stops at its cube, the cube jumps into its hands and grows there
    /// (<see cref="WalkerLook.HeldShare"/> of the walker), and the style plays at its hands: Munchers one of four gags chosen by
    /// the cube's cell (chomp, squish, toss onto its head, shake like a gift box), Blossom a flower opening out of the cube,
    /// Bubbles a bubble carrying it off, Fireworks a fizzing rocket, Fireflies glowing flies, Pushers a crate pushed home, the
    /// Parade a juggle ending in confetti. Then it runs home empty-handed (Pushers behind its crate). The rules' times stay; only
    /// the act after the clear is longer (<see cref="WalkerLook.Linger"/>).
    /// </summary>
    public static partial class ClearLook
    {
        private enum Gag
        {
            Chomp,
            Squish,
            Toss,
            Shake,
        }

        /// <summary>
        /// Where a round walker of <paramref name="walkerCells"/> cells stops at its cube in the "in hands" act (on its way, a little
        /// before the cube) and the way it faces there (the unit step toward the cube), in its way's cell units: the hosts choose its
        /// aside spot from here (<see cref="AsideSpots"/>).
        /// </summary>
        public static (float X, float Y, float Dx, float Dy) StopOf(IReadOnlyList<(float X, float Y)> points, float walkerCells)
        {
            int segments = Math.Max(1, points.Count - 1);
            float stop = Math.Min(WalkerLook.CarryStopCells, walkerCells / 3f);
            (float x, float y) = Along(points, Math.Max(0f, 1f - (stop / segments)));
            (float dx, float dy) = Toward(points, points.Count - 2);
            return (x, y, dx, dy);
        }

        private static void Hands(FxList list, ClearStyle style, in ClearWalk walk, Spot spot, float now)
        {
            float walker = WalkerSize * (_pop > 0f ? _pop : 1f);
            int segments = Math.Max(1, walk.Points.Count - 1);
            float stop = Math.Min(WalkerLook.CarryStopCells, walker / 3f);
            float end = Math.Max(0f, 1f - (stop / segments));
            bool aside = style == ClearStyle.Munchers && _look.AsideMunch;
            if (style == ClearStyle.Munchers && _look.Trails)
            {
                TrailsMunch(list, walk, spot, walker, end, now);
                return;
            }

            switch (spot.Leg)
            {
                case Leg.Out:
                {
                    (spot.X, spot.Y) = Along(walk.Points, spot.U * end);
                    if (_look.Trails)
                    {
                        (float ox, float oy) = LaneOffset(walk, spot.U * end, 1f, walker);
                        spot.X += ox;
                        spot.Y += oy;
                    }

                    Walking(list, walk, spot, WalkerSize);
                    return;
                }

                case Leg.Act:
                {
                    (float x, float y) = Along(walk.Points, end);
                    if (aside)
                    {
                        AsideAct(list, walk, x, y, walker, spot.U);
                        return;
                    }

                    HandsAct(list, style, walk, x, y, walker, spot.U, now);
                    return;
                }

                default:
                {
                    (spot.X, spot.Y) = Along(walk.Points, (1f - spot.U) * end);
                    if (_look.Trails)
                    {
                        (float ox, float oy) = LaneOffset(walk, (1f - spot.U) * end, -1f, walker);
                        spot.X += ox;
                        spot.Y += oy;
                    }

                    if (aside && walk.HasAside)
                    {
                        // Back from its spot onto its way home.
                        float since = now - (walk.ClearAt + ClearStyles.Linger);
                        float k = Smooth(since / WalkerLook.AsideBackSeconds);
                        if (k < 1f)
                        {
                            spot.X = Lerp(walk.AsideX, spot.X, k);
                            spot.Y = Lerp(walk.AsideY, spot.Y, k);
                            float dx = spot.X - walk.AsideX;
                            _facing = dx < -0.01f ? -1 : dx > 0.01f ? 1 : _facing;
                        }
                    }

                    if (style != ClearStyle.Pushers)
                    {
                        Walking(list, walk, spot, WalkerSize);
                        return;
                    }

                    // Pushers: home behind its crate, the crate a little ahead along the way, on the ground.
                    float g = WalkerLook.HeldShare * walker;
                    float ahead = ((0.45f * walker) + (0.5f * g)) / segments;
                    (float cx, float cy) = Along(walk.Points, Math.Max(0f, ((1f - spot.U) * end) - ahead));
                    float swing = Swing(spot);
                    Body(list, spot.X, spot.Y, WalkerSize, _hero ? Math.Abs(swing) * 0.05f : 0f, 8f * _facing, 1f, 1f, 1f, walk.Variant);
                    Cube(list, walk, cx, cy + (0.4f * walker) - (0.5f * g), g, 3f * swing);
                    return;
                }
            }
        }

        // The act at the cube: the jump into the hands, the style's play, then a little turn for home.
        private static void HandsAct(FxList list, ClearStyle style, in ClearWalk walk, float x, float y, float walker, float u, float now)
        {
            float g = WalkerLook.HeldShare * walker;
            float feet = y + (0.4f * walker);
            float hx = x;
            float hy = HoldY(feet, walker, g);
            (float tx, float ty) = walk.Target;
            if (style == ClearStyle.Pushers)
            {
                // The crate lands on the ground before it, toward home.
                (float dx, float dy) = Toward(walk.Points, walk.Points.Count - 2);
                hx = x - (dx * ((0.45f * walker) + (0.5f * g)));
                hy = feet - (dy * ((0.45f * walker) + (0.5f * g))) - (0.5f * g);
            }

            if (u < WalkerLook.JumpEnd)
            {
                // The cube hops out of its cell into the hands (or onto the ground before it), growing and turning a quarter.
                float k = Smooth(u / WalkerLook.JumpEnd);
                float crouch = u < 0.08f ? 0.08f * (float)Math.Sin(u / 0.08f * Math.PI) : 0f;
                Figure(list, walk, x, y, WalkerLook.PoseFront, 1f + crouch, 1f - crouch);
                float cx = Lerp(tx, hx, k);
                float cy = Lerp(ty, hy, k) - ((float)Math.Sin(k * Math.PI) * walker * 0.55f);
                Cube(list, walk, cx, cy, Lerp(1f, g, k), 90f * k * _facing);
                list.Lifted = true;
                if (k < 0.6f)
                {
                    list.Ring(tx, ty, 0.35f + (0.5f * k), 0.08f * (1f - k), Rgba.White.WithAlpha(0.85f * (1f - (k / 0.6f))));
                }

                list.Lifted = false;
                return;
            }

            float p = Clamp01((u - WalkerLook.JumpEnd) / (WalkerLook.GagEnd - WalkerLook.JumpEnd));
            if (u > WalkerLook.GagEnd)
            {
                // Done: a little happy hop, then home.
                float h = (float)Math.Sin(Clamp01((u - WalkerLook.GagEnd) / (1f - WalkerLook.GagEnd)) * Math.PI) * 0.06f;
                Figure(list, walk, x, y, WalkerLook.PoseFront, 1f, 1f, h * walker);
                return;
            }

            Rgba color = ColorOf(walk);
            int seed = Seed(tx, ty);
            switch (style)
            {
                case ClearStyle.Blossom:
                    BlossomHands(list, walk, x, y, hx, hy, g, walker, p, color);
                    break;
                case ClearStyle.Bubbles:
                    BubbleHands(list, walk, x, y, hx, hy, g, walker, p, now);
                    break;
                case ClearStyle.Fireworks:
                    RocketHands(list, walk, x, y, hx, hy, g, walker, p, color, seed);
                    break;
                case ClearStyle.Fireflies:
                    FireflyHands(list, walk, x, y, hx, hy, g, walker, p, seed);
                    break;
                case ClearStyle.Pushers:
                {
                    // It braces behind the crate.
                    float lean = 10f * Smooth(p / 0.3f) * _facing;
                    Figure(list, walk, x, y, WalkerLook.PoseFront, 1f, 1f, 0f, lean);
                    Cube(list, walk, hx, hy, g, 0f);
                    break;
                }

                case ClearStyle.Parade:
                    JuggleHands(list, walk, x, y, hx, hy, g, walker, p, color, seed);
                    break;
                case ClearStyle.Bonk:
                    Toss(list, walk, x, y, hx, hy, g, walker, p, color, seed);
                    break;
                default:
                    switch ((Gag)(seed % 4))
                    {
                        case Gag.Chomp:
                            Chomp(list, walk, x, y, hx, hy, g, walker, p, color);
                            break;
                        case Gag.Squish:
                            Squish(list, walk, x, y, hx, hy, g, walker, p, color, seed);
                            break;
                        case Gag.Toss:
                            Toss(list, walk, x, y, hx, hy, g, walker, p, color, seed);
                            break;
                        default:
                            Shake(list, walk, x, y, hx, hy, g, walker, p, color, seed);
                            break;
                    }

                    break;
            }
        }

        // A flat family picture has no hands: it holds the cube over its head; the creatures and heroes hold it before their belly.
        private static float HoldY(float feet, float walker, float g) =>
            _creature || _hero ? feet - (WalkerLook.HeldUp * walker) : feet - (1.02f * walker) - (0.32f * g);

        // Munchers stepping aside (WalkerLook.AsideMunch): the cube jumps into its hands, it walks to its spot (ClearWalk.Aside),
        // eats the cube there in three bites with crumbs, and hops; its way home starts from the spot.
        private static void AsideAct(FxList list, in ClearWalk walk, float x, float y, float walker, float u)
        {
            float g = WalkerLook.HeldShare * walker;
            (float tx, float ty) = walk.Target;
            float sx = walk.HasAside ? walk.AsideX : x;
            float sy = walk.HasAside ? walk.AsideY : y;
            if (u < WalkerLook.AsideJumpEnd)
            {
                float k = Smooth(u / WalkerLook.AsideJumpEnd);
                float crouch = u < 0.07f ? 0.08f * (float)Math.Sin(u / 0.07f * Math.PI) : 0f;
                Figure(list, walk, x, y, WalkerLook.PoseFront, 1f + crouch, 1f - crouch);
                float hy = HoldY(y + (0.4f * walker), walker, g);
                float cx = Lerp(tx, x, k);
                float cy = Lerp(ty, hy, k) - ((float)Math.Sin(k * Math.PI) * walker * 0.5f);
                Cube(list, walk, cx, cy, Lerp(1f, g, k), 90f * k * _facing);
                list.Lifted = true;
                if (k < 0.6f)
                {
                    list.Ring(tx, ty, 0.35f + (0.5f * k), 0.08f * (1f - k), Rgba.White.WithAlpha(0.85f * (1f - (k / 0.6f))));
                }

                list.Lifted = false;
                if (k > 0.85f)
                {
                    HandsOver(list, walk, x, y, 1f, 1f);
                }

                return;
            }

            if (u < WalkerLook.AsideWalkEnd)
            {
                // To its spot with the cube in its hands, a little hop each step.
                float k = Smooth((u - WalkerLook.AsideJumpEnd) / (WalkerLook.AsideWalkEnd - WalkerLook.AsideJumpEnd));
                float px = Lerp(x, sx, k);
                float py = Lerp(y, sy, k);
                float hop = (float)Math.Abs(Math.Sin(k * Math.PI * 3f)) * 0.05f * walker;
                Figure(list, walk, px, py, WalkerLook.PoseHold, 1f, 1f, hop);
                Cube(list, walk, px, HoldY(py + (0.4f * walker), walker, g) - hop, g, 0f);
                HandsOver(list, walk, px, py, 1f, 1f, hop);
                return;
            }

            float p = Clamp01((u - WalkerLook.AsideWalkEnd) / (WalkerLook.AsideEatEnd - WalkerLook.AsideWalkEnd));
            if (u < WalkerLook.AsideEatEnd)
            {
                Chomp(list, walk, sx, sy, sx, HoldY(sy + (0.4f * walker), walker, g), g, walker, p, ColorOf(walk));
                return;
            }

            float h = (float)Math.Sin(Clamp01((u - WalkerLook.AsideEatEnd) / (1f - WalkerLook.AsideEatEnd)) * Math.PI) * 0.06f;
            Figure(list, walk, sx, sy, WalkerLook.PoseFront, 1f, 1f, h * walker);
        }

        // Bonk on a level without the live timeline (the Store's previews): out, the toss onto its head, home, a hop into the arch.
        private static void BonkTrip(FxList list, in ClearWalk walk, Spot spot)
        {
            float walker = WalkerSize * (_pop > 0f ? _pop : 1f);
            switch (spot.Leg)
            {
                case Leg.Out:
                case Leg.Back:
                    Walking(list, walk, spot, WalkerSize);
                    return;
                case Leg.Act:
                    HandsAct(list, ClearStyle.Bonk, walk, spot.X, spot.Y, walker, spot.U, 0f);
                    return;
                default:
                {
                    float u = spot.U;
                    float sink = 1f - (0.35f * u);
                    Body(list, spot.X, spot.Y, WalkerSize, (float)Math.Sin(u * Math.PI) * 0.16f, 0f, sink, sink, 1f - u, walk.Variant);
                    return;
                }
            }
        }

        // The Trails mock's Munchers (WalkerLook.Trails): out on the right-hand lane, the cube hops into its hands at its cube (a
        // quarter of a second, no standing), and it eats it in three bites walking home on the other lane, crumbs falling behind.
        private static void TrailsMunch(FxList list, in ClearWalk walk, Spot spot, float walker, float end, float now)
        {
            // The cube in its hands never smaller than on the board (the owner, 2026-11-03); one bigger than the hands can hold it carries
            // on its head (Overhead), like the reference game's ants.
            float g = Math.Max(1f, WalkerLook.HeldShare * walker);
            bool overhead = g > WalkerLook.HeldShare * walker * 1.05f;
            switch (spot.Leg)
            {
                case Leg.Out:
                {
                    float prog = spot.U * end;
                    (float x, float y) = Along(walk.Points, prog);
                    (float ox, float oy) = LaneOffset(walk, prog, 1f, walker);
                    spot.X = x + ox;
                    spot.Y = y + oy;
                    Walking(list, walk, spot, WalkerSize);
                    return;
                }

                case Leg.Act:
                {
                    (float x, float y) = Along(walk.Points, end);
                    (float tx, float ty) = walk.Target;
                    float k = Smooth(spot.U);
                    float hy = TrailHoldY(y + (0.4f * walker), walker, g, overhead);
                    Figure(list, walk, x, y, k > 0.8f && !overhead ? WalkerLook.PoseHold : WalkerLook.PoseFront, WalkerLook.TrailSquash, 1f);
                    Cube(list, walk, Lerp(tx, x, k), Lerp(ty, hy, k) - ((float)Math.Sin(k * Math.PI) * walker * 0.4f), Lerp(1f, g, k), 90f * k * _facing);
                    if (k > 0.8f)
                    {
                        if (!overhead) HandsOver(list, walk, x, y, WalkerLook.TrailSquash, 1f);
                    }

                    return;
                }

                default:
                {
                    float prog = (1f - spot.U) * end;
                    (float x, float y) = Along(walk.Points, prog);
                    (float ox, float oy) = LaneOffset(walk, prog, -1f, walker);
                    spot.X = x + ox;
                    spot.Y = y + oy;

                    // It carries the cube a little first (WalkerLook.TrailCarrySeconds or TrailCarryCells), then eats it in three
                    // bites spread over its way home up to WalkerLook.TrailEatEnd.
                    float from = walk.ClearAt + ClearStyles.Linger;
                    float home = ClearStyles.LiveHomeAt(walk.Start, walk.ArriveAt, walk.ClearAt, walk.DoneAt);
                    int segments = Math.Max(1, walk.Points.Count - 1);
                    float carry = Math.Min(0.5f, Math.Max(WalkerLook.TrailCarrySeconds / Math.Max(0.05f, home - from), WalkerLook.TrailCarryCells / segments));
                    float eatSpan = Math.Max(0.05f, WalkerLook.TrailEatEnd - carry);
                    float p = Clamp01((spot.U - carry) / eatSpan);
                    IReadOnlyList<float> at = WalkerLook.TrailBites;
                    float bite = WalkerLook.TrailBiteShare;
                    float left = 1f;
                    left = Lerp(left, 0.66f, Smooth((p - at[0]) / bite));
                    left = Lerp(left, 0.33f, Smooth((p - at[1]) / bite));
                    left = Lerp(left, 0f, Smooth((p - at[2]) / bite));
                    if (left <= 0.02f)
                    {
                        Walking(list, walk, spot, WalkerSize);
                    }
                    else
                    {
                        float hop = (float)Math.Abs(Math.Sin(spot.Hops * Math.PI)) * 0.05f * walker;
                        float chew = p > 0f ? Math.Abs((float)Math.Sin(p * Math.PI * 9f)) * 0.05f : 0f;
                        Figure(list, walk, spot.X, spot.Y, overhead ? WalkerLook.PoseFront : WalkerLook.PoseHold, WalkerLook.TrailSquash, 1f - chew, hop, WalkerLook.TrailLean * _facing * 0.5f);
                        float cy = TrailHoldY(spot.Y + (0.4f * walker), walker, g, overhead) - hop;
                        list.Lifted = true;
                        list.PushSquash(1f, left, spot.X, cy + (g / 2f));
                        list.Tile(Box.FromCenter(spot.X, cy, g, g), walk.Variant);
                        list.Pop();
                        list.Lifted = false;
                        if (!overhead) HandsOver(list, walk, spot.X, spot.Y, WalkerLook.TrailSquash, 1f - chew, hop);
                    }

                    // Each bite's crumbs where it was taken, falling as the walker goes on.
                    Rgba color = ColorOf(walk);
                    list.Lifted = true;
                    for (int b = 0; b < at.Count; b++)
                    {
                        float t = (p - at[b]) / 0.3f;
                        if (t <= 0f || t >= 1f)
                        {
                            continue;
                        }

                        float bp = (1f - (carry + (at[b] * eatSpan))) * end;
                        (float bx, float by) = Along(walk.Points, bp);
                        (float bo, float bq) = LaneOffset(walk, bp, -1f, walker);
                        float top = TrailHoldY(by + bq + (0.4f * walker), walker, g, overhead) - (g * (0.5f - (b / 3f)));
                        for (int i = 0; i < 5; i++)
                        {
                            float dir = (i - 2f) * 0.5f;
                            float cx = bx + bo + (dir * walker * 0.4f * t);
                            float cy = top - (walker * 0.35f * t) + (walker * 1.2f * t * t);
                            float r = g * 0.1f * (1f - (0.4f * t));
                            list.PushAlpha(1f - (t * t));
                            list.Round(Box.FromCenter(cx, cy, r * 2.5f, r * 2.5f), r * 0.6f, Rgba.White);
                            list.Round(Box.FromCenter(cx, cy, r * 2f, r * 2f), r * 0.5f, color);
                            list.Pop();
                        }
                    }

                    list.Lifted = false;
                    return;
                }
            }
        }

        // Where a Trails walker holds its cube: before its belly, or on its head when the cube is bigger than its hands can hold.
        private static float TrailHoldY(float feet, float walker, float g, bool overhead) =>
            overhead ? feet - (0.92f * walker) - (0.4f * g) : HoldY(feet, walker, g);

        // The sideways shift of a lane at a way's progress: perpendicular to the way (its direction smoothed over a cell or so),
        // WalkerLook.TrailLane walker boxes to the right of the way toward the cube for side 1 (out) and to the left for -1 (home),
        // easing to the centre line within a cell or so of the entry and of the cube.
        private static (float X, float Y) LaneOffset(in ClearWalk walk, float progress, float side, float walker)
        {
            IReadOnlyList<(float X, float Y)> points = walk.Points;
            int segments = Math.Max(1, points.Count - 1);
            float e = 0.6f / segments;
            (float ax, float ay) = Along(points, progress - e);
            (float bx, float by) = Along(points, progress + e);
            float dx = bx - ax;
            float dy = by - ay;
            float len = (float)Math.Sqrt((dx * dx) + (dy * dy));
            if (len < 1e-4f)
            {
                return (0f, 0f);
            }

            float fade = Smooth(Math.Min(progress, 1f - progress) * segments / 1.2f);
            float off = side * WalkerLook.TrailLane * walker * fade * LaneFree(walk.Lanes, segments, progress, e, side);
            return (-dy / len * off, dx / len * off);
        }

        // How free its lane's side is about a way's progress (TrailLanes flags of the segments a little before and after, so a
        // lane eases on and off its side over a cell): 1 free, 0 taken (it keeps to the centre line), 1 without flags.
        private static float LaneFree(IReadOnlyList<byte>? lanes, int segments, float progress, float e, float side)
        {
            if (lanes == null || lanes.Count == 0)
            {
                return 1f;
            }

            byte bit = side > 0f ? TrailLanes.Right : TrailLanes.Left;
            float Free(float at)
            {
                int i = Math.Max(0, Math.Min(lanes.Count - 1, (int)(Clamp01(at) * segments)));
                return (lanes[i] & bit) != 0 ? 1f : 0f;
            }

            return (Free(progress - e) + Free(progress) + Free(progress + e)) / 3f;
        }

        // ---- Munchers' gags ----

        // Chomp: three bites off the top, crumbs in its colour, then a gulp.
        private static void Chomp(FxList list, in ClearWalk walk, float x, float y, float hx, float hy, float g, float walker, float p, Rgba color)
        {
            float[] at = { 0.15f, 0.4f, 0.65f };
            float left = 1f;
            left = Lerp(left, 0.66f, Smooth((p - at[0]) / 0.06f));
            left = Lerp(left, 0.33f, Smooth((p - at[1]) / 0.06f));
            left = Lerp(left, 0f, Smooth((p - at[2]) / 0.06f));
            bool holding = left > 0.02f;
            float chew = p < 0.72f ? Math.Abs((float)Math.Sin(p * Math.PI * 8f)) * 0.06f : 0f;
            float gulp = p > 0.72f ? (float)Math.Sin(Clamp01((p - 0.72f) / 0.2f) * Math.PI) * 0.12f : 0f;
            Figure(list, walk, x, y, holding ? WalkerLook.PoseHold : WalkerLook.PoseFront, 1f + gulp, 1f - chew - (gulp * 0.6f));
            if (holding)
            {
                list.Lifted = true;
                list.PushSquash(1f, left, hx, hy + (g / 2f));
                list.Tile(Box.FromCenter(hx, hy, g, g), walk.Variant);
                list.Pop();
                list.Lifted = false;
                HandsOver(list, walk, x, y, 1f, 1f - chew);
            }

            list.Lifted = true;
            for (int b = 0; b < at.Length; b++)
            {
                float t = (p - at[b]) / 0.3f;
                if (t <= 0f || t >= 1f)
                {
                    continue;
                }

                float top = hy + (g / 2f) - (g * (1f - (b / 3f)));
                for (int i = 0; i < 6; i++)
                {
                    float dir = (i - 2.5f) * 0.45f;
                    float cx = hx + (dir * walker * 0.5f * t);
                    float cy = top - (walker * (0.45f + (0.1f * (i % 3))) * t) + (walker * 1.1f * t * t);
                    float r = g * 0.11f * (1f - (0.4f * t));
                    list.PushAlpha(1f - (t * t));
                    list.PushTurn(t * 300f * (i % 2 == 0 ? 1f : -1f), cx, cy);
                    list.Round(Box.FromCenter(cx, cy, r * 2.5f, r * 2.5f), r * 0.6f, Rgba.White);
                    list.Round(Box.FromCenter(cx, cy, r * 2f, r * 2f), r * 0.5f, color);
                    list.Pop();
                    list.Pop();
                }
            }

            list.Lifted = false;
        }

        // Squish: it squeezes the cube, the cube wobbles, swells and pops into little cubes.
        private static void Squish(FxList list, in ClearWalk walk, float x, float y, float hx, float hy, float g, float walker, float p, Rgba color, int seed)
        {
            const float PopAt = 0.62f;
            if (p < PopAt)
            {
                float s = Smooth(p / 0.45f);
                float wob = (float)Math.Sin(p * 46f) * 0.07f * s;
                float swell = Smooth((p - 0.45f) / (PopAt - 0.45f));
                float sx = (1f - (0.3f * s) + wob) * (1f + (0.45f * swell));
                float sy = (1f + (0.22f * s) - wob) * (1f + (0.3f * swell));
                Figure(list, walk, x, y, WalkerLook.PoseHold, 1f - (0.06f * s), 1f + (0.05f * s));
                Cube(list, walk, hx, hy, g, 0f, sx, sy);
                HandsOver(list, walk, x, y, 1f - (0.06f * s), 1f + (0.05f * s));
                return;
            }

            float t = (p - PopAt) / (1f - PopAt);
            float recoil = (float)Math.Sin(Clamp01(t / 0.4f) * Math.PI);
            Figure(list, walk, x, y, WalkerLook.PoseFront, 1f + (0.08f * recoil), 1f - (0.08f * recoil), 0f, -6f * recoil * _facing);
            list.Lifted = true;
            list.PushAlpha(1f - t);
            list.Ring(hx, hy, g * (0.5f + (1.1f * Ease(t))), Math.Max(0.03f, g * 0.12f * (1f - t)), Rgba.White);
            list.Pop();
            for (int i = 0; i < 10; i++)
            {
                double a = (i * Math.PI * 2 / 10) + (seed % 5 * 0.3);
                float speed = walker * (0.9f + (0.35f * ((seed + (i * 7)) % 4) / 3f));
                float px = hx + ((float)Math.Cos(a) * speed * Ease(t));
                float py = hy + ((float)Math.Sin(a) * speed * Ease(t) * 0.8f) - (walker * 0.4f * t) + (walker * 1.2f * t * t);
                float side = g * 0.26f * (1f - (0.3f * t));
                list.PushAlpha(1f - Smooth((t - 0.6f) / 0.4f));
                list.PushTurn(t * 360f * (i % 2 == 0 ? 1f : -1f), px, py);
                list.Tile(Box.FromCenter(px, py, side, side), walk.Variant);
                list.Pop();
                list.Pop();
            }

            list.Lifted = false;
        }

        // Toss: it throws the cube up, the cube spins, falls on its head, bonks and shatters; stars circle its head.
        private static void Toss(FxList list, in ClearWalk walk, float x, float y, float hx, float hy, float g, float walker, float p, Rgba color, int seed)
        {
            float feet = y + (0.4f * walker);
            float headY = feet - ((_hero ? 1.22f : 0.98f) * walker) - (g * 0.35f);
            if (p < 0.1f)
            {
                float dip = 0.08f * walker * Smooth(p / 0.1f);
                Figure(list, walk, x, y, WalkerLook.PoseHold, 1.04f, 0.94f);
                Cube(list, walk, hx, hy + dip, g, 0f);
                HandsOver(list, walk, x, y, 1.04f, 0.94f);
                return;
            }

            const float Bonk = 0.6f;
            if (p < Bonk)
            {
                float t = (p - 0.1f) / (Bonk - 0.1f);
                float cy = Lerp(hy, headY, t) - (walker * 1.5f * 4f * t * (1f - t));
                Figure(list, walk, x, y, WalkerLook.PoseFront, 1f, 1f + (0.04f * (1f - t)));
                Cube(list, walk, hx, cy, g, 540f * t * _facing);
                return;
            }

            float k = (p - Bonk) / (1f - Bonk);
            float squash = (float)Math.Sin(Clamp01(k / 0.35f) * Math.PI) * 0.18f;
            Figure(list, walk, x, y, WalkerLook.PoseFront, 1f + (squash * 0.7f), 1f - squash);
            list.Lifted = true;
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float spread = 0.3f + (0.25f * (i / 2));
                float px = x + (side * walker * spread * Ease(k));
                float py = headY - (walker * (0.35f + (0.1f * (i % 3))) * k) + (walker * 2.2f * k * k);
                float s = g * 0.42f * (1f - (0.3f * k));
                list.PushAlpha(1f - Smooth((k - 0.55f) / 0.45f));
                list.PushTurn(k * 420f * side, px, py);
                list.Tile(Box.FromCenter(px, py, s, s), walk.Variant);
                list.Pop();
                list.Pop();
            }

            // The stars circling its dizzy head.
            for (int i = 0; i < 3; i++)
            {
                double a = (k * Math.PI * 3.0) + (i * Math.PI * 2.0 / 3.0);
                float sx = x + ((float)Math.Cos(a) * walker * 0.38f);
                float sy = headY + (g * 0.2f) + ((float)Math.Sin(a) * walker * 0.1f);
                float size = walker * 0.26f;
                list.PushAlpha(1f - Smooth((k - 0.7f) / 0.3f));
                list.Shape("fx.sparkle", Box.FromCenter(sx, sy, size, size), DesignTokens.Colors.FxFireflyGlow);
                list.Pop();
            }

            list.Lifted = false;
        }

        // Shake: it shakes the cube like a gift box (shaking along with it), then the cube puffs into sparkles.
        private static void Shake(FxList list, in ClearWalk walk, float x, float y, float hx, float hy, float g, float walker, float p, Rgba color, int seed)
        {
            const float PuffAt = 0.62f;
            if (p < PuffAt)
            {
                float amp = walker * 0.09f * (0.4f + (0.6f * Smooth(p / 0.2f)));
                float off = amp * (float)Math.Sin(p * 52f);
                float lift = walker * 0.05f * Math.Abs((float)Math.Sin(p * 52f));
                Figure(list, walk, x + off, y, WalkerLook.PoseHold, 1f, 1f, 0f, 6f * (float)Math.Sin(p * 52f));
                Cube(list, walk, hx + off, hy - lift, g, 14f * (float)Math.Sin((p * 52f) + 0.6f));
                HandsOver(list, walk, x + off, y, 1f, 1f);
                return;
            }

            float t = (p - PuffAt) / (1f - PuffAt);
            Figure(list, walk, x, y, WalkerLook.PoseFront, 1f, 1f, walker * 0.04f * (float)Math.Sin(t * Math.PI));
            list.Lifted = true;
            if (t < 0.35f)
            {
                float k = t / 0.35f;
                list.PushAlpha(1f - k);
                list.Tile(Box.FromCenter(hx, hy, g * (1f + (0.25f * k)), g * (1f + (0.25f * k))), walk.Variant);
                list.Pop();
            }

            list.PushAlpha(1f - t);
            list.Circle(hx, hy, g * (0.45f + (0.5f * Ease(t))), Rgba.White.WithAlpha(0.55f));
            list.Pop();
            for (int i = 0; i < 8; i++)
            {
                double a = (i * Math.PI * 2 / 8) + 0.3;
                float d = g * (0.4f + (1.2f * Ease(t)));
                float size = g * (0.42f - (0.2f * t)) * (i % 2 == 0 ? 1f : 0.7f);
                list.PushAlpha(1f - Smooth((t - 0.5f) / 0.5f));
                list.Shape("fx.sparkle", Box.FromCenter(hx + ((float)Math.Cos(a) * d), hy + ((float)Math.Sin(a) * d), size, size), i % 2 == 0 ? Rgba.White : color.Lighten(0.5f));
                list.Pop();
            }

            list.Lifted = false;
        }

        // ---- The other styles at the hands ----

        // Blossom: the cube in its hands opens into a flower of its colour, which floats up.
        private static void BlossomHands(FxList list, in ClearWalk walk, float x, float y, float hx, float hy, float g, float walker, float p, Rgba color)
        {
            const float Open = 0.5f;
            float k = Clamp01(p / Open);
            bool holding = p < Open;
            Figure(list, walk, x, y, holding ? WalkerLook.PoseHold : WalkerLook.PoseFront, 1f, 1f, holding ? 0f : walker * 0.03f * (float)Math.Sin((p - Open) * 12f));
            float side = g * (1f - (0.7f * Smooth(k)));
            if (holding)
            {
                Cube(list, walk, hx, hy, side, 45f * Smooth(k) * _facing);
            }

            float rise = walker * 0.9f * Smooth((p - Open) / (1f - Open));
            float fade = 1f - Smooth((p - 0.8f) / 0.2f);
            list.Lifted = true;
            list.PushAlpha(fade);
            Flower(list, hx, hy - rise - (g * 0.1f), g * 1.25f * Back(Clamp01(k * 1.1f)), (40f * k) + (20f * (float)Math.Sin(p * 6f)), color);
            list.Pop();
            list.Lifted = false;
            if (holding)
            {
                HandsOver(list, walk, x, y, 1f, 1f);
            }
        }

        // Bubbles: it blows a bubble round the cube in its hands; the bubble floats off on a lazy wave and pops.
        private static void BubbleHands(FxList list, in ClearWalk walk, float x, float y, float hx, float hy, float g, float walker, float p, float now)
        {
            const float Blown = 0.4f;
            const float PopAt = 0.88f;
            bool holding = p < Blown;
            float puff = holding ? 0.08f * (float)Math.Sin(p * 34f) : 0f;
            Figure(list, walk, x, y, holding ? WalkerLook.PoseHold : WalkerLook.PoseFront, 1f + puff, 1f - puff);
            float k = Smooth(p / Blown);
            float r = g * 0.78f * k;
            float t = Smooth((p - Blown) / (PopAt - Blown));
            float bx = hx + (walker * 0.25f * (float)Math.Sin(t * 7f));
            float by = hy - (walker * 1.7f * t);
            if (p < PopAt)
            {
                Cube(list, walk, bx, by, g * (1f - (0.3f * k)), 8f * (float)Math.Sin(p * 9f));
                list.Lifted = true;
                Bubble(list, bx, by, Math.Max(r, 0.02f), now);
                list.Lifted = false;
            }
            else
            {
                list.Lifted = true;
                Pop(list, bx, by, g * 0.78f, (p - PopAt) / (1f - PopAt));
                list.Lifted = false;
            }

            if (holding)
            {
                HandsOver(list, walk, x, y, 1f + puff, 1f - puff);
            }
        }

        // Fireworks: the cube fizzes in its hands, shoots up as a rocket and bursts into sparks of its colour.
        private static void RocketHands(FxList list, in ClearWalk walk, float x, float y, float hx, float hy, float g, float walker, float p, Rgba color, int seed)
        {
            const float Fizz = 0.38f;
            const float Burst = 0.62f;
            bool holding = p < Fizz;
            Figure(list, walk, x, y, holding ? WalkerLook.PoseHold : WalkerLook.PoseFront, 1f, 1f, 0f, holding ? 0f : -5f * _facing);
            list.Lifted = true;
            if (holding)
            {
                list.Lifted = false;
                Cube(list, walk, hx, hy, g, 6f * (float)Math.Sin(p * 60f));
                list.Lifted = true;
                for (int i = 0; i < 5; i++)
                {
                    float flick = (float)Math.Abs(Math.Sin((p * 40f) + (i * 1.7f)));
                    double a = (i * 1.3) + (p * 9f);
                    float sx = hx + ((float)Math.Cos(a) * g * 0.45f);
                    float sy = hy - (g * 0.5f) - (flick * g * 0.25f);
                    list.Shape("fx.sparkle", Box.FromCenter(sx, sy, g * 0.3f * flick, g * 0.3f * flick), i % 2 == 0 ? DesignTokens.Colors.FxFireflyGlow : Rgba.White);
                }
            }
            else if (p < Burst)
            {
                float t = (p - Fizz) / (Burst - Fizz);
                float cy = hy - (walker * 2.2f * t * t);
                for (int i = 1; i <= 6; i++)
                {
                    float tt = Math.Max(0f, t - (i * 0.06f));
                    list.Circle(hx, hy - (walker * 2.2f * tt * tt) + (g * 0.3f), g * 0.12f * (1f - (i / 7f)), DesignTokens.Colors.FxFireflyGlow.WithAlpha(0.8f * (1f - (i / 7f))));
                }

                list.PushTurn(360f * t, hx, cy);
                list.Tile(Box.FromCenter(hx, cy, g * (1f - (0.45f * t)), g * (1f - (0.45f * t))), walk.Variant);
                list.Pop();
            }
            else
            {
                float t = (p - Burst) / (1f - Burst);
                float cy = hy - (walker * 2.2f);
                list.PushAlpha(1f - Smooth((t - 0.5f) / 0.5f));
                for (int i = 0; i < 14; i++)
                {
                    double a = i * Math.PI * 2 / 14;
                    float d = walker * (0.3f + (1.1f * Ease(t))) * (i % 2 == 0 ? 1f : 0.75f);
                    float px = hx + ((float)Math.Cos(a) * d);
                    float py = cy + ((float)Math.Sin(a) * d) + (walker * 0.5f * t * t);
                    if (i % 2 == 0)
                    {
                        list.Circle(px, py, g * 0.13f * (1f - (0.5f * t)), color.Lighten(0.15f));
                    }
                    else
                    {
                        list.Shape("fx.sparkle", Box.FromCenter(px, py, g * 0.32f, g * 0.32f), Rgba.White);
                    }
                }

                list.Pop();
            }

            list.Lifted = false;
            if (holding)
            {
                HandsOver(list, walk, x, y, 1f, 1f);
            }
        }

        // Fireflies: the cube glows in its hands and breaks into glowing flies that drift away.
        private static void FireflyHands(FxList list, in ClearWalk walk, float x, float y, float hx, float hy, float g, float walker, float p, int seed)
        {
            const float Glow = 0.35f;
            bool holding = p < Glow;
            Figure(list, walk, x, y, holding ? WalkerLook.PoseHold : WalkerLook.PoseFront, 1f, 1f, holding ? 0f : walker * 0.03f * (float)Math.Abs(Math.Sin(p * 14f)));
            Rgba glow = DesignTokens.Colors.FxFireflyGlow;
            if (p < Glow + 0.1f)
            {
                float k = Clamp01(p / Glow);
                float gone = Clamp01((p - Glow) / 0.1f);
                float pulse = 1f + (0.05f * (float)Math.Sin(p * 40f));
                Cube(list, walk, hx, hy, g * pulse * (1f + (0.2f * gone)), 0f, 1f, 1f, 1f - gone);
                list.Lifted = true;
                list.Round(Box.FromCenter(hx, hy, g * pulse, g * pulse), g * 0.18f, glow.WithAlpha(0.65f * k * (1f - gone)));
                list.Circle(hx, hy, g * 0.9f * k, glow.WithAlpha(0.25f * k * (1f - gone)));
                list.Lifted = false;
            }

            if (p > Glow)
            {
                float t = (p - Glow) / (1f - Glow);
                list.Lifted = true;
                for (int i = 0; i < 9; i++)
                {
                    double a = (-Math.PI / 2) + ((i - 4) * 0.42) + ((seed % 3) * 0.1);
                    float sx = hx + (((i % 3) - 1) * g * 0.3f);
                    float sy = hy + (((i / 3) - 1) * g * 0.3f);
                    float d = walker * (1.4f + (0.3f * (i % 4))) * Ease(t);
                    float px = sx + ((float)Math.Cos(a) * d) + (walker * 0.12f * (float)Math.Sin((t * 12f) + i));
                    float py = sy + ((float)Math.Sin(a) * d);
                    float fade = 1f - Smooth((t - 0.6f) / 0.4f);
                    float blink = 0.75f + (0.25f * (float)Math.Sin((t * 25f) + (i * 2f)));
                    list.Circle(px, py, g * 0.2f, glow.WithAlpha(0.35f * fade * blink));
                    list.Circle(px, py, g * 0.07f, glow.WithAlpha(fade));
                    list.Circle(px, py, g * 0.035f, Rgba.White.WithAlpha(fade));
                }

                list.Lifted = false;
            }

            if (holding)
            {
                HandsOver(list, walk, x, y, 1f, 1f);
            }
        }

        // The Parade: it juggles the cube three times, then throws it high and it bursts into confetti.
        private static void JuggleHands(FxList list, in ClearWalk walk, float x, float y, float hx, float hy, float g, float walker, float p, Rgba color, int seed)
        {
            const float Juggle = 0.7f;
            if (p < Juggle)
            {
                float t = (p / Juggle * 3f) % 1f;
                bool caught = t < 0.12f || t > 0.88f;
                float up = walker * 0.75f * 4f * t * (1f - t);
                float bob = caught ? 0f : walker * 0.03f;
                Figure(list, walk, x, y, WalkerLook.PoseHold, 1f, 1f, bob);
                Cube(list, walk, hx, hy - up - bob, g, 180f * t * _facing);
                if (caught)
                {
                    HandsOver(list, walk, x, y, 1f, 1f, bob);
                }

                return;
            }

            float k = (p - Juggle) / (1f - Juggle);
            Figure(list, walk, x, y, WalkerLook.PoseFront, 1f, 1f, walker * 0.05f * (float)Math.Sin(k * Math.PI));
            float apex = hy - (walker * 1.4f);
            if (k < 0.4f)
            {
                float t = k / 0.4f;
                Cube(list, walk, hx, Lerp(hy, apex, Ease(t)), g, 270f * t);
                return;
            }

            float c = (k - 0.4f) / 0.6f;
            list.Lifted = true;
            list.PushAlpha(1f - c);
            list.Ring(hx, apex, g * (0.4f + (1.2f * Ease(c))), Math.Max(0.03f, g * 0.12f * (1f - c)), Rgba.White);
            for (int i = 0; i < 14; i++)
            {
                double a = (i * Math.PI * 2 / 14) + (seed % 7 * 0.2);
                float speed = walker * (0.8f + (0.4f * ((seed + (i * 13)) % 5) / 4f));
                float px = hx + ((float)Math.Cos(a) * speed * Ease(c));
                float py = apex + ((float)Math.Sin(a) * speed * Ease(c)) + (walker * 1.3f * c * c);
                float size = g * 0.3f * (1f - (0.35f * c));
                Rgba piece = i % 3 == 0 ? DesignTokens.Colors.GardenGlow : i % 3 == 1 ? color : DesignTokens.Colors.FxConfettiPink;
                list.PushTurn(c * 500f * (i % 2 == 0 ? 1f : -1f), px, py);
                list.Round(Box.FromCenter(px, py, size + (g * 0.06f), (size * 0.62f) + (g * 0.06f)), size * 0.2f, Rgba.White);
                list.Round(Box.FromCenter(px, py, size, size * 0.62f), size * 0.15f, piece);
                list.Pop();
            }

            list.Pop();
            list.Lifted = false;
        }

        // ---- Pieces ----

        // The walker at (x, y) in a pose (its shadow, then its figure), squashed and leaning about its feet.
        private static void Figure(FxList list, in ClearWalk walk, float x, float y, int pose, float sx = 1f, float sy = 1f, float hop = 0f, float lean = 0f)
        {
            _pose = pose;
            Body(list, x, y, WalkerSize, hop, lean, sx, sy, 1f, walk.Variant);
            _pose = -1;
        }

        // Its hands over the held cube (the same squash), drawn after every walker with the cube.
        private static void HandsOver(FxList list, in ClearWalk walk, float x, float y, float sx, float sy, float hop = 0f)
        {
            if (!_creature && !_hero)
            {
                // A flat family picture has no hands to draw over the cube.
                return;
            }

            _pose = WalkerLook.PoseHands;
            list.Lifted = true;
            Body(list, x, y, WalkerSize, hop, 0f, sx, sy, 1f, walk.Variant, false);
            list.Lifted = false;
            _pose = -1;
        }

        // A cube (the board's own cube picture in the mock's hosts), drawn after every walker.
        private static void Cube(FxList list, in ClearWalk walk, float cx, float cy, float side, float turn, float sx = 1f, float sy = 1f, float alpha = 1f)
        {
            list.Lifted = true;
            list.PushAlpha(alpha);
            list.PushTurn(turn, cx, cy);
            list.PushSquash(sx, sy, cx, cy);
            list.Tile(Box.FromCenter(cx, cy, side, side), walk.Variant);
            list.Pop();
            list.Pop();
            list.Pop();
            list.Lifted = false;
        }

        // A deterministic number from a cube's cell (its gag, its sparks).
        private static int Seed(float x, float y) => ((int)Math.Floor(x) * 7) + ((int)Math.Floor(y) * 13) & 0x7fffffff;
    }
}
