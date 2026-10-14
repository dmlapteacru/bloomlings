using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Shared gameplay art coordinates and smooth colored pod/stone surfaces.</summary>
    public static class GameplaySkin
    {
        public const string Atlas = "pod-icons-v1";
        public const float PodRadiusShare = 0.24f;
        public static Box BoosterPurchase(Box tile)
        {
            float side = Math.Min(tile.Width, tile.Height);
            return Box.FromCenter(tile.CenterX, tile.Bottom - side * .15f, side * 1.04f, side * .60f);
        }
        private static readonly string[] PodKeys = { "leaf", "moss", "flower", "violet_bud", "water", "dew", "wood", "acorn", "vine", "berry", "mist", "bark" };
        // Opaque subject bounds in each 362-pixel cell, with a 3-pixel gutter. Source PNG stays unchanged.
        private static readonly Box[] PodBounds = {
            new Box(51,62,317,349), new Box(18,95,343,339), new Box(35,52,344,346), new Box(45,60,321,352),
            new Box(54,35,311,341), new Box(61,43,301,334), new Box(29,40,332,344), new Box(62,35,329,334),
            new Box(45,22,352,320), new Box(33,20,334,316), new Box(13,42,333,319), new Box(27,12,322,325)
        };
        public static (int Column, int Row)? PodCell(string key)
        {
            int index = Array.IndexOf(PodKeys, key);
            return index < 0 ? null : (index % 4, index / 4);
        }
        public static (int Left, int Top, int Width, int Height) PodCrop(string key, int w, int h)
        {
            int index = Array.IndexOf(PodKeys, key);
            Box b = index < 0 ? new Box(0, 0, 362, 362) : PodBounds[index];
            return ((int)(b.Left * w / 362), (int)(b.Top * h / 362), (int)(b.Width * w / 362), (int)(b.Height * h / 362));
        }
        public static string PodIcon(VariantId id) => "gameplay/pod-" + id.Key;
        public static Box SlotPanel(ReferenceGameplayRegions r) => r.Slots.Count == 0 ? r.SlotRow : new Box(r.Slots[0].Left - r.W * 0.02f, r.SlotRow.Top - r.W * 0.012f, r.Slots[r.Slots.Count - 1].Right + r.W * 0.02f, r.SlotRow.Bottom + r.W * 0.012f);
        public static readonly TypeStyle PodCount = new TypeStyle("type.shop.pod_count", 46, true, false, 0, 24);
        public static readonly ColorSet StoneRim = new ColorSet("gameplay.stone.rim", Rgba.FromHex("#E9BC75"), Rgba.FromHex("#FFF7D7"), Rgba.FromHex("#9E693A"), Rgba.FromHex("#B8884C"));
        public static readonly ColorSet StoneFace = new ColorSet("gameplay.stone.face", Rgba.FromHex("#F3D39C"), Rgba.FromHex("#FFF2D1"), Rgba.FromHex("#CAA268"), Rgba.FromHex("#D9B57A"));
        public static byte[] Stone(int w, int h, float radius) => ShopBodyRaster.Framed(w, h, radius, StoneRim, StoneFace, 0.07f, true, 0.10f, 0, 0.5f);
        public static byte[] Booster(int w, int h) => ShopBodyRaster.Framed(w, h, 0.23f, StoneRim, ShopBodyDesign.Card, 0.04f, true, 0.08f);
        public static float BoardCornerRadius(float borderThickness)=>borderThickness*1.6f;
        public static byte[] BoardRim(int w, int h, float thicknessShare)
        {
            // This underlay is covered by the grid and individual fence stones. A scanline gradient needs no
            // supersampled lighting/trigonometry for the million hidden interior pixels of a phone-sized board.
            float size = Math.Min(w, h), radius = Math.Max(1, size * BoardCornerRadius(thicknessShare));
            float padding = Math.Max(.5f, size * .0015f);
            var pixels = new byte[checked(w * h * 4)];
            for (int y = 0; y < h; y++)
            {
                Rgba color = StoneRim.Top.Mix(StoneRim.Lip, (float)y / Math.Max(1, h - 1));
                for (int x = 0; x < w; x++)
                {
                    float qx = Math.Abs(x + .5f - w * .5f) - (w * .5f - padding - radius);
                    float qy = Math.Abs(y + .5f - h * .5f) - (h * .5f - padding - radius);
                    float ox = Math.Max(0, qx), oy = Math.Max(0, qy);
                    float d = (ox > 0 && oy > 0 ? (float)Math.Sqrt(ox * ox + oy * oy) : ox + oy) + Math.Min(Math.Max(qx, qy), 0) - radius;
                    int i = (y * w + x) * 4;
                    pixels[i] = color.R; pixels[i + 1] = color.G; pixels[i + 2] = color.B;
                    pixels[i + 3] = (byte)(255 * Math.Max(0, Math.Min(1, .5f - d)));
                }
            }
            return pixels;
        }
        public static byte[] Pod(int w, int h, VariantId? variant, bool waiting, bool locked)
        {
            Rgba color = locked ? Rgba.FromHex("#83978C") : variant.HasValue && VariantCatalog.Default.TryGet(variant.Value, out VariantInfo info) ? Rgba.FromHex(info.ColorHex) : Rgba.FromHex("#B99ACC");
            Rgba tint = color;
            bool cool = variant.HasValue && (variant.Value.Key == "leaf" || variant.Value.Key == "moss" || variant.Value.Key == "vine" || variant.Value.Key == "water" || variant.Value.Key == "dew");
            if (!locked) color = UiRaster.Vivid(color, 1.06f).Darken(cool ? 0.18f : 0.10f);
            Rgba top = locked ? color.Lighten(0.88f) : Rgba.FromHex("#FFF4DE").Mix(tint, 0.055f);
            Rgba bottom = locked ? color.Lighten(0.75f) : Rgba.FromHex("#FBE6C7").Mix(tint, 0.11f);
            bool raised = !waiting && !locked;
            float size = Math.Min(w, h), padding = Math.Max(1, size * 0.008f), radius = size * PodRadiusShare, stroke = Math.Max(1.5f, size * 0.055f), lip = raised ? size * 0.085f : 0;
            byte[] pixels = new byte[w * h * 4];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float red = 0, green = 0, blue = 0, alpha = 0;
                for (int sy = 0; sy < 2; sy++) for (int sx = 0; sx < 2; sx++)
                {
                    float px = x + 0.25f + sx * 0.5f, py = y + 0.25f + sy * 0.5f;
                    float qx = Math.Abs(px - w * 0.5f) - (w * 0.5f - padding - radius), qy = Math.Abs(py - h * 0.5f) - (h * 0.5f - padding - radius);
                    float ox = Math.Max(0, qx), oy = Math.Max(0, qy);
                    float distance = (float)Math.Sqrt(ox * ox + oy * oy) + Math.Min(Math.Max(qx, qy), 0) - radius;
                    // The selectable pod has a rounded upper face over a separate, deeper colored base.
                    float ty = Math.Abs(py - (h - lip) * 0.5f) - ((h - lip) * 0.5f - padding - radius);
                    float topDistance = (float)Math.Sqrt(ox * ox + Math.Max(0, ty) * Math.Max(0, ty)) + Math.Min(Math.Max(qx, ty), 0) - radius;
                    float across = Math.Max(0, Math.Min(1, -topDistance / stroke));
                    float shine = (float)Math.Sin(Math.PI * across) * (0.34f - 0.18f * py / h);
                    Rgba rim = color.Lighten(0.035f + shine).Mix(color.Darken(0.17f), (1 - Smooth(0, size * 0.014f, -topDistance)) * 0.40f);
                    float edgeGlow = (float)Math.Exp(-Math.Pow((-topDistance - size * 0.017f) / (size * 0.010f), 2));
                    rim = rim.Mix(color.Lighten(0.60f), edgeGlow * (0.24f + 0.18f * (1 - py / h)));
                    float faceRadius = Math.Max(1, radius - stroke);
                    float fx = Math.Abs(px - w * 0.5f) - (w * 0.5f - padding - stroke - faceRadius);
                    float fy = Math.Abs(py - (h - lip) * 0.5f) - ((h - lip) * 0.5f - padding - stroke - faceRadius);
                    float faceDistance = (float)Math.Sqrt(Math.Max(0, fx) * Math.Max(0, fx) + Math.Max(0, fy) * Math.Max(0, fy)) + Math.Min(Math.Max(fx, fy), 0) - faceRadius;
                    float channelWidth = Math.Max(1, size * 0.018f);
                    float insetShadow = (float)Math.Exp(-Math.Max(0, -faceDistance) / channelWidth) * 0.27f;
                    Rgba face = top.Mix(bottom, py / h).Mix(color.Darken(0.42f), insetShadow);
                    Rgba c = rim.Mix(face, 1 - Smooth(-0.6f, 0.6f, faceDistance));
                    // A fine, antialiased near-black tinted contour separates the rim from the cream face.
                    float innerLine = 1 - Smooth(size * 0.003f, size * 0.008f, Math.Abs(faceDistance));
                    c = c.Mix(color.Darken(0.70f).Mix(Rgba.Black, 0.30f), innerLine * 0.62f);
                    if (raised)
                    {
                        // One continuous side plane: light at the front edge, smoothly shaded toward its foot.
                        // Avoid stacked reflection/seam bands, which read as corrugation on small pods.
                        float sideDepth = Smooth(h - lip - padding, h - padding, py);
                        Rgba side = color.Darken(0.06f + sideDepth * 0.28f);
                        side = side.Mix(color.Darken(0.45f), Smooth(0.55f, 0.98f, px / w) * 0.12f);
                        c = c.Mix(side, Smooth(-0.6f, 0.6f, topDistance));
                    }
                    float cover = 1 - Smooth(-0.75f, 0.75f, distance);
                    float shadow = 0.035f * (float)Math.Exp(-Math.Max(0, distance) * Math.Max(0, distance) / 4f);
                    float a = cover + shadow * (1 - cover);
                    c = c.Mix(ShopBodyDesign.Ink, (1 - cover) * shadow / Math.Max(a, 0.001f));
                    red += c.R * a; green += c.G * a; blue += c.B * a; alpha += a;
                }
                int i = (y * w + x) * 4;
                if (alpha > 0.001f) { pixels[i] = (byte)(red / alpha); pixels[i + 1] = (byte)(green / alpha); pixels[i + 2] = (byte)(blue / alpha); }
                pixels[i + 3] = (byte)(255 * alpha * 0.25f);
            }
            return pixels;
        }
        private static float Smooth(float from, float to, float value) { float t = Math.Max(0, Math.Min(1, (value - from) / (to - from))); return t * t * (3 - 2 * t); }

        /// <summary>Touching straight stones and four quarter-ring corner stones; -1 means a straight stone.</summary>
        public static List<(Box Box, int Corner)> Fence(Box grid, float cell, float thickness)
        {
            var pieces = new List<(Box, int)>();
            float t = cell * thickness, corner = t * 1.6f;
            Box outer = grid.Inset(-cell * (0.04f + thickness));
            pieces.Add((new Box(outer.Left, outer.Top, outer.Left + corner, outer.Top + corner), 0));
            pieces.Add((new Box(outer.Right - corner, outer.Top, outer.Right, outer.Top + corner), 1));
            pieces.Add((new Box(outer.Left, outer.Bottom - corner, outer.Left + corner, outer.Bottom), 2));
            pieces.Add((new Box(outer.Right - corner, outer.Bottom - corner, outer.Right, outer.Bottom), 3));
            void Run(float from, float to, Func<float, float, Box> box)
            {
                int count = Math.Max(1, (int)Math.Round((to - from) / cell));
                for (int i = 0; i < count; i++) pieces.Add((box(from + (to - from) * i / count, from + (to - from) * (i + 1) / count), -1));
            }
            Run(outer.Left + corner, outer.Right - corner, (a, b) => new Box(a, outer.Top, b, outer.Top + t));
            Run(outer.Left + corner, outer.Right - corner, (a, b) => new Box(a, outer.Bottom - t, b, outer.Bottom));
            Run(outer.Top + corner, outer.Bottom - corner, (a, b) => new Box(outer.Left, a, outer.Left + t, b));
            Run(outer.Top + corner, outer.Bottom - corner, (a, b) => new Box(outer.Right - t, a, outer.Right, b));
            return pieces;
        }
        public static byte[] CornerStone(int w, int h, int corner)
        {
            byte[] pixels = new byte[w * h * 4];
            float radius = Math.Min(w, h), thickness = radius / 1.6f;
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float red = 0, green = 0, blue = 0, alpha = 0;
                for (int sy = 0; sy < 2; sy++) for (int sx = 0; sx < 2; sx++)
                {
                    float px = x + 0.25f + sx * 0.5f, py = y + 0.25f + sy * 0.5f;
                    float cx = corner % 2 == 1 ? w - px : px, cy = corner >= 2 ? h - py : py;
                    float distance = (float)Math.Sqrt((cx - radius) * (cx - radius) + (cy - radius) * (cy - radius));
                    float a = Math.Max(0, Math.Min(1, radius - distance + 0.5f)) * Math.Max(0, Math.Min(1, distance - radius + thickness + 0.5f));
                    float across = Math.Max(0, Math.Min(1, (radius - distance) / thickness));
                    float highlight = (float)Math.Sin(Math.PI * across);
                    Rgba color = StoneRim.Lip.Mix(StoneFace.Face, 0.50f + highlight * 0.38f).Mix(StoneFace.Top, highlight * (0.50f - py / h * 0.20f));
                    red += color.R * a; green += color.G * a; blue += color.B * a; alpha += a;
                }
                int i = (y * w + x) * 4;
                if (alpha > 0.001f) { pixels[i] = (byte)(red / alpha); pixels[i + 1] = (byte)(green / alpha); pixels[i + 2] = (byte)(blue / alpha); }
                pixels[i + 3] = (byte)(255 * alpha * 0.25f);
            }
            return pixels;
        }
    }
}
