using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>Renders the shared shop composition as real uGUI images, text and buttons.</summary>
    public static class ShopBodyView
    {
        public static void Draw(Transform parent, Box parentBox, IReadOnlyList<ShopVisual> nodes, Func<string, StoreItem?> item, Action<StoreItem> tap)
        {
            foreach (ShopVisual n in nodes)
            {
                if (n.Kind == ShopVisualKind.Text) { Text(parent, parentBox, n); continue; }
                if (n.Kind == ShopVisualKind.Sprite)
                {
                    Sprite? sprite = n.Value.StartsWith("icon/", StringComparison.Ordinal) ? OwnerArt.Icon(n.Value.Substring(5)) : ShopArt.Get(n.Value);
                    if (sprite == null) continue;
                    Image art = UiFactory.CreateImage("ShopArt", parent, sprite, Color.white);
                    art.preserveAspect = true;
                    UiKit.PlaceBox(art.rectTransform, n.Box, parentBox);
                    continue;
                }
                if (n.Kind == ShopVisualKind.Divider)
                {
                    Image line = UiKit.RoundRect("VineDivider", parent, UiTheme.Of(ShopBodyDesign.Green.Face), _ => n.Box.Height * 0.5f);
                    UiKit.PlaceBox(line.rectTransform, n.Box, parentBox);
                    continue;
                }
                bool painted = n.Kind == ShopVisualKind.Button && !n.Dark;
                Image surface = UiFactory.CreateImage("Shop" + n.Kind, parent, painted ? ShopArt.Get("shop/price-button") : null, Color.white, n.Kind == ShopVisualKind.Button);
                if (painted) surface.pixelsPerUnitMultiplier = 190f / (n.Box.Height / UiKit.PixelsPerUnit * 0.4f);
                ShopVisualKind surfaceKind = n.Kind == ShopVisualKind.Button && n.Dark ? ShopVisualKind.Well : n.Kind;
                if (!painted) PictureFit.On(surface, (w, h) => ProceduralSprites.Picture("shop.surface/" + surfaceKind, w, h, (rw, rh) => ShopBodyDesign.Surface(surfaceKind, rw, rh)));
                UiKit.PlaceBox(surface.rectTransform, n.Box, parentBox);
                if (n.Kind != ShopVisualKind.Button) continue;
                StoreItem? offer = item(n.Id);
                if (offer == null) continue;
                Button button = surface.gameObject.AddComponent<Button>();
                button.targetGraphic = surface;
                button.transition = Selectable.Transition.None;
                button.interactable = offer.Enabled || offer.PreviewOnly;
                if (button.interactable)
                {
                    StoreItem chosen = offer;
                    button.onClick.AddListener(() => tap(chosen));
                    surface.gameObject.AddComponent<PressMotion>();
                }
                else surface.gameObject.AddComponent<CanvasGroup>().alpha = 0.55f;
                Box textBox = n.Box.Inset(n.Box.Width * 0.055f, n.Box.Height * 0.055f);
                if (n.Dark)
                {
                    Image petals = UiKit.PetalIcon("Petals", surface.transform);
                    Box icon = new Box(n.Box.Left + n.Box.Width * 0.04f, n.Box.Top + n.Box.Height * 0.1f, n.Box.Left + n.Box.Width * 0.24f, n.Box.Bottom - n.Box.Height * 0.1f);
                    UiKit.PlaceBox(petals.rectTransform, icon, n.Box);
                    textBox = new Box(n.Box.Left + n.Box.Width * 0.25f, textBox.Top, textBox.Right, textBox.Bottom);
                }
                Text(surface.transform, n.Box, n with { Kind = ShopVisualKind.Text, Box = textBox });
                float min = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
                Image hit = UiFactory.CreateImage("Touch", surface.transform, null, Color.clear, raycast: true);
                UiKit.PlaceBox(hit.rectTransform, Box.FromCenter(n.Box.CenterX, n.Box.CenterY, Math.Max(n.Box.Width, min), Math.Max(n.Box.Height, min)), n.Box);
            }
        }
        private static void Text(Transform parent, Box parentBox, ShopVisual n)
        {
            TypeStyle type = n.Type ?? ShopBodyDesign.Title;
            TextLook look = n.Dark ? ShopBodyDesign.DarkLetters : ShopBodyDesign.Letters;
            TextMeshProUGUI label = UiKit.Label("ShopLabel", parent, n.Value, type, UiTheme.Of(look.FillTop), look: look);
            float pixels = Math.Min(type.Size * DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height), n.Box.Height * 0.79f);
            label.fontSizeMax = pixels / UiKit.PixelsPerUnit;
            label.fontSize = label.fontSizeMax;
            label.fontSizeMin = Math.Min(label.fontSizeMax, 14f);
            UiKit.PlaceBox(label.rectTransform, n.Box, parentBox);
        }
    }
}
