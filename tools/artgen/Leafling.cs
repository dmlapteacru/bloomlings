using System;
using System.IO;
using System.Threading.Tasks;

namespace Bloomlings.ArtGen
{
    /// <summary>
    /// The Leafling experiment (research R17): a sculpted model the owner made with Meshy AI
    /// (<c>tools/artgen/models/leafling.fbx</c>, geometry only), rendered to a flat picture in the 3D heroes' light, so
    /// Home can show it as a pre-rendered illustration (constitution VII). The model has no texture, so this paints it:
    /// pale green skin, green leaves, dark eyes with highlights, an open mouth with a tongue, brows and blush. Triangles are
    /// rasterized with a depth buffer at 3 × 3 samples per pixel, with a soft shadow map, screen-space ambient occlusion,
    /// a warm key light and a golden back light. Bands of rows render independently, so the output is deterministic.
    /// </summary>
    public static class Leafling
    {
        public const int Width = 512;
        public const int Height = 576;

        private const int Samples = 3;
        private const int ShadowSize = 2048;

        private static readonly V3 Key = new V3(-0.45, 0.7, 0.7).Normalized;
        private static readonly V3 Back = new V3(0.25, 0.45, -1).Normalized;
        private static readonly Rotation Turn = new Rotation(0.16);

        /// <summary>The model file, relative to the repository root.</summary>
        public const string ModelPath = "tools/artgen/models/leafling.fbx";

        private sealed class Model
        {
            public V3[] P = Array.Empty<V3>();
            public int[] T = Array.Empty<int>();
            public V3[] N = Array.Empty<V3>();
        }

        private static Model? cached;

        /// <summary>The picture as straight-alpha RGBA rows from the top.</summary>
        public static byte[] Render(string root)
        {
            Model m = Load(root);
            int sw = Width * Samples;
            int sh = Height * Samples;

            // View: the model turned slightly, seen from a little above, fitted into the picture with a margin.
            var view = new V3[m.P.Length];
            double minX = 1e9, maxX = -1e9, minY = 1e9, maxY = -1e9;
            for (int i = 0; i < m.P.Length; i++)
            {
                V3 v = ToView(m.P[i]);
                view[i] = v;
                minX = Math.Min(minX, v.X);
                maxX = Math.Max(maxX, v.X);
                minY = Math.Min(minY, v.Y);
                maxY = Math.Max(maxY, v.Y);
            }

            // Leave room for the ground shadow below and a 4% margin around.
            double scale = Math.Min(sw * 0.9 / (maxX - minX), sh * 0.86 / (maxY - minY));
            double ox = (sw / 2.0) - (((minX + maxX) / 2) * scale);
            double oy = (sh * 0.04) + (maxY * scale);
            var screen = new V3[view.Length];
            for (int i = 0; i < view.Length; i++)
            {
                screen[i] = new V3(ox + (view[i].X * scale), oy - (view[i].Y * scale), view[i].Z);
            }

            (double[] depth, int[] tri, float[] b0, float[] b1) = Rasterize(m.T, screen, sw, sh);

            // The shadow map: depth along the key light, orthographic.
            (double[] shadow, Func<V3, (double X, double Y, double Z)> toLight) = ShadowMap(m, view);

            double feetY = oy - (minY * scale);
            var rgba = new byte[Width * Height * 4];
            Parallel.For(0, Height, row =>
            {
                for (int x = 0; x < Width; x++)
                {
                    double r = 0, g = 0, b = 0, a = 0;
                    for (int sy = 0; sy < Samples; sy++)
                    {
                        for (int sx = 0; sx < Samples; sx++)
                        {
                            int px = (x * Samples) + sx;
                            int py = (row * Samples) + sy;
                            int k = (py * sw) + px;
                            (V3 color, double alpha) = tri[k] < 0
                                ? Ground(px, py, sw, ox, feetY, scale)
                                : Shade(m, view, tri[k], b0[k], b1[k], depth, sw, sh, px, py, scale, shadow, toLight);
                            r += color.X * alpha;
                            g += color.Y * alpha;
                            b += color.Z * alpha;
                            a += alpha;
                        }
                    }

                    int o = ((row * Width) + x) * 4;
                    double count = Samples * Samples;
                    if (a > 0)
                    {
                        rgba[o] = Byte(r / a);
                        rgba[o + 1] = Byte(g / a);
                        rgba[o + 2] = Byte(b / a);
                        rgba[o + 3] = Byte(a / count);
                    }
                }
            });

            return rgba;
        }

        private static byte Byte(double v) => (byte)Math.Round(Sdf.Clamp(v, 0, 1) * 255);

        private static Model Load(string root)
        {
            if (cached != null)
            {
                return cached;
            }

            TriangleMesh mesh = Fbx.ReadMesh(Path.Combine(root, ModelPath));

            // The file is Z-up with the face toward −Y (Blender). Turn it to Y-up, face toward +Z, feet at 0, height 1.
            double minZ = 1e9, maxZ = -1e9;
            foreach (V3 p in mesh.Positions)
            {
                minZ = Math.Min(minZ, p.Z);
                maxZ = Math.Max(maxZ, p.Z);
            }

            double h = maxZ - minZ;
            var model = new Model { T = mesh.Triangles, P = new V3[mesh.Positions.Length], N = new V3[mesh.CornerNormals.Length] };
            for (int i = 0; i < mesh.Positions.Length; i++)
            {
                V3 p = mesh.Positions[i];
                model.P[i] = new V3(p.X / h, (p.Z - minZ) / h, -p.Y / h);
            }

            for (int i = 0; i < mesh.CornerNormals.Length; i++)
            {
                V3 n = mesh.CornerNormals[i];
                model.N[i] = new V3(n.X, n.Z, -n.Y).Normalized;
            }

            Reface(model);
            cached = model;
            return model;
        }

        // ---- The face ----
        //
        // The sculpted face is lopsided: Meshy put the left eye higher and larger than the right, and the eyes, brows and
        // mouth left of the head's center, while the head and body are level. So the tool smooths the sculpted features
        // away along the head's own curve and gives the Leafling a level face, centered on the head: two raised glossy
        // eyes, a recessed open smile, short brows and blush (Paint draws their colors on the same places).

        /// <summary>The old sculpted features (front view): eyes, mouth and brows, each smoothed away.</summary>
        private static readonly (double X, double Y, double Rx, double Ry)[] OldFeatures =
        {
            (-0.145, 0.47, 0.07, 0.074),
            (0.083, 0.432, 0.066, 0.08),
            (-0.026, 0.36, 0.092, 0.064),
            (-0.15, 0.577, 0.065, 0.035),
            (0.06, 0.548, 0.058, 0.042),
        };

        /// <summary>The head's front (front view), where the smooth face surface is fitted.</summary>
        private static readonly (double X, double Y, double Rx, double Ry) FaceArea = (-0.03, 0.45, 0.3, 0.19);

        /// <summary>The new eyes: centers and radii (front view), mirrored about the head's center line x = 0.</summary>
        private static readonly (double X, double Y, double Rx, double Ry)[] Eyes = { (-0.115, 0.452, 0.06, 0.068), (0.115, 0.452, 0.06, 0.068) };

        private const double MouthX = 0;
        private const double MouthY = 0.355;

        /// <summary>Whether a point (front view) is inside the open mouth: a smile, flat-curved on top and round below.</summary>
        private static bool InMouth(double x, double y) =>
            Ellipse(x - MouthX, y - MouthY, 0.058, 0.042) < 1 && y < 0.37 + (3 * (x - MouthX) * (x - MouthX));

        /// <summary>The new face's relief over the smooth head surface: raised eyes and a recessed mouth.</summary>
        private static double Relief(double x, double y)
        {
            double z = 0;
            foreach ((double ex, double ey, double rx, double ry) in Eyes)
            {
                double e = Ellipse(x - ex, y - ey, rx, ry);
                if (e < 1)
                {
                    double k = 1 - (e * e);
                    z += 0.012 * k * k;
                }
            }

            // The mouth: recessed in its own smile shape, with soft edges.
            double m = Ellipse(x - MouthX, y - MouthY, 0.058, 0.042);
            double top = y - (0.37 + (3 * (x - MouthX) * (x - MouthX)));
            if (m < 1.1 && top < 0.006)
            {
                z -= 0.014 * (1 - Sdf.Smoothstep(0.8, 1.1, m)) * (1 - Sdf.Smoothstep(-0.008, 0.006, top));
            }

            return z;
        }

        /// <summary>How much of a point (front view) lies on an old sculpted feature: 1 inside one, 0 clear of them all.</summary>
        private static double OnOldFeature(double x, double y)
        {
            double w = 0;
            foreach ((double fx, double fy, double rx, double ry) in OldFeatures)
            {
                w = Math.Max(w, 1 - Sdf.Smoothstep(1, 1.3, Ellipse(x - fx, y - fy, rx, ry)));
            }

            return w;
        }

        private static void Reface(Model m)
        {
            var vertexNormal = new V3[m.P.Length];
            for (int i = 0; i < m.T.Length; i++)
            {
                vertexNormal[m.T[i]] = vertexNormal[m.T[i]] + m.N[i];
            }

            // The head's front as a height map: the front-most skin in 0.01 cells (one sample per cell, so dense areas do
            // not outweigh sparse ones). A quadratic height z = f(x, y) is fitted to the cells of the face clear of the old
            // features: the head is flattened in front and turned a little to its left, and the fit follows it.
            const double cell = 0.01;
            const int cols = 70;
            const int rows = 40;
            var front = new double[cols * rows];
            Array.Fill(front, double.NegativeInfinity);
            for (int i = 0; i < m.P.Length; i++)
            {
                V3 p = m.P[i];
                int cx = (int)Math.Floor((p.X + 0.35) / cell);
                int cy = (int)Math.Floor((p.Y - 0.25) / cell);
                if (cx >= 0 && cy >= 0 && cx < cols && cy < rows && p.Z > 0 && Core(p) <= 0.01)
                {
                    front[(cy * cols) + cx] = Math.Max(front[(cy * cols) + cx], p.Z);
                }
            }

            var ata = new double[6, 6];
            var atb = new double[6];
            var basis = new double[6];
            for (int cy = 0; cy < rows; cy++)
            {
                for (int cx = 0; cx < cols; cx++)
                {
                    double z = front[(cy * cols) + cx];
                    double x = -0.35 + ((cx + 0.5) * cell);
                    double y = 0.25 + ((cy + 0.5) * cell);
                    if (double.IsNegativeInfinity(z) || z < 0.1 || Ellipse(x - FaceArea.X, y - FaceArea.Y, FaceArea.Rx, FaceArea.Ry) > 1 || OnOldFeature(x, y) > 0)
                    {
                        continue;
                    }

                    Quadratic(x, y, basis);
                    for (int a = 0; a < 6; a++)
                    {
                        atb[a] += basis[a] * z;
                        for (int b = 0; b < 6; b++)
                        {
                            ata[a, b] += basis[a] * basis[b];
                        }
                    }
                }
            }

            double[] q = Solve(ata, atb);
            double Smooth(double x, double y)
            {
                Quadratic(x, y, basis);
                double z = 0;
                for (int a = 0; a < 6; a++)
                {
                    z += q[a] * basis[a];
                }

                return z;
            }

            // The face skin: the old features flattened onto the smooth surface, then the new relief added.
            const double h = 0.002;
            var moved = new V3?[m.P.Length];
            for (int i = 0; i < m.P.Length; i++)
            {
                V3 p = m.P[i];
                if (Ellipse(p.X - FaceArea.X, p.Y - FaceArea.Y, FaceArea.Rx, FaceArea.Ry) > 1.1 || Core(p) > 0.03 || p.Z < Smooth(p.X, p.Y) - 0.1)
                {
                    continue;
                }

                double w = OnOldFeature(p.X, p.Y);
                double relief = Relief(p.X, p.Y);
                if (w <= 0 && relief == 0)
                {
                    continue;
                }

                double smooth = Smooth(p.X, p.Y);
                m.P[i] = new V3(p.X, p.Y, Sdf.Mix(p.Z, smooth, w) + relief);
                var smoothNormal = new V3(-(Smooth(p.X + h, p.Y) - Smooth(p.X - h, p.Y)) / (2 * h), -(Smooth(p.X, p.Y + h) - Smooth(p.X, p.Y - h)) / (2 * h), 1).Normalized;
                V3 n = V3.Mix(vertexNormal[i].Normalized, smoothNormal, w).Normalized;
                double rx = (Relief(p.X + h, p.Y) - Relief(p.X - h, p.Y)) / (2 * h);
                double ry = (Relief(p.X, p.Y + h) - Relief(p.X, p.Y - h)) / (2 * h);
                moved[i] = (n - new V3(rx, ry, 0)).Normalized;
            }

            for (int i = 0; i < m.T.Length; i++)
            {
                if (moved[m.T[i]] is V3 normal)
                {
                    m.N[i] = normal;
                }
            }
        }

        /// <summary>The terms of a quadratic height in (x, y), centered on the face.</summary>
        private static void Quadratic(double x, double y, double[] b)
        {
            double u = (x - FaceArea.X) / FaceArea.Rx;
            double v = (y - FaceArea.Y) / FaceArea.Ry;
            b[0] = 1;
            b[1] = u;
            b[2] = v;
            b[3] = u * u;
            b[4] = u * v;
            b[5] = v * v;
        }

        /// <summary>Solves a small linear system by Gaussian elimination with partial pivoting.</summary>
        private static double[] Solve(double[,] a, double[] b)
        {
            int n = b.Length;
            var m = (double[,])a.Clone();
            var r = (double[])b.Clone();
            for (int col = 0; col < n; col++)
            {
                int pivot = col;
                for (int row = col + 1; row < n; row++)
                {
                    if (Math.Abs(m[row, col]) > Math.Abs(m[pivot, col]))
                    {
                        pivot = row;
                    }
                }

                for (int k = 0; k < n; k++)
                {
                    (m[col, k], m[pivot, k]) = (m[pivot, k], m[col, k]);
                }

                (r[col], r[pivot]) = (r[pivot], r[col]);
                for (int row = col + 1; row < n; row++)
                {
                    double f = m[row, col] / m[col, col];
                    for (int k = col; k < n; k++)
                    {
                        m[row, k] -= f * m[col, k];
                    }

                    r[row] -= f * r[col];
                }
            }

            var x = new double[n];
            for (int row = n - 1; row >= 0; row--)
            {
                double s = r[row];
                for (int k = row + 1; k < n; k++)
                {
                    s -= m[row, k] * x[k];
                }

                x[row] = s / m[row, row];
            }

            return x;
        }

        /// <summary>Model space to view space: turned about the vertical, tilted 6° toward the viewer (z toward the camera).</summary>
        private static V3 ToView(V3 p)
        {
            (double x, double z) = Turn.Apply(p.X, p.Z);
            const double tilt = 0.105;

            double y = (p.Y * Math.Cos(tilt)) - (z * Math.Sin(tilt));
            double zz = (p.Y * Math.Sin(tilt)) + (z * Math.Cos(tilt));
            return new V3(x, y, zz);
        }

        private static V3 NormalToView(V3 n) => ToView(n);

        /// <summary>Depth (larger is nearer), triangle and barycentrics per sample, in bands of rows.</summary>
        private static (double[] Depth, int[] Tri, float[] B0, float[] B1) Rasterize(int[] t, V3[] s, int w, int h)
        {
            var depth = new double[w * h];
            var tri = new int[w * h];
            var b0 = new float[w * h];
            var b1 = new float[w * h];
            Array.Fill(depth, double.NegativeInfinity);
            Array.Fill(tri, -1);
            const int band = 32;
            Parallel.For(0, (h + band - 1) / band, bi =>
            {
                int top = bi * band;
                int bottom = Math.Min(h - 1, top + band - 1);
                for (int i = 0; i < t.Length; i += 3)
                {
                    V3 a = s[t[i]];
                    V3 b = s[t[i + 1]];
                    V3 c = s[t[i + 2]];
                    double minY = Math.Min(a.Y, Math.Min(b.Y, c.Y));
                    double maxY = Math.Max(a.Y, Math.Max(b.Y, c.Y));
                    if (maxY < top || minY > bottom + 1)
                    {
                        continue;
                    }

                    double area = ((b.X - a.X) * (c.Y - a.Y)) - ((b.Y - a.Y) * (c.X - a.X));
                    if (Math.Abs(area) < 1e-12)
                    {
                        continue;
                    }

                    int x0 = Math.Max(0, (int)Math.Floor(Math.Min(a.X, Math.Min(b.X, c.X))));
                    int x1 = Math.Min(w - 1, (int)Math.Ceiling(Math.Max(a.X, Math.Max(b.X, c.X))));
                    int y0 = Math.Max(top, (int)Math.Floor(minY));
                    int y1 = Math.Min(bottom, (int)Math.Ceiling(maxY));
                    for (int y = y0; y <= y1; y++)
                    {
                        double py = y + 0.5;
                        for (int x = x0; x <= x1; x++)
                        {
                            double px = x + 0.5;
                            double w0 = (((b.X - px) * (c.Y - py)) - ((b.Y - py) * (c.X - px))) / area;
                            double w1 = (((c.X - px) * (a.Y - py)) - ((c.Y - py) * (a.X - px))) / area;
                            double w2 = 1 - w0 - w1;
                            if (w0 < 0 || w1 < 0 || w2 < 0)
                            {
                                continue;
                            }

                            double z = (w0 * a.Z) + (w1 * b.Z) + (w2 * c.Z);
                            int k = (y * w) + x;
                            if (z > depth[k])
                            {
                                depth[k] = z;
                                tri[k] = i;
                                b0[k] = (float)w0;
                                b1[k] = (float)w1;
                            }
                        }
                    }
                }
            });

            return (depth, tri, b0, b1);
        }

        /// <summary>An orthographic depth map along the key light (larger is nearer the light).</summary>
        private static (double[] Map, Func<V3, (double X, double Y, double Z)> ToLight) ShadowMap(Model m, V3[] view)
        {
            V3 forward = Key;
            V3 right = V3.Cross(new V3(0, 1, 0), forward).Normalized;
            V3 up = V3.Cross(forward, right);
            var light = new V3[view.Length];
            double minX = 1e9, maxX = -1e9, minY = 1e9, maxY = -1e9;
            for (int i = 0; i < view.Length; i++)
            {
                var l = new V3(V3.Dot(view[i], right), V3.Dot(view[i], up), V3.Dot(view[i], forward));
                light[i] = l;
                minX = Math.Min(minX, l.X);
                maxX = Math.Max(maxX, l.X);
                minY = Math.Min(minY, l.Y);
                maxY = Math.Max(maxY, l.Y);
            }

            double scale = (ShadowSize - 8) / Math.Max(maxX - minX, maxY - minY);
            double ox = 4 - (minX * scale);
            double oy = 4 + (maxY * scale);
            var s = new V3[light.Length];
            for (int i = 0; i < light.Length; i++)
            {
                s[i] = new V3(ox + (light[i].X * scale), oy - (light[i].Y * scale), light[i].Z);
            }

            (double[] map, _, _, _) = Rasterize(m.T, s, ShadowSize, ShadowSize);
            (double X, double Y, double Z) ToLight(V3 v) =>
                (ox + (V3.Dot(v, right) * scale), oy - (V3.Dot(v, up) * scale), V3.Dot(v, forward));
            return (map, ToLight);
        }

        private static (V3 Color, double Alpha) Ground(int px, int py, int sw, double ox, double feetY, double scale)
        {
            // A soft contact shadow on the ground, as under the heroes.
            double dx = (px + 0.5 - ox) / (scale * 0.34);
            double dy = (py + 0.5 - (feetY - (scale * 0.01))) / (scale * 0.06);
            double d = Math.Sqrt((dx * dx) + (dy * dy));
            double alpha = 0.34 * (1 - Sdf.Smoothstep(0.25, 1, d));
            return (new V3(0.24, 0.16, 0.1), alpha);
        }

        private static (V3 Color, double Alpha) Shade(Model m, V3[] view, int t, float w0f, float w1f, double[] depth, int sw, int sh, int px, int py, double scale, double[] shadow, Func<V3, (double X, double Y, double Z)> toLight)
        {
            double w0 = w0f;
            double w1 = w1f;
            double w2 = 1 - w0 - w1;
            int ia = m.T[t];
            int ib = m.T[t + 1];
            int ic = m.T[t + 2];
            V3 model = (m.P[ia] * w0) + (m.P[ib] * w1) + (m.P[ic] * w2);
            V3 modelNormal = ((m.N[t] * w0) + (m.N[t + 1] * w1) + (m.N[t + 2] * w2)).Normalized;
            V3 p = (view[ia] * w0) + (view[ib] * w1) + (view[ic] * w2);
            V3 n = NormalToView(modelNormal);
            var toEye = new V3(0, 0, 1);
            if (V3.Dot(n, toEye) < 0)
            {
                n = -n;
            }

            (V3 albedo, double gloss) = Paint(model, modelNormal);

            // Soft shadow: percentage-closer filtering over a 5 × 5 neighborhood of the shadow map.
            (double lx, double ly, double lz) = toLight(p);
            double lit = 0;
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int sx = (int)Math.Clamp(lx + (dx * 1.5), 0, ShadowSize - 1);
                    int sy = (int)Math.Clamp(ly + (dy * 1.5), 0, ShadowSize - 1);
                    lit += lz >= shadow[(sy * ShadowSize) + sx] - 0.004 ? 1 : 0;
                }
            }

            lit /= 25;

            // Ambient occlusion from the depth buffer: nearer samples around the point occlude it.
            double ao = 0;
            double radius = scale * 0.035;
            int taps = 0;
            for (int i = 0; i < 12; i++)
            {
                double angle = i * 2.399963;
                double dist = radius * Math.Sqrt((i + 0.5) / 12);
                int qx = (int)(px + (Math.Cos(angle) * dist));
                int qy = (int)(py + (Math.Sin(angle) * dist));
                if (qx < 0 || qy < 0 || qx >= sw || qy >= sh)
                {
                    continue;
                }

                double qz = depth[(qy * sw) + qx];
                taps++;
                if (!double.IsNegativeInfinity(qz))
                {
                    double rise = qz - p.Z;
                    ao += Sdf.Clamp(rise / 0.03, 0, 1) * (1 - Sdf.Smoothstep(0.03, 0.12, rise));
                }
            }

            ao = 1 - (0.75 * ao / Math.Max(1, taps));

            double dif = Sdf.Clamp((V3.Dot(n, Key) + 0.25) / 1.25, 0, 1);
            var sky = new V3(0.95, 0.85, 0.7);
            V3 ambient = V3.Mix(new V3(0.55, 0.5, 0.42), sky, 0.5 + (0.5 * n.Y)) * (0.45 * ao);
            V3 direct = new V3(1, 0.92, 0.78) * (1.05 * dif * Sdf.Mix(0.42, 1, lit));
            V3 col = albedo * (ambient + direct);
            double rim = Math.Pow(Sdf.Clamp(1 - V3.Dot(n, toEye), 0, 1), 2.5) * Sdf.Clamp((V3.Dot(n, Back) * 0.5) + 0.6, 0, 1);
            col = col + (new V3(1, 0.78, 0.45) * (rim * 0.6 * ao));
            V3 half = (Key + toEye).Normalized;
            col = col + (new V3(1, 0.95, 0.85) * (gloss * Math.Pow(Sdf.Clamp(V3.Dot(n, half), 0, 1), 40) * lit));
            return (ToSrgb(col), 1);
        }

        private static V3 ToSrgb(V3 c)
        {
            static double Ch(double v) => Math.Pow(Sdf.Clamp(v / (1 + (0.15 * v)), 0, 1), 1 / 2.2);
            return new V3(Ch(c.X), Ch(c.Y), Ch(c.Z));
        }

        // ---- Paint (model space: x right, y up from the feet, z toward the face; the model is 1 tall) ----

        /// <summary>The model's colors at a surface point: albedo (linear) and gloss.</summary>
        public static (V3 Albedo, double Gloss) Paint(V3 p, V3 n)
        {
            double core = Core(p);
            V3 skin = Sdf.Linear(0.83, 0.93, 0.62);
            V3 leaf = V3.Mix(Sdf.Linear(0.25, 0.55, 0.18), Sdf.Linear(0.5, 0.76, 0.27), Sdf.Smoothstep(0.3, 0.95, p.Y));
            double leafiness = Sdf.Smoothstep(0.01, 0.025, core);
            V3 c = V3.Mix(skin, leaf, leafiness);
            double gloss = Sdf.Mix(0.18, 0.3, leafiness);

            // The face: only on the front of the head (the mouth's inner surfaces face any way).
            bool face = p.Z > 0.05 && p.Y > 0.25 && p.Y < 0.62 && Ellipse(p.X - FaceArea.X, p.Y - FaceArea.Y, FaceArea.Rx, FaceArea.Ry) < 1.1;
            bool front = face && n.Z > 0.1;
            if (face)
            {
                // Blush on the cheeks.
                foreach (double sx in new[] { -1.0, 1.0 })
                {
                    double d = Ellipse(p.X - (sx * 0.19), p.Y - 0.372, 0.042, 0.028);
                    c = V3.Mix(c, Sdf.Linear(1, 0.6, 0.6), front ? 0.55 * (1 - Sdf.Smoothstep(0.4, 1, d)) * (1 - leafiness) : 0);
                }

                // Short brows above the eyes, their outer ends a little lower.
                if (front && (Tilted(p.X + 0.115, p.Y - 0.556, 0.15, 0.036, 0.008) < 1 || Tilted(p.X - 0.115, p.Y - 0.556, -0.15, 0.036, 0.008) < 1))
                {
                    c = Sdf.Linear(0.3, 0.45, 0.18);
                }

                // Eyes: dark and glossy, with two highlights each (the light comes from the upper left).
                foreach ((double cx, double cy, double rx, double ry) in Eyes)
                {
                    double ex = p.X - cx;
                    double ey = p.Y - cy;
                    if (Ellipse(ex, ey, rx, ry) < 1)
                    {
                        c = Sdf.Linear(0.16, 0.1, 0.08);
                        gloss = 0.45;
                        if (Ellipse(ex + (rx * 0.34), ey - (ry * 0.36), rx * 0.32, rx * 0.32) < 1 || Ellipse(ex - (rx * 0.38), ey + (ry * 0.36), rx * 0.14, rx * 0.14) < 1)
                        {
                            c = new V3(1, 1, 1);
                        }
                    }
                }

                // The open smile, with a tongue.
                if (InMouth(p.X, p.Y))
                {
                    c = Sdf.Linear(0.36, 0.1, 0.12);
                    gloss = 0.1;
                    if (Ellipse(p.X - MouthX, p.Y - (MouthY - 0.024), 0.036, 0.017) < 1)
                    {
                        c = Sdf.Linear(0.95, 0.47, 0.53);
                    }
                }
            }

            return (c, gloss);
        }

        private static double Ellipse(double x, double y, double rx, double ry) => Math.Sqrt(((x / rx) * (x / rx)) + ((y / ry) * (y / ry)));

        /// <summary>An ellipse turned by <paramref name="angle"/> radians (counterclockwise).</summary>
        private static double Tilted(double x, double y, double angle, double rx, double ry)
        {
            double c = Math.Cos(angle);
            double s = Math.Sin(angle);
            return Ellipse((c * x) + (s * y), (-s * x) + (c * y), rx, ry);
        }

        /// <summary>The distance outside the body core (head, torso, arms, legs): skin is on the core, leaves stand off it.</summary>
        public static double Core(V3 p)
        {
            // The leaves at the sides of the head (its "ears") lie partly inside the head's shape: always leaves.
            if (p.X < -0.27 && Ellipse(p.X + 0.36, p.Y - 0.39, 0.12, 0.085) < 1)
            {
                return 1;
            }

            if (p.X > 0.2 && p.Z > -0.06 && p.Z < 0.13 && Ellipse(p.X - 0.32, p.Y - 0.35, 0.14, 0.08) < 1)
            {
                return 1;
            }

            double d = Sdf.Ellipsoid(p - new V3(0, 0.445, 0.035), new V3(0.345, 0.19, 0.27));
            d = Math.Min(d, Sdf.Ellipsoid(p - new V3(0, 0.33, 0.06), new V3(0.27, 0.095, 0.21)));
            d = Math.Min(d, Sdf.Ellipsoid(p - new V3(0, 0.14, -0.01), new V3(0.17, 0.12, 0.14)));
            foreach (double s in new[] { -1.0, 1.0 })
            {
                d = Math.Min(d, Sdf.Capsule(p, new V3(s * 0.1, 0.2, 0), new V3(s * 0.25, 0.14, 0.02), 0.065));
                d = Math.Min(d, Sdf.Capsule(p, new V3(s * 0.095, 0.12, -0.01), new V3(s * 0.095, 0.04, 0.0), 0.09));
                d = Math.Min(d, Sdf.Ellipsoid(p - new V3(s * 0.095, 0.03, 0.03), new V3(0.1, 0.055, 0.12)));
            }

            return d;
        }
    }
}
