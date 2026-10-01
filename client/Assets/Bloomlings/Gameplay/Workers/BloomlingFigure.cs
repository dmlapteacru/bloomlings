using Bloomlings.Client.Art;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Workers
{
    /// <summary>
    /// A Bloomling drawn from a generated picture (spec 004): a variant's 2D character on the board, or a family's 3D
    /// hero on the meta screens, with what it wears (FR-063): a skin pattern laid thinly over the picture and masked by
    /// it, a hat above it, an expression on its face and a trail behind it. A worn expression shows over the picture's
    /// blank face, so it never doubles the eyes. Without the picture it draws the spec 002 family silhouette (FR-021). The
    /// character's colors are never changed by a cosmetic. Used by the workers, the win cheer, the Wardrobe, Home and the
    /// profile.
    /// </summary>
    public sealed class BloomlingFigure
    {
        private readonly RectTransform _root;
        private readonly Image _body;
        private readonly AspectRatioFitter _fitter;
        private readonly Image _skinMask;
        private readonly Image _skin;
        private readonly Image _trail;
        private readonly Image _expression;
        private readonly Image _hat;

        private BloomlingFigure(RectTransform root)
        {
            // The picture keeps its own shape inside the placed rect, so the cosmetic boxes land on it.
            _root = root;
            _body = UiFactory.CreateImage("Picture", root, null, Color.white);
            _body.preserveAspect = true;
            UiFactory.Stretch(_body.rectTransform);
            _fitter = _body.gameObject.AddComponent<AspectRatioFitter>();
            _fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            _fitter.aspectRatio = 1f;

            // The skin sits under a mask in the picture's shape, so the pattern stays on the character.
            _skinMask = Part("SkinMask");
            UiFactory.Stretch(_skinMask.rectTransform);
            Mask mask = _skinMask.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            _skin = UiFactory.CreateImage("Skin", _skinMask.transform, null, Color.white);
            _skin.raycastTarget = false;
            UiFactory.Stretch(_skin.rectTransform);
            _trail = Part("Trail");
            _expression = Part("Expression");
            _hat = Part("Hat");
        }

        /// <summary>The picture (it keeps the picture's aspect inside <see cref="Rect"/>).</summary>
        public Image Body => _body;

        /// <summary>The rect callers place and move; the picture fits inside it.</summary>
        public RectTransform Rect => _root;

        /// <summary>The trail's image, for the worker's sway.</summary>
        public RectTransform Trail => _trail.rectTransform;

        /// <summary>The hat's image, for the worker's hop.</summary>
        public RectTransform Hat => _hat.rectTransform;

        public static BloomlingFigure Create(string name, Transform parent) => new BloomlingFigure(UiFactory.CreateRect(name, parent));

        /// <summary>
        /// A variant's 2D character (square), happy, wearing <paramref name="outfit"/> (null: nothing). Its picture is the
        /// blank one under a worn expression.
        /// </summary>
        public void ShowCharacter(VariantId variant, Outfit? outfit)
        {
            bool expression = outfit?.Expression != null;
            Sprite? picture = CharacterSprites.Character(variant, expression ? CharacterMood.Blank : CharacterMood.Happy);
            bool known = VariantCatalog.Default.TryGet(variant, out VariantInfo info);
            (float X, float Y) face = known ? CharacterArt.FaceCenter2D(info.IconId) : (0.5f, 0.6f);
            Family family = known ? info.Family : Family.Sprig;
            Color color = known && ColorUtility.TryParseHtmlString(info.ColorHex, out Color parsed) ? parsed : Color.gray;
            Show(picture, family, color, outfit, face, 1f);
        }

        /// <summary>A family's 3D hero (meta screens only; size the rect to 512:576) wearing <paramref name="outfit"/>.</summary>
        public void ShowHero(Family family, Outfit? outfit)
        {
            Sprite? picture = CharacterSprites.Hero(family, blank: outfit?.Expression != null);
            Show(picture, family, Color.white, outfit, CharacterArt.FaceCenterHero(family), (float)CharacterArt.HeroHeight / CharacterArt.HeroWidth);
        }

        public static Color Tint(CosmeticItem item) => ColorUtility.TryParseHtmlString(item.Tint, out Color color) ? color : Color.white;

        private void Show(Sprite? picture, Family family, Color fallbackColor, Outfit? outfit, (float X, float Y) face, float aspect)
        {
            // The figure's boxes in picture units (width 1, y down), placed relative to the picture's rect.
            var frame = new Box(0f, 0f, 1f, aspect);
            _fitter.aspectRatio = picture != null ? 1f / aspect : 1f;
            if (picture != null)
            {
                _body.sprite = picture;
                _body.color = Color.white;
                _skinMask.sprite = picture;
            }
            else
            {
                // FR-021: the spec 002 family body in the variant color.
                _body.sprite = ProceduralSprites.Silhouette(family);
                _body.color = fallbackColor;
                _skinMask.sprite = _body.sprite;
            }

            ShowSkin(outfit?.Skin);
            ShowAccessory(_trail, outfit?.Trail, CharacterArt.TrailBox(frame), frame);
            ShowAccessory(_expression, outfit?.Expression, CharacterArt.ExpressionBox(frame, face), frame);
            ShowAccessory(_hat, outfit?.Hat, CharacterArt.HatBox(frame), frame);
        }

        private void ShowSkin(CosmeticItem? skin)
        {
            if (skin == null)
            {
                _skinMask.gameObject.SetActive(false);
                return;
            }

            _skin.sprite = ProceduralSprites.SkinPatternFull(skin.Shape);
            Color tint = Tint(skin);
            tint.a = CosmeticCatalog.SkinOpacity;
            _skin.color = tint;
            _skinMask.gameObject.SetActive(true);
        }

        private static void ShowAccessory(Image image, CosmeticItem? item, Box box, Box frame)
        {
            if (item == null)
            {
                image.gameObject.SetActive(false);
                return;
            }

            image.sprite = ProceduralSprites.Accessory(item.Shape);
            image.color = Tint(item);
            UiFactory.Place(image.rectTransform, box.Left / frame.Width, 1f - (box.Bottom / frame.Height), box.Right / frame.Width, 1f - (box.Top / frame.Height));
            image.gameObject.SetActive(true);
        }

        private Image Part(string name)
        {
            Image image = UiFactory.CreateImage(name, _body.transform, null, Color.white);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.gameObject.SetActive(false);
            return image;
        }
    }
}
