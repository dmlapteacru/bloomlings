using System;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
namespace Bloomlings.Client.UI
{
    public static partial class UiKit
    {
        internal static void PriceButtonContent(RectTransform icon,TextMeshProUGUI text,TypeStyle type,Box f) => IconAndText(icon,text,type,f,f.Height*.5f,Units(14f),f.Width-f.Height*1.4f);
        internal static RectTransform PopupTray(string name,Transform parent) => CardFrame(name,parent,false).rectTransform;
        internal static void PopupCell(Transform parent,Box cell,Box host,string label,Action onClick,string? glyph=null,string? picture=null)
        {
            Image touch=UiFactory.CreateImage(label,parent,null,Color.clear,raycast:true); PlaceBox(touch.rectTransform,cell,host);TapTarget(touch,onClick,press:true);
            Image icon=picture != null ? UiFactory.CreateImage("Icon",touch.transform,OwnerArt.Icon(picture),Color.white) : RaisedGlyph(touch.transform,ProceduralSprites.RaisedGlyph(glyph ?? "ui.star",C.InkBrown));
            icon.preserveAspect=true;icon.raycastTarget=false; PlaceBox(icon.rectTransform,PopupDesign.CellIcon(cell),cell);
            TextMeshProUGUI text=Label("Label",touch.transform,label,PopupDesign.Label,UiTheme.Of(C.InkBrown),look:TextLook.Plain(C.InkBrown));
            PlaceBox(text.rectTransform,PopupDesign.CellLabel(cell),cell);
        }
    }
}
