using System;
using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// One board cell in the reference look (spec 005 contracts/look.md §3.1, §4.1; the playtest's <c>BoardPainter</c>).
    /// <list type="bullet">
    /// <item><description>A target is a candy tile in the board style (<c>tile.base</c>): a satin tile in its variant's
    /// color with the symbol as a small raised bead, nearly filling its cell (inset 0.8%), so only the dark board gap
    /// and the tiles' outlines part neighbors. A hidden mystery tile is the lilac "?" tile (FR-039).</description></item>
    /// <item><description>The next hidden layer peeks from a chip in the tile's top-right corner (<c>tile.layer_peek</c>,
    /// FR-036): a small candy tile of its variant in a cream ring with a dark rim. On a level whose data stores the icons
    /// look (boards over 288 cells) <see cref="BoardView"/> passes no next layer, so no chip shows (FR-036 as amended on
    /// 2026-10-06).</description></item>
    /// <item><description>A key waiting under the tile is the gold key on a cream disc in its top-left corner
    /// (<c>tile.key</c>, FR-033), clear of the symbol.</description></item>
    /// <item><description>A stone is a raised block of the border's sandy stone with a crack (<c>tile.stone</c>) over the
    /// restored ground.</description></item>
    /// <item><description>Open cells hide the tile, so the restored ground (<see cref="FinishedPictureRenderer"/>) shows
    /// through.</description></item>
    /// <item><description>A cell that counts toward a special's condition carries a ring in the special's color
    /// (FR-037, FR-038); while Bloom Burst waits for its target, a gently pulsing <c>booster.bloom_burst</c> ring marks
    /// every tile it can take (<c>fx.burst</c>).</description></item>
    /// </list>
    /// Changes animate: a cleared tile shrinks away, a revealed layer or mystery tile flips in. The view is the whole
    /// cell; its parts are laid out from the cell's box, and the pieces only some tiles need are made when first shown.
    /// </summary>
    public sealed class TileView : MonoBehaviour
    {
        private Image _gap = null!;
        private RectTransform _body = null!;
        private BoxLayout _layout = null!;
        private CandyTileView _tile = null!;
        private RectTransform? _peek;
        private CandyTileView? _peekTile;
        private RectTransform? _key;
        private Image? _keyGlyph;
        private Image? _counted;
        private Image? _target;
        private Image? _stone;
        private Coroutine? _animation;
        private bool _targetable;
        private float _time;
        private float _countedRadius;
        private float _countedWidth;
        private float _targetRadius;
        private float _targetWidth;

        /// <summary>Raised when the tile is tapped while it is tappable (Bloom Burst picks its variant, T120).</summary>
        public event Action<CellPos>? Tapped;

        public CellPos Cell { get; private set; }

        /// <summary>The variant currently shown, or null when the tile is hidden (open ground, a stone or a mystery tile).</summary>
        public VariantId? Shown { get; private set; }

        public static TileView Create(Transform parent, CellPos cell, VariantVisualCatalog? visuals)
        {
            RectTransform root = UiFactory.CreateRect($"Tile {cell.X},{cell.Y}", parent);
            var view = root.gameObject.AddComponent<TileView>();
            view.Cell = cell;

            // The dark board gap under the tile (the stone border's gap, §3.6): it parts the tiles and takes the taps while
            // the tile is a Bloom Burst target. It hides at once when the tile goes, so the ground shows under the shrinking tile.
            view._gap = UiFactory.CreateImage("Gap", root, null, UiTheme.Of(GardenLook.BoardGap));
            UiFactory.Stretch(view._gap.rectTransform);
            Button button = view._gap.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = view._gap;
            button.onClick.AddListener(() => view.Tapped?.Invoke(view.Cell));

            // The body moves in the animations (shrink, flip); the tile and its marks are laid out in it from the cell's box.
            view._body = UiFactory.Stretch(UiFactory.CreateRect("Body", root));
            view._layout = BoxLayout.On(view._body);
            view._tile = UiKit.CandyTile("Candy", view._body, null, TileStyle.Board);
            view._layout.Add((RectTransform)view._tile.transform, TileBox);
            return view;
        }

        /// <summary>The candy tile's box in the cell's box: inset 0.8% of the cell on each side.</summary>
        private static Box TileBox(Box cell) => cell.Inset(cell.Width * BoardPictures.TileInset);

        /// <param name="animate">Flip in (a revealed layer or mystery tile); false on build and restart.</param>
        public void ShowTarget(VariantId? visible, VariantId? next, string? keyId, bool animate = false)
        {
            StopAnimation();
            gameObject.SetActive(true);
            _gap.enabled = true;
            _tile.gameObject.SetActive(true);
            if (_stone != null)
            {
                _stone.gameObject.SetActive(false);
            }

            // A hidden mystery tile (FR-039) is the lilac "?" tile, and shows no next layer.
            _tile.Show(visible, TileState.Normal);
            ShowPeek(visible.HasValue ? next : null);
            ShowKey(keyId != null);
            Shown = visible;
            if (animate && isActiveAndEnabled)
            {
                _animation = StartCoroutine(FlipIn());
            }
        }

        /// <summary>A stone obstacle (frame 9): a raised stone block with a crack, on the restored ground.</summary>
        public void ShowStone()
        {
            StopAnimation();
            gameObject.SetActive(true);
            _gap.enabled = false;
            _gap.raycastTarget = false;
            _targetable = false;
            _tile.gameObject.SetActive(false);
            ShowPeek(null);
            ShowKey(false);
            SetTarget(false);
            if (_stone == null)
            {
                _stone = UiFactory.CreateImage("Stone", _body, null, Color.white);
                int seed = 21 + (((Cell.X * 5) + (Cell.Y * 3)) % 6);
                bool flip = ((Cell.X + Cell.Y) % 2) == 1;
                string key = "tile.stone/" + seed + (flip ? "/flip" : string.Empty);
                PictureFit.On(_stone, (w, h) => ProceduralSprites.Picture(key, Mathf.Min(w, h), Mathf.Min(w, h), (pw, ph) => BoardPictures.StoneObstacle(Mathf.Min(pw, ph), seed, flip)), sliced: false, square: true);
                _layout.Add(_stone.rectTransform, b => Box.FromCenter(b.CenterX, b.CenterY, b.Width * (1f + (2f * BoardPictures.ObstacleMargin)), b.Height * (1f + (2f * BoardPictures.ObstacleMargin))));
                _stone.transform.SetAsFirstSibling();
            }

            _stone.gameObject.SetActive(true);
            Shown = null;
        }

        /// <summary>The key mark's position, where a collected key starts its flight (T107).</summary>
        public Vector3 KeyPosition => _keyGlyph != null ? _keyGlyph.transform.position : transform.position;

        public bool HasKey => _key != null && _key.gameObject.activeSelf;

        /// <summary>Tiles take taps only while a booster waits for a target; such tiles carry the pulsing Bloom Burst ring.</summary>
        public void SetTappable(bool tappable)
        {
            _gap.raycastTarget = tappable;
            _targetable = tappable;
            _time = 0f;
            SetTarget(tappable);
        }

        /// <summary>Rings the cell in a special's color while it counts toward that special; null removes it.</summary>
        public void SetCounted(Color? color)
        {
            if (!color.HasValue)
            {
                if (_counted != null)
                {
                    _counted.gameObject.SetActive(false);
                }

                return;
            }

            if (_counted == null)
            {
                _counted = UiKit.RoundRing("Counted", _body, Color.white, _ => _countedRadius, _ => _countedWidth);
                _layout.Add(_counted.rectTransform, b =>
                {
                    Box tile = TileBox(b);
                    _countedWidth = Mathf.Max(UiKit.Units(3f), b.Width * 0.06f);
                    _countedRadius = Mathf.Max(UiKit.Units(3f), tile.Width * 0.1f);
                    return tile;
                });
            }

            _counted.color = color.Value;
            _counted.gameObject.SetActive(true);
            _counted.GetComponent<RoundShape>().Apply();
        }

        public void HideKey()
        {
            if (_key != null)
            {
                _key.gameObject.SetActive(false);
            }
        }

        /// <summary>Draws attention to this tile's key (a locked pod was tapped, T109).</summary>
        public void FlashKey()
        {
            if (HasKey && isActiveAndEnabled)
            {
                StartCoroutine(Flash());
            }
        }

        private System.Collections.IEnumerator Flash()
        {
            Transform key = _key!;
            for (float t = 0f; t < 0.9f; t += Time.unscaledDeltaTime)
            {
                key.localScale = Vector3.one * (1f + (0.35f * Mathf.Abs(Mathf.Sin(t * 10f))));
                yield return null;
            }

            key.localScale = Vector3.one;
        }

        /// <summary>
        /// The clearing style holds this tile (spec 005 FR-038, <c>ClearLook.Holds</c>): it draws the tile itself (eaten,
        /// lifted, in a bubble), so the cell shows its ground until the clear.
        /// </summary>
        public void SetHeld(bool held) => transform.localScale = held ? Vector3.zero : Vector3.one;

        /// <summary>Blossom: the tile sways by <paramref name="degrees"/> beside a just-opened flower (clockwise on screen).</summary>
        public void SetSway(float degrees) => transform.localEulerAngles = new Vector3(0f, 0f, -degrees);

        /// <summary>Open ground: the tile disappears and the restored ground shows through.</summary>
        /// <param name="animate">Shrink away (a Bloomling restored it); false on build and restart.</param>
        public void ShowOpen(bool animate = false)
        {
            StopAnimation();
            Shown = null;
            SetCounted(null);
            SetTarget(false);
            _targetable = false;
            _gap.raycastTarget = false;
            _gap.enabled = false;
            if (animate && isActiveAndEnabled)
            {
                _animation = StartCoroutine(ShrinkAway());
                return;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// The next layer's chip (<c>tile.layer_peek</c>): 40% of the tile in its top-right corner, 3% in from its edges,
        /// a small board-style candy tile in a cream ring (10% of the chip) with a dark rim, over a soft shadow.
        /// </summary>
        private void ShowPeek(VariantId? next)
        {
            if (!next.HasValue)
            {
                if (_peek != null)
                {
                    _peek.gameObject.SetActive(false);
                }

                return;
            }

            if (_peek == null)
            {
                _peek = UiFactory.Stretch(UiFactory.CreateRect("NextLayer", _body));
                BoxLayout layout = BoxLayout.On(_peek);
                float radius = 0f;
                float rimRadius = 0f;
                UiKit.SoftShadow(layout, b => Chip(TileBox(b)), b => b.Width * 0.26f, 0.3f, 0.06f);
                Image rim = UiKit.RoundRect("Rim", _peek, UiTheme.Of(GardenLook.BoardGap), _ => rimRadius);
                Image ring = UiKit.RoundGradient("Ring", _peek, C.CreamTop, C.CreamFace, _ => radius);
                layout.Add(rim.rectTransform, b =>
                {
                    Box chip = Chip(TileBox(b));
                    float line = Mathf.Max(OnePixel, chip.Width * 0.045f);
                    radius = chip.Width * 0.24f;
                    rimRadius = radius + line;
                    return chip.Inset(-line);
                });
                layout.Add(ring.rectTransform, b => Chip(TileBox(b)));
                _peekTile = UiKit.CandyTile("Next", _peek, next, TileStyle.Board);
                layout.Add((RectTransform)_peekTile.transform, b =>
                {
                    Box chip = Chip(TileBox(b));
                    return chip.Inset(Mathf.Max(OnePixel, chip.Width * 0.1f));
                });
                layout.Then(_ =>
                {
                    rim.GetComponent<RoundShape>().Apply();
                    ring.GetComponent<RoundShape>().Apply();
                });
            }

            _peekTile!.Show(next, TileState.Normal);
            _peek.gameObject.SetActive(true);
            _peek.SetAsLastSibling();
        }

        /// <summary>The peek chip's box in a tile's box.</summary>
        private static Box Chip(Box tile)
        {
            float s = tile.Width;
            float chip = s * 0.4f;
            float m = s * 0.03f;
            return new Box(tile.Right - m - chip, tile.Top + m, tile.Right - m, tile.Top + m + chip);
        }

        /// <summary>
        /// A key waiting under the tile (<c>tile.key</c>): the gold key with a brown outline on a cream disc (38% of the
        /// tile, a <c>cream.line</c> ring) in its top-left corner, over a soft shadow.
        /// </summary>
        private void ShowKey(bool show)
        {
            if (!show)
            {
                HideKey();
                return;
            }

            if (_key == null)
            {
                _key = UiFactory.CreateRect("Key", _body);
                BoxLayout layout = BoxLayout.On(_key);
                UiKit.SoftShadow(layout, b => b, b => b.Width / 2f, 0.3f, 0.06f);
                Image line = UiKit.RoundRect("Ring", _key, UiTheme.Of(C.CreamLine));
                Image disc = UiKit.RoundGradient("Disc", _key, C.CreamTop, C.CreamFace);
                Func<float, float, float> sdf = ShapeLibrary.Get("tile.key");
                Image outline = UiFactory.CreateImage("KeyLine", _key, ProceduralSprites.Composite("tile.key/line", (x, y) => sdf(x, y) - 0.09f), UiTheme.Of(C.InkBrown));
                outline.preserveAspect = true;
                _keyGlyph = UiKit.ShapeImage("KeyGlyph", _key, "tile.key", C.MedalGold);
                layout.Add(line.rectTransform, b => b.Inset(-Mathf.Max(OnePixel, b.Width * 0.05f)));
                layout.Add(disc.rectTransform, b => b);
                layout.Add(outline.rectTransform, b => b.Inset(b.Width * 0.14f));
                layout.Add(_keyGlyph.rectTransform, b => b.Inset(b.Width * 0.14f));

                // The key's own rect is the disc, so the flash scales it about its middle.
                _layout.Add(_key, b =>
                {
                    Box tile = TileBox(b);
                    float d = tile.Width * 0.38f;
                    float m = tile.Width * 0.03f;
                    return new Box(tile.Left + m, tile.Top + m, tile.Left + m + d, tile.Top + m + d);
                });
            }

            _key.localScale = Vector3.one;
            _key.gameObject.SetActive(true);
            _key.SetAsLastSibling();
        }

        /// <summary>The Bloom Burst target ring (<c>fx.burst</c>): 5% inside the cell, radius 12% of it, pulsing gently.</summary>
        private void SetTarget(bool on)
        {
            if (!on)
            {
                if (_target != null)
                {
                    _target.gameObject.SetActive(false);
                }

                return;
            }

            if (_target == null)
            {
                _target = UiKit.RoundRing("BurstTarget", _body, UiTheme.Of(C.BoosterBloomBurst), _ => _targetRadius, _ => _targetWidth);
                _layout.Add(_target.rectTransform, b =>
                {
                    float cell = b.Width;
                    _targetWidth = Mathf.Max(UiKit.Units(4f), cell * 0.05f);
                    _targetRadius = (cell * 0.12f) + (_targetWidth / 2f);
                    return b.Inset((cell * 0.05f) - (_targetWidth / 2f));
                });
            }

            _target.gameObject.SetActive(true);
            _target.transform.SetAsLastSibling();
            _target.GetComponent<RoundShape>().Apply();
            Pulse();
        }

        private void Update()
        {
            if (_targetable && _target != null && _target.gameObject.activeSelf)
            {
                _time += Time.unscaledDeltaTime;
                Pulse();
            }
        }

        /// <summary>The target ring's alpha: 0.85 × (0.75 + 0.25 sin(6t)), as the playtest's.</summary>
        private void Pulse()
        {
            if (_target != null)
            {
                Color c = UiTheme.Of(C.BoosterBloomBurst);
                _target.color = new Color(c.r, c.g, c.b, 0.85f * (0.75f + (0.25f * Mathf.Sin(_time * 6f))));
            }
        }

        private System.Collections.IEnumerator ShrinkAway()
        {
            _targetable = false;
            for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.18f;
                _body.localScale = Vector3.one * (1f - (0.8f * k * k));
                _body.localEulerAngles = new Vector3(0f, 0f, 25f * k);
                yield return null;
            }

            _body.localScale = Vector3.one;
            _body.localEulerAngles = Vector3.zero;
            _animation = null;
            gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator FlipIn()
        {
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.2f;
                _body.localScale = new Vector3(Mathf.Abs(Mathf.Cos(k * Mathf.PI)), 1f, 1f);
                yield return null;
            }

            _body.localScale = Vector3.one;
            _animation = null;
        }

        private void StopAnimation()
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
                _animation = null;
            }

            _body.localScale = Vector3.one;
            _body.localEulerAngles = Vector3.zero;
        }

        /// <summary>One screen pixel in canvas units (the playtest's 1 px minimums).</summary>
        private static float OnePixel => 1f / Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
    }
}
