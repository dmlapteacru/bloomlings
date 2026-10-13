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
        internal static GardenButton NavigationButton(string name, Transform parent, int index, bool selected, bool price = false)
        {
            GardenButton view = NewButton(name, parent, NavigationSkin.Color(index, selected), raycast: true);
            view.GreyWhenDisabled = false;
            string? surface = price ? "navigation/plus-v2" : NavigationSkin.ButtonSprite(index, selected);
            Sprite? sprite = surface == null ? null : NavigationArt.Get(surface);
            if (selected && !price && sprite == null)
            {
                Image glow = RoundRect("SelectedGlow", view.Body, UiTheme.Of(ShopBodyDesign.BadgeLight.WithAlpha(.30f)), b => b.Height * .52f);
                BoxLayout.On(view.Body).Add(glow.rectTransform, b => b.Inset(-b.Height * .025f));
            }
            RectTransform face = UiFactory.Stretch(UiFactory.CreateRect("Face", view.Body));
            RectTransform topBox = UiFactory.Stretch(UiFactory.CreateRect("TopBox", face));
            Image top = UiFactory.CreateImage("Surface", topBox, sprite, Color.white);
            if (sprite != null)
            {
                top.type = price ? Image.Type.Simple : Image.Type.Sliced;
                top.preserveAspect = price;
            }
            BoxLayout topLayout = BoxLayout.On(topBox).Add(top.rectTransform, b => b);
            RectTransform content = UiFactory.Stretch(UiFactory.CreateRect("Content", face));
            view.BuildPictureFace(topBox, top, content, topLayout, _ =>
            {
                if (sprite == null) PictureFit.On(top, (w, h) => ProceduralSprites.Picture("navigation.tab/" + index + "/" + selected, w, h, (pw, ph) => NavigationSkin.Button(pw, ph, index, selected)));
            });
            BoxLayout.On(view.Body).Then(b =>
            {
                if (sprite != null && !price)
                {
                    var crop = NavigationSkin.ButtonCrop(surface!, sprite.texture.width, sprite.texture.height);
                    top.pixelsPerUnitMultiplier = crop.Height * NavigationSkin.ButtonBorderY / Mathf.Max(.01f, b.Height * NavigationSkin.ButtonTargetBorderY);
                }
                view.SetGeometry(0, 0, b.Height * .035f, b.Height * .48f, 0, b.Height * .80f);
            });
            return view;
        }
        internal static void NavigationTabContent(GardenButton button, string label, string icon)
        {
            Image art = UiFactory.CreateImage("Icon", button.Content, icon.StartsWith("gameplay/", StringComparison.Ordinal) ? GameplayArt.Pod(new Bloomlings.Core.Variants.VariantId(icon.Substring(13))) : NavigationArt.Get(icon), Color.white);
            art.preserveAspect = true;
            TextMeshProUGUI text = KitLabel("Label", button.Content, label, NavigationSkin.Tab, NavigationSkin.TabLetters);
            BoxLayout.On(button.Content).Add(art.rectTransform, NavigationSkin.TabIcon).Watch(text).Then(b =>
            {
                Box t = NavigationSkin.TabLabel(b); KitText.Place(text, NavigationSkin.Tab, t.CenterX, t.CenterY, t.Height * .78f, t.Width);
            });
        }
    }
}
