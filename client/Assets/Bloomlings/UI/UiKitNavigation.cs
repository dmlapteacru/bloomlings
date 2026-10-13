using System;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI
{
    public static partial class UiKit
    {
        internal static RectTransform NavigationBar(string name, Transform parent, int count)
        {
            RectTransform root = UiFactory.CreateRect(name, parent);
            Image face = UiFactory.CreateImage("Cream", root, NavigationArt.Get(NavigationSkin.CreamBar), Color.white);
            face.raycastTarget=true;
            var dark = new Image[Math.Max(0,count-1)];
            var light = new Image[dark.Length];
            for (int i=0;i<dark.Length;i++)
            {
                dark[i]=RoundRect("Divider"+i,root,UiTheme.Of(NavigationSkin.DividerInk),b=>b.Width*.5f);
                light[i]=RoundRect("DividerLight"+i,root,UiTheme.Of(NavigationSkin.DividerLight),b=>b.Width*.5f);
            }
            BoxLayout.On(root).Add(face.rectTransform,b=>b).Then(b=>
            {
                SetNavigationSlice(face,NavigationSkin.CreamBar,b);
                for(int i=0;i<dark.Length;i++)
                {
                    Box d=NavigationSkin.Divider(b,i+1,count);
                    BoxLayout.Place(dark[i].rectTransform,d);
                    BoxLayout.Place(light[i].rectTransform,new Box(d.Right,d.Top,d.Right+d.Width*.55f,d.Bottom));
                }
            });
            return root;
        }
        internal static void SetNavigationSlice(Image image, string name, Box b)
        {
            Sprite? sprite=image.sprite;
            if(sprite==null)return;
            var c=NavigationSkin.CreamCrop(name,sprite.texture.width,sprite.texture.height);
            image.type=Image.Type.Sliced;
            image.pixelsPerUnitMultiplier=c.Height/Mathf.Max(.01f,b.Height);
        }
        internal static GardenButton NavigationButton(string name, Transform parent, int index, bool selected, bool price = false)
        {
            GardenButton view = NewButton(name, parent, NavigationSkin.Color(index, selected), raycast: true);
            view.GreyWhenDisabled = false;
            string? surface = price ? GameNavigationDesign.Plus : selected ? NavigationSkin.CreamSelected : null;
            Sprite? sprite = surface == null ? null : NavigationArt.Get(surface);
            RectTransform face = UiFactory.Stretch(UiFactory.CreateRect("Face", view.Body));
            RectTransform topBox = UiFactory.Stretch(UiFactory.CreateRect("TopBox", face));
            Image top = UiFactory.CreateImage("Surface", topBox, sprite, !price && !selected ? Color.clear : Color.white);
            if (sprite != null)
            {
                top.type = price ? Image.Type.Simple : Image.Type.Sliced;
                top.preserveAspect = price;
            }
            BoxLayout topLayout = BoxLayout.On(topBox).Add(top.rectTransform, b => price ? b : NavigationSkin.SelectedFace(b));
            RectTransform content = UiFactory.Stretch(UiFactory.CreateRect("Content", face));
            view.BuildPictureFace(topBox, top, content, topLayout, _ =>
            {
                if (sprite == null && (price || selected)) PictureFit.On(top, (w, h) => ProceduralSprites.Picture("navigation.tab/" + index + "/" + selected, w, h, (pw, ph) => NavigationSkin.Button(pw, ph, index, selected)));
            });
            BoxLayout.On(view.Body).Then(b =>
            {
                if (sprite != null && !price)
                {
                    SetNavigationSlice(top,NavigationSkin.CreamSelected,NavigationSkin.SelectedFace(b));
                }
                view.SetGeometry(0, 0, b.Height * .035f, b.Height * .48f, 0, b.Height * .80f);
            });
            return view;
        }
        internal static void NavigationTabContent(GardenButton button, string label, string icon, bool selected)
        {
            Image art = UiFactory.CreateImage("Icon", button.Content, icon.StartsWith("gameplay/", StringComparison.Ordinal) ? GameplayArt.Pod(new Bloomlings.Core.Variants.VariantId(icon.Substring(13))) : NavigationArt.Get(icon), Color.white);
            art.preserveAspect = true;
            TextMeshProUGUI text = KitLabel("Label", button.Content, label, NavigationSkin.Tab, NavigationSkin.TabLook(selected));
            BoxLayout.On(button.Content).Watch(text).Then(b =>
            {
                float size=b.Height*NavigationSkin.TabTextHeight;
                var content=NavigationSkin.TabContent(b,KitText.Measure(text,size));
                BoxLayout.Place(art.rectTransform,content.Icon);
                Box t=content.Label; KitText.Place(text,NavigationSkin.Tab,t.CenterX,t.CenterY,size,t.Width);
            });
        }
    }
}
