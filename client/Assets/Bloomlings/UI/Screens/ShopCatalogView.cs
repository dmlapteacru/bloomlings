using System;
using Bloomlings.Client.Art;
using Bloomlings.Client.Meta.Clearing;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>Live hero/board content inside generated nine-slice catalog skins.</summary>
    public static class ShopCatalogView
    {
        public static RectTransform Create(Transform parent,bool cosmetics,bool wide,string name,ClearingAction state,int price,bool selected,bool free,Action? tap,Action<RectTransform> preview,string? status=null)
        {
            Image hit=UiFactory.CreateImage("CatalogCard",parent,null,Color.clear,raycast:tap!=null);
            RectTransform root=hit.rectTransform;
            if(tap!=null){UiKit.TapTarget(hit,tap);root.gameObject.AddComponent<PressMotion>();}
            string asset=cosmetics ? ShopCatalogDesign.CosmeticsFrame : selected ? ShopCatalogDesign.SelectedFrame : ShopCatalogDesign.AnimationFrame;
            Image frame=UiFactory.CreateImage("Frame",root,ShopArt.Get(asset),Color.white);
            RectTransform picture=UiFactory.CreateRect("Picture",root);
            picture.gameObject.AddComponent<RectMask2D>();
            preview(picture);
            TextMeshProUGUI title=UiKit.KitLabel("Name",root,name,ShopCatalogDesign.Name,ShopCatalogDesign.NameLook(cosmetics));
            bool money=state==ClearingAction.Buy||state==ClearingAction.Locked;
            Image button=UiFactory.CreateImage("Action",root,money ? ShopArt.Get("shop/price-button") : null,Color.white);
            if(!money)PictureFit.On(button,(w,h)=>ProceduralSprites.Picture("shop.catalog.action/"+(state==ClearingAction.Chosen&&status==null),w,h,(pw,ph)=>ShopCatalogDesign.ActionSurface(pw,ph,state==ClearingAction.Chosen&&status==null)));
            if(state==ClearingAction.Locked)button.gameObject.AddComponent<CanvasGroup>().alpha=.62f;
            TextLook actionLook=money ? ShopBodyDesign.MoneyLetters : TextLook.Plain(state==ClearingAction.Chosen&&status==null ? ShopCatalogDesign.ChosenRim.Line : ShopBodyDesign.Ink);
            TextMeshProUGUI label=UiKit.KitLabel("ActionLabel",button.transform,money ? NumberText.Group(price) : status??Loc.T(state==ClearingAction.Chosen ? "clearing.chosen" : "clearing.choose"),ShopCatalogDesign.ActionType,actionLook);
            Image? lotus=money ? UiKit.PetalIcon("Petals",button.transform) : null;
            Image? tick=!money&&state==ClearingAction.Chosen&&status==null ? UiFactory.CreateImage("Tick",button.transform,ProceduralSprites.Shape("ui.check"),UiTheme.Of(ShopCatalogDesign.ChosenRim.Face)) : null;
            RectTransform? check=null;
            if(cosmetics&&selected)
            {
                Image white=UiKit.RoundRect("SelectedCheck",root,Color.white,b=>b.Width*.5f);
                check=white.rectTransform;
                Image green=UiKit.RoundRect("Green",check,UiTheme.Of(ShopCatalogDesign.ChosenRim.Face),b=>b.Width*.5f);
                Image mark=UiFactory.CreateImage("Tick",check,ProceduralSprites.Shape("ui.check"),Color.white);
                BoxLayout.On(check).Add(green.rectTransform,b=>b.Inset(b.Width*.045f)).Add(mark.rectTransform,b=>b.Inset(b.Width*.24f));
            }
            RectTransform? locked=state==ClearingAction.Locked ? UiKit.LockBadge("Locked",root) : null;
            Image? ribbon=null; TextMeshProUGUI? freeLabel=null;
            if(free)
            {
                ribbon=UiFactory.CreateImage("FreeRibbon",root,null,Color.white);
                PictureFit.On(ribbon,(w,h)=>ProceduralSprites.Picture("shop.catalog.free",w,h,(pw,ph)=>ShopCatalogDesign.ActionSurface(pw,ph,false)));
                freeLabel=UiKit.KitLabel("Free",ribbon.transform,Loc.T("common.free").ToUpperInvariant(),ShopCatalogDesign.BadgeType,ShopBodyDesign.DarkLetters);
                BoxLayout.On(ribbon.rectTransform).Then(b=>KitText.Place(freeLabel,ShopCatalogDesign.BadgeType,b.CenterX,b.CenterY,b.Height*.62f,b.Width*.86f));
            }
            BoxLayout.On(root).Then(b=>
            {
                ShopCatalogParts parts=ShopCatalogDesign.Parts(b,cosmetics,wide);
                BoxLayout.Place(frame.rectTransform,b);
                if(frame.sprite!=null)
                {
                    var crop=ShopCatalogDesign.FrameCrop(asset,frame.sprite.texture.width,frame.sprite.texture.height);
                    var borders=ShopCatalogDesign.Borders(asset);
                    frame.pixelsPerUnitMultiplier=crop.Height*borders.Y/Mathf.Max(.01f,b.Height*borders.TargetY);
                }
                BoxLayout.Place(picture,parts.Picture);
                BoxLayout.Place(button.rectTransform,parts.Action);
                if(money)button.pixelsPerUnitMultiplier=190f/Mathf.Max(.01f,parts.Action.Height*.4f);
                KitText.Place(title,ShopCatalogDesign.Name,parts.Name.CenterX,parts.Name.CenterY,Mathf.Min(UiKit.Units(ShopCatalogDesign.Name.Size),parts.Name.Height*.77f),parts.Name.Width);
                if(check!=null)BoxLayout.Place(check,parts.Check);
                if(locked!=null)BoxLayout.Place(locked,parts.Check);
                if(ribbon!=null){BoxLayout.Place(ribbon.rectTransform,parts.FreeBadge);ribbon.transform.localEulerAngles=new Vector3(0,0,32);}
            });
            BoxLayout.On(button.rectTransform).Then(b=>
            {
                float size=b.Height*.54f;
                Box letters=b.Inset(b.Height*.24f);
                if(lotus!=null)
                {
                    var parts=ShopCatalogDesign.PriceParts(b,KitText.Measure(label,size));
                    BoxLayout.Place(lotus.rectTransform,parts.Petal);letters=parts.Label;
                }
                if(tick!=null)
                {
                    Box c=Box.FromCenter(b.Left+b.Height*.62f,b.CenterY,b.Height*.43f,b.Height*.43f);
                    BoxLayout.Place(tick.rectTransform,c);letters=new Box(c.Right+b.Height*.1f,b.Top,b.Right-b.Height*.25f,b.Bottom);
                }
                KitText.Place(label,ShopCatalogDesign.ActionType,letters.CenterX,letters.CenterY,size,letters.Width);
            });
            return root;
        }
    }
}
