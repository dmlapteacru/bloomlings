using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Colored card rims, inset reward panels and price capsules, shared by Unity and the playtest.</summary>
    public static class ShopBodyRaster
    {
        public static byte[] Framed(int width, int height, float radiusShare, ColorSet border, ColorSet face, float strokeShare)
        {
            float size = Math.Min(width, height), radius = size * radiusShare, stroke = Math.Max(2, size * strokeShare);
            float padding = Math.Max(1.5f, size * 0.01f), bevel = Math.Max(2, size * 0.025f);
            byte[] pixels = new byte[width * height * 4];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                float qx = Math.Abs(x + 0.5f - width * 0.5f) - (width * 0.5f - padding - radius);
                float qy = Math.Abs(y + 0.5f - height * 0.5f) - (height * 0.5f - padding - radius);
                float outside = (float)Math.Sqrt(Math.Max(0, qx) * Math.Max(0, qx) + Math.Max(0, qy) * Math.Max(0, qy));
                float distance = outside + Math.Min(Math.Max(qx, qy), 0) - radius;
                float along = (y + 0.5f) / height;
                if (distance > 0.5f)
                {
                    // A narrow, even halo around all four edges, with no raised lower slab.
                    float shadow = 0.14f * (float)Math.Exp(-distance * distance / (padding * padding));
                    pixels[i] = ShopBodyDesign.Ink.R; pixels[i + 1] = ShopBodyDesign.Ink.G; pixels[i + 2] = ShopBodyDesign.Ink.B;
                    pixels[i + 3] = (byte)(255 * shadow);
                    continue;
                }
                Rgba color = face.Top.Mix(face.Face, 0.3f + along * 0.4f);
                float edge = Math.Max(0, Math.Min(1, 1 - (-distance - stroke) / bevel));
                color = color.Mix(face.Lip, edge * 0.12f);
                if (distance >= -stroke - 1.5f)
                {
                    color = border.Face.Mix(border.Lip, along * 0.25f);
                    if (distance > -stroke * 0.2f) color = border.Line.Mix(border.Face, 0.35f);
                    else if (distance > -stroke * 0.45f) color = border.Top;
                    else if (distance < -stroke + 1.5f) color = border.Top.Mix(face.Top, 0.5f);
                }
                pixels[i] = color.R; pixels[i + 1] = color.G; pixels[i + 2] = color.B;
                pixels[i + 3] = (byte)(255 * Math.Max(0, Math.Min(1, 0.5f - distance)));
            }
            return pixels;
        }
    }
}
