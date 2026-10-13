using System;
using Bloomlings.Client.Art;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>Shared profile material and live labels; all skins preserve their source alpha and use nine-slice.</summary>
    public static class ProfileGameView
    {
        public static Image Skin(string name,Transform parent,string asset)
        {
            Image image=UiFactory.CreateImage(name,parent,ShopArt.Get(asset),Color.white);
            BoxLayout.On(image.rectTransform).Then(b=>
            {
                float source=asset==ProfileGameDesign.Gold ? 1054*.16f : ShopCatalogDesign.FrameCrop(asset,2172,724).Height*ShopCatalogDesign.Borders(asset).Y;
                float target=asset==ProfileGameDesign.Gold ? .16f : ShopCatalogDesign.Borders(asset).TargetY;
                image.pixelsPerUnitMultiplier=source/Mathf.Max(.01f,b.Height*target);
            });
            return image;
        }
        public static TextMeshProUGUI Label(string name,Transform parent,string text,TypeStyle type,TextLook? look=null,TextAlignmentOptions align=TextAlignmentOptions.Center)
        {
            var label=UiKit.Label(name,parent,text,type,UiTheme.Of((look??ProfileGameDesign.Ink).FillTop),align,look??ProfileGameDesign.Ink);
            return label;
        }
        public static Image Price(Transform parent,string value,Action? tap=null,bool petals=true)
        {
            Image button=UiFactory.CreateImage("Purchase",parent,ShopArt.Get("shop/price-button"),Color.white,raycast:tap!=null);
            if(tap!=null)
            {
                Image touch=UiFactory.CreateImage("Touch",button.transform,null,Color.clear,raycast:true);UiKit.TapTarget(touch,tap,press:true);
                BoxLayout.On(button.rectTransform).Add(touch.rectTransform,b=>Box.FromCenter(b.CenterX,b.CenterY,Mathf.Max(b.Width,UiKit.Units(DesignTokens.Size.TouchMin)),Mathf.Max(b.Height,UiKit.Units(DesignTokens.Size.TouchMin))));
            }
            var text=Label("Price",button.transform,value,ShopBodyDesign.Price,ShopBodyDesign.MoneyLetters);
            Image? lotus=petals ? UiKit.PetalIcon("Petals",button.transform) : null;
            BoxLayout.On(button.rectTransform).Then(b=>
            {
                button.pixelsPerUnitMultiplier=190/Mathf.Max(.01f,b.Height*.4f);
                float size=b.Height*.54f;
                Box label=b.Inset(b.Height*.2f);
                if(lotus!=null){var parts=ShopCatalogDesign.PriceParts(b,KitText.Measure(text,size));BoxLayout.Place(lotus.rectTransform,parts.Petal);label=parts.Label;}
                KitText.Place(text,ShopBodyDesign.Price,label.CenterX,label.CenterY,size,label.Width);
            });return button;
        }
        public static RectTransform Check(Transform parent)
        {
            Image white=UiKit.RoundRect("SelectedCheck",parent,Color.white,b=>b.Width*.5f);
            Image green=UiKit.RoundRect("Green",white.transform,UiTheme.Of(ShopCatalogDesign.ChosenRim.Face),b=>b.Width*.5f);
            Image tick=UiFactory.CreateImage("Tick",white.transform,ProceduralSprites.Shape("ui.check"),Color.white);
            BoxLayout.On(white.rectTransform).Add(green.rectTransform,b=>b.Inset(b.Width*.045f)).Add(tick.rectTransform,b=>b.Inset(b.Width*.24f));return white.rectTransform;
        }
        public static RectTransform Portrait(Transform parent,AvatarItem avatar)
        {
            Image mask=UiKit.RoundRect("Portrait",parent,Color.white,b=>b.Width*.12f);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            RectTransform rawRoot=UiFactory.Stretch(UiFactory.CreateRect("Image",mask.transform));
            RawImage raw=rawRoot.gameObject.AddComponent<RawImage>();raw.texture=OwnerArt.Avatar(avatar.Picture);raw.raycastTarget=false;
            return mask.rectTransform;
        }
        public static string ItemName(CosmeticItem item)
        {
            string key=item.Id.StartsWith(CosmeticCatalog.AchievementBadgePrefix,StringComparison.Ordinal) ? "profile.achievement."+item.Id.Substring(CosmeticCatalog.AchievementBadgePrefix.Length) : "cosmetic."+item.Id;
            string translated=Localization.Loc.T(key);return translated==key ? item.Name : translated;
        }
        public static Sprite? Badge(CosmeticItem item)
        {
            string? asset=ShopCatalogDesign.Accessory(item.Id);
            if(asset!=null)return ShopArt.Get(asset);
            if(item.Id.StartsWith(CosmeticCatalog.AchievementBadgePrefix,StringComparison.Ordinal))return OwnerArt.Badge(item.Id.Substring(CosmeticCatalog.AchievementBadgePrefix.Length));
            if(item.Id=="badge.sprout")return OwnerArt.Badge("first_bloom");
            if(item.Id=="badge.golden_leaf")return OwnerArt.Badge("young_gardener");
            return ProceduralSprites.Accessory(item.Shape);
        }
        public static Color BadgeColor(CosmeticItem item) => ShopCatalogDesign.Accessory(item.Id)!=null||item.Id.StartsWith(CosmeticCatalog.AchievementBadgePrefix,StringComparison.Ordinal)||item.Id=="badge.sprout"||item.Id=="badge.golden_leaf" ? Color.white : global::Bloomlings.Client.Gameplay.Workers.BloomlingFigure.Tint(item);
        public static RectTransform NoBadge(Transform parent)
        {
            Image disc=UiKit.RoundRect("NoBadge",parent,UiTheme.Of(ShopBodyDesign.Well.Face),b=>b.Width*.5f);
            Image line=UiKit.RoundRect("Dash",disc.transform,UiTheme.Of(ShopBodyDesign.InsetBorder.Line),b=>b.Height*.5f);
            BoxLayout.On(disc.rectTransform).Add(line.rectTransform,b=>Box.FromCenter(b.CenterX,b.CenterY,b.Width*.30f,b.Width*.075f));return disc.rectTransform;
        }
    }
}
