using Bloomlings.Client.Art;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Workers
{
    /// <summary>
    /// A Bloomling drawn from its family silhouette in a body color, with what it wears (FR-063): a skin pattern laid
    /// thinly over the body, a hat above it, an expression on its face and a trail behind it. The body color is never
    /// changed by a cosmetic, so on the board it stays the variant color. Used by the workers, the Wardrobe and Home.
    /// </summary>
    public sealed class BloomlingFigure
    {
        private readonly Image _body;
        private readonly Image _skin;
        private readonly Image _trail;
        private readonly Image _expression;
        private readonly Image _hat;

        private BloomlingFigure(Image body)
        {
            _body = body;
            _skin = Part("Skin", 0f, 0f, 1f, 1f);
            _trail = Part("Trail", -0.3f, -0.05f, 0.05f, 0.3f);
            _expression = Part("Expression", 0.33f, 0.3f, 0.67f, 0.55f);
            _hat = Part("Hat", 0.2f, 0.72f, 0.8f, 1.22f);
        }

        public Image Body => _body;

        public RectTransform Rect => _body.rectTransform;

        /// <summary>The trail's image, for the worker's sway.</summary>
        public RectTransform Trail => _trail.rectTransform;

        /// <summary>The hat's image, for the worker's hop.</summary>
        public RectTransform Hat => _hat.rectTransform;

        public static BloomlingFigure Create(string name, Transform parent)
        {
            Image body = UiFactory.CreateImage(name, parent, null, Color.white);
            body.preserveAspect = true;
            return new BloomlingFigure(body);
        }

        /// <summary>Draws the family in <paramref name="bodyColor"/> wearing <paramref name="outfit"/> (null: nothing).</summary>
        public void Show(Family family, Color bodyColor, Outfit? outfit)
        {
            _body.sprite = ProceduralSprites.Silhouette(family);
            _body.color = bodyColor;
            ShowSkin(family, outfit?.Skin);
            ShowAccessory(_trail, outfit?.Trail);
            ShowAccessory(_expression, outfit?.Expression);
            ShowAccessory(_hat, outfit?.Hat);
        }

        public static Color Tint(CosmeticItem item) => ColorUtility.TryParseHtmlString(item.Tint, out Color color) ? color : Color.white;

        private void ShowSkin(Family family, CosmeticItem? skin)
        {
            if (skin == null)
            {
                _skin.gameObject.SetActive(false);
                return;
            }

            _skin.sprite = ProceduralSprites.SkinPattern(family, skin.Shape);
            Color tint = Tint(skin);
            tint.a = CosmeticCatalog.SkinOpacity;
            _skin.color = tint;
            _skin.gameObject.SetActive(true);
        }

        private static void ShowAccessory(Image image, CosmeticItem? item)
        {
            if (item == null)
            {
                image.gameObject.SetActive(false);
                return;
            }

            image.sprite = ProceduralSprites.Accessory(item.Shape);
            image.color = Tint(item);
            image.gameObject.SetActive(true);
        }

        private Image Part(string name, float x0, float y0, float x1, float y1)
        {
            Image image = UiFactory.CreateImage(name, _body.transform, null, Color.white);
            image.preserveAspect = true;
            image.raycastTarget = false;
            UiFactory.Place(image.rectTransform, x0, y0, x1, y1);
            image.gameObject.SetActive(false);
            return image;
        }
    }
}
