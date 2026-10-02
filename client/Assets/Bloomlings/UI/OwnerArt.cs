using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The owner's pictures in Unity (spec 005 <c>pictures.md</c> B, C and D, research D16): the backgrounds from
    /// <c>Resources/Backgrounds/{name}</c>, the logo from <c>Resources/Brand/logo</c>, the booster icons from
    /// <c>Resources/Icons/booster-{id}</c> and the leaf decorations from <c>Resources/Decor/{name}</c>, by the names of
    /// <see cref="OwnerPictures"/>. Each loader returns null while the picture is missing, and the hooks then draw the
    /// code-drawn stand-in (the <see cref="BackdropRaster"/> backdrop, the wooden wordmark letters, the drawn icons and
    /// leaves). Pictures are loaded once.
    /// </summary>
    public static class OwnerArt
    {
        private static readonly Dictionary<string, Texture2D?> Textures = new Dictionary<string, Texture2D?>();
        private static readonly Dictionary<string, Sprite?> Sprites = new Dictionary<string, Sprite?>();

        /// <summary>An owner background (<c>home</c>, <c>gameplay-pond</c>, …), or null while it is missing.</summary>
        public static Texture2D? Background(string name) => Load(OwnerPictures.BackgroundFolder + "/" + name);

        /// <summary>The owner's logo picture, or null while it is missing.</summary>
        public static Texture2D? LogoPicture() => Load(OwnerPictures.BrandFolder + "/" + OwnerPictures.Logo);

        /// <summary>An owner icon (<c>booster-shuffle</c>, pictures.md D1–D4) as a sprite, or null while it is missing.</summary>
        public static Sprite? Icon(string name) => SpriteOf(OwnerPictures.IconFolder + "/" + name);

        /// <summary>An owner leaf picture (<c>ivy</c>, <c>flowers</c>, …, pictures.md D5–D8) as a sprite, or null while it is missing.</summary>
        public static Sprite? Decor(string name) => SpriteOf(OwnerPictures.DecorFolder + "/" + name);

        /// <summary>
        /// Shows an owner picture on an image with its aspect kept (it follows the rect's size), mirrored left to right
        /// when <paramref name="mirror"/> and upside down when <paramref name="turn"/> (both: turned half way) by a
        /// negative <c>localScale</c>: the playtest's <c>Kit.OwnerPicture</c>.
        /// </summary>
        public static void Show(Image image, Sprite picture, bool mirror = false, bool turn = false)
        {
            PictureFit.On(image, (w, h) => picture, square: true);
            image.rectTransform.localScale = new Vector3(mirror ? -1f : 1f, turn ? -1f : 1f, 1f);
        }

        /// <summary>
        /// The Home wordmark (spec 005 §4.5, pictures.md C1): the owner's logo picture fitted into the rect with its aspect
        /// kept, or while it is missing the wooden wordmark letters in their mossy band with leaves and flowers
        /// (<see cref="UiKit.WoodLogo"/>) spelling <paramref name="text"/>. Never a touch target; the caller places the
        /// returned rect.
        /// </summary>
        public static RectTransform Logo(string name, Transform parent, string text)
        {
            Texture2D? picture = LogoPicture();
            if (picture == null)
            {
                return UiKit.WoodLogo(name, parent, text);
            }

            // The caller places the box; the picture fits inside it with its aspect kept.
            RectTransform box = UiFactory.CreateRect(name, parent);
            var image = UiFactory.CreateRect("Logo", box).gameObject.AddComponent<RawImage>();
            image.texture = picture;
            image.raycastTarget = false;
            var fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = picture.width / (float)Mathf.Max(1, picture.height);
            return box;
        }

        /// <summary>Shows a picture cover-fitted on a raw image of the given size in canvas units (the overflow cropped).</summary>
        public static void Cover(RawImage image, Texture texture, float width, float height)
        {
            image.texture = texture;
            (float x, float y, float w, float h) = PicturePixels.CoverUv(texture.width, texture.height, width, height);
            image.uvRect = new Rect(x, y, w, h);
        }

        private static Sprite? SpriteOf(string path)
        {
            if (!Sprites.TryGetValue(path, out Sprite? sprite))
            {
                Texture2D? texture = Load(path);
                sprite = texture == null
                    ? null
                    : Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
                Sprites[path] = sprite;
            }

            return sprite;
        }

        private static Texture2D? Load(string path)
        {
            if (!Textures.TryGetValue(path, out Texture2D? texture))
            {
                texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    texture.wrapMode = TextureWrapMode.Clamp;
                }

                Textures[path] = texture;
            }

            return texture;
        }
    }
}
