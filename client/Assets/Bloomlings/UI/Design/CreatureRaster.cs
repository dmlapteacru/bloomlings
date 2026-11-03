using System;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    public static partial class UiRaster
    {
        // ---- The walker mock's running creature (WalkerLook; review material of 2026-11-03, not the game's look) ----

        /// <summary>
        /// A round walker drawn as a little creature with arms and legs (the walker mock, <see cref="WalkerLook.Creature"/>,
        /// <see cref="WalkerLook.HandsCreature"/>): its family's body (<paramref name="family"/>: a leaf with veins, a flower with
        /// a light middle, a drop, an acorn with its cap) in <paramref name="body"/>, lit from the upper left with a gloss spot and
        /// a dark <paramref name="line"/> outline, two eyes, stick arms with round hands and stick legs with feet.
        /// <paramref name="pose"/> is a <see cref="WalkerLook"/> pose: 0 to <see cref="WalkerLook.RunFrames"/> − 1 the side
        /// view's run cycle facing right (the legs stride, the arms swing, the body bobs and leans; a host mirrors it for the
        /// left), <see cref="WalkerLook.PoseSide"/> standing from the side, <see cref="WalkerLook.PoseFront"/> facing the player,
        /// <see cref="WalkerLook.PoseHold"/> facing the player with its arms forward to hold a cube in front of its belly (the
        /// cube's sides at a quarter of the picture either side, its middle at <see cref="WalkerLook.Held"/>), and
        /// <see cref="WalkerLook.PoseHands"/> only that pose's hands, drawn over the cube. A picture of <paramref name="width"/>
        /// × <paramref name="height"/> (the creature in its middle square), straight alpha, deterministic.
        /// </summary>
        public static byte[] Creature(int width, int height, Rgba body, Rgba line, Family family, int pose)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float s = Math.Min(width, height);
            float ox = (width - s) / 2f;
            float oy = (height - s) / 2f;
            bool handsOnly = pose == WalkerLook.PoseHands;
            bool hold = pose == WalkerLook.PoseHold || handsOnly;
            bool front = hold || pose == WalkerLook.PoseFront;
            bool running = pose >= 0 && pose < WalkerLook.RunFrames;
            double phi = running ? pose * Math.PI * 2.0 / WalkerLook.RunFrames : 0.0;
            float sin = (float)Math.Sin(phi);
            float cos = (float)Math.Cos(phi);
            float bob = running ? 0.035f * Math.Abs(sin) : 0f;
            float cx = front ? 0.5f : 0.47f;
            float cy = (front ? 0.5f : 0.58f) - bob;
            float tilt = (front ? 0f : running ? 16f : 8f) * (float)Math.PI / 180f;
            float ts = (float)Math.Sin(tilt);
            float tc = (float)Math.Cos(tilt);
            float lw = Math.Max(1.4f / s, 0.04f);
            float limb = Math.Max(1.2f / s, 0.04f);
            const float Hand = 0.047f;
            Rgba backLine = line.Lighten(0.22f);
            Rgba cap = body.Darken(0.38f);
            var light = (X: -0.45f, Y: -0.65f, Z: 0.61f);

            // The legs: from the side each foot sweeps back on the ground and lifts on its way forward; from the front they stand apart.
            (float hx, float hy, float fx, float fy) Leg(float hipX, double phase)
            {
                if (front)
                {
                    return (hipX, cy + 0.22f, cx + ((hipX - cx) * 1.35f), 0.925f);
                }

                float c = running ? (float)Math.Cos(phase) : 0f;
                float sn = running ? (float)Math.Sin(phase) : 0f;
                return (hipX, 0.8f - bob, hipX + (0.14f * c), 0.925f - (0.085f * Math.Max(0f, -sn)));
            }

            // The arms: from the side they swing against the legs; from the front they hang by the body or reach forward to hold.
            (float sx, float sy, float hx, float hy) Arm(int side, bool back)
            {
                if (hold)
                {
                    return (cx + (side * 0.17f), cy + 0.09f, cx + (side * 0.25f), 0.77f);
                }

                if (front)
                {
                    return (cx + (side * 0.19f), cy + 0.07f, cx + (side * 0.28f), cy + 0.22f);
                }

                // From the side both arms hang behind the body (the far one lighter), only their hands and wrists showing past it.
                float a = running ? 1.1f * (back ? cos : -cos) : (back ? -0.35f : 0.35f);
                float shx = cx + (back ? -0.02f : 0.02f);
                float shy = cy + 0.04f;
                return (shx, shy, shx + (0.27f * (float)Math.Sin(a)), shy + (0.22f * (float)Math.Cos(a)));
            }

            (float bhx, float bhy, float bfx, float bfy) = Leg(cx - (front ? 0.07f : 0.07f), phi + Math.PI);
            (float ahx, float ahy, float afx, float afy) = Leg(cx + (front ? 0.07f : 0.03f), phi);
            (float b1x, float b1y, float b2x, float b2y) = Arm(front ? -1 : -1, true);
            (float a1x, float a1y, float a2x, float a2y) = Arm(1, false);

            // The body's distance (normalized units, negative inside), its local frame leaning forward from the side.
            float Local(float x, float y, out float lx, out float ly)
            {
                float qx = x - cx;
                float qy = y - cy;
                lx = (qx * tc) + (qy * ts);
                ly = (qx * ts) - (qy * tc);
                return 0f;
            }

            float Body(float x, float y)
            {
                Local(x, y, out float lx, out float ly);
                switch (family)
                {
                    case Family.Bloom:
                    {
                        // Five round petals round a middle, one petal up.
                        float d0 = Length(lx, ly - 0.02f) - 0.17f;
                        for (int i = 0; i < 5; i++)
                        {
                            double th = (Math.PI / 2.0) + (i * Math.PI * 2.0 / 5.0);
                            d0 = Math.Min(d0, Length(lx - (0.15f * (float)Math.Cos(th)), ly - 0.02f - (0.15f * (float)Math.Sin(th))) - 0.115f);
                        }

                        return d0;
                    }

                    case Family.Twig:
                    {
                        float nut = Ellipse(lx, ly + 0.03f, 0.225f, 0.235f);
                        float top = Ellipse(lx, ly - 0.14f, 0.27f, 0.13f);
                        return Math.Min(nut, top);
                    }

                    case Family.Drop:
                        return UnevenCapsule(lx, ly + 0.02f, 0.255f, 0.025f, 0.32f);
                    default:
                        return UnevenCapsule(lx, ly, 0.25f, 0.05f, 0.3f);
                }
            }

            for (int py = 0; py < height; py++)
            {
                float y = (py + 0.5f - oy) / s;
                for (int px = 0; px < width; px++)
                {
                    float x = (px + 0.5f - ox) / s;
                    var c = new Color(line);
                    float a = 0f;
                    if (handsOnly)
                    {
                        HandDisc(ref c, ref a, x, y, b2x, b2y, Hand, body, line, lw, s);
                        HandDisc(ref c, ref a, x, y, a2x, a2y, Hand, body, line, lw, s);
                        if (a > 0f)
                        {
                            Put(pixels, width, px, py, c, a);
                        }

                        continue;
                    }

                    // Behind the body: the back leg, and from the side the back arm (lighter).
                    Rgba backLimb = front ? line : backLine;
                    Layer(ref c, ref a, backLimb, Coverage((Segment(x, y, bhx, bhy, bfx, bfy) - (limb / 2f)) * s));
                    Layer(ref c, ref a, backLimb, Coverage(Ellipse(x - bfx - (front ? -0.012f : 0.022f), y - bfy, 0.058f, 0.034f) * s));
                    if (!front)
                    {
                        Layer(ref c, ref a, backLine, Coverage((Segment(x, y, b1x, b1y, b2x, b2y) - (limb / 2f)) * s));
                        Layer(ref c, ref a, backLine, Coverage((Length(x - b2x, y - b2y) - Hand) * s));
                        Layer(ref c, ref a, line, Coverage((Segment(x, y, a1x, a1y, a2x, a2y) - (limb / 2f)) * s));
                        HandDisc(ref c, ref a, x, y, a2x, a2y, Hand, body, line, lw, s);
                    }

                    // From the front both legs stand behind the body too.
                    if (front)
                    {
                        Layer(ref c, ref a, line, Coverage((Segment(x, y, ahx, ahy, afx, afy) - (limb / 2f)) * s));
                        Layer(ref c, ref a, line, Coverage(Ellipse(x - afx - 0.012f, y - afy, 0.058f, 0.034f) * s));
                    }

                    // The body: outline, lit fill, its family's marks, gloss and face.
                    float d = Body(x, y);
                    float cover = Coverage(d * s);
                    if (cover > 0f)
                    {
                        float e = 0.012f;
                        float gx = Body(x + e, y) - Body(x - e, y);
                        float gy = Body(x, y + e) - Body(x, y - e);
                        float gl = Math.Max(1e-5f, Length(gx, gy));
                        gx /= gl;
                        gy /= gl;
                        float edge = 1f - Clamp01(-d / 0.17f);
                        float nz = (float)Math.Sqrt(Math.Max(0f, 1f - (edge * edge)));
                        float diff = (gx * edge * light.X) + (gy * edge * light.Y) + (nz * light.Z);
                        Local(x, y, out float lx, out float ly);
                        Rgba baseColor = family == Family.Twig && ly > 0.07f && Ellipse(lx, ly - 0.14f, 0.27f, 0.13f) < 0f ? cap : body;
                        var fill = new Color(baseColor);
                        fill.Scale(0.6f + (0.55f * Math.Max(0f, diff)));
                        switch (family)
                        {
                            case Family.Sprig:
                            {
                                float rib = Segment(lx, ly, 0f, 0.1f, 0f, 0.32f);
                                float side1 = Math.Min(Segment(lx, ly, 0f, 0.16f, -0.09f, 0.24f), Segment(lx, ly, 0f, 0.16f, 0.09f, 0.24f));
                                float side2 = Math.Min(Segment(lx, ly, 0f, 0.24f, -0.055f, 0.3f), Segment(lx, ly, 0f, 0.24f, 0.055f, 0.3f));
                                fill.Mix(body.Darken(0.35f), 0.75f * Coverage((Math.Min(rib, Math.Min(side1, side2)) - 0.009f) * s));
                                break;
                            }

                            case Family.Bloom:
                            {
                                float mid = Length(lx, ly - 0.0f) - 0.13f;
                                fill.Mix(body.Lighten(0.45f), 0.85f * Coverage(mid * s));
                                break;
                            }

                            case Family.Twig:
                            {
                                if (baseColor.Equals(cap))
                                {
                                    float u = ((lx + ly) * 16f) - (float)Math.Floor((lx + ly) * 16f);
                                    float v = ((lx - ly) * 16f) - (float)Math.Floor((lx - ly) * 16f);
                                    fill.Mix(cap.Darken(0.3f), Math.Min(u, v) < 0.16f ? 0.6f : 0f);
                                    fill.Mix(line, Clamp01(0.5f + ((-Math.Abs(Ellipse(lx, ly - 0.14f, 0.27f, 0.13f)) + (lw * 0.6f)) * s)) * (ly < 0.12f ? 1f : 0f));
                                }

                                break;
                            }
                        }

                        // The gloss spot, upper left (bigger on a drop).
                        float gr = family == Family.Drop ? 1.35f : 1f;
                        float glx = x - (cx - 0.085f);
                        float gly = y - (cy - 0.12f);
                        float gloss = Ellipse(Rot(glx, gly, 0.5f), RotY(glx, gly, 0.5f), 0.07f * gr, 0.042f * gr);
                        fill.Mix(Rgba.White, 0.6f * Clamp01(0.5f - (gloss * s / 2.5f)));

                        // The face: from the side the eyes look ahead; from the front, two eyes and a smile.
                        float faceY = cy + (family == Family.Twig ? 0.03f : 0f);
                        if (front)
                        {
                            Eye(ref fill, x, y, cx - 0.065f, faceY - 0.03f, s, line, 0f);
                            Eye(ref fill, x, y, cx + 0.065f, faceY - 0.03f, s, line, 0f);
                            float smile = Math.Abs(Length(x - cx, y - (faceY - 0.02f)) - 0.065f);
                            fill.Mix(line, y > faceY + 0.02f ? Coverage((smile - 0.011f) * s) : 0f);
                        }
                        else
                        {
                            Eye(ref fill, x, y, cx + 0.06f, faceY - 0.035f, s, line, 0.014f);
                            Eye(ref fill, x, y, cx + 0.165f, faceY - 0.05f, s, line, 0.014f);
                            fill.Mix(line, Coverage(Ellipse(x - (cx + 0.15f), y - (faceY + 0.055f), 0.026f, running ? 0.02f : 0.012f) * s));
                        }

                        // The outline.
                        fill.Mix(line, Clamp01(0.5f + ((d + lw) * s)));
                        Layer(ref c, ref a, fill.ToRgba(), cover);
                    }

                    // The acorn's stem.
                    if (family == Family.Twig)
                    {
                        Local(x, y, out float sx, out float sy);
                        Layer(ref c, ref a, line, Coverage((Segment(sx, sy, 0f, 0.26f, 0.035f, 0.33f) - 0.018f) * s));
                    }

                    // In front: from the side the front arm, then the front leg; from the front the arms (the hands too unless holding).
                    if (front)
                    {
                        Layer(ref c, ref a, line, Coverage((Segment(x, y, b1x, b1y, b2x, b2y) - (limb / 2f)) * s));
                        Layer(ref c, ref a, line, Coverage((Segment(x, y, a1x, a1y, a2x, a2y) - (limb / 2f)) * s));
                        if (!hold)
                        {
                            HandDisc(ref c, ref a, x, y, b2x, b2y, Hand, body, line, lw, s);
                            HandDisc(ref c, ref a, x, y, a2x, a2y, Hand, body, line, lw, s);
                        }
                    }
                    else
                    {
                        Layer(ref c, ref a, line, Coverage((Segment(x, y, ahx, ahy, afx, afy) - (limb / 2f)) * s));
                        float foot = Ellipse(x - afx - 0.022f, y - afy, 0.06f, 0.036f);
                        if (Coverage(foot * s) > 0f)
                        {
                            var fc = new Color(line);
                            fc.Mix(line.Lighten(0.45f), 0.8f * Coverage(Ellipse(x - afx - 0.005f, y - afy + 0.012f, 0.025f, 0.011f) * s));
                            Layer(ref c, ref a, fc.ToRgba(), Coverage(foot * s));
                        }
                    }

                    if (a > 0f)
                    {
                        Put(pixels, width, px, py, c, a);
                    }
                }
            }

            return pixels;
        }

        // A round hand in the body's colour with an outline.
        private static void HandDisc(ref Color c, ref float a, float x, float y, float hx, float hy, float r, Rgba body, Rgba line, float lw, float s)
        {
            float d = Length(x - hx, y - hy) - r;
            float cover = Coverage(d * s);
            if (cover <= 0f)
            {
                return;
            }

            var hc = new Color(body.Lighten(0.08f));
            hc.Mix(Rgba.White, 0.35f * Coverage((Length(x - hx + (r * 0.35f), y - hy + (r * 0.35f)) - (r * 0.35f)) * s));
            hc.Mix(line, Clamp01(0.5f + ((d + (lw * 0.8f)) * s)));
            Layer(ref c, ref a, hc.ToRgba(), cover);
        }

        // A creature's eye: white with a thin line, the pupil shifted by ahead (toward the right) and a glint.
        private static void Eye(ref Color c, float x, float y, float ex, float ey, float s, Rgba line, float ahead)
        {
            float white = Ellipse(x - ex, y - ey, 0.046f, 0.058f);
            if (white * s > 1.5f)
            {
                return;
            }

            c.Mix(line, Coverage((white - 0.012f) * s));
            c.Mix(Rgba.White, Coverage((white + 0.004f) * s));
            c.Mix(line, Coverage(Ellipse(x - ex - ahead, y - ey - 0.008f, 0.029f, 0.035f) * s));
            c.Mix(Rgba.White, Coverage((Length(x - ex - ahead + 0.01f, y - ey + 0.006f) - 0.011f) * s));
        }

        // Paints a colour over (c, a) with coverage t (straight alpha).
        private static void Layer(ref Color c, ref float a, Rgba color, float t)
        {
            if (t <= 0f)
            {
                return;
            }

            if (a <= 0f)
            {
                c = new Color(color);
            }
            else
            {
                c.Mix(color, t);
            }

            a += t * (1f - a);
        }

        private static float Rot(float x, float y, float r) => (x * (float)Math.Cos(r)) + (y * (float)Math.Sin(r));

        private static float RotY(float x, float y, float r) => (-x * (float)Math.Sin(r)) + (y * (float)Math.Cos(r));

        // The distance to a capsule from a circle of r1 at the origin to one of r2 at (0, h), y up.
        private static float UnevenCapsule(float x, float y, float r1, float r2, float h)
        {
            x = Math.Abs(x);
            float b = (r1 - r2) / h;
            float a = (float)Math.Sqrt(1f - (b * b));
            float k = (-b * x) + (a * y);
            if (k < 0f)
            {
                return Length(x, y) - r1;
            }

            if (k > a * h)
            {
                return Length(x, y - h) - r2;
            }

            return (a * x) + (b * y) - r1;
        }
    }
}
