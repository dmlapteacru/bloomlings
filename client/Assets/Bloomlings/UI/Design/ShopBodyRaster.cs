using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Colored card rims, inset reward panels and price capsules, shared by Unity and the playtest.</summary>
    public static class ShopBodyRaster
    {
        public static byte[] Framed(int width, int height, float radiusShare, ColorSet border, ColorSet face, float strokeShare)
        {
            float size = Math.Min(width, height), radius = size * radiusShare, stroke = Math.Max(2, size * strokeShare);
            byte[] pixels = UiRaster.RaisedRow(width, height, radius, face);
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                if (pixels[i + 3] == 0) continue;
                float qx = Math.Abs(x + 0.5f - width * 0.5f) - (width * 0.5f - radius);
                float qy = Math.Abs(y + 0.5f - height * 0.5f) - (height * 0.5f - radius);
                float outside = (float)Math.Sqrt(Math.Max(0, qx) * Math.Max(0, qx) + Math.Max(0, qy) * Math.Max(0, qy));
                float distance = outside + Math.Min(Math.Max(qx, qy), 0) - radius;
                if (distance < -stroke - 1.5f) continue;
                float along = (y + 0.5f) / height;
                Rgba rim = border.Face.Mix(border.Lip, along * 0.55f);
                if (distance > -stroke * 0.2f) rim = border.Line.Mix(border.Face, 0.35f);
                else if (distance > -stroke * 0.45f) rim = border.Top;
                else if (distance < -stroke + 1.5f) rim = border.Top.Mix(face.Top, 0.5f);
                pixels[i] = rim.R; pixels[i + 1] = rim.G; pixels[i + 2] = rim.B;
            }
            return pixels;
        }
    }
}
