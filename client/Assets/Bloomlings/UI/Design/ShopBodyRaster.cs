using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Colored card rims, inset reward panels and price capsules, shared by Unity and the playtest.</summary>
    public static class ShopBodyRaster
    {
        public static byte[] Framed(int width, int height, float radiusShare, ColorSet border, ColorSet face, float strokeShare, bool cushion = false, float bulge = 0, float paddingShare = 0.016f)
        {
            float size = Math.Min(width, height), radius = size * radiusShare, stroke = Math.Max(2, size * strokeShare);
            float padding = Math.Max(2, size * paddingShare), bevel = Math.Max(3, size * (cushion ? 0.16f : 0.05f));
            radius = Math.Min(radius, size * 0.5f - padding);
            byte[] pixels = new byte[width * height * 4];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                float red = 0, green = 0, blue = 0, alpha = 0;
                // Supersample both the silhouette and the curved rim; no hard concentric color steps.
                for (int sy = 0; sy < 2; sy++) for (int sx = 0; sx < 2; sx++)
                {
                    float px = x + 0.25f + sx * 0.5f, py = y + 0.25f + sy * 0.5f;
                    float dx = px - width * 0.5f, dy = py - height * 0.5f;
                    float qx = Math.Abs(dx) - (width * 0.5f - padding - radius), qy = Math.Abs(dy) - (height * 0.5f - padding - radius);
                    float ox = Math.Max(0, qx), oy = Math.Max(0, qy), outside = (float)Math.Sqrt(ox * ox + oy * oy);
                    float distance = outside + Math.Min(Math.Max(qx, qy), 0) - radius;
                    float nx = outside > 0.001f ? Math.Sign(dx) * ox / outside : qx > qy ? Math.Sign(dx) : 0;
                    float ny = outside > 0.001f ? Math.Sign(dy) * oy / outside : qy >= qx ? Math.Sign(dy) : 0;
                    float across = Clamp(-distance / stroke);
                    float radial = (float)Math.Cos(Math.PI * across), normalZ = (float)Math.Sin(Math.PI * across);
                    float light = Clamp(-nx * radial * 0.36f - ny * radial * 0.54f + normalZ * 0.76f);
                    Rgba rim = border.Lip.Mix(border.Face, 0.3f + light * 0.7f).Mix(border.Top, (float)Math.Pow(light, 6) * 0.85f);
                    Rgba interior = face.Top.Mix(face.Face, 0.2f + py / height * 0.42f);
                    float edge = 1 - Smooth(0, bevel, -distance - stroke);
                    interior = interior.Mix(face.Lip, edge * (0.13f + 0.07f * (nx + ny + 2) * 0.25f));
                    if (cushion)
                    {
                        float shine = (float)Math.Exp(-Math.Pow((py - height * 0.26f) / (height * 0.18f), 2));
                        interior = interior.Mix(face.Lip, Smooth(0.5f, 0.96f, py / height) * bulge);
                        interior = interior.Mix(face.Top, shine * (0.35f + bulge));
                    }
                    Rgba color = interior.Mix(rim, 1 - Smooth(stroke - 0.8f, stroke + 0.8f, -distance));
                    float cover = 1 - Smooth(-0.75f, 0.75f, distance);
                    float shadow = 0.12f * (float)Math.Exp(-Math.Max(0, distance) * Math.Max(0, distance) / (padding * padding));
                    float a = cover + shadow * (1 - cover);
                    color = color.Mix(ShopBodyDesign.Ink, (1 - cover) * shadow / Math.Max(a, 0.001f));
                    red += color.R * a; green += color.G * a; blue += color.B * a; alpha += a;
                }
                if (alpha > 0.001f) { pixels[i] = (byte)(red / alpha); pixels[i + 1] = (byte)(green / alpha); pixels[i + 2] = (byte)(blue / alpha); }
                pixels[i + 3] = (byte)(255 * alpha * 0.25f);
            }
            return pixels;
        }
        private static float Clamp(float value) => Math.Max(0, Math.Min(1, value));
        private static float Smooth(float from, float to, float value) { float t = Clamp((value - from) / (to - from)); return t * t * (3 - 2 * t); }
    }
}
