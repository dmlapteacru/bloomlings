using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Art.Variants;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Boards;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Variants;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// A special object on the board (T108): the Garden Gate / hedge seal, the Fountain, the Chest (L150), and the Statue
    /// or Bridge (L250), in the reference look (spec 005 contracts/look.md §4.1; the playtest's <c>BoardPainter.Special</c>):
    /// a candy-like raised block in the special's color made vivid (<see cref="GardenLook.SpecialFace"/>: a crisp outline
    /// darkened 0.45, a lip darkened 0.28, a face lightened 0.28 at the top, a light bevel and a faint gloss) with its white
    /// glyph outlined in the darker shade, and until it opens its counter on a big green count badge over its bottom edge.
    /// Its condition is always visible (FR-037, FR-038): a key door shows the gold key, "restore N &lt;variant&gt; around
    /// it" a small candy tile of that exact variant and "restore this region" the region mark, on a cream chip in the
    /// block's top-left corner. When it triggers: a gate swings open and fades; a chest pops open with a sparkle and fades;
    /// a statue glows and fades; a Fountain sprays droplets and stays as a landmark; a bridge is repaired and stays.
    /// </summary>
    public sealed class SpecialView : MonoBehaviour
    {
        private CanvasGroup _fade = null!;
        private BoxLayout _layout = null!;
        private Image _glyph = null!;
        private Image _glyphLine = null!;
        private RectTransform? _condition;
        private Image _badge = null!;
        private TextMeshProUGUI _counter = null!;
        private Rgba _color;
        private int _columns = 1;
        private bool _showsCounter;
        private bool _triggered;
        private Coroutine? _animation;

        public string SpecialId { get; private set; } = string.Empty;

        public SpecialType Type { get; private set; }

        public IReadOnlyList<CellPos> Cells { get; private set; } = new CellPos[0];

        public static SpecialView Create(Transform parent, SpecialInfo special, VariantVisualCatalog? visuals)
        {
            RectTransform root = UiFactory.CreateRect("Special " + special.Id, parent);
            var view = root.gameObject.AddComponent<SpecialView>();
            view.SpecialId = special.Id;
            view.Type = special.Type;
            view.Cells = special.Cells;
            view._color = RgbaOf(special.Type);
            view._fade = root.gameObject.AddComponent<CanvasGroup>();
            view._fade.blocksRaycasts = false;
            view._fade.interactable = false;
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            foreach (CellPos cell in special.Cells)
            {
                minX = Mathf.Min(minX, cell.X);
                maxX = Mathf.Max(maxX, cell.X);
            }

            view._columns = special.Cells.Count > 0 ? maxX - minX + 1 : 1;
            view.Build(special);
            view.SetProgress(special.Progress, special.Total, special.Condition.Kind);
            if (special.Triggered)
            {
                view.ShowTriggered();
            }

            return view;
        }

        /// <summary>The block, the glyph, the condition chip and the counter, laid out from the special's box.</summary>
        private void Build(SpecialInfo special)
        {
            RectTransform root = (RectTransform)transform;
            _layout = BoxLayout.On(root);
            // The block in its token made vivid (GardenLook.SpecialFace), candy-bright beside the tiles.
            Rgba color = GardenLook.SpecialFace(_color);
            float radius = 0f;
            float innerRadius = 0f;
            float bevelRadius = 0f;
            float bevelWidth = 0f;
            Image outline = UiKit.RoundRect("Outline", root, UiTheme.Of(color.Darken(0.45f)), _ => radius);
            Image lip = UiKit.RoundRect("Lip", root, UiTheme.Of(color.Darken(0.28f)), _ => innerRadius);
            Image face = UiKit.RoundGradient("Face", root, color.Lighten(0.28f), color, _ => innerRadius);
            Image bevel = UiKit.RoundRing("Bevel", root, UiTheme.Of(color.Lighten(0.45f).WithAlpha(0.45f)), _ => bevelRadius, _ => bevelWidth);
            Image gloss = UiKit.RoundGradient("Gloss", root, Rgba.White.WithAlpha(0.16f), Rgba.White.WithAlpha(0f));
            _layout.Add(outline.rectTransform, b =>
            {
                Box block = Block(b);
                float s = Mathf.Min(block.Width, block.Height);
                radius = Mathf.Max(3f * OnePixel, s * 0.1f);
                return block;
            });
            _layout.Add(lip.rectTransform, b =>
            {
                Box block = Block(b);
                float line = Line(block);
                innerRadius = Mathf.Max(OnePixel, radius - line);
                return block.Inset(line);
            });
            _layout.Add(face.rectTransform, Face);
            _layout.Add(bevel.rectTransform, b =>
            {
                float line = Line(Block(b));
                bevelWidth = line * 0.8f;
                bevelRadius = innerRadius + (line * 0.4f);
                return Face(b).Inset(line * 0.2f);
            });
            _layout.Add(gloss.rectTransform, b =>
            {
                Box f = Face(b);
                return new Box(f.Left + (f.Width * 0.1f), f.Top + (f.Height * 0.06f), f.Right - (f.Width * 0.1f), f.Top + (f.Height * 0.26f));
            });

            _glyphLine = UiFactory.CreateImage("GlyphLine", root, null, UiTheme.Of(color.Darken(0.45f)));
            _glyphLine.preserveAspect = true;
            _glyph = UiFactory.CreateImage("Glyph", root, null, Color.white);
            _glyph.preserveAspect = true;
            SetGlyph(ShapeOf(Type, special.Triggered));
            _layout.Add(_glyphLine.rectTransform, GlyphBox);
            _layout.Add(_glyph.rectTransform, GlyphBox);

            _condition = Condition(root, special.Condition);
            _counter = UiKit.CountBadge("Counter", root, out _badge);
            _layout.Add(_badge.rectTransform, b =>
            {
                // The counter on a count badge over the block's bottom edge, big enough to read at a glance.
                Box f = Face(b);
                float size = Cell(b) * 0.36f;
                return Box.FromCenter(f.CenterX, f.Bottom - (size * 0.12f), size * 1.26f, size * 1.26f);
            });
            _layout.Then(_ =>
            {
                foreach (Image image in new[] { outline, lip, face, bevel, gloss })
                {
                    image.GetComponent<RoundShape>().Apply();
                }
            });
        }

        /// <summary>One cell's side in the special's box.</summary>
        private float Cell(Box box) => box.Width / Mathf.Max(1, _columns);

        /// <summary>The block: the special's box inset like a tile (0.8% of a cell).</summary>
        private Box Block(Box box) => box.Inset(Cell(box) * BoardPictures.TileInset);

        private static float Line(Box block) => Mathf.Max(OnePixel, Mathf.Min(block.Width, block.Height) * 0.022f);

        /// <summary>The block's face: inside its outline, above its lip (7% of its shorter side).</summary>
        private Box Face(Box box)
        {
            Box block = Block(box);
            Box inner = block.Inset(Line(block));
            float lip = Mathf.Min(block.Width, block.Height) * 0.07f;
            return new Box(inner.Left, inner.Top, inner.Right, inner.Bottom - lip);
        }

        /// <summary>The glyph: 66% of the face, or 56% and a little higher above the counter.</summary>
        private Box GlyphBox(Box box)
        {
            Box f = Face(box);
            float g = Mathf.Min(f.Width, f.Height) * (_showsCounter ? 0.56f : 0.66f);
            return Box.FromCenter(f.CenterX, f.CenterY - (f.Height * (_showsCounter ? 0.1f : 0f)), g, g);
        }

        private void SetGlyph(string shape)
        {
            Func<float, float, float> sdf = ShapeLibrary.Get(shape);
            _glyphLine.sprite = ProceduralSprites.Composite(shape + "/line", (x, y) => sdf(x, y) - 0.08f);
            _glyph.sprite = ProceduralSprites.Shape(shape);
        }

        /// <summary>
        /// The condition chip (FR-037, FR-038) in the block's top-left corner: the gold key on a cream disc for a key
        /// door, a small candy tile of the exact variant in a cream ring for "restore N &lt;variant&gt;", the region mark on a
        /// cream disc for "restore this region".
        /// </summary>
        private RectTransform? Condition(RectTransform root, SpecialCondition condition)
        {
            if (condition.Kind != SpecialConditionKind.Key && !condition.Variant.HasValue && condition.Kind != SpecialConditionKind.ClearRegion)
            {
                return null;
            }

            RectTransform chip = UiFactory.CreateRect("Condition", root);
            BoxLayout layout = BoxLayout.On(chip);
            UiKit.SoftShadow(layout, b => b, b => b.Width / 2f, 0.3f, 0.06f);
            bool variant = condition.Kind != SpecialConditionKind.Key && condition.Variant.HasValue;
            float ringRadius = 0f;
            Image rim = UiKit.RoundRect("Rim", chip, UiTheme.Of(variant ? GardenLook.BoardGap : C.CreamLine), _ => ringRadius);
            Image disc = UiKit.RoundGradient("Disc", chip, C.CreamTop, C.CreamFace, b => variant ? b.Width * 0.24f : b.Width / 2f);
            layout.Add(rim.rectTransform, b =>
            {
                float line = Mathf.Max(OnePixel, b.Width * 0.05f);
                ringRadius = (variant ? b.Width * 0.24f : b.Width / 2f) + line;
                return b.Inset(-line);
            });
            layout.Add(disc.rectTransform, b => b);
            if (variant)
            {
                CandyTileView tile = UiKit.CandyTile("Variant", chip, condition.Variant!.Value, TileStyle.Board);
                layout.Add((RectTransform)tile.transform, b => b.Inset(Mathf.Max(OnePixel, b.Width * 0.1f)));
            }
            else if (condition.Kind == SpecialConditionKind.Key)
            {
                Func<float, float, float> sdf = ShapeLibrary.Get("tile.key");
                Image line = UiFactory.CreateImage("KeyLine", chip, ProceduralSprites.Composite("tile.key/line", (x, y) => sdf(x, y) - 0.09f), UiTheme.Of(C.InkBrown));
                line.preserveAspect = true;
                Image key = UiKit.ShapeImage("Key", chip, "tile.key", C.MedalGold);
                layout.Add(line.rectTransform, b => b.Inset(b.Width * 0.14f));
                layout.Add(key.rectTransform, b => b.Inset(b.Width * 0.14f));
            }
            else
            {
                Image region = UiKit.ShapeImage("Region", chip, "special.region", C.InkBrown);
                layout.Add(region.rectTransform, b => b.Inset(b.Width * 0.18f));
            }

            layout.Then(_ =>
            {
                rim.GetComponent<RoundShape>().Apply();
                disc.GetComponent<RoundShape>().Apply();
            });
            _layout.Add(chip, b =>
            {
                Box block = Block(b);
                float s = Mathf.Min(block.Width, block.Height);
                float d = s * 0.38f;
                float m = s * 0.03f;
                return new Box(block.Left + m, block.Top + m, block.Left + m + d, block.Top + m + d);
            });
            return chip;
        }

        /// <summary>The counter: "3/6" on a count badge until the special opens; a key door, or a one-step condition, shows none.</summary>
        public void SetProgress(int progress, int total, SpecialConditionKind kind)
        {
            _showsCounter = !_triggered && kind != SpecialConditionKind.Key && total > 1;
            _counter.text = progress.ToString(CultureInfo.InvariantCulture) + "/" + total.ToString(CultureInfo.InvariantCulture);
            _badge.gameObject.SetActive(_showsCounter);
            _layout.Apply();
            if (isActiveAndEnabled && kind != SpecialConditionKind.Key)
            {
                Play(Bump());
            }
        }

        /// <summary>The color a special is drawn in; its counted cells are outlined in it too.</summary>
        public static Color ColorOf(SpecialType type) => UiTheme.Of(RgbaOf(type));

        private static Rgba RgbaOf(SpecialType type) => type switch
        {
            SpecialType.Fountain => C.SpecialFountain,
            SpecialType.Chest => C.SpecialChest,
            SpecialType.Statue => C.SpecialStatue,
            SpecialType.Bridge => C.SpecialBridge,
            _ => C.SpecialGate,
        };

        /// <summary>Whether the object stays on the board after it triggers (a landmark) or its cells turn to open ground.</summary>
        public static bool StaysAfterTrigger(SpecialType type) => type == SpecialType.Fountain || type == SpecialType.Bridge;

        private static string ShapeOf(SpecialType type, bool triggered) => type switch
        {
            SpecialType.Fountain => "special.fountain",
            SpecialType.Chest => "special.chest",
            SpecialType.Statue => "special.statue",
            SpecialType.Bridge => triggered ? "special.bridge" : "special.bridge_broken",
            _ => "special.gate",
        };

        /// <summary>The trigger animation; a gate, chest or statue then disappears (its cells are open ground now).</summary>
        /// <param name="overlay">Where the droplets and sparkles are drawn.</param>
        public void Trigger(RectTransform overlay, float cellSize)
        {
            HideCondition();
            if (!isActiveAndEnabled)
            {
                ShowTriggered();
                return;
            }

            switch (Type)
            {
                case SpecialType.Fountain:
                    Effects.UiFx.Puff(overlay, transform.position, new Color(0.62f, 0.82f, 0.95f, 0.9f), 10, cellSize * 1.8f, cellSize * 0.25f, 0.7f);
                    Play(Spray());
                    break;
                case SpecialType.Chest:
                    Effects.UiFx.Puff(overlay, transform.position, UiTheme.Of(C.MedalGold), 8, cellSize * 1.4f, cellSize * 0.3f, 0.5f, ProceduralSprites.Star);
                    Play(PopOpen());
                    break;
                case SpecialType.Statue:
                    Effects.UiFx.Puff(overlay, transform.position, Color.white, 8, cellSize * 1.2f, cellSize * 0.25f, 0.5f, ProceduralSprites.Star);
                    Play(Glow());
                    break;
                case SpecialType.Bridge:
                    SetGlyph(ShapeOf(Type, triggered: true));
                    Play(Bump());
                    break;
                default:
                    Play(SwingOpen());
                    break;
            }
        }

        /// <summary>The counter and the condition chip go once the special has opened.</summary>
        private void HideCondition()
        {
            _triggered = true;
            _showsCounter = false;
            _badge.gameObject.SetActive(false);
            if (_condition != null)
            {
                _condition.gameObject.SetActive(false);
            }

            _layout.Apply();
        }

        private void ShowTriggered()
        {
            HideCondition();
            SetGlyph(ShapeOf(Type, triggered: true));
            if (!StaysAfterTrigger(Type))
            {
                gameObject.SetActive(false);
            }
        }

        private void Play(IEnumerator routine)
        {
            if (_animation != null)
            {
                StopCoroutine(_animation);
                transform.localScale = Vector3.one;
                _fade.alpha = 1f;
            }

            _animation = StartCoroutine(routine);
        }

        private IEnumerator Bump()
        {
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                transform.localScale = Vector3.one * (1f + (0.12f * Mathf.Sin(t / 0.2f * Mathf.PI)));
                yield return null;
            }

            transform.localScale = Vector3.one;
            _animation = null;
        }

        private IEnumerator SwingOpen()
        {
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.5f;
                transform.localScale = new Vector3(1f - (0.9f * k), 1f + (0.1f * k), 1f);
                _fade.alpha = 1f - k;
                yield return null;
            }

            gameObject.SetActive(false);
            _animation = null;
        }

        private IEnumerator PopOpen()
        {
            for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.45f;
                transform.localScale = new Vector3(1f + (0.15f * Mathf.Sin(k * Mathf.PI)), 1f + (0.35f * k), 1f);
                _fade.alpha = 1f - (k * k);
                yield return null;
            }

            gameObject.SetActive(false);
            _animation = null;
        }

        private IEnumerator Glow()
        {
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.5f;
                _glyph.color = Color.Lerp(Color.white, UiTheme.Of(C.MedalGold.Lighten(0.5f)), Mathf.Sin(k * Mathf.PI));
                _fade.alpha = 1f - k;
                transform.localScale = Vector3.one * (1f + (0.1f * k));
                yield return null;
            }

            gameObject.SetActive(false);
            _animation = null;
        }

        private IEnumerator Spray()
        {
            for (float t = 0f; t < 0.8f; t += Time.unscaledDeltaTime)
            {
                transform.localScale = Vector3.one * (1f + (0.2f * Mathf.Abs(Mathf.Sin(t * 12f)) * (1f - (t / 0.8f))));
                yield return null;
            }

            transform.localScale = Vector3.one;
            _animation = null;
        }

        /// <summary>One screen pixel in canvas units (the playtest's 1 px minimums).</summary>
        private static float OnePixel => 1f / Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
    }
}
