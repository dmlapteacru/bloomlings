using System;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Shared illustrated headers and icon-bearing secondary navigation.</summary>
    public static class NavigationSkin
    {
        public const float SignBorderX = 0.28f;
        public const float SignBorderY = 0.16f;
        public const string CreamBar = "navigation/cream-bar-v1";
        public const string CreamSelected = "navigation/cream-selected-v1";
        public const float CreamBarBorderX = 0.08f;
        public const float CreamSelectedBorderX = 0.15f;
        public const float CreamBorderY = 0.40f;
        public const float TabTextHeight = 0.42f;
        public static (int Left, int Top, int Width, int Height) CreamCrop(string name, int w, int h)
        {
            Box b = name == CreamBar ? new Box(42,214,2131,472) : new Box(76,105,2097,596);
            return ((int)(b.Left*w/2172), (int)(b.Top*h/724), (int)(b.Width*w/2172), (int)(b.Height*h/724));
        }
        public static Box Segment(Box bar, int index, int count)
        {
            Box inner = bar.Inset(bar.Height * 0.065f);
            float width = inner.Width / Math.Max(1, count);
            return new Box(inner.Left + width*index, inner.Top, inner.Left + width*(index+1), inner.Bottom);
        }
        public static Box SelectedFace(Box cell) => cell.Inset(cell.Height * 0.018f, cell.Height * 0.018f);
        public static Box Divider(Box bar, int index, int count)
        {
            Box inner = bar.Inset(bar.Height * 0.065f);
            return Box.FromCenter(inner.Left + inner.Width*index/Math.Max(1,count), bar.CenterY, bar.Height*0.022f, bar.Height*0.62f);
        }
        public static readonly Rgba DividerInk = Rgba.FromHex("#B99061").WithAlpha(0.55f);
        public static readonly Rgba DividerLight = Rgba.FromHex("#FFF9DF").WithAlpha(0.75f);
        public static TextLook TabLook(bool selected) => selected
            ? new TextLook(Rgba.White, Rgba.FromHex("#FFF8E8"), Rgba.FromHex("#24551A"), 0.055f, 0.015f, 0.06f)
            : TextLook.Plain(Rgba.FromHex("#60351B"));
        public static (Box Icon, Box Label) TabContent(Box box, float measuredLabelWidth)
        {
            float padding = box.Height*0.22f, gap = box.Height*0.10f;
            float side = Math.Min(box.Height*0.64f, box.Width*0.24f);
            float available = Math.Max(1, box.Width - padding*2 - side - gap);
            float width = measuredLabelWidth > 0 ? Math.Min(available, measuredLabelWidth) : available;
            float left = box.CenterX - (side + gap + width)*0.5f;
            return (Box.FromCenter(left+side*0.5f,box.CenterY-box.Height*0.018f,side,side),
                Box.FromCenter(left+side+gap+width*0.5f,box.CenterY-box.Height*0.018f,width,box.Height*0.68f));
        }
        public const float ButtonBorderX = 0.17f;
        public const float ButtonBorderY = 0.32f;
        public const float ButtonTargetBorderY = 0.34f;
        private static readonly Box[] ButtonBounds = {
            new Box(6,24,774,264), new Box(766,25,1534,265),
            new Box(6,263,774,503), new Box(766,265,1534,505),
            new Box(6,508,774,748), new Box(766,510,1534,750),
            new Box(6,753,774,993), new Box(765,754,1533,994) };
        public static (int Left, int Top, int Width, int Height) ButtonCrop(string name, int w, int h)
        {
            string[] parts = name.Substring("navigation/button-".Length).Split('-');
            Box b = ButtonBounds[int.Parse(parts[0]) * 2 + int.Parse(parts[1])];
            return ((int)(b.Left*w/1536), (int)(b.Top*h/1024), (int)(b.Width*w/1536), (int)(b.Height*h/1024));
        }
        public static (int Left, int Top, int Width, int Height) PlusCrop(int w, int h) =>
            ((int)(193f*w/1254), (int)(188f*h/1254), (int)(867f*w/1254), (int)(869f*h/1254));
        public static readonly TypeStyle Title = new TypeStyle("type.shop.navigation_title", 76, true, false, 0, 18);
        public static readonly TypeStyle Tab = new TypeStyle("type.shop.navigation_tab", 44, true, false, 0, 12);
        public static readonly TypeStyle Balance = new TypeStyle("type.shop.navigation_balance", 46, true, false, 0, 14);
        public static TextLook TitleLetters => new TextLook(Rgba.FromHex("#FFF8E9"), Rgba.FromHex("#FFF0D3"), Rgba.FromHex("#542713"), 0.070f, 0.02f, 0.09f);
        public static TextLook TabLetters => new TextLook(Rgba.White, Rgba.FromHex("#FFF8E8"), Rgba.FromHex("#292454"), 0.070f, 0.02f, 0.09f);
        public static readonly ColorSet Green = new ColorSet("nav.green", Rgba.FromHex("#4CAF25"), Rgba.FromHex("#A7E65B"), Rgba.FromHex("#246F16"), Rgba.FromHex("#1D5914"));
        private static readonly ColorSet[] Colors = {
            new ColorSet("nav.blue", Rgba.FromHex("#686CD2"), Rgba.FromHex("#AFB7FF"), Rgba.FromHex("#3E3F87"), Rgba.FromHex("#36316D")),
            new ColorSet("nav.purple", Rgba.FromHex("#8070CE"), Rgba.FromHex("#C7B7FC"), Rgba.FromHex("#51418E"), Rgba.FromHex("#423272")),
            new ColorSet("nav.violet", Rgba.FromHex("#7568C4"), Rgba.FromHex("#BAB4FA"), Rgba.FromHex("#494083"), Rgba.FromHex("#383169")) };
        private static readonly ColorSet ActiveRim = new ColorSet("nav.rim.active", Rgba.FromHex("#FFE35F"), Rgba.FromHex("#FFFBD1"), Rgba.FromHex("#A88627"), Rgba.FromHex("#8A7721"));
        private static readonly ColorSet IdleRim = new ColorSet("nav.rim.idle", Rgba.FromHex("#AAA0E9"), Rgba.FromHex("#E4DFFF"), Rgba.FromHex("#504084"), Rgba.FromHex("#46376F"));
        public static ColorSet Color(int index, bool selected) => selected ? Green : Colors[Math.Abs(index) % Colors.Length];
        public static byte[] Button(int w, int h, int index, bool selected) => ShopBodyRaster.Framed(w, h, 0.48f, selected ? ActiveRim : IdleRim, Color(index, selected), 0.05f, true, 0.22f, 0.006f, 0.7f);
        public static string Sign(TypeStyle style) => style == DesignTokens.Type.LevelPill || style == DesignTokens.Type.LevelHome ? "navigation/level-sign-v1" : "navigation/page-sign-v1";
        public static float SignTextShare(TypeStyle style) => style == DesignTokens.Type.LevelPill || style == DesignTokens.Type.LevelHome ? 0.62f : 0.46f;
        public static float SignRoom(Box box) => Math.Max(box.Width * 0.40f, box.Width - box.Height * 1.30f);
        public static Box TabIcon(Box box) { float s = Math.Min(box.Height * 0.72f, box.Width * 0.24f); return Box.FromCenter(box.Left + box.Height * 0.14f + s * 0.5f, box.CenterY - box.Height * 0.025f, s, s); }
        public static Box TabLabel(Box box) { Box icon = TabIcon(box); return new Box(icon.Right + box.Height * 0.06f, box.Top + box.Height * 0.14f, box.Right - box.Height * 0.14f, box.Bottom - box.Height * 0.18f); }
        public static Box FamilyButton(Box box) => Box.FromCenter(box.CenterX, box.CenterY, box.Width, Math.Min(box.Height, box.Width * 0.46f));
        public static Box FamilyBar(Box tabs, int count) => Box.FromCenter(tabs.CenterX,tabs.CenterY,tabs.Width,Math.Min(tabs.Height,tabs.Width/Math.Max(1,count)*0.46f));
        public static string FamilyIcon(Family family) => family switch { Family.Sprig => "gameplay/pod-leaf", Family.Bloom => "gameplay/pod-flower", Family.Drop => "gameplay/pod-water", _ => "gameplay/pod-acorn" };
        public static string Icon(string key) => "navigation/icon-" + (key switch
        {
            "store.tab_shop" => "packs",
            "store.tab_cosmetics" => "cosmetics",
            "store.tab_animations" => "animations",
            "profile.tab_avatar" => "avatar",
            "wardrobe.tab_profile" => "avatar",
            "profile.tab_frame" => "frame",
            "wardrobe.kind_frame" => "frame",
            "profile.tab_badge" => "badge",
            "wardrobe.kind_badge" => "badge",
            "profile.tab_name" => "name",
            "wardrobe.kind_hat" => "hat",
            "wardrobe.kind_trail" => "trail",
            "wardrobe.kind_expression" => "expression",
            "wardrobe.kind_skin" => "skin",
            "wardrobe.kind_marker" => "marker",
            _ => key.ToLowerInvariant()
        });
        private static readonly string[] Keys = { "packs", "cosmetics", "animations", "avatar", "frame", "badge", "hat", "trail", "skin", "expression", "marker", "name" };
        // Full-atlas subject bounds: a shirt sleeve and the marker slightly cross the nominal grid cells.
        private static readonly Box[] Bounds = { new Box(35, 33, 355, 353), new Box(398, 68, 746, 344), new Box(791, 40, 1047, 354), new Box(1116, 51, 1405, 360), new Box(45, 389, 349, 674), new Box(430, 398, 690, 693), new Box(751, 419, 1072, 678), new Box(1124, 398, 1402, 664), new Box(36, 722, 353, 1028), new Box(414, 727, 701, 1014), new Box(785, 711, 1074, 1025), new Box(1159, 724, 1399, 1015) };
        public static (int Left, int Top, int Width, int Height) IconCrop(string name, int w, int h)
        {
            int i = Array.IndexOf(Keys, name.Substring("navigation/icon-".Length));
            Box b = i < 0 ? new Box(0, 0, 1448, 1086) : Bounds[i];
            return ((int)(b.Left * w / 1448), (int)(b.Top * h / 1086), (int)(b.Width * w / 1448), (int)(b.Height * h / 1086));
        }
        public static string Resource(string name) => "navigation/" + (name.StartsWith("navigation/icon-", StringComparison.Ordinal) ? "tab-icons-v1" : name.StartsWith("navigation/button-", StringComparison.Ordinal) ? "button-skins-v2" : name.Substring(11)) + ".png";
    }
}
