using System.Collections.Generic;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Themes
{
    /// <summary>
    /// The procedural garden backdrop in Unity (spec 002 FR-008, research R8): <see cref="BackdropRaster"/> pixels
    /// in a texture stretched over the screen, tinted by the level band's theme (<see cref="ThemeRotation"/>). It is
    /// rendered at a fifth of the screen resolution, smoothed by bilinear filtering, and cached per theme and scene. It
    /// stands in for the <c>bg.*</c> asset slots until final art exists.
    /// </summary>
    public sealed class BackdropView : MonoBehaviour
    {
        private const int Downscale = 5;
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        private RawImage _image = null!;
        private BackdropScene _scene;

        public static BackdropView Create(RectTransform parent, BackdropScene scene)
        {
            var image = UiFactory.CreateRect("Backdrop", parent).gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            UiFactory.Stretch(image.rectTransform);
            image.rectTransform.SetAsFirstSibling();
            var view = image.gameObject.AddComponent<BackdropView>();
            view._image = image;
            view._scene = scene;
            view.Show(null);
            return view;
        }

        /// <summary>Shows the backdrop of a theme; null shows the first theme (levels before the rotation starts).</summary>
        public void Show(BackgroundTheme? theme)
        {
            (float w, float h, Insets _) = UiKit.ScreenFrame();
            int width = Mathf.Max(32, (int)w / Downscale);
            int height = Mathf.Max(32, (int)h / Downscale);
            string key = (theme?.Id ?? "default") + "/" + _scene + "/" + width + "x" + height;
            if (!Cache.TryGetValue(key, out Texture2D? texture))
            {
                texture = Render(width, height, DesignTokens.Backdrop(theme?.Background, theme?.Accent), _scene, key);
                Cache[key] = texture;
            }

            _image.texture = texture;
            _image.color = Color.white;
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
