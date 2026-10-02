using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The owner's pictures in Unity (spec 005 <c>pictures.md</c> B, C, D and G, research D16): the backgrounds from
    /// <c>Resources/Backgrounds/{name}</c>, the logo from <c>Resources/Brand/logo</c>, the booster icons, the variant icons
    /// and the lotus from <c>Resources/Icons/</c> (<c>booster-{id}</c>, <c>variant-{id}</c>, <c>field-{id}</c>,
    /// <c>currency-lotus</c>) and the leaf decorations from <c>Resources/Decor/{name}</c>, by the names of
    /// <see cref="OwnerPictures"/>. Each loader returns null while the picture is missing, and the hooks then draw the
    /// code-drawn stand-in (the <see cref="BackdropRaster"/> backdrop, the wooden wordmark letters, the drawn icons, symbols
    /// and leaves). Pictures are loaded once.
    /// </summary>
    public static class OwnerArt
    {
        private static readonly Dictionary<string, Texture2D?> Textures = new Dictionary<string, Texture2D?>();
        private static readonly Dictionary<string, Sprite?> Sprites = new Dictionary<string, Sprite?>();
        private static readonly Dictionary<(string Icon, bool Sticker, bool Grey), Sprite?> TileIcons = new Dictionary<(string, bool, bool), Sprite?>();
        private static readonly Dictionary<string, (byte[] Rgba, int Width, int Height)?> Pixels = new Dictionary<string, (byte[], int, int)?>();

        /// <summary>An owner background (<c>home</c>, <c>gameplay-pond</c>, …), or null while it is missing.</summary>
        public static Texture2D? Background(string name) => Load(OwnerPictures.BackgroundFolder + "/" + name);

        /// <summary>
        /// The owner background a scene shows (<see cref="OwnerPictures.Resolve"/>: the splash takes the Home garden while
        /// its own picture is missing), or null while it is missing too.
        /// </summary>
        public static Texture2D? BackgroundOf(BackdropScene scene, string themeId = "") =>
            Background(OwnerPictures.Resolve(scene, themeId, name => Background(name) != null));

        /// <summary>The owner's logo picture, or null while it is missing.</summary>
        public static Texture2D? LogoPicture() => Load(OwnerPictures.BrandFolder + "/" + OwnerPictures.Logo);

        /// <summary>
        /// Where the Home (and splash) wordmark goes: the owner's logo picture sized by width
        /// (<see cref="ReferenceHomeRegions.LogoPicture"/>, its letters about 0.8 W), or Home's logo box for the drawn letters.
        /// </summary>
        public static Box LogoBox(ReferenceHomeRegions r)
        {
            Texture2D? picture = LogoPicture();
            return picture == null ? r.Logo : r.LogoPicture(picture.width, picture.height);
        }

        /// <summary>
        /// An owner icon (<c>booster-shuffle</c>, pictures.md D1–D4; <c>variant-leaf</c>, <c>field-leaf</c> and
        /// <c>currency-lotus</c>, G9–G24) as a sprite, or null while it is missing.
        /// </summary>
        public static Sprite? Icon(string name) => SpriteOf(OwnerPictures.IconFolder + "/" + name);

        /// <summary>
        /// The owner's icon picture a candy tile of <paramref name="iconId"/> shows over its face
        /// (<see cref="OwnerPictures.TileIcon"/>: the field icon on board and flat tiles, the detailed one on stickers), its
        /// grey copy on a stuck tile (<see cref="TileState.Grey"/>), or null: the mystery tile, or the picture missing (the
        /// tile then keeps its drawn symbol). Looked up once per icon, style and greyness.
        /// </summary>
        public static Sprite? TileIcon(string iconId, TileStyle style, TileState state)
        {
            bool sticker = style == TileStyle.Sticker;
            bool grey = state == TileState.Grey;
            if (state == TileState.Mystery)
            {
                return null;
            }

            if (!TileIcons.TryGetValue((iconId, sticker, grey), out Sprite? sprite))
            {
                string? name = OwnerPictures.TileIcon(iconId, style, state);
                sprite = name == null ? null : grey ? GreyIcon(name) : Icon(name);
                TileIcons[(iconId, sticker, grey)] = sprite;
            }

            return sprite;
        }

        /// <summary>
        /// The pixels of an owner icon (<paramref name="name"/>, in the Icons folder) as straight-alpha RGBA bytes, rows
        /// from the top, with its size, or null while it is missing: the finished picture bakes the field icons into its
        /// texture (<c>BoardPictures.Finished</c>). The imported texture keeps no readable copy, so its pixels are read
        /// back once through a render texture and kept.
        /// </summary>
        public static (byte[] Rgba, int Width, int Height)? IconPixels(string name)
        {
            string path = OwnerPictures.IconFolder + "/" + name;
            if (!Pixels.TryGetValue(path, out (byte[] Rgba, int Width, int Height)? pixels))
            {
                Texture2D? texture = Load(path);
                byte[]? rows = texture == null ? null : ReadBack(texture);
                pixels = rows == null ? ((byte[], int, int)?)null : (FlipRows(rows, texture!.width, texture.height), texture.width, texture.height);
                Pixels[path] = pixels;
            }

            return pixels;
        }

        /// <summary>
        /// The grey copy of an owner icon (a stuck slot's tile, <see cref="OwnerPictures.GreyPixels"/>), with mipmaps, or
        /// null while the picture is missing. Made once from the picture's pixels.
        /// </summary>
        private static Sprite? GreyIcon(string name)
        {
            string path = OwnerPictures.IconFolder + "/" + name;
            Texture2D? texture = Load(path);
            byte[]? rows = texture == null ? null : ReadBack(texture);
            if (rows == null)
            {
                // No read-back on this device: the colored picture, faded by the tile's alpha.
                return Icon(name);
            }

            OwnerPictures.GreyPixels(rows);
            var grey = new Texture2D(texture!.width, texture.height, TextureFormat.RGBA32, true)
            {
                name = name + "#grey",
                filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            grey.SetPixelData(rows, 0);
            grey.Apply(true, true);
            return Sprite.Create(grey, new Rect(0f, 0f, grey.width, grey.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
        }

        /// <summary>
        /// A texture's pixels as RGBA bytes in Unity's row order (from the bottom), read back through a temporary render
        /// texture (the imported texture is not readable); null when it fails.
        /// </summary>
        private static byte[]? ReadBack(Texture2D texture)
        {
            int w = texture.width;
            int h = texture.height;
            if (w < 1 || h < 1)
            {
                return null;
            }

            RenderTexture target = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture? previous = RenderTexture.active;
            try
            {
                Graphics.Blit(texture, target);
                RenderTexture.active = target;
                var copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0f, 0f, w, h), 0, 0, false);
                byte[] rows = copy.GetRawTextureData();
                Object.Destroy(copy);
                return rows.Length == w * h * 4 ? rows : null;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        /// <summary>RGBA rows from the bottom (Unity's order) to rows from the top (the engine-free pictures'), as a new array.</summary>
        private static byte[] FlipRows(byte[] rows, int width, int height)
        {
            var flipped = new byte[rows.Length];
            int stride = width * 4;
            for (int y = 0; y < height; y++)
            {
                System.Buffer.BlockCopy(rows, (height - 1 - y) * stride, flipped, y * stride, stride);
            }

            return flipped;
        }

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

        /// <summary>
        /// Shows a picture on a raw image of the given size, <paramref name="zoom"/> times its cover-fitted size, centered
        /// across and anchored at the top (the full-screen win: its painted disc under the layout's pedestal,
        /// <see cref="OwnerPictures.WinZoom"/>).
        /// </summary>
        public static void CoverTop(RawImage image, Texture texture, float width, float height, float zoom)
        {
            image.texture = texture;
            float tw = Mathf.Max(1f, texture.width);
            float th = Mathf.Max(1f, texture.height);
            float scale = Mathf.Max(width / tw, height / th) * Mathf.Max(1f, zoom);
            float w = Mathf.Min(1f, width / (tw * scale));
            float h = Mathf.Min(1f, height / (th * scale));

            // Texture coordinates start at the bottom: the top of the picture shows.
            image.uvRect = new Rect((1f - w) / 2f, 1f - h, w, h);
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
