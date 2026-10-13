using System;
using Bloomlings.Client.Meta.Clearing;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest.Design
{
    public static class ShopCatalogPainter
    {
        public static void Card(IPainter p,Box box,Box viewport,bool cosmetics,bool wide,string name,ClearingAction state,int price,bool selected,bool free,Action? tap,Action<Box> picture,string? status=null)
        {
            p.Mark("ui.shop.card"); p.Mark("ui.shop.illustrations"); p.Mark("font.shop");
            ShopCatalogParts parts=ShopCatalogDesign.Parts(box,cosmetics,wide);
            string frame=cosmetics ? ShopCatalogDesign.CosmeticsFrame : selected ? ShopCatalogDesign.SelectedFrame : ShopCatalogDesign.AnimationFrame;
            var borders=ShopCatalogDesign.Borders(frame);
            Kit.SlicedSprite(p,frame,box,borders.X,borders.Y,borders.TargetY);
            p.PushClipRound(parts.Picture,box.Width*.025f); picture(parts.Picture); p.PopClip();
            float font=Math.Min(p.U(ShopCatalogDesign.Name.Size),parts.Name.Height*.77f);
            TextLook look=ShopCatalogDesign.NameLook(cosmetics);
            if(parts.Name.Bottom>viewport.Top&&parts.Name.Top<viewport.Bottom)p.Text(name,parts.Name.CenterX,parts.Name.CenterY,ShopCatalogDesign.Name,look.FillTop,parts.Name.Width,font/p.U(ShopCatalogDesign.Name.Size),look);
            if(parts.Action.Bottom>viewport.Top&&parts.Action.Top<viewport.Bottom)ActionButton(p,parts.Action,state,price,status);
            if(cosmetics&&selected) Check(p,parts.Check);
            if(state==ClearingAction.Locked)
            {
                Kit.LockBadge(p,parts.Check.CenterX,parts.Check.CenterY,parts.Check.Width*.8f);
            }
            if(free&&parts.FreeBadge.Bottom>viewport.Top&&parts.FreeBadge.Top<viewport.Bottom)
            {
                p.PushRotate(-32,parts.FreeBadge.CenterX,parts.FreeBadge.CenterY);
                p.Picture("shop.catalog.free",parts.FreeBadge,(w,h)=>ShopCatalogDesign.ActionSurface(w,h,false));
                TextLook badge=ShopBodyDesign.DarkLetters;
                p.Text(PlaytestText.T("common.free").ToUpperInvariant(),parts.FreeBadge.CenterX,parts.FreeBadge.CenterY,ShopCatalogDesign.BadgeType,badge.FillTop,parts.FreeBadge.Width*.86f,parts.FreeBadge.Height*.62f/p.U(ShopCatalogDesign.BadgeType.Size),badge);
                p.PopTransform();
            }
            if(tap!=null)
            {
                Box hit=new Box(Math.Max(box.Left,viewport.Left),Math.Max(box.Top,viewport.Top),Math.Min(box.Right,viewport.Right),Math.Min(box.Bottom,viewport.Bottom));
                if(hit.Width>=p.U(DesignTokens.Size.TouchMin)*.95f&&hit.Height>=p.U(DesignTokens.Size.TouchMin)*.95f)p.Hit(hit,tap);
            }
        }
        private static void ActionButton(IPainter p,Box b,ClearingAction state,int price,string? status)
        {
            bool money=state==ClearingAction.Buy||state==ClearingAction.Locked;
            if(state==ClearingAction.Locked)p.PushAlpha(.62f);
            if(money)Kit.SlicedSprite(p,"shop/price-button",b,350f/1985,190f/505,.4f);
            else p.Picture("shop.catalog.action/"+(state==ClearingAction.Chosen&&status==null),b,(w,h)=>ShopCatalogDesign.ActionSurface(w,h,state==ClearingAction.Chosen&&status==null));
            float size=b.Height*.54f;
            if(money)
            {
                string value=NumberText.Group(price);
                var parts=ShopCatalogDesign.PriceParts(b,p.MeasureText(value,ShopCatalogDesign.ActionType,size/p.U(ShopCatalogDesign.ActionType.Size)));
                Kit.Petal(p,parts.Petal);
                p.Text(value,parts.Label.CenterX,parts.Label.CenterY,ShopCatalogDesign.ActionType,ShopBodyDesign.MoneyLetters.FillTop,parts.Label.Width,size/p.U(ShopCatalogDesign.ActionType.Size),ShopBodyDesign.MoneyLetters);
            }
            else
            {
                string value=status??PlaytestText.T(state==ClearingAction.Chosen ? "clearing.chosen" : "clearing.choose");
                Box label=b.Inset(b.Height*.24f);
                if(state==ClearingAction.Chosen&&status==null)
                {
                    Box check=Box.FromCenter(b.Left+b.Height*.62f,b.CenterY,b.Height*.43f,b.Height*.43f);
                    p.Shape("ui.check",check,ShopCatalogDesign.ChosenRim.Face);
                    label=new Box(check.Right+b.Height*.1f,b.Top,b.Right-b.Height*.25f,b.Bottom);
                }
                TextLook look=TextLook.Plain(state==ClearingAction.Chosen&&status==null ? ShopCatalogDesign.ChosenRim.Line : ShopBodyDesign.Ink);
                p.Text(value,label.CenterX,label.CenterY,ShopCatalogDesign.ActionType,look.FillTop,label.Width,size/p.U(ShopCatalogDesign.ActionType.Size),look);
            }
            if(state==ClearingAction.Locked)p.PopAlpha();
        }
        private static void Check(IPainter p,Box b)
        {
            p.FillCircle(b.CenterX,b.CenterY,b.Width*.5f,Rgba.White);
            p.FillCircle(b.CenterX,b.CenterY,b.Width*.455f,ShopCatalogDesign.ChosenRim.Face);
            p.Shape("ui.check",b.Inset(b.Width*.24f),Rgba.White);
        }
    }
}
