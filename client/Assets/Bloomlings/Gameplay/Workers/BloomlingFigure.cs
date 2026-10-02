using System;
using System.Collections.Generic;
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
    /// blank face, so it never doubles the eyes; a hero without a matching blank twin (an owner hero, pictures.md A5) keeps
    /// its own face and shows the expression on a cream badge at its upper right. Without the picture it draws the spec 002
    /// family silhouette (FR-021). The character's colors are never changed by a cosmetic. Used by the workers, the win
    /// cheer, the Wardrobe, Home and the profile.
    /// </summary>
    public sealed class BloomlingFigure
    {
        private readonly RectTransform _root;
        private readonly Image _body;
        private readonly AspectRatioFitter _fitter;
        private readonly Image _skinMask;
        private readonly Image _skin;
        private readonly Image _trail;
        private readonly Image _badge;
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
            _badge = Part("ExpressionBadge");
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

        /// <summary>
        /// A family's 3D hero (meta screens only; size the rect to 512:576) wearing <paramref name="outfit"/>: a worn hat
        /// sits on the hero's head in full color (<see cref="CharacterArt.HatOnHero"/>, the playtest's <c>Visuals.Hero</c>).
        /// Without the picture, the family body in its family's color, a little smaller (the playtest's fallback).
        /// </summary>
        public void ShowHero(Family family, Outfit? outfit)
        {
            // The blank-faced twin only when it is the same character (CharacterSprites.HasMatchingBlank); else the hero
            // keeps its face and the expression goes on the badge.
            bool expression = outfit?.Expression != null;
            bool blank = expression && CharacterSprites.HasMatchingBlank(family);
            Sprite? picture = CharacterSprites.Hero(family, blank);
            Show(picture, family, HeroColor(family), outfit, CharacterArt.FaceCenterHero(family), (float)CharacterArt.HeroHeight / CharacterArt.HeroWidth, hero: true, badge: expression && !blank && picture != null);
        }

        public static Color Tint(CosmeticItem item) => ColorUtility.TryParseHtmlString(item.Tint, out Color color) ? color : Color.white;

        /// <summary>A family's color: its first variant's (Leaf, Flower, Water, Wood), the hero's fallback silhouette.</summary>
        public static Color HeroColor(Family family)
        {
            VariantId variant = family switch
            {
                Family.Bloom => VariantId.Flower,
                Family.Drop => VariantId.Water,
                Family.Twig => VariantId.Wood,
                _ => VariantId.Leaf,
            };
            return UiTheme.Of(Rgba.FromHex(VariantCatalog.Default.Get(variant).ColorHex));
        }

        private void Show(Sprite? picture, Family family, Color fallbackColor, Outfit? outfit, (float X, float Y) face, float aspect, bool hero = false, bool badge = false)
        {
            // The figure's boxes in picture units (width 1, y down), placed relative to the picture's rect.
            var frame = new Box(0f, 0f, 1f, aspect);
            _fitter.aspectRatio = picture != null ? 1f / aspect : 1f;
            if (picture != null)
            {
                _body.sprite = picture;
                _body.color = Color.white;
                _skinMask.sprite = picture;
                _body.rectTransform.localScale = Vector3.one;
            }
            else
            {
                // FR-021: the spec 002 family body in the variant color; a hero's is inset 12% of its width on each side.
                _body.sprite = ProceduralSprites.Silhouette(family);
                _body.color = fallbackColor;
                _skinMask.sprite = _body.sprite;
                _body.rectTransform.localScale = Vector3.one * (hero ? 0.76f : 1f);
            }

            ShowSkin(outfit?.Skin);
            ShowAccessory(_trail, outfit?.Trail, CharacterArt.TrailBox(frame), frame);
            if (badge && outfit?.Expression != null)
            {
                // A cream disc at the hero's upper right holding the expression in ink.brown, never over the drawn face.
                Box disc = CharacterArt.ExpressionBadge(frame);
                _badge.sprite = BadgeDisc;
                _badge.color = Color.white;
                Place(_badge, disc, frame);
                _badge.gameObject.SetActive(true);
                ShowAccessory(_expression, outfit.Expression, disc.Inset(disc.Width * 0.16f), frame);
                _expression.color = UiTheme.Of(DesignTokens.Colors.InkBrown);
            }
            else
            {
                _badge.gameObject.SetActive(false);
                ShowAccessory(_expression, outfit?.Expression, CharacterArt.ExpressionBox(frame, face), frame);
            }
            if (hero && picture != null && outfit?.Hat != null)
            {
                ShowHat(outfit.Hat, CharacterArt.HatOnHero(frame, family), frame);
            }
            else
            {
                ShowAccessory(_hat, outfit?.Hat, CharacterArt.HatBox(frame), frame);
            }
        }

        /// <summary>
        /// A hat on a 3D hero's head in full color (the playtest's <c>Visuals.Hero</c>): a darker outline of its own tint
        /// (darkened 0.45), the fill, and a light top-left (lightened 0.35, at half alpha), baked into one sprite.
        /// </summary>
        private void ShowHat(CosmeticItem hat, Box box, Box frame)
        {
            string id = ShapeLibrary.CosmeticId(hat.Shape);
            if (!ShapeLibrary.Has(id))
            {
                ShowAccessory(_hat, hat, box, frame);
                return;
            }

            Rgba tint = UiTheme.ToRgba(Tint(hat));
            Func<float, float, float> sdf = ShapeLibrary.Get(id);
            var layers = new List<(Func<float, float, float> Sdf, Rgba Color)>
            {
                ((x, y) => sdf(x, y) - 0.06f, tint.Darken(0.45f)),
                (sdf, tint),
                ((x, y) => Math.Max(sdf(x + 0.05f, y - 0.06f) + 0.07f, sdf(x, y) + 0.03f), tint.Lighten(0.35f).WithAlpha(0.5f)),
            };
            _hat.sprite = ProceduralSprites.Baked(id + "/on_hero/" + tint.Hex, 128, layers);
            _hat.color = Color.white;
            UiFactory.Place(_hat.rectTransform, box.Left / frame.Width, 1f - (box.Bottom / frame.Height), box.Right / frame.Width, 1f - (box.Top / frame.Height));
            _hat.gameObject.SetActive(true);
        }

        /// <summary>The expression badge's disc: the cream of the round buttons with a <c>cream.line</c> outline.</summary>
        private static Sprite BadgeDisc
        {
            get
            {
                Func<float, float, float> circle = ShapeLibrary.Get("ui.circle");
                var layers = new List<(Func<float, float, float> Sdf, Rgba Color)>
                {
                    (circle, DesignTokens.Colors.CreamLine),
                    ((x, y) => circle(x, y) + 0.09f, DesignTokens.Colors.CreamTop),
                };
                return ProceduralSprites.Baked("expression_badge", 96, layers);
            }
        }

        private static void Place(Image image, Box box, Box frame) =>
            UiFactory.Place(image.rectTransform, box.Left / frame.Width, 1f - (box.Bottom / frame.Height), box.Right / frame.Width, 1f - (box.Top / frame.Height));

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
