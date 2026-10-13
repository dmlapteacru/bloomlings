using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Shared gameplay art coordinates and smooth colored pod/stone surfaces.</summary>
    public static class GameplaySkin
    {
        public const string Atlas = "pod-icons-v1";
        public static (int Column, int Row)? PodCell(string key)
        {
            string[] keys = { "leaf", "moss", "flower", "violet_bud", "water", "dew", "wood", "acorn", "vine", "berry", "mist", "bark" };
            int index = Array.IndexOf(keys, key);
            return index < 0 ? null : (index % 4, index / 4);
        }
        public static string PodIcon(VariantId id) => "gameplay/pod-" + id.Key;
        public static Box SlotPanel(ReferenceGameplayRegions r) => r.Slots.Count == 0 ? r.SlotRow : new Box(r.Slots[0].Left - r.W * 0.02f, r.SlotRow.Top - r.W * 0.012f, r.Slots[r.Slots.Count - 1].Right + r.W * 0.02f, r.SlotRow.Bottom + r.W * 0.012f);
        public static readonly TypeStyle PodCount = new TypeStyle("type.shop.pod_count", 46, true, false, 0, 24);
        public static readonly ColorSet StoneRim = new ColorSet("gameplay.stone.rim", Rgba.FromHex("#E9BC75"), Rgba.FromHex("#FFF7D7"), Rgba.FromHex("#9E693A"), Rgba.FromHex("#B8884C"));
        public static readonly ColorSet StoneFace = new ColorSet("gameplay.stone.face", Rgba.FromHex("#F3D39C"), Rgba.FromHex("#FFF2D1"), Rgba.FromHex("#CAA268"), Rgba.FromHex("#D9B57A"));
        public static byte[] Stone(int w, int h, float radius) => ShopBodyRaster.Framed(w, h, radius, StoneRim, StoneFace, 0.07f, true, 0.10f, 0, 0.5f);
        public static byte[] Booster(int w, int h) => ShopBodyRaster.Framed(w, h, 0.23f, StoneRim, ShopBodyDesign.Card, 0.04f, true, 0.08f);
        public static byte[] BoardRim(int w, int h, float thicknessShare) => ShopBodyRaster.Framed(w, h, thicknessShare * 1.6f, StoneRim, StoneFace, thicknessShare * 0.30f, true, 0.12f, 0.0015f);
        public static byte[] Pod(int w, int h, VariantId? variant, bool waiting, bool locked)
        {
            Rgba color = locked ? Rgba.FromHex("#83978C") : variant.HasValue && VariantCatalog.Default.TryGet(variant.Value, out VariantInfo info) ? Rgba.FromHex(info.ColorHex) : Rgba.FromHex("#B99ACC");
            var rim = new ColorSet("gameplay.pod.rim", color.Lighten(0.12f), color.Lighten(0.50f), color.Darken(0.22f), color.Darken(0.18f));
            var face = new ColorSet("gameplay.pod.face", color.Lighten(waiting ? 0.84f : 0.48f), color.Lighten(waiting ? 0.94f : 0.79f), color.Darken(0.15f), color.Lighten(0.30f));
            return ShopBodyRaster.Framed(w, h, 0.15f, rim, face, 0.04f, false, 0.04f);
        }

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
