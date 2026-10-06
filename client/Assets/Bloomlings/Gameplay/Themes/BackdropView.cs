using System.Collections.Generic;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Themes
{
    /// <summary>
    /// The garden backdrop in Unity (spec 002 FR-008, research R8; spec 005 §4.2, pictures.md B). The owner's background
    /// picture of the scene (<c>Resources/Backgrounds/{name}</c>, names from <see cref="OwnerPictures"/>: <c>home</c>,
    /// <c>splash</c>, <c>gameplay-daylight</c>, …) is cover-fitted over the screen when it exists. While it is missing,
    /// <see cref="BackdropRaster"/> pixels (the lawn in gameplay, the sky with arches on Home and the splash) are rendered
    /// at a fraction of the screen resolution (<see cref="BackdropRaster.Downscale"/>: a third for the lawn's blades and
    /// flowers, a fifth for the smooth skies), smoothed by bilinear filtering, cached per theme and scene, tinted by the
    /// level band's theme (<see cref="ThemeRotation"/>); Home, the splash and the Wardrobe in the warmer garden colors of
    /// <see cref="HomeStage.Garden"/>. It stands in for the <c>bg.*</c> asset slots.
    /// </summary>
    public sealed class BackdropView : MonoBehaviour
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        private RawImage _image = null!;
        private BackdropScene _scene;
        private string? _picture;
        private Texture2D? _owner;
        private float _zoom;

        public static BackdropView Create(RectTransform parent, BackdropScene scene)
        {
            BackdropView view = NewView(parent, scene);
            view.Show(null);
            return view;
        }

        /// <summary>
        /// A backdrop that shows the owner's picture <paramref name="picture"/> (for example <see cref="OwnerPictures.Wardrobe"/>),
        /// or the code-drawn <paramref name="fallback"/> scene while it is missing.
        /// </summary>
        public static BackdropView Create(RectTransform parent, string picture, BackdropScene fallback)
        {
            BackdropView view = NewView(parent, fallback);
            view._picture = picture;
            view.Show(null);
            return view;
        }

        private static BackdropView NewView(RectTransform parent, BackdropScene scene)
        {
            var image = UiFactory.CreateRect("Backdrop", parent).gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            UiFactory.Stretch(image.rectTransform);
            image.rectTransform.SetAsFirstSibling();
            var view = image.gameObject.AddComponent<BackdropView>();
            view._image = image;
            view._scene = scene;
            return view;
        }

        /// <summary>Whether the owner's picture shows (else the code-drawn backdrop).</summary>
        public bool ShowsPicture => _owner != null;

        /// <summary>
        /// Shows the backdrop of a theme: the owner's picture for the scene and theme when it exists, else the code-drawn
        /// one; null shows the first theme (levels before the rotation starts).
        /// </summary>
        public void Show(BackgroundTheme? theme)
        {
            // The splash takes the Home garden while its own picture is missing (OwnerPictures.Resolve).
            _owner = _picture != null ? OwnerArt.Background(_picture) : OwnerArt.BackgroundOf(_scene, theme?.Id ?? ThemeRotation.Default.Themes[0].Id);
            if (_owner != null)
            {
                _image.color = Color.white;
                Fit();
                return;
            }

            (float w, float h, Insets _) = UiKit.ScreenFrame();
            int downscale = BackdropRaster.Downscale(_scene);
            int width = Mathf.Max(32, (int)w / downscale);
            int height = Mathf.Max(32, (int)h / downscale);
            // Home, the splash and the Wardrobe take the warmer garden colors (spec 005 §4.2), as the playtest's
            // DesignApp.DrawBackdrop does; the key keeps them apart from a gameplay backdrop of the same theme. The win's
            // garden (BackdropScene.Win) is the level's lawn, blurred and lightened.
            bool warm = !BackdropRaster.IsLawn(_scene);
            string key = (theme?.Id ?? "default") + "/" + _scene + (warm ? "/warm" : string.Empty) + "/" + width + "x" + height;
            if (!Cache.TryGetValue(key, out Texture2D? texture))
            {
                BackdropColors colors = DesignTokens.Backdrop(theme?.Background, theme?.Accent);
                texture = Render(width, height, warm ? HomeStage.Garden(colors) : colors, _scene, key);
                Cache[key] = texture;
            }

            _image.texture = texture;
            _image.uvRect = new Rect(0f, 0f, 1f, 1f);
            _image.color = Color.white;
        }

        private void OnRectTransformDimensionsChange() => Fit();

        /// <summary>
        /// The full-screen win (spec 005 §6.3): anchors the owner's win picture at the top of <paramref name="screen"/> and
        /// zooms it (<see cref="OwnerPictures.WinZoom"/>) so the stone disc painted in it lies on <paramref name="stageY"/>,
        /// where the hero's feet stand; the painted disc is then the stage. Returns whether the owner's picture shows (else
        /// nothing changes and the caller draws its pedestal).
        /// </summary>
        public bool StandOnPicture(Box screen, float stageY)
        {
            if (_owner == null)
            {
                return false;
            }

            _zoom = OwnerPictures.WinZoom(screen, stageY, _owner.width, _owner.height);
            Fit();
            return true;
        }

        /// <summary>Cover-fits the owner's picture over the backdrop's rect (the screen), top-anchored and zoomed on the win.</summary>
        private void Fit()
        {
            if (_owner == null || _image == null)
            {
                return;
            }

            Rect rect = _image.rectTransform.rect;
            (float w, float h, Insets _) = UiKit.ScreenFrame();
            float width = rect.width > 0f ? rect.width : w;
            float height = rect.height > 0f ? rect.height : h;
            if (_zoom > 0f)
            {
                OwnerArt.CoverTop(_image, _owner, width, height, _zoom);
                return;
            }

            OwnerArt.Cover(_image, _owner, width, height);
        }

        private static Texture2D Render(int width, int height, BackdropColors colors, BackdropScene scene, string name)
        {
            byte[] rgba = BackdropRaster.Render(width, height, colors, scene);
            var pixels = new Color32[width * height];
            for (int row = 0; row < height; row++)
            {
                // Texture rows start at the bottom; the raster's rows start at the top.
                int source = (height - 1 - row) * width * 4;
                for (int x = 0; x < width; x++)
                {
                    int i = source + (x * 4);
                    pixels[(row * width) + x] = new Color32(rgba[i], rgba[i + 1], rgba[i + 2], 255);
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Backdrop " + name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
