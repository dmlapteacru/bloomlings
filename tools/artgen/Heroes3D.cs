using System;
using System.Threading.Tasks;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;

namespace Bloomlings.ArtGen
{
    /// <summary>
    /// The 3D family heroes (research R3): a CPU signed-distance raymarcher ported from the approved WebGL concept
    /// (<c>concept-home-3d.jpg</c>). Sprig is a green bean wrapped in a big leaf, Bloom a pink flower with a face in its
    /// center, Drop a water drop with raised arms, Twig a cut log with a branch. Warm key light with soft shadows,
    /// ambient occlusion, a golden back light around the edges, translucent leaves, petals and water. Each row is
    /// rendered independently, so the output does not depend on thread scheduling.
    /// </summary>
    public static class Heroes3D
    {
        // Materials.
        private const int Bound = 0;
        private const int BeanBody = 1;
        private const int BigLeaf = 2;
        private const int Petal = 4;
        private const int FlowerFace = 5;
        private const int Green = 6;
        private const int Water = 7;
        private const int Bark = 8;
        private const int WoodTop = 9;
        private const int BeanFeet = 12;
        private const int BackPetal = 13;

        private static readonly V3 FlowerCenter = new V3(0, 0.98, 0.06);
        private static readonly V3 Key = new V3(-0.45, 0.7, 0.7).Normalized;
        private static readonly V3 Back = new V3(0.25, 0.45, -1).Normalized;
        private static readonly V3 Ink = Sdf.Linear(0.16, 0.09, 0.07);

        // Fixed rotations, computed once.
        private static readonly Rotation[] Turns = { new Rotation(0.35), new Rotation(0.1), new Rotation(-0.1), new Rotation(-0.35) };
        private static readonly Rotation LeafSpin = new Rotation(0.42);
        private static readonly Rotation LeafTilt = new Rotation(-0.12);
        private static readonly Rotation SproutLeaf = new Rotation(-0.9);
        private static readonly Rotation[] BloomLeaves = { new Rotation(-1.0), new Rotation(1.0) };
        private static readonly PetalFrame[] Petals = PetalFrames();

        /// <summary>A petal's place on the flower: its center, spin, cup, size and material.</summary>
        private readonly struct PetalFrame
        {
            public readonly V3 Center;
            public readonly Rotation Spin;
            public readonly Rotation Cup;
            public readonly V3 Size;
            public readonly int Material;

            public PetalFrame(V3 center, Rotation spin, Rotation cup, V3 size, int material)
            {
                Center = center;
                Spin = spin;
                Cup = cup;
                Size = size;
                Material = material;
            }
        }

        /// <summary>Five big petals in front and five smaller, deeper ones behind, cupped forward.</summary>
        private static PetalFrame[] PetalFrames()
        {
            var frames = new PetalFrame[10];
            for (int ring = 0; ring < 2; ring++)
            {
                for (int i = 0; i < 5; i++)
                {
                    double a = ((i + (ring == 1 ? 0.5 : 0)) * 2 * Math.PI / 5) + (Math.PI / 2);
                    double rad = ring == 0 ? 0.37 : 0.4;
                    frames[(ring * 5) + i] = new PetalFrame(
                        FlowerCenter + new V3(Math.Cos(a) * rad, Math.Sin(a) * rad, ring == 0 ? -0.03 : -0.1),
                        new Rotation(-a + (Math.PI / 2)),
                        new Rotation(ring == 0 ? 0.35 : 0.2),
                        ring == 0 ? new V3(0.24, 0.25, 0.035) : new V3(0.19, 0.24, 0.03),
                        ring == 0 ? Petal : BackPetal);
                }
            }

            return frames;
        }

        /// <summary>
        /// One hero alone, slightly turned toward the viewer, with its contact shadow, as straight-alpha RGBA rows from the
        /// top. With a <paramref name="rowStride"/> above 1 only every that many rows are rendered (the art check), the
        /// others stay transparent; each rendered row is exactly what a full render writes.
        /// </summary>
        public static byte[] Solo(Family family, bool blank, int rowStride = 1) =>
            Render(new Scene(Index(family), blank), CharacterArt.HeroWidth, CharacterArt.HeroHeight, rowStride);

        /// <summary>The four heroes standing side by side on the ground, without a base of their own (rows as in <see cref="Solo"/>).</summary>
        public static byte[] Group(int rowStride = 1) => Render(new Scene(-1, blank: false), CharacterArt.GroupWidth, CharacterArt.GroupHeight, rowStride);

        /// <summary>Where a hero's face center lands in its solo picture, as a share of the picture (y down).</summary>
        public static (double X, double Y) FaceCenter(Family family)
        {
            int i = Index(family);
            var scene = new Scene(i, blank: false);
            (V3 center, _, _, _) = FaceOf(i);

            // The face lies on the front of the body: march from the camera toward the face center's direction.
            (double lx, double lz) = Sdf.Rot(-Turn(i), center.X, center.Z);
            V3 world = new V3(lx, center.Y, lz);
            Camera camera = scene.Camera;
            V3 d = world - camera.Eye;
            double z = V3.Dot(d, camera.W);
            double u = V3.Dot(d, camera.U) / z * camera.Zoom;
            double v = V3.Dot(d, camera.V) / z * camera.Zoom;
            double h = CharacterArt.HeroHeight;
            double w = CharacterArt.HeroWidth;
            double px = (u * h) + (0.5 * w);
            double py = (v * h) + (0.5 * h);
            return (px / w, 1 - (py / h));
        }

        private static int Index(Family family) => family switch
        {
            Family.Sprig => 0,
            Family.Bloom => 1,
            Family.Drop => 2,
            _ => 3,
        };

        private static byte[] Render(Scene scene, int width, int height, int rowStride)
        {
            var rgba = new byte[width * height * 4];
            Parallel.For(0, (height + rowStride - 1) / rowStride, n =>
            {
                int row = n * rowStride;
                double glY = height - 1 - row + 0.5;
                for (int x = 0; x < width; x++)
                {
                    double r = 0;
                    double g = 0;
                    double b = 0;
                    double a = 0;
                    for (int sx = 0; sx < 2; sx++)
                    {
                        for (int sy = 0; sy < 2; sy++)
                        {
                            double ox = (sx / 2.0) - 0.25;
                            double oy = (sy / 2.0) - 0.25;
                            double ux = (x + 0.5 + ox - (0.5 * width)) / height;
                            double uy = (glY + oy - (0.5 * height)) / height;
                            Camera c = scene.Camera;
                            V3 rd = ((c.U * ux) + (c.V * uy) + (c.W * c.Zoom)).Normalized;
                            (V3 color, double alpha) = scene.Shade(c.Eye, rd);
                            r += color.X * alpha;
                            g += color.Y * alpha;
                            b += color.Z * alpha;
                            a += alpha;
                        }
                    }

                    int o = ((row * width) + x) * 4;
                    a /= 4;
                    if (a > 0)
                    {
                        rgba[o] = Byte(r / 4 / a);
                        rgba[o + 1] = Byte(g / 4 / a);
                        rgba[o + 2] = Byte(b / 4 / a);
                        rgba[o + 3] = Byte(a);
                    }
                }
            });

            return rgba;
        }

        private static byte Byte(double v) => (byte)Math.Round(Sdf.Clamp(v, 0, 1) * 255);

        private static V3 HeroPos(int i) => new V3(-1.38 + (i * 0.92), 0, i == 1 || i == 2 ? -0.05 : 0.2);

        private static double Turn(int i) => i switch
        {
            0 => 0.35,
            1 => 0.1,
            2 => -0.1,
            _ => -0.35,
        };

        /// <summary>A hero's face: its center (local), size, eye style (0 dots, 1 happy arcs) and open mouth.</summary>
        private static (V3 Center, double Size, int Eyes, bool Open) FaceOf(int i) => i switch
        {
            0 => (new V3(0, 0.5, 0.43), 1, 0, false),
            1 => (new V3(0, 1.0, 0.21), 1.1, 1, true),
            2 => (new V3(0, 0.57, 0.4), 1, 1, true),
            _ => (new V3(0, 0.52, 0.42), 1, 0, false),
        };

        private static (double D, int M) U((double D, int M) a, (double D, int M) b) => a.D < b.D ? a : b;

        private static (double D, int M) Sprig(V3 p)
        {
            var r = (Sdf.Ellipsoid(p - new V3(0, 0.5, 0), new V3(0.47, 0.5, 0.43)), BeanBody);
            for (int s = -1; s <= 1; s += 2)
            {
                r = U(r, (Sdf.Ellipsoid(p - new V3(s * 0.17, 0.05, 0.12), new V3(0.12, 0.07, 0.14)), BeanFeet));
            }

            // The big leaf wraps from behind and over the left side.
            V3 q = p - new V3(0.08, 0.02, -0.28);
            q = q.WithXY(LeafSpin.Apply(q.X, q.Y));
            q = q.WithYZ(LeafTilt.Apply(q.Y, q.Z));
            r = U(r, (Sdf.Leaf(q, 1.45, 0.62, 0.55, 0.022), BigLeaf));

            // A little sprout on top.
            r = U(r, (Sdf.Capsule(p, new V3(0.02, 0.96, 0), new V3(0.06, 1.1, 0.02), 0.025), Green));
            V3 l = p - new V3(0.14, 1.13, 0.03);
            l = l.WithXY(SproutLeaf.Apply(l.X, l.Y));
            return U(r, (Sdf.Ellipsoid(l, new V3(0.1, 0.035, 0.06)), Green));
        }

        private static (double D, int M) Bloom(V3 p)
        {
            V3 c = FlowerCenter;
            var r = (Sdf.Ellipsoid(p - c, new V3(0.27, 0.25, 0.15)), FlowerFace);

            foreach (PetalFrame petal in Petals)
            {
                // A petal lies within 0.29 of its center: farther than the nearest part so far, it cannot be nearer.
                V3 q = p - petal.Center;
                if (q.Length - 0.29 > r.Item1)
                {
                    continue;
                }

                q = q.WithXY(petal.Spin.Apply(q.X, q.Y));
                q = q.WithYZ(petal.Cup.Apply(q.Y, q.Z));
                double d = Sdf.Ellipsoid(q - new V3(0, 0.02, 0), petal.Size);
                d += 0.01 * Math.Sin(q.X * 34) * Sdf.Smoothstep(0.08, 0.22, q.Y);
                r = U(r, (d, petal.Material));
            }

            r = U(r, (Sdf.Capsule(p, new V3(0, 0.02, -0.05), new V3(0, 0.7, -0.05), 0.055), Green));
            for (int s = -1; s <= 1; s += 2)
            {
                V3 q = p - new V3(s * 0.24, 0.2, 0);
                q = q.WithXY(BloomLeaves[(s + 1) / 2].Apply(q.X, q.Y));
                r = U(r, (Sdf.Leaf(new V3(q.Y, q.X, q.Z), 0.4, 0.16, 0.6, 0.02), Green));
            }

            return r;
        }

        private static (double D, int M) Drop(V3 p)
        {
            double d = Sdf.Ellipsoid(p - new V3(0, 0.5, 0), new V3(0.43, 0.42, 0.4));
            d = Sdf.Smin(d, Sdf.RoundCone(p - new V3(0, 0.55, 0), 0.36, 0.02, 0.78), 0.12);

            // Arms raised in joy, tiny legs.
            for (int s = -1; s <= 1; s += 2)
            {
                d = Sdf.Smin(d, Sdf.Capsule(p, new V3(s * 0.32, 0.55, 0.08), new V3(s * 0.6, 0.86, 0.14), 0.055), 0.05);
                d = Sdf.Smin(d, (p - new V3(s * 0.62, 0.89, 0.15)).Length - 0.075, 0.03);
                d = Sdf.Smin(d, Sdf.Capsule(p, new V3(s * 0.14, 0.15, 0.06), new V3(s * 0.17, 0.04, 0.12), 0.07), 0.05);
            }

            return (d, Water);
        }

        private static (double D, int M) Twig(V3 p)
        {
            V3 q = p - new V3(0, 0.46, 0);
            double taper = 1 + (0.08 * -q.Y);
            double grooves = Sdf.Smoothstep(0.38, 0.1, Math.Abs(q.Y));
            double d = Sdf.RoundCylinder(new V3(q.X / taper, q.Y, q.Z / taper), 0.42, 0.42, 0.12);
            if (d < 0.05)
            {
                // Bark grooves near the surface; farther out the deepest groove bounds the distance.
                double a = Math.Atan2(q.Z, q.X);
                d -= 0.016 * ((Math.Sin((a * 22) + (3 * Sdf.Fbm(p * 3))) * 0.5) + 0.5) * grooves;
            }
            else
            {
                d -= 0.016 * grooves;
            }

            var r = (d, Bark);

            // The top cut.
            if (q.Y > 0.38 && Math.Sqrt((q.X * q.X) + (q.Z * q.Z)) < 0.4)
            {
                r = (d, WoodTop);
            }

            // A branch with a round knob, little stubby arms and feet.
            r = U(r, (Sdf.Smin(Sdf.Capsule(p, new V3(0.16, 0.8, 0), new V3(0.3, 1.12, 0.02), 0.045), (p - new V3(0.33, 1.2, 0.02)).Length - 0.1, 0.05), Bark));
            for (int s = -1; s <= 1; s += 2)
            {
                r = U(r, (Sdf.Capsule(p, new V3(s * 0.4, 0.45, 0.06), new V3(s * 0.55, 0.38, 0.16), 0.07), Bark));
                r = U(r, (Sdf.Ellipsoid(p - new V3(s * 0.18, 0.04, 0.18), new V3(0.13, 0.07, 0.13)), Bark));
            }

            return r;
        }

        private static (double D, int M) Hero(int i, V3 local) => i switch
        {
            0 => Sprig(local),
            1 => Bloom(local),
            2 => Drop(local),
            _ => Twig(local),
        };

        /// <summary>Eyes and mouth in a hero's face plane: ink, and the tongue inside an open mouth.</summary>
        private static (double Ink, double Tongue) FaceDecal(double qx, double qy, double cx, double cy, double s, int eyes, bool open)
        {
            double ink = 0;
            double tongue = 0;
            for (int k = -1; k <= 1; k += 2)
            {
                double ex = qx - (cx + (k * 0.11 * s));
                double ey = qy - (cy + (0.03 * s));
                if (eyes == 0)
                {
                    double nx = ex / 0.032 / s;
                    double ny = ey / 0.042 / s;
                    if (Math.Sqrt((nx * nx) + (ny * ny)) < 1)
                    {
                        ink = 1;
                    }
                }
                else
                {
                    double dy = ey + (0.035 * s);
                    double d = Math.Abs(Math.Sqrt((ex * ex) + (dy * dy)) - (0.045 * s)) - (0.012 * s);
                    if (d < 0 && ey > -0.02 * s)
                    {
                        ink = 1;
                    }
                }
            }

            double mx = qx - cx;
            double my = qy - (cy - (0.07 * s));
            if (open)
            {
                double d = Math.Max(Math.Sqrt(((mx / 1.25) * (mx / 1.25)) + (my * my)) - (0.065 * s), my - (0.005 * s));
                if (d < 0)
                {
                    ink = 1;
                    double ty = my + (0.055 * s);
                    if (Math.Sqrt((mx * mx) + (ty * ty)) < 0.04 * s)
                    {
                        tongue = 1;
                    }
                }
            }
            else
            {
                double sy = my - (0.03 * s);
                double d = Math.Abs(Math.Sqrt((mx * mx) + (sy * sy)) - (0.05 * s)) - (0.011 * s);
                if (d < 0 && my < 0.01 * s)
                {
                    ink = 1;
                }
            }

            return (ink, tongue);
        }

        private static double Blush(double qx, double qy, double cx, double cy, double s)
        {
            double b = 0;
            for (int k = -1; k <= 1; k += 2)
            {
                double dx = qx - (cx + (k * 0.19 * s));
                double dy = (qy - (cy - (0.04 * s))) / 0.6;
                b = Math.Max(b, 1 - Sdf.Smoothstep(0.02 * s, 0.07 * s, Math.Sqrt((dx * dx) + (dy * dy))));
            }

            return b;
        }

        private static V3 ToSrgb(V3 c)
        {
            static double Ch(double v) => Math.Pow(Sdf.Clamp(v / (1 + (0.15 * v)), 0, 1), 1 / 2.2);
            return new V3(Ch(c.X), Ch(c.Y), Ch(c.Z));
        }

        private readonly struct Camera
        {
            public readonly V3 Eye;
            public readonly V3 U;
            public readonly V3 V;
            public readonly V3 W;
            public readonly double Zoom;

            public Camera(V3 target, V3 eye, double zoom)
            {
                Eye = eye;
                W = (target - eye).Normalized;
                U = V3.Cross(W, new V3(0, 1, 0)).Normalized;
                V = V3.Cross(U, W);
                Zoom = zoom;
            }
        }

        /// <summary>One scene: a single hero (<c>solo</c> ≥ 0) or the four together (−1).</summary>
        private sealed class Scene
        {
            private readonly int solo;
            private readonly bool blank;

            public Scene(int solo, bool blank)
            {
                this.solo = solo;
                this.blank = blank;
                Camera = solo >= 0
                    ? new Camera(new V3(0, 0.76, 0), new V3(0, 0.76 + 0.4, 6), 3.0)
                    : new Camera(new V3(0, 0.42, 0), new V3(0, 0.42 + 1.7, 9.5), 2.6);
            }

            public Camera Camera { get; }

            /// <summary>
            /// The ground under the heroes' feet, where the soft contact shadow falls. The group has no pedestal of its own
            /// (spec 005 pictures.md): the hosts stand it on their stone pedestal.
            /// </summary>
            private const double GroundY = 0;

            private V3 Local(int i, V3 p)
            {
                V3 q = solo >= 0 ? p : p - HeroPos(i);
                return q.WithXZ(Turns[i].Apply(q.X, q.Z));
            }

            private int Which(V3 p)
            {
                if (solo >= 0)
                {
                    return solo;
                }

                int best = 0;
                double bd = 1e9;
                for (int i = 0; i < 4; i++)
                {
                    V3 h = HeroPos(i);
                    double d = Math.Sqrt(((p.X - h.X) * (p.X - h.X)) + ((p.Z - h.Z) * (p.Z - h.Z)));
                    if (d < bd)
                    {
                        bd = d;
                        best = i;
                    }
                }

                return best;
            }

            public (double D, int M) Map(V3 p)
            {
                (double D, int M) r = (1e9, Bound);
                for (int i = 0; i < 4; i++)
                {
                    if (solo >= 0 && solo != i)
                    {
                        continue;
                    }

                    V3 hc = (solo >= 0 ? new V3(0, 0, 0) : HeroPos(i)) + new V3(0, 0.7, 0);
                    double bound = (p - hc).Length - 0.95;
                    if (bound > 0.1)
                    {
                        r = U(r, (bound, Bound));
                        continue;
                    }

                    r = U(r, Hero(i, Local(i, p)));
                }

                return r;
            }

            private V3 Normal(V3 p)
            {
                const double e = 0.0015;
                var a = new V3(e, -e, -e);
                var b = new V3(-e, -e, e);
                var c = new V3(-e, e, -e);
                var d = new V3(e, e, e);
                return ((a * Map(p + a).D) + (b * Map(p + b).D) + (c * Map(p + c).D) + (d * Map(p + d).D)).Normalized;
            }

            private double SoftShadow(V3 ro, V3 rd)
            {
                double res = 1;
                double t = 0.02;
                for (int i = 0; i < 48; i++)
                {
                    double h = Map(ro + (rd * t)).D;
                    res = Math.Min(res, 8 * h / t);
                    t += Sdf.Clamp(h, 0.01, 0.2);
                    if (res < 0.01 || t > 5)
                    {
                        break;
                    }
                }

                return Sdf.Clamp(res, 0, 1);
            }

            private double Occlusion(V3 p, V3 n)
            {
                double occ = 0;
                double sca = 1;
                for (int i = 0; i < 5; i++)
                {
                    double h = 0.01 + (0.09 * i);
                    occ += (h - Map(p + (n * h)).D) * sca;
                    sca *= 0.8;
                }

                return Sdf.Clamp(1 - (2 * occ), 0, 1);
            }

            /// <summary>A soft contact shadow on the ground where a ray misses everything (sRGB color, alpha).</summary>
            private (V3 Color, double Alpha) Ground(V3 ro, V3 rd)
            {
                if (rd.Y >= 0)
                {
                    return (default, 0);
                }

                double t = (GroundY - ro.Y) / rd.Y;
                V3 p = ro + (rd * t);
                // Each hero casts the same soft ellipse under its feet; the group keeps the darkest of the four.
                double alpha = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (solo >= 0 && solo != i)
                    {
                        continue;
                    }

                    V3 h = solo >= 0 ? new V3(0, 0, 0) : HeroPos(i);
                    double ex = (p.X - h.X) / 0.62;
                    double ez = (p.Z - h.Z - 0.05) / 0.4;
                    alpha = Math.Max(alpha, 0.34 * (1 - Sdf.Smoothstep(0.25, 1, Math.Sqrt((ex * ex) + (ez * ez)))));
                }

                return (new V3(0.24, 0.16, 0.1), alpha);
            }

            public (V3 Color, double Alpha) Shade(V3 ro, V3 rd)
            {
                double t = 0;
                int hit = -1;
                for (int i = 0; i < 180; i++)
                {
                    (double d, int mat) = Map(ro + (rd * t));
                    if (d < 0.0007 && mat != Bound)
                    {
                        hit = mat;
                        break;
                    }

                    t += Math.Max(d * 0.65, 0.0015);
                    if (t > 20)
                    {
                        break;
                    }
                }

                if (hit < 0)
                {
                    return Ground(ro, rd);
                }

                V3 p = ro + (rd * t);
                V3 n = Normal(p);
                int hi = Which(p);
                V3 q = Local(hi, p);
                V3 b = new V3(0.5, 0.5, 0.5);
                double gloss = 0.2;
                double spow = 24;
                double alpha = 1;
                switch (hit)
                {
                    case BeanBody:
                        b = Sdf.Linear(0.55, 0.8, 0.28) * Sdf.Mix(0.9, 1.15, Sdf.Smoothstep(0.1, 0.9, q.Y));
                        if (q.Z > 0)
                        {
                            b = V3.Mix(b, Sdf.Linear(1, 0.55, 0.55), 0.5 * Blush(q.X, q.Y, 0, 0.5, 1));
                            b = Face(b, q, 0, 0.5, 1, 0, false, Ink, default);
                        }

                        gloss = 0.25;
                        break;
                    case BeanFeet:
                        b = Sdf.Linear(0.45, 0.68, 0.22);
                        break;
                    case BigLeaf:
                    {
                        V3 l = q - new V3(0.08, 0.02, -0.28);
                        l = l.WithXY(LeafSpin.Apply(l.X, l.Y));
                        double vein = 1 - Sdf.Smoothstep(0.006, 0.016, Math.Abs(l.X));
                        double side = 1 - Sdf.Smoothstep(0.005, 0.014, Math.Abs(Sdf.Fract((l.Y - (Math.Abs(l.X) * 0.9)) * 5) - 0.5) * 0.12);
                        b = V3.Mix(Sdf.Linear(0.24, 0.55, 0.18), Sdf.Linear(0.42, 0.72, 0.25), Sdf.Smoothstep(-0.4, 0.4, l.X));
                        b = V3.Mix(b, Sdf.Linear(0.62, 0.85, 0.4), 0.6 * Math.Max(vein, side * 0.5));
                        gloss = 0.35;
                        break;
                    }

                    case Petal:
                    {
                        V3 c = FlowerCenter;
                        double rr = Math.Sqrt(((q.X - c.X) * (q.X - c.X)) + ((q.Y - c.Y) * (q.Y - c.Y)));
                        b = V3.Mix(Sdf.Linear(1, 0.84, 0.88), Sdf.Linear(0.96, 0.5, 0.64), Sdf.Smoothstep(0.18, 0.62, rr));
                        double vein = Math.Abs(Math.Sin(Math.Atan2(q.Y - c.Y, q.X - c.X) * 30));
                        b = b * (0.94 + (0.06 * vein));
                        gloss = 0.15;
                        break;
                    }

                    case BackPetal:
                        b = Sdf.Linear(0.9, 0.42, 0.56);
                        gloss = 0.1;
                        break;
                    case FlowerFace:
                    {
                        V3 c = FlowerCenter;
                        b = Sdf.Linear(1, 0.9, 0.9) * (0.9 + (0.14 * Sdf.Fbm(p * 45)));
                        b = V3.Mix(b, Sdf.Linear(1, 0.55, 0.6), 0.55 * Blush(q.X, q.Y, c.X, c.Y, 1.1));
                        b = Face(b, q, c.X, c.Y + 0.02, 1.1, 1, true, Ink, Sdf.Linear(0.95, 0.42, 0.45));
                        break;
                    }

                    case Green:
                        b = Sdf.Linear(0.35, 0.62, 0.22);
                        gloss = 0.25;
                        break;
                    case Water:
                        b = Sdf.Linear(0.25, 0.58, 0.98);
                        if (q.Z > 0.1)
                        {
                            b = V3.Mix(b, Sdf.Linear(0.6, 0.6, 0.95), 0.5 * Blush(q.X, q.Y, 0, 0.55, 1));
                            b = Face(b, q, 0, 0.57, 1, 1, true, Sdf.Linear(0.05, 0.1, 0.3), Sdf.Linear(0.95, 0.45, 0.55));
                        }

                        gloss = 1.4;
                        spow = 90;
                        break;
                    case Bark:
                    {
                        double a = Math.Atan2(q.Z, q.X);
                        double grain = Sdf.Fbm(new V3(a * 5, q.Y * 18, 1));
                        b = V3.Mix(Sdf.Linear(0.42, 0.26, 0.15), Sdf.Linear(0.6, 0.4, 0.24), grain);
                        if (q.Z > 0 && q.Y < 0.82)
                        {
                            b = V3.Mix(b, Sdf.Linear(0.85, 0.45, 0.4), 0.45 * Blush(q.X, q.Y, 0, 0.5, 1));
                            b = Face(b, q, 0, 0.52, 1, 0, false, Ink, default);
                        }

                        gloss = 0.1;
                        break;
                    }

                    case WoodTop:
                    {
                        double rr = Math.Sqrt((q.X * q.X) + (q.Z * q.Z));
                        b = V3.Mix(Sdf.Linear(0.86, 0.68, 0.45), Sdf.Linear(0.72, 0.52, 0.32), 0.5 + (0.5 * Math.Sin(rr * 70)));
                        gloss = 0.08;
                        break;
                    }

                }

                double sh = SoftShadow(p + (n * 0.004), Key);
                double ao = Occlusion(p, n);
                double dif = Sdf.Clamp((V3.Dot(n, Key) + 0.25) / 1.25, 0, 1);
                var sky = new V3(0.95, 0.85, 0.7);
                V3 ambient = V3.Mix(new V3(0.55, 0.5, 0.42), sky, 0.5 + (0.5 * n.Y)) * (0.42 * ao);
                V3 direct = new V3(1, 0.92, 0.78) * (1.05 * dif * Sdf.Mix(0.4, 1, sh));
                V3 col = b * (ambient + direct);

                // A warm golden back light wraps the edges (the sun behind them).
                double rim = Math.Pow(Sdf.Clamp(1 - V3.Dot(n, -rd), 0, 1), 2.5) * Sdf.Clamp((V3.Dot(n, Back) * 0.5) + 0.6, 0, 1);
                col = col + (new V3(1, 0.78, 0.45) * (rim * 0.75));
                V3 h = (Key - rd).Normalized;
                col = col + (new V3(1, 0.95, 0.85) * (gloss * Math.Pow(Sdf.Clamp(V3.Dot(n, h), 0, 1), spow) * sh));

                // Translucency: petals, leaves and water glow where light passes through.
                if (hit == Petal || hit == BackPetal || hit == BigLeaf || hit == Green || hit == Water)
                {
                    double through = Sdf.Clamp((V3.Dot(-n, Back) * 0.5) + 0.5, 0, 1);
                    col = col + (b * new V3(1, 0.9, 0.7) * (through * (hit == Water ? 0.5 : 0.45)));
                }

                if (hit == Water)
                {
                    // Water: a darker core, a bright fresnel edge, a second highlight, slightly see-through.
                    double fr = Math.Pow(1 - Sdf.Clamp(V3.Dot(n, -rd), 0, 1), 3);
                    col = V3.Mix(col * 0.85, new V3(0.85, 0.95, 1), fr * 0.55);
                    double spec = Math.Pow(Sdf.Clamp(V3.Dot(n, (new V3(0.5, 0.6, 0.8).Normalized - rd).Normalized), 0, 1), 120);
                    col = col + new V3(0.8, 0.8, 0.8) * spec;
                    alpha = 0.9 + (0.1 * fr);
                }

                return (ToSrgb(col), alpha);
            }

            private V3 Face(V3 b, V3 q, double cx, double cy, double s, int eyes, bool open, V3 ink, V3 tongue)
            {
                if (blank)
                {
                    return b;
                }

                (double i, double tg) = FaceDecal(q.X, q.Y, cx, cy, s, eyes, open);
                b = V3.Mix(b, ink, i);
                return open ? V3.Mix(b, tongue, tg) : b;
            }
        }
    }
}
