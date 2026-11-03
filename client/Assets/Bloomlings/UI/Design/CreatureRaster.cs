using System;

namespace Bloomlings.Client.UI.Design
{
    public static partial class UiRaster
    {
        // ---- The walker mock's running creature (WalkerLook.Creature; review material of 2026-11-03, not the game's look) ----

        /// <summary>
        /// A round walker drawn as a little running creature (the walker mock's variant 4, <see cref="WalkerLook.Creature"/>), seen
        /// from the side and facing right (a host mirrors it for the left): a leaf-shaped body in <paramref name="body"/>, lit
        /// from the upper left with a gloss spot and a dark <paramref name="line"/> outline, two eyes and two legs with feet. <paramref name="frame"/> is its run cycle's frame (0 to <paramref name="frames"/> − 1: the legs
        /// stride, the body bobs and leans) or −1 standing. <paramref name="vein"/> draws the leaf's midrib and veins (the
        /// Sprig family). A picture of <paramref name="width"/> × <paramref name="height"/> (the creature in its middle square), straight
        /// alpha, deterministic.
        /// </summary>
        public static byte[] Creature(int width, int height, Rgba body, Rgba line, int frame, int frames, bool vein)
        {
            Check(width, height);
            var pixels = new byte[width * height * 4];
            float s = Math.Min(width, height);
            float ox = (width - s) / 2f;
            float oy = (height - s) / 2f;
            bool running = frame >= 0 && frames > 0;
            double phi = running ? frame * Math.PI * 2.0 / frames : 0.0;
            float sin = (float)Math.Sin(phi);
            float bob = running ? 0.035f * Math.Abs(sin) : 0f;
            float cx = 0.47f;
            float cy = 0.6f - bob;
            float tilt = (running ? 18f : 10f) * (float)Math.PI / 180f;
            float ts = (float)Math.Sin(tilt);
            float tc = (float)Math.Cos(tilt);
            const float R1 = 0.255f;
            const float R2 = 0.05f;
            const float H = 0.3f;
            float lw = Math.Max(1.4f / s, 0.04f);
            float legW = Math.Max(1.2f / s, 0.042f);

            // The legs: hips under the body, each foot sweeping back on the ground and lifted on its way forward.
            (float hx, float hy, float fx, float fy) Leg(float hipX, double phase)
            {
                float c = running ? (float)Math.Cos(phase) : 0f;
                float sn = running ? (float)Math.Sin(phase) : 0f;
                float x = hipX + (0.14f * c);
                float lift = 0.085f * Math.Max(0f, -sn);
                return (hipX, 0.8f - bob, x, 0.925f - lift);
            }

            (float bhx, float bhy, float bfx, float bfy) = Leg(cx - 0.07f, phi + Math.PI);
            (float ahx, float ahy, float afx, float afy) = Leg(cx + 0.03f, phi);
            Rgba backLine = line.Lighten(0.22f);
            var light = (X: -0.45f, Y: -0.65f, Z: 0.61f);

            // The body's distance (normalized units, negative inside): an uneven capsule leaning forward, its tip up.
            float Body(float x, float y)
            {
                float qx = x - cx;
                float qy = y - cy;
                float lx = (qx * tc) + (qy * ts);
                float ly = (qx * ts) - (qy * tc);
                return UnevenCapsule(lx, ly, R1, R2, H);
            }

            for (int py = 0; py < height; py++)
            {
                float y = (py + 0.5f - oy) / s;
                for (int px = 0; px < width; px++)
                {
                    float x = (px + 0.5f - ox) / s;
                    var c = new Color(line);
                    float a = 0f;

                    // The back leg and foot, a little lighter (behind).
                    Layer(ref c, ref a, backLine, Coverage((Segment(x, y, bhx, bhy, bfx, bfy) - (legW / 2f)) * s));
                    Layer(ref c, ref a, backLine, Coverage(Ellipse(x - bfx - 0.022f, y - bfy, 0.058f, 0.034f) * s));

                    // The body: outline, lit fill, veins, gloss and face.
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
                        var fill = new Color(body);
                        fill.Scale(0.6f + (0.55f * Math.Max(0f, diff)));
                        if (vein)
                        {
                            float qx = x - cx;
                            float qy = y - cy;
                            float lx = (qx * tc) + (qy * ts);
                            float ly = (qx * ts) - (qy * tc);
                            float rib = Segment(lx, ly, 0f, 0.1f, 0f, H + (R2 * 0.4f));
                            float side1 = Math.Min(Segment(lx, ly, 0f, 0.16f, -0.09f, 0.24f), Segment(lx, ly, 0f, 0.16f, 0.09f, 0.24f));
                            float side2 = Math.Min(Segment(lx, ly, 0f, 0.24f, -0.055f, 0.3f), Segment(lx, ly, 0f, 0.24f, 0.055f, 0.3f));
                            float v = Math.Min(rib, Math.Min(side1, side2));
                            fill.Mix(body.Darken(0.35f), 0.75f * Coverage((v - 0.009f) * s));
                        }

                        // The gloss spot, upper left.
                        float gloss = Ellipse(Rot(x - (cx - 0.085f), y - (cy - 0.12f), 0.5f), RotY(x - (cx - 0.085f), y - (cy - 0.12f), 0.5f), 0.07f, 0.042f);
                        fill.Mix(Rgba.White, 0.6f * Clamp01(0.5f - (gloss * s / 2.5f)));

                        // The eyes, looking ahead (right), and a small open mouth.
                        Eye(ref fill, x, y, cx + 0.06f, cy - 0.035f, s, line);
                        Eye(ref fill, x, y, cx + 0.165f, cy - 0.05f, s, line);
                        fill.Mix(line, Coverage(Ellipse(x - (cx + 0.15f), y - (cy + 0.055f), 0.026f, running ? 0.02f : 0.012f) * s));

                        // The outline.
                        fill.Mix(line, Clamp01(0.5f + ((d + lw) * s)));
                        Layer(ref c, ref a, fill.ToRgba(), cover);
                    }

                    // The front leg and foot, with a small light on the shoe.
                    Layer(ref c, ref a, line, Coverage((Segment(x, y, ahx, ahy, afx, afy) - (legW / 2f)) * s));
                    float foot = Ellipse(x - afx - 0.022f, y - afy, 0.06f, 0.036f);
                    if (Coverage(foot * s) > 0f)
                    {
                        var fc = new Color(line);
                        fc.Mix(line.Lighten(0.45f), 0.8f * Coverage(Ellipse(x - afx - 0.005f, y - afy + 0.012f, 0.025f, 0.011f) * s));
                        Layer(ref c, ref a, fc.ToRgba(), Coverage(foot * s));
                    }

                    if (a > 0f)
                    {
                        Put(pixels, width, px, py, c, a);
                    }
                }
            }

            return pixels;
        }

        // A creature's eye: white with a thin line, the pupil ahead and a glint.
        private static void Eye(ref Color c, float x, float y, float ex, float ey, float s, Rgba line)
        {
            float white = Ellipse(x - ex, y - ey, 0.046f, 0.06f);
            if (white * s > 1.5f)
            {
                return;
            }

            c.Mix(line, Coverage((white - 0.012f) * s));
            c.Mix(Rgba.White, Coverage((white + 0.004f) * s));
            c.Mix(line, Coverage(Ellipse(x - ex - 0.014f, y - ey - 0.006f, 0.03f, 0.036f) * s));
            c.Mix(Rgba.White, Coverage((Length(x - ex - 0.004f, y - ey + 0.008f) - 0.011f) * s));
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
