using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Core.Definitions;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// The finished picture (FR-007, research R7; spec 005 contracts/look.md §4.1, §4.4, research D14).
    /// <list type="bullet">
    /// <item><description>On the board it is the restored ground under the tiles (<c>tile.ground</c>): each cell a pale
    /// flat cell of its role's variant color (lightened 0.55, radius 10%, a faint inner shadow along its top), so restored
    /// cells read as one calm mosaic next to the saturated candy tiles (SC-003). A cell shows as soon as its tile is
    /// gone.</description></item>
    /// <item><description><see cref="Render"/> draws it in full color for the win and the Collection
    /// (<c>tile.picture</c>): flat candy tiles (<see cref="Bloomlings.Client.UI.Design.TileStyle.Flat"/>) of each role's
    /// variant, stone blocks and cream ground cells in a thin stone border.</description></item>
    /// </list>
    /// The pixels come from the engine-free <see cref="BoardPictures"/>. A bespoke illustration can replace the texture
    /// later (finishedLook.mode = illustration).
    /// </summary>
    public sealed class FinishedPictureRenderer : MonoBehaviour
    {
        /// <summary>Pixels per cell of the full-color picture (<see cref="Render"/>).</summary>
        public const int PicturePixelsPerCell = 48;

        /// <summary>
        /// The most pixels per cell of the restored ground: its cells are flat pale colors with a soft shade along their
        /// top, so a larger board stretches it under bilinear filtering without showing it.
        /// </summary>
        public const int GroundPixelsPerCell = 48;

        private RawImage? _image;
        private Image? _shine;
        private Texture2D? _texture;
        private LevelDefinition? _definition;
        private BasePicture? _picture;
        private int _width;
        private int _height;
        private int _cellPixels;

        /// <summary>The pixels per cell the ground was last drawn at.</summary>
        public int CellPixels => _cellPixels;

        /// <summary>
        /// Draws the restored ground of a <paramref name="width"/> × <paramref name="height"/> board under the tiles of
        /// <paramref name="container"/> (the grid: the ground covers it), <paramref name="cellPixels"/> per cell.
        /// </summary>
        public void Build(LevelDefinition definition, BasePicture picture, int width, int height, int cellPixels, RectTransform container)
        {
            if (_image == null)
            {
                RectTransform rect = UiFactory.CreateRect("FinishedPicture", container);
                UiFactory.Stretch(rect);
                rect.SetAsFirstSibling();
                _image = rect.gameObject.AddComponent<RawImage>();
                _image.raycastTarget = false;
            }

            _definition = definition;
            _picture = picture;
            _width = Mathf.Max(1, width);
            _height = Mathf.Max(1, height);

            // A new level always draws its own ground, even at the cell size of the last one.
            _cellPixels = 0;
            Redraw(cellPixels);
            _image.color = Color.white;
            _image.transform.localScale = Vector3.one;
            if (_shine != null)
            {
                _shine.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        /// <summary>
        /// Draws the ground again at another cell size (the board was laid out at a new size), at most
        /// <see cref="GroundPixelsPerCell"/>; same size: nothing.
        /// </summary>
        public void Redraw(int cellPixels)
        {
            int cell = Mathf.Clamp(cellPixels, 16, GroundPixelsPerCell);
            if (_image == null || _definition == null || _picture == null || (cell == _cellPixels && _texture != null))
            {
                return;
            }

            _cellPixels = cell;
            if (_texture != null)
            {
                Destroy(_texture);
            }

            int w = _width * cell;
            int h = _height * cell;
            _texture = ToTexture(BoardPictures.Ground(_definition, _picture, _width, _height, cell), w, h, "Ground_" + _picture.Id);
            _image.texture = _texture;
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

        /// <summary>
        /// The finished picture in full color (the win, the Collection; research D14): flat candy tiles of each role's
        /// variant (mirror applied), stone blocks and cream ground cells, in a thin stone border when
        /// <paramref name="framed"/> (with a little room for the border's shadow) or on the dark board gap otherwise.
        /// The texture's aspect is the picture's with its border. <paramref name="visuals"/> is kept for older callers:
        /// the colors are the core catalog's, as the candy tiles'.
        /// </summary>
        public static Texture2D Render(LevelDefinition definition, BasePicture picture, VariantVisualCatalog? visuals, bool framed = true, int cellPixels = PicturePixelsPerCell)
        {
            // The owner's field icons (spec 005 pictures.md G17–G24) are baked into the flat tiles when they exist.
            byte[] rgba = BoardPictures.Finished(definition, picture, cellPixels, framed, out int width, out int height, OwnerArt.IconPixels);
            return ToTexture(rgba, width, height, "FinishedPicture_" + picture.Id);
        }

        /// <summary>
        /// The pixels per cell that fit the finished picture (framed or not) into <paramref name="maxSide"/> pixels, from
        /// 8 to <see cref="PicturePixelsPerCell"/>: a Collection thumbnail at its frame's own resolution.
        /// </summary>
        public static int CellPixelsToFit(BasePicture picture, int maxSide, bool framed = true) =>
            Mathf.Min(PicturePixelsPerCell, BoardPictures.CellPixelsToFit(picture.Width, picture.Height, maxSide, framed));

        /// <summary>
        /// A texture of top-down RGBA bytes (rows flipped for Unity, edges bled), bilinear and clamped. The bytes go up
        /// as they are and the texture keeps no readable copy.
        /// </summary>
        private static Texture2D ToTexture(byte[] rgba, int width, int height, string name)
        {
            byte[] rows = PicturePixels.ForTexture(rgba, width, height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixelData(rows, 0);
            texture.Apply(false, true);
            return texture;
        }
    }
}
