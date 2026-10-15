using System;

namespace Bloomlings.Client.UI.Design
{
    public sealed record ShopCatalogParts(Box Frame, Box Picture, Box Name, Box Action, Box Check, Box FreeBadge, bool Wide);

    /// <summary>Approved Cosmetics and Animations compositions; shared by Unity and the device painter.</summary>
    public static class ShopCatalogDesign
    {
        public const string CosmeticsFrame = "shop/catalog-cosmetics";
        public const string AnimationFrame = "shop/catalog-animation";
        public const string SelectedFrame = "shop/catalog-selected";
        public static readonly TypeStyle Name = new TypeStyle("type.shop.catalog_name", 44, true, false, 0, 18);
        public static readonly TypeStyle ActionType = new TypeStyle("type.shop.catalog_action", 48, true, false, 0, 18);
        public static readonly TypeStyle BadgeType = new TypeStyle("type.shop.catalog_badge", 32, true, false, 0, 16);
        public static TextLook NameLook(bool cosmetics) => cosmetics ? ShopBodyDesign.Letters : TextLook.Plain(ShopBodyDesign.Ink);
        public static readonly ColorSet ChosenRim = new ColorSet("shop.chosen", Rgba.FromHex("#67BC39"), Rgba.FromHex("#D0F98A"), Rgba.FromHex("#348121"), Rgba.FromHex("#2C6A22"));
        public static Box FamilyTabs(ReferenceStoreRegions r) => new Box(r.Tabs.Left, r.Tabs.Bottom+r.W*.045f, r.Tabs.Right, r.Tabs.Bottom+r.W*.130f);
        public static Box Viewport(ReferenceStoreRegions r, bool cosmetics)
        {
            Box area=ShopBodyDesign.Bounds(r);
            return new Box(area.Left,cosmetics ? FamilyTabs(r).Bottom+r.W*.028f : r.Tabs.Bottom+r.W*.028f,area.Right,area.Bottom);
        }
        public static Box CosmeticCard(Box area, int index)
        {
            float gap=area.Width*.028f, width=(area.Width-gap)*.5f, height=width*1.10f;
            float x=area.Left+(index%2)*(width+gap), y=area.Top+(index/2)*(height+gap);
            return new Box(x,y,x+width,y+height);
        }
        public static float CosmeticsHeight(Box area,int count) => Math.Max(area.Height, CosmeticCard(area,Math.Max(0,count-1)).Bottom-area.Top+area.Width*.035f);
        public static Box AnimationCard(Box area,int index)
        {
            float gap=area.Width*.028f, width=(area.Width-gap)*.5f;
            float wide=width*.65f;
            float height=width*1.06f;
            if(index==6) return new Box(area.Left,area.Top+3*(height+gap),area.Right,area.Top+3*(height+gap)+wide);
            float x=area.Left+(index%2)*(width+gap), y=area.Top+(index/2)*(height+gap);
            return new Box(x,y,x+width,y+height);
        }
        public static Box Caption(Box area) { Box last=AnimationCard(area,6); return new Box(area.Left,last.Bottom+area.Width*.018f,area.Right,last.Bottom+area.Width*.075f); }
        public static float AnimationsHeight(Box area) => Math.Max(area.Height,Caption(area).Bottom-area.Top+area.Width*.025f);
        public static ShopCatalogParts Parts(Box b,bool cosmetics,bool wide=false)
        {
            Box picture=cosmetics ? In(b,.075f,.025f,.925f,.585f) : wide ? In(b,.035f,.065f,.57f,.925f) : In(b,.055f,.035f,.945f,.59f);
            Box name=wide ? In(b,.58f,.16f,.965f,.40f) : In(b,.06f,.59f,.94f,.71f);
            Box action=wide ? In(b,.58f,.46f,.97f,.92f) : In(b,.07f,.72f,.93f,.97f);
            float side=Math.Min(b.Width,b.Height)*.17f;
            Box check=Box.FromCenter(picture.Right-side*.38f,picture.Bottom-side*.52f,side,side);
            Box free=In(b,.025f,.075f,.34f,.19f);
            return new ShopCatalogParts(b,picture,name,ProfileGameDesign.PurchaseFace(action),check,free,wide);
        }
        public static (Box Petal,Box Label) PriceParts(Box b,float measured)
        {
            float icon=Math.Min(b.Height*.74f,b.Width*.26f), gap=b.Height*.06f, width=Math.Min(measured,Math.Max(1,b.Width-icon-gap-b.Height*.24f));
            float left=b.CenterX-(icon+gap+width)*.5f;
            return (Box.FromCenter(left+icon*.5f,b.CenterY,icon,icon),Box.FromCenter(left+icon+gap+width*.5f,b.CenterY,width,b.Height*.88f));
        }
        public static byte[] ActionSurface(int w,int h,bool chosen) => ShopBodyRaster.Framed(w,h,.48f,chosen ? ChosenRim : ShopBodyDesign.InsetBorder,GardenLook.Cream,.038f,true,.12f,.006f,.65f);
        public static (float X,float Y,float TargetY) Borders(string name) => name==CosmeticsFrame ? (.30f,.43f,.33f) : name==SelectedFrame ? (.20f,.21f,.18f) : (.16f,.18f,.18f);
        public static (int Left,int Top,int Width,int Height) FrameCrop(string name,int w,int h)
        {
            Box b=name==CosmeticsFrame ? new Box(41,36,723,674) : name==SelectedFrame ? new Box(1430,18,2143,690) : new Box(753,37,1419,673);
            return ((int)(b.Left*w/2172),(int)(b.Top*h/724),(int)(b.Width*w/2172),(int)(b.Height*h/724));
        }
        public static string? Accessory(string id) => MetaScreenArt.Hat(id) ?? (id switch
        {
            "hat.bucket_hat" => "shop/accessory-bucket",
            "hat.bell_cap" => "shop/accessory-bell",
            "frame.bramble" => "shop/accessory-bramble",
            "badge.watering_can" => "shop/accessory-watering",
            _ => null
        });
        public static (int Left,int Top,int Width,int Height) AccessoryCrop(string name,int w,int h)
        {
            Box b=name=="shop/accessory-bucket" ? new Box(35,159,656,573) : name=="shop/accessory-bell" ? new Box(686,57,1214,584) : name=="shop/accessory-bramble" ? new Box(50,639,639,1170) : new Box(692,672,1186,1162);
            return ((int)(b.Left*w/1254),(int)(b.Top*h/1254),(int)(b.Width*w/1254),(int)(b.Height*h/1254));
        }
        private static Box In(Box b,float l,float t,float r,float bottom) => new Box(b.Left+b.Width*l,b.Top+b.Height*t,b.Left+b.Width*r,b.Top+b.Height*bottom);
    }
}
