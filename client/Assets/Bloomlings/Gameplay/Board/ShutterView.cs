using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// A Twig wooden shutter on a round board in the Unity client (rules 17 §8 «Twig Деревянные створки», spec 006 FR-070; the
    /// owner's look B of 2026-11-05; the playtest's <c>Kit.ShutterLid</c> and <c>Kit.ShutterOver</c>), drawn from the kit's
    /// <see cref="ShutterLook"/>: the crate lid (its shadow and front edge, its slats, its side battens) in the cubes' layer
    /// right after the cubes of its bottom row, so the nearer rows stand in front of its edge; over the board the sparks of its
    /// counted cubes flying to the tag, the tag on its nail (the pods' cube with the count still to go, swaying and popping),
    /// and as it opens the slats popping off with dust and leaves. Placed in screen pixels relative to the board's grid.
    /// Presentation only.
    /// </summary>
    public sealed class ShutterView
    {
        private readonly Image _base;
        private readonly List<Image> _slats = new List<Image>();
        private readonly Image _batten0;
        private readonly Image _batten1;
        private readonly RectTransform _over;
        private readonly List<Image> _sparks = new List<Image>();
        private readonly List<Image> _dust = new List<Image>();
        private readonly List<Image> _leaves = new List<Image>();
        private readonly RectTransform _tag;
        private readonly CanvasGroup _tagGroup;
        private readonly Image _hanger;
        private readonly Image _cube;
        private readonly TextMeshProUGUI _count;
        private readonly List<ShutterBit> _bits = new List<ShutterBit>();
        private readonly Dictionary<Image, (string Key, int W, int H)> _shown = new Dictionary<Image, (string Key, int W, int H)>();
        private ShutterLook _look;
        private Box _grid;
        private int _left = -1;
        private bool _symbols;
        private bool _cubeSet;

        private ShutterView(Transform lidParent, Transform overParent, ShutterLook look, Box grid)
        {
            _look = look;
            _grid = grid;
            _base = NewImage("ShutterBase", lidParent);
            for (int k = 0; k < look.SlatCount; k++)
            {
                _slats.Add(NewImage("Slat", lidParent));
            }

            _batten0 = NewImage("Batten", lidParent);
            _batten1 = NewImage("Batten", lidParent);
            _over = UiFactory.Stretch(UiFactory.CreateRect("Shutter", overParent));
            _tag = UiFactory.CreateRect("Tag", _over);
            _tagGroup = _tag.gameObject.AddComponent<CanvasGroup>();
            _tagGroup.blocksRaycasts = false;
            _hanger = NewImage("Hanger", _tag);
            _cube = NewImage("TagCube", _tag);
            _count = UiKit.KitLabel("Count", _tag, string.Empty, GameplaySkin.PodCount, PodChip.CubeCountLook(false));
            _count.raycastTarget = false;
        }

        /// <summary>A shutter's view: its lid's images at the end of <paramref name="lidParent"/> (the cubes' layer), the rest in <paramref name="overParent"/>.</summary>
        public static ShutterView Create(Transform lidParent, Transform overParent, ShutterLook look, Box grid) => new ShutterView(lidParent, overParent, look, grid);

        public ShutterLook Look => _look;

        /// <summary>The same shutter on a new layout of the board.</summary>
        public void Relayout(ShutterLook look, Box grid)
        {
            _look = look;
            _grid = grid;
            _cubeSet = false;
        }

        /// <summary>
        /// Shows the shutter with <paramref name="left"/> still to go, <paramref name="sinceCount"/> seconds after its last
        /// counted cube (negative: none), at <paramref name="open"/> of its opening (0 closed, 1 gone), at
        /// <paramref name="seconds"/>; <paramref name="a"/> and <paramref name="layout"/> give the sparks (null: none).
        /// </summary>
        public void Show(int left, float sinceCount, float open, float seconds, bool symbols, RoundAnimator? a, RoundBoardLayout? layout)
        {
            ShutterLook look = _look;
            bool gone = open >= 1f;
            float baseAlpha = gone ? 0f : ShutterLook.BaseAlpha(open);
            _base.gameObject.SetActive(baseAlpha > 0f);
            if (baseAlpha > 0f)
            {
                (Box box, string key, Func<int, int, byte[]> render) = look.Base();
                Picture(_base, box, key, render);
                _base.color = new Color(1f, 1f, 1f, baseAlpha);
            }

            for (int k = 0; k < _slats.Count; k++)
            {
                ShutterSlat slat = look.Slat(k, open);
                bool on = !gone && slat.Alpha > 0f;
                _slats[k].gameObject.SetActive(on);
                if (on)
                {
                    int seed = slat.Seed;
                    Picture(_slats[k], slat.Box, ShutterLook.SlatKey(seed), (w, h) => ShutterLook.SlatPicture(w, h, seed));
                    _slats[k].rectTransform.localEulerAngles = new Vector3(0f, 0f, -slat.Degrees);
                    _slats[k].color = new Color(1f, 1f, 1f, slat.Alpha);
                }
            }

            float sides = gone ? 0f : ShutterLook.SidesAlpha(open);
            (Box l, Box r) = look.Battens();
            foreach ((Image image, Box box) in new[] { (_batten0, l), (_batten1, r) })
            {
                image.gameObject.SetActive(sides > 0f);
                if (sides > 0f)
                {
                    Picture(image, box, UiRaster.ShutterBattenKey, UiRaster.ShutterBatten);
                    image.color = new Color(1f, 1f, 1f, sides);
                }
            }

            ShowSparks(open, a, layout);
            look.Dust(gone ? 0f : open, _bits);
            Bits(_dust, "Dust", (image, bit) =>
            {
                Picture(image, bit.Box, UiRaster.ShutterDustKey, (w, h) => UiRaster.ShutterDust(Math.Min(w, h)));
                image.color = new Color(1f, 1f, 1f, bit.Alpha);
            });
            look.Leaves(gone ? 0f : open, _bits);
            Bits(_leaves, "Leaf", (image, bit) =>
            {
                UiKit.PlaceBox(image.rectTransform, bit.Box, _grid);
                image.rectTransform.localEulerAngles = new Vector3(0f, 0f, -bit.Degrees);
                Color moss = UiTheme.Of(C.StoneMoss);
                image.color = new Color(moss.r, moss.g, moss.b, bit.Alpha);
            }, "symbol.leaf");

            float fade = gone ? 0f : ShutterLook.TagAlpha(open);
            _tag.gameObject.SetActive(fade > 0f);
            if (fade <= 0f)
            {
                return;
            }

            // The tag swings round its nail: its holder is a square round the nail, turned.
            (float nx, float ny) = look.Nail;
            Box tag = look.Tag;
            float reach = Math.Max(tag.Bottom - ny, tag.Width) * 1.6f;
            var holder = Box.FromCenter(nx, ny, reach * 2f, reach * 2f);
            UiKit.PlaceBox(_tag, holder, _grid);
            _tag.localEulerAngles = new Vector3(0f, 0f, -ShutterLook.Sway(seconds));
            _tagGroup.alpha = fade;
            PictureIn(_hanger, look.Hanger, holder, UiRaster.ShutterHangerKey, UiRaster.ShutterHanger);
            float scale = ShutterLook.TagScale(sinceCount, open);
            Box cube = Box.FromCenter(tag.CenterX, tag.CenterY, tag.Width * scale, tag.Height * scale);
            UiKit.PlaceBox(_cube.rectTransform, cube, holder);
            if (!_cubeSet || symbols != _symbols)
            {
                _cubeSet = true;
                _symbols = symbols;
                int material = look.Material.Index;
                _cube.sprite = RoundSprites.PodCube(material, RoundPodLook.Placed, symbols, UiRaster.Quantize(tag.Width));
                _cube.type = Image.Type.Simple;
            }

            if (left != _left)
            {
                _left = left;
                _count.text = left.ToString(CultureInfo.InvariantCulture);
            }

            Box count = UiKit.ToLocal(PodChip.Cube(cube, false).Count, holder);
            KitText.Place(_count, GameplaySkin.PodCount, count.CenterX, count.CenterY, count.Height, count.Width);
        }

        private void ShowSparks(float open, RoundAnimator? a, RoundBoardLayout? layout)
        {
            int n = 0;
            if (a != null && layout != null && open <= 0f)
            {
                Rgba color = RoundLook.ColorOf(_look.Material);
                char code = _look.Material.Code;
                foreach (int cell in a.ShutterCells(_look.Index))
                {
                    long at = a.ClearedAt(cell);
                    if (at < 0 || at > a.Now || _look.Covers(cell))
                    {
                        continue;
                    }

                    Box? spark = _look.Spark(layout.CellBox(cell), (a.Now - at) / 1_000_000f);
                    if (!spark.HasValue)
                    {
                        continue;
                    }

                    if (n == _sparks.Count)
                    {
                        _sparks.Add(NewImage("Spark", _over));
                        _tag.SetAsLastSibling();
                    }

                    Image image = _sparks[n++];
                    image.gameObject.SetActive(true);
                    Picture(image, spark.Value, ShutterLook.SparkKey(code), (w, h) => UiRaster.ShutterSpark(Math.Min(w, h), color));
                }
            }

            for (int i = n; i < _sparks.Count; i++)
            {
                _sparks[i].gameObject.SetActive(false);
            }
        }

        private void Bits(List<Image> pool, string name, Action<Image, ShutterBit> show, string? shape = null)
        {
            for (int i = 0; i < _bits.Count; i++)
            {
                if (i == pool.Count)
                {
                    Image image = shape != null ? UiKit.ShapeImage(name, _over, shape, Rgba.White) : NewImage(name, _over);
                    image.raycastTarget = false;
                    pool.Add(image);
                    _tag.SetAsLastSibling();
                }

                pool[i].gameObject.SetActive(true);
                show(pool[i], _bits[i]);
            }

            for (int i = _bits.Count; i < pool.Count; i++)
            {
                pool[i].gameObject.SetActive(false);
            }
        }

        private void Picture(Image image, Box box, string key, Func<int, int, byte[]> render) => PictureIn(image, box, _grid, key, render);

        // An image's picture at its box's pixel size rounded up to 8, placed relative to parent (screen boxes).
        private void PictureIn(Image image, Box box, Box parent, string key, Func<int, int, byte[]> render)
        {
            UiKit.PlaceBox(image.rectTransform, box, parent);
            int w = UiRaster.Quantize(box.Width);
            int h = UiRaster.Quantize(box.Height);
            if (!_shown.TryGetValue(image, out (string Key, int W, int H) was) || was.Key != key || was.W != w || was.H != h)
            {
                _shown[image] = (key, w, h);
                image.sprite = ProceduralSprites.Picture(key, w, h, render);
                image.type = Image.Type.Simple;
            }
        }

        private static Image NewImage(string name, Transform parent)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            image.raycastTarget = false;
            return image;
        }
    }
}
