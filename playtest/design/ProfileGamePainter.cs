using System;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest.Design
{
    public static class ProfileGamePainter
    {
        public static void Skin(IPainter p,Box box,string asset)
        {
            p.Mark("ui.shop.illustrations");p.Mark("ui.shop.card");
            var borders=asset==ProfileGameDesign.Gold ? (.16f,.16f,.16f) : ShopCatalogDesign.Borders(asset);
            Kit.SlicedSprite(p,asset,box,borders.Item1,borders.Item2,borders.Item3);
        }
        public static void Label(IPainter p,Box b,string value,TypeStyle type,TextLook? look=null,bool left=false,float heightShare=.72f)
        {
            p.Mark("font.shop");TextLook letters=look??ProfileGameDesign.Ink;
            float size=Math.Min(p.U(type.Size),b.Height*heightShare),scale=size/p.U(type.Size);
            float measured=p.MeasureText(value,type,scale);if(measured>b.Width)scale*=b.Width/measured;
            if(left)p.TextLeft(value,b.Left,b.CenterY,type,letters.FillTop,b.Width,scale,letters);
            else p.Text(value,b.CenterX,b.CenterY,type,letters.FillTop,b.Width,scale,letters);
        }
        public static void Price(IPainter p,Box b,string text,Action? tap=null,bool petals=true,Box? touch=null)
        {
            Box original = b;
            if (petals)
                b = ProfileGameDesign.PurchaseFace(b, p is IPurchaseSizePreview preview ? preview.PurchaseSize : ProfileGameDesign.PurchaseScale);
            if(!petals){Kit.PrimaryButton(p,b,text,tap,ShopBodyDesign.Price);return;}
            p.Mark("ui.shop.button");p.Mark("font.shop");Kit.SlicedSprite(p,"shop/price-button",b,350f/1985,190f/505,.4f);
            float size=b.Height*.54f;Box label=b.Inset(b.Height*.20f);
            if(petals){var parts=ShopCatalogDesign.PriceParts(b,p.MeasureText(text,ShopBodyDesign.Price,size/p.U(ShopBodyDesign.Price.Size)));Kit.Petal(p,parts.Petal);label=parts.Label;}
            float scale=size/p.U(ShopBodyDesign.Price.Size);float measured=p.MeasureText(text,ShopBodyDesign.Price,scale);if(measured>label.Width)scale*=label.Width/measured;
            p.Text(text,label.CenterX,label.CenterY,ShopBodyDesign.Price,ShopBodyDesign.MoneyLetters.FillTop,label.Width,scale,ShopBodyDesign.MoneyLetters);
            if(tap!=null)p.Hit(touch??original,tap);
        }
        public static void Check(IPainter p,Box b)
        {
            p.FillCircle(b.CenterX,b.CenterY,b.Width*.50f,Rgba.White);p.FillCircle(b.CenterX,b.CenterY,b.Width*.455f,ShopCatalogDesign.ChosenRim.Face);p.Shape("ui.check",b.Inset(b.Width*.24f),Rgba.White);
        }
        public static void Badge(IPainter p,Box b,CosmeticItem? badge)
        {
            if(badge==null){float side=Math.Min(b.Width,b.Height)*.74f;p.FillCircle(b.CenterX,b.CenterY,side*.5f,ShopBodyDesign.Well.Face);Box dash=Box.FromCenter(b.CenterX,b.CenterY,side*.30f,side*.075f);p.FillRound(dash,dash.Height*.5f,ShopBodyDesign.InsetBorder.Line);return;}
            string? asset=ShopCatalogDesign.Accessory(badge.Id);
            if(asset!=null&&p.HasSprite(asset)){p.Sprite(asset,b);return;}
            string? image=badge.Id.StartsWith(CosmeticCatalog.AchievementBadgePrefix,StringComparison.Ordinal) ? PainterBase.BadgePrefix+badge.Id.Substring(CosmeticCatalog.AchievementBadgePrefix.Length) : badge.Id=="badge.sprout" ? PainterBase.BadgePrefix+"first_bloom" : badge.Id=="badge.golden_leaf" ? PainterBase.BadgePrefix+"young_gardener" : null;
            if(image!=null&&p.HasSprite(image))p.Sprite(image,b);else p.Shape("cosmetic.badge",b,Visuals.Tint(badge));
        }
    }
}
