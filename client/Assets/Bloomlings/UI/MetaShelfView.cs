using System;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI
{
    /// <summary>Independent illustrated surfaces and live labels, including nine-slice name plates.</summary>
    public static class MetaShelfView
    {
        public static Image Skin(string name, Transform parent, string asset)
        {
            Image image = UiFactory.CreateImage(name, parent, ShopArt.Get(asset), Color.white);
            var border = MetaScreenArt.Borders(asset);
            image.type = border.X > 0 ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = border.X == 0 && asset != MetaScreenArt.Shelf;
            if (border.X > 0)
                BoxLayout.On(image.rectTransform).Then(b =>
                {
                    Sprite? sprite = image.sprite;
                    if (sprite != null) image.pixelsPerUnitMultiplier = (asset == MetaScreenArt.Plank ? 185f : 136f) / Mathf.Max(.01f, b.Height);
                });
            return image;
        }
        public static TextMeshProUGUI Label(string name, Transform parent, string text, TypeStyle style, TextLook? look = null) =>
            GameNavigationView.Label(name, parent, text, style, look ?? ShopBodyDesign.DarkLetters, .80f);

        public static RectTransform Check(Transform parent)
        {
            Image outer = UiKit.RoundRect("Equipped", parent, Color.white, b => b.Width * .5f);
            Image inner = UiKit.RoundRect("Green", outer.transform, UiTheme.Of(ShopCatalogDesign.ChosenRim.Face), b => b.Width * .5f);
            Image mark = UiFactory.CreateImage("Check", outer.transform, Bloomlings.Client.Art.ProceduralSprites.Shape("ui.check"), Color.white);
            BoxLayout.On(outer.rectTransform).Add(inner.rectTransform, b => b.Inset(b.Width*.045f)).Add(mark.rectTransform, b => b.Inset(b.Width*.24f));
            return outer.rectTransform;
        }

        public static Image Price(Transform parent, string text)
        {
            Image skin = UiFactory.CreateImage("Price", parent, ShopArt.Get("shop/price-button"), Color.white);
            skin.type = Image.Type.Sliced;
            Image petal = UiKit.PetalIcon("Petals", skin.transform);
            TextMeshProUGUI label = Label("PriceValue", skin.transform, text, ShopBodyDesign.Price, ShopBodyDesign.MoneyLetters);
            BoxLayout.On(skin.rectTransform).Watch(label).Then(b =>
            {
                skin.pixelsPerUnitMultiplier = 190f / Mathf.Max(.01f,b.Height*.4f);
                var parts = ShopCatalogDesign.PriceParts(b, KitText.Measure(label,b.Height*.54f));
                BoxLayout.Place(petal.rectTransform, parts.Petal);
                KitText.Place(label, ShopBodyDesign.Price, parts.Label.CenterX, parts.Label.CenterY, b.Height*.54f, parts.Label.Width);
            });
            return skin;
        }
    }
}
