using System;
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
        public static Box SlotPanel(ReferenceGameplayRegions r) => r.SlotRow.Inset(-r.W * 0.015f, -r.W * 0.012f);
        public static readonly TypeStyle PodCount = new TypeStyle("type.shop.pod_count", 46, true, false, 0, 24);
        public static readonly ColorSet StoneRim = new ColorSet("gameplay.stone.rim", Rgba.FromHex("#E9BC75"), Rgba.FromHex("#FFF7D7"), Rgba.FromHex("#9E693A"), Rgba.FromHex("#B8884C"));
        public static readonly ColorSet StoneFace = new ColorSet("gameplay.stone.face", Rgba.FromHex("#F3D39C"), Rgba.FromHex("#FFF2D1"), Rgba.FromHex("#CAA268"), Rgba.FromHex("#D9B57A"));
        public static byte[] Stone(int w, int h, float radius) => ShopBodyRaster.Framed(w, h, radius, StoneRim, StoneFace, 0.10f, true, 0.14f);
        public static byte[] BoardRim(int w, int h, float thicknessShare) => ShopBodyRaster.Framed(w, h, thicknessShare * 1.6f, StoneRim, StoneFace, thicknessShare * 0.30f, true, 0.12f, 0.0015f);
        public static byte[] Pod(int w, int h, VariantId? variant, bool waiting, bool locked)
        {
            Rgba color = locked ? Rgba.FromHex("#83978C") : variant.HasValue && VariantCatalog.Default.TryGet(variant.Value, out VariantInfo info) ? Rgba.FromHex(info.ColorHex) : Rgba.FromHex("#B99ACC");
            var rim = new ColorSet("gameplay.pod.rim", color.Lighten(0.12f), color.Lighten(0.72f), color.Darken(0.38f), color.Darken(0.25f));
            var face = new ColorSet("gameplay.pod.face", color.Lighten(waiting ? 0.84f : 0.48f), color.Lighten(waiting ? 0.94f : 0.79f), color.Darken(0.15f), color.Lighten(0.30f));
            return ShopBodyRaster.Framed(w, h, 0.19f, rim, face, 0.10f, true, 0.17f);
        }
    }
}
