using System.Collections.Generic;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// The automatic finished look (FR-007, research R7): flat cells in a soft light version of each role's mapped
    /// variant color, merged into regions per role with rounded outer corners, and no symbols. It sits under the
    /// tiles; a cell shows through as soon as its tile is gone, so restored cells stay distinct from active tiles
    /// (SC-003). A bespoke illustration can replace the texture later (finishedLook.mode = illustration).
    /// </summary>
    public sealed class FinishedPictureRenderer : MonoBehaviour
    {
        private const int PixelsPerCell = 24;

        private RawImage? _image;
        private Image? _shine;
        private Texture2D? _texture;

        public void Build(LevelDefinition definition, BasePicture picture, VariantVisualCatalog? visuals, RectTransform container)
        {
            if (_image == null)
            {
                RectTransform rect = UiFactory.CreateRect("FinishedPicture", container);
                UiFactory.Stretch(rect);
                rect.SetAsFirstSibling();
                _image = rect.gameObject.AddComponent<RawImage>();
                _image.raycastTarget = false;
            }

            if (_texture != null)
            {
                Destroy(_texture);
            }

            _texture = Render(definition, picture, visuals);
            _image.texture = _texture;
            _image.color = Color.white;
            _image.transform.localScale = Vector3.one;
            if (_shine != null)
            {
                _shine.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        /// <summary>The final full reveal on a win (FR-025, doc 12 §12): a soft white shine passes over the picture.</summary>
        public void RevealAll()
        {
            if (_image == null || !isActiveAndEnabled)
            {
                return;
            }

            if (_shine == null)
            {
                _shine = UiFactory.CreateImage("Shine", _image.transform, null, new Color(1f, 1f, 1f, 0f));
                _shine.raycastTarget = false;
                UiFactory.Stretch(_shine.rectTransform);
            }

            StartCoroutine(Shine());
        }

        private System.Collections.IEnumerator Shine()
        {
            Transform picture = _image!.transform;
            for (float t = 0f; t < 0.9f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.9f;
                _shine!.color = new Color(1f, 1f, 1f, 0.55f * Mathf.Sin(k * Mathf.PI));
                picture.localScale = Vector3.one * (1f + (0.03f * Mathf.Sin(k * Mathf.PI)));
                yield return null;
            }

            _shine!.color = new Color(1f, 1f, 1f, 0f);
            picture.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
            }
        }

        /// <summary>Renders the picture as seen on the board (mirror applied), rows bottom-first like the grid.</summary>
        public static Texture2D Render(LevelDefinition definition, BasePicture picture, VariantVisualCatalog? visuals)
        {
            int w = picture.Width;
            int h = picture.Height;
            var roleColor = new Color[picture.Roles.Count];
            for (int r = 0; r < picture.Roles.Count; r++)
            {
                roleColor[r] = definition.Mapping.TryGetValue(picture.Roles[r].RoleId, out VariantId variant)
                    ? UiTheme.Light(visuals != null ? visuals.Get(variant).Color : VariantVisualCatalog.Default(variant).Color)
                    : UiTheme.Ground;
            }

            bool mirror = definition.Picture.Mirror == Mirror.Horizontal;
            int Value(int x, int y) => x < 0 || y < 0 || x >= w || y >= h ? int.MinValue : picture.CellAt(mirror ? w - 1 - x : x, y);

            int size = PixelsPerCell;
            float radius = size * 0.38f;
            var pixels = new Color32[w * size * h * size];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int v = Value(x, y);
                    Color fill = v >= 0 ? roleColor[v] : v == BasePicture.Stone ? UiTheme.StoneColor : UiTheme.Ground;
                    bool region = v >= 0;

                    // Round a corner when both orthogonal neighbours at that corner belong to another region.
                    bool roundBL = region && Value(x - 1, y) != v && Value(x, y - 1) != v;
                    bool roundBR = region && Value(x + 1, y) != v && Value(x, y - 1) != v;
                    bool roundTL = region && Value(x - 1, y) != v && Value(x, y + 1) != v;
                    bool roundTR = region && Value(x + 1, y) != v && Value(x, y + 1) != v;

                    for (int py = 0; py < size; py++)
                    {
                        for (int px = 0; px < size; px++)
                        {
                            Color c = fill;
                            if (region && OutsideCorner(px, py, size, radius, roundBL, roundBR, roundTL, roundTR))
                            {
                                c = UiTheme.Ground;
                            }

                            pixels[(((y * size) + py) * w * size) + (x * size) + px] = c;
                        }
                    }
                }
            }

            var texture = new Texture2D(w * size, h * size, TextureFormat.RGBA32, false)
            {
                name = "FinishedPicture_" + picture.Id,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static bool OutsideCorner(int px, int py, int size, float radius, bool bl, bool br, bool tl, bool tr)
        {
            float fx = px + 0.5f;
            float fy = py + 0.5f;
            return (bl && fx < radius && fy < radius && Dist(fx, fy, radius, radius) > radius)
                || (br && fx > size - radius && fy < radius && Dist(fx, fy, size - radius, radius) > radius)
                || (tl && fx < radius && fy > size - radius && Dist(fx, fy, radius, size - radius) > radius)
                || (tr && fx > size - radius && fy > size - radius && Dist(fx, fy, size - radius, size - radius) > radius);
        }

        private static float Dist(float x, float y, float cx, float cy) => Mathf.Sqrt(((x - cx) * (x - cx)) + ((y - cy) * (y - cy)));
    }
}
