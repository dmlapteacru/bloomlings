using System;
using System.Collections.Generic;
using System.Globalization;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Workers;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Screens;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using Bloomlings.Core.Rounds;
using Bloomlings.Core.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Effects
{
    /// <summary>
    /// Draws one layer of the clearing style's items (spec 005 FR-038; the kit's <see cref="ClearLook"/>, the same list
    /// the playtest draws) with pooled Bloomling figures (the walkers, in their family's outfit) and images: candy tiles,
    /// circles, rings, rounded boxes and the shape library's shapes. Each item is placed by its center, turned and
    /// squashed about it; the items keep the list's order. <see cref="Map"/> turns the kit's cell units into this
    /// layer's coordinates (from its bottom left), <see cref="Cell"/> is a cell's size in them. A round level's items
    /// (<see cref="FxItem.Material"/>, spec 006 FR-048) are the material's plain cube (its symbol only in the symbols mode,
    /// <see cref="Symbols"/>) and the hero of the material's family tinted in its colour; with <see cref="HeroWalkers"/>
    /// (the Store's previews) a variant's walker is its family's hero tinted in the variant's colour too.
    /// </summary>
    public sealed class ClearFxView : MonoBehaviour
    {
        public bool CatalogIcons { get; set; }
        private readonly List<Image> _images = new List<Image>();
        private readonly List<Image> _creatures = new List<Image>();
        private readonly List<HeroWalker> _heroes = new List<HeroWalker>();
        private readonly List<(BloomlingFigure Figure, CanvasGroup Group, (VariantId Variant, int Material, bool Hero)? Shown)> _figures = new List<(BloomlingFigure, CanvasGroup, (VariantId, int, bool)?)>();
        private RectTransform _layer = null!;

        /// <summary>A point in cell units (y down) to this layer's coordinates from its bottom left.</summary>
        public Func<float, float, Vector2> Map { get; set; } = (x, y) => new Vector2(x, -y);

        /// <summary>A cell's size in this layer's units.</summary>
        public float Cell { get; set; } = 1f;

        /// <summary>What each family wears (the Wardrobe, FR-063); presentation only.</summary>
        public Func<Family, Outfit>? Outfits { get; set; }

        /// <summary>
        /// The walkers are the family hero tinted in their colour (spec 006 FR-048: round levels and the Store's previews); a
        /// round item (<see cref="FxItem.Material"/>) always is.
        /// </summary>
        public bool HeroWalkers { get; set; }

        /// <summary>A round level's symbols mode (spec 006 FR-047): the held cubes show their material's symbol.</summary>
        public bool Symbols { get; set; }

        public static ClearFxView Create(Transform parent, string name)
        {
            RectTransform rect = UiFactory.Stretch(UiFactory.CreateRect(name, parent));
            var view = rect.gameObject.AddComponent<ClearFxView>();
            view._layer = rect;
            return view;
        }

        /// <summary>Draws the items of <paramref name="layer"/>, hiding the pooled pieces it does not need.</summary>
        public void Render(IReadOnlyList<FxItem> items, FxLayer layer)
        {
            int images = 0;
            int figures = 0;
            int creatures = 0;
            int heroes = 0;
            int order = 0;
            WalkerLook look = WalkerLook.Current;

            // The lifted and carried cubes, a held cube's effects and the hands over it come after the layer's other items
            // (FxItem.Lifted; the playtest's ClearPainter), so no other walker hides them.
            for (int pass = 0; pass < 2; pass++)
            foreach (FxItem item in items)
            {
                if (item.Layer != layer || Mathf.Abs(item.Sx) < 0.001f || Mathf.Abs(item.Sy) < 0.001f || item.Lifted != (pass == 1))
                {
                    continue;
                }

                Vector2 center = Map(item.X, item.Y);
                if (item.Kind == FxKind.Character && item.Material >= 0 && item.Pose >= 0 && !look.IsDefault)
                {
                    Family family = RoundMaterial.All[item.Material].Family;
                    if (look.Heroes)
                    {
                        // The real V2 hero of the cube's family (the owner's experiment of 2026-11-04), or only his hands over the cube.
                        HeroWalker hero = HeroAt(heroes++, family);
                        Box cell = WalkerLook.HeroCell(item.Box);
                        hero.Show(item.Pose == WalkerLook.PoseHands, cell, RoundLook.HeroHands(cell, family), Cell, item.Alpha);
                        Place(hero.Root, Map(cell.CenterX, cell.CenterY), cell.Width * Cell, cell.Height * Cell, item);
                        hero.Root.SetSiblingIndex(order++);
                        continue;
                    }

                    if (look.DrawsCreature(item.Material))
                    {
                        // The drawn creature with arms and legs (UiRaster.Creature), mirrored for the left from the side.
                        Image creature = CreatureAt(creatures++);
                        RoundMaterial material = RoundMaterial.All[item.Material];
                        int pose = item.Pose;
                        Rgba body = look.BodyOf(RoundLook.ColorOf(material));
                        creature.sprite = ProceduralSprites.Picture("walker.creature." + material.Code + "." + pose, CreaturePixels, CreaturePixels,
                            (w, h) => UiRaster.Creature(w, h, body, C.InkBrown, family, pose));
                        creature.color = new Color(1f, 1f, 1f, item.Alpha);
                        Box box = WalkerLook.CreatureBox(item.Box);
                        Place(creature.rectTransform, Map(box.CenterX, box.CenterY), box.Width * Cell, box.Height * Cell, item);
                        if (item.Facing < 0 && pose <= WalkerLook.PoseSide)
                        {
                            creature.rectTransform.localScale = new Vector3(-item.Sx, item.Sy, 1f);
                        }

                        creature.rectTransform.SetSiblingIndex(order++);
                        continue;
                    }
                }

                if (item.Kind == FxKind.Tile && item.Material >= 0 && (item.Lifted || !look.IsDefault))
                {
                    // A held or carried cube: the board's own cube picture, placed as the board places one in its cell.
                    Image cube = ImageAt(images++);
                    BoardCubeLook cubeLook = BoardCubeLook.Default;
                    float edge = item.W;
                    float top = item.Y - (edge * cubeLook.RowShare / 2f);
                    float height = edge * cubeLook.PictureShare;
                    float width = edge * (1f + (2f * cubeLook.Overlap));
                    cube.sprite = RoundSprites.BoardCube(item.Material, Symbols, CubePixels, UiRaster.Quantize(CubePixels * cubeLook.PictureShare / (1f + (2f * cubeLook.Overlap))), cubeLook);
                    cube.color = new Color(1f, 1f, 1f, item.Alpha);
                    Place(cube.rectTransform, Map(item.X, top + (height / 2f)), width * Cell, height * Cell, item);
                    cube.rectTransform.SetSiblingIndex(order++);
                    continue;
                }

                if (item.Kind == FxKind.Character)
                {
                    RectTransform figure = Figure(figures++, item.Variant, item.Material, item.Alpha);
                    Place(figure, center, item.W * Cell*(CatalogIcons ? 1.5f : 1), item.H * Cell*(CatalogIcons ? 1.5f : 1), item);
                    figure.SetSiblingIndex(order++);
                    continue;
                }

                Image image = ImageAt(images++);
                float margin = item.Kind == FxKind.Tile || item.Kind == FxKind.Shape ? 1f : ShapeRaster.Margin;
                float w = item.W * Cell;
                float h = item.H * Cell;
                (Sprite sprite, float side) = SpriteOf(item);
                image.sprite = sprite;
                Color color = UiTheme.Of(item.Kind == FxKind.Tile ? Rgba.White : item.Color);
                image.color = new Color(color.r, color.g, color.b, color.a * item.Alpha);
                if (side > 0f)
                {
                    // A rounded box drawn in a square picture of its longer side, so its corners keep their round.
                    w = h = side * Cell;
                }

                Place(image.rectTransform, center, w * margin, h * margin, item);
                image.rectTransform.SetSiblingIndex(order++);
            }

            for (int i = images; i < _images.Count; i++)
            {
                _images[i].gameObject.SetActive(false);
            }

            for (int i = figures; i < _figures.Count; i++)
            {
                _figures[i].Figure.Rect.gameObject.SetActive(false);
            }

            for (int i = creatures; i < _creatures.Count; i++)
            {
                _creatures[i].gameObject.SetActive(false);
            }

            for (int i = heroes; i < _heroes.Count; i++)
            {
                _heroes[i].Root.gameObject.SetActive(false);
            }
        }

        // The creature pictures' size in pixels, and a held cube's width (its picture's height follows the board's cube look).
        private const int CreaturePixels = 96;
        private const int CubePixels = 64;

        private Image CreatureAt(int index)
        {
            if (index >= _creatures.Count)
            {
                Image image = UiFactory.CreateImage("Creature", _layer, null, Color.white);
                image.raycastTarget = false;
                _creatures.Add(image);
            }

            Image found = _creatures[index];
            found.gameObject.SetActive(true);
            return found;
        }

        private HeroWalker HeroAt(int index, Family family)
        {
            if (index >= _heroes.Count)
            {
                _heroes.Add(new HeroWalker(_layer));
            }

            HeroWalker found = _heroes[index];
            found.Wear(family);
            found.Root.gameObject.SetActive(true);
            return found;
        }

        /// <summary>
        /// A real V2 hero as a walker (the owner's experiment of 2026-11-04; <see cref="WalkerLook.HeroCell"/>): his figure in his
        /// cell, or only his hands, the hero drawn again clipped to a disc round each (<see cref="RoundLook.HeroHands"/>, the latch
        /// hero's trick), over the cube he holds. Stock, without an outfit.
        /// </summary>
        private sealed class HeroWalker
        {
            private readonly CanvasGroup _group;
            private readonly HeroGearView _gear;
            private readonly (Image Mask, RectTransform Cell, HeroGearView Gear)[] _hands = new (Image, RectTransform, HeroGearView)[2];
            private Family? _family;

            public HeroWalker(Transform parent)
            {
                Root = UiFactory.CreateRect("HeroWalker", parent);
                _group = Root.gameObject.AddComponent<CanvasGroup>();
                _group.blocksRaycasts = false;
                RectTransform body = UiFactory.Stretch(UiFactory.CreateRect("Body", Root));
                _gear = HeroGearView.Create(body);
                for (int i = 0; i < _hands.Length; i++)
                {
                    Image mask = UiFactory.CreateImage("Hand", Root, ProceduralSprites.Circle, Color.white);
                    mask.raycastTarget = false;
                    mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                    RectTransform cell = UiFactory.CreateRect("Cell", mask.transform);
                    _hands[i] = (mask, cell, HeroGearView.Create(cell));
                }
            }

            public RectTransform Root { get; }

            public void Wear(Family family)
            {
                if (_family == family)
                {
                    return;
                }

                _family = family;
                _gear.Wear(family, null);
                foreach ((Image _, RectTransform _, HeroGearView gear) in _hands)
                {
                    gear.Wear(family, null);
                }
            }

            /// <summary>His figure, or only his <paramref name="hands"/> discs (cell units like <paramref name="cell"/>, <paramref name="unit"/> layer units a cell).</summary>
            public void Show(bool handsOnly, Box cell, (Box Left, Box Right) hands, float unit, float alpha)
            {
                _group.alpha = alpha;
                _gear.Rect.parent.gameObject.SetActive(!handsOnly);
                for (int i = 0; i < _hands.Length; i++)
                {
                    (Image mask, RectTransform inner, HeroGearView _) = _hands[i];
                    mask.gameObject.SetActive(handsOnly);
                    if (!handsOnly)
                    {
                        continue;
                    }

                    Box disc = i == 0 ? hands.Left : hands.Right;
                    Centered(mask.rectTransform, (disc.CenterX - cell.CenterX) * unit, -(disc.CenterY - cell.CenterY) * unit, disc.Width * unit, disc.Height * unit);
                    Centered(inner, (cell.CenterX - disc.CenterX) * unit, -(cell.CenterY - disc.CenterY) * unit, cell.Width * unit, cell.Height * unit);
                }
            }

            private static void Centered(RectTransform rect, float x, float y, float w, float h)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(x, y);
                rect.sizeDelta = new Vector2(w, h);
            }
        }

        /// <summary>Hides everything (a restart, leaving the level).</summary>
        public void Clear() => Render(Array.Empty<FxItem>(), FxLayer.Board);

        private static void Place(RectTransform rect, Vector2 center, float w, float h, FxItem item)
        {
            UiFactory.PlaceAbsolute(rect, center, new Vector2(w, h));
            rect.localScale = new Vector3(item.Sx, item.Sy, 1f);
            rect.localEulerAngles = new Vector3(0f, 0f, -item.Turn);
        }

        // The sprite for an item and, for a rounded box, the side of the square picture it is drawn in (cell units).
        private (Sprite Sprite, float Side) SpriteOf(FxItem item)
        {
            switch (item.Kind)
            {
                case FxKind.Tile:
                    return (item.Material >= 0
                        ? RoundSprites.MaterialTile(item.Material, RoundLook.CubeStyle, Symbols, 128)
                        : ProceduralSprites.CandyTile(item.Variant, CatalogIcons ? TileStyle.MosaicIcon : TileStyle.Mosaic), 0f);
                case FxKind.Ring:
                    return (Ring(item.Line / Math.Max(0.001f, item.W / 2f)), 0f);
                case FxKind.Round:
                case FxKind.RoundRing:
                {
                    float side = Math.Max(item.W, item.H);
                    return (RoundBox(item.W / side, item.H / side, item.Radius / (side / 2f), item.Kind == FxKind.RoundRing ? item.Line / (side / 2f) : 0f), side);
                }

                case FxKind.Shape:
                    return (ProceduralSprites.Shape(item.Shape ?? ShapeLibrary.Fallback), 0f);
                default:
                    return (ProceduralSprites.Circle, 0f);
            }
        }

        // A ring of radius 1 whose line is <paramref name="line"/> of the radius (cached by a 2% step).
        private static Sprite Ring(float line)
        {
            float t = Mathf.Clamp((float)Math.Round(line * 50f) / 50f, 0.02f, 1f);
            return ProceduralSprites.Composite("fx.ring." + t.ToString("0.00", CultureInfo.InvariantCulture), (x, y) => Math.Abs((float)Math.Sqrt((x * x) + (y * y)) - (1f - (t / 2f))) - (t / 2f), 64);
        }

        // A rounded box of half sizes (w, h) at most 1, corner r and, for an outline, line l (cached by a 5% step).
        private static Sprite RoundBox(float w, float h, float r, float l)
        {
            float qw = (float)Math.Round(w * 20f) / 20f;
            float qh = (float)Math.Round(h * 20f) / 20f;
            float qr = (float)Math.Round(Mathf.Min(r, Mathf.Min(qw, qh)) * 20f) / 20f;
            float ql = (float)Math.Round(l * 50f) / 50f;
            string key = string.Format(CultureInfo.InvariantCulture, "fx.round.{0:0.00}.{1:0.00}.{2:0.00}.{3:0.00}", qw, qh, qr, ql);
            return ProceduralSprites.Composite(key, (x, y) =>
            {
                float qx = Math.Abs(x) - qw + qr;
                float qy = Math.Abs(y) - qh + qr;
                float outside = (float)Math.Sqrt((Math.Max(qx, 0f) * Math.Max(qx, 0f)) + (Math.Max(qy, 0f) * Math.Max(qy, 0f)));
                float d = outside + Math.Min(Math.Max(qx, qy), 0f) - qr;
                return ql > 0f ? Math.Abs(d + (ql / 2f)) - (ql / 2f) : d;
            }, 64);
        }

        private Image ImageAt(int index)
        {
            if (index >= _images.Count)
            {
                Image image = UiFactory.CreateImage("Fx", _layer, ProceduralSprites.Circle, Color.white);
                image.raycastTarget = false;
                _images.Add(image);
            }

            Image found = _images[index];
            found.gameObject.SetActive(true);
            return found;
        }

        private RectTransform Figure(int index, VariantId variant, int material, float alpha)
        {
            if (index >= _figures.Count)
            {
                BloomlingFigure created = BloomlingFigure.Create("Walker", _layer);
                created.Body.raycastTarget = false;
                CanvasGroup group = created.Rect.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                _figures.Add((created, group, null));
            }

            (BloomlingFigure figure, CanvasGroup canvas, (VariantId Variant, int Material, bool Hero)? shown) = _figures[index];
            bool hero = material < 0 && HeroWalkers && !CatalogIcons;
            var wanted = (variant, material, hero);
            if (!shown.HasValue || !shown.Value.Equals(wanted))
            {
                bool known = VariantCatalog.Default.TryGet(variant, out VariantInfo info);
                Family family = known ? info.Family : Family.Sprig;
                if (material >= 0)
                {
                    // A round walker: the hero of the material's family in the material's colour (FR-048).
                    figure.ShowWalker(RoundMaterial.All[material]);
                }
                else if (CatalogIcons)
                {
                    figure.ShowHero(family, Outfits?.Invoke(family));
                }
                else if (hero)
                {
                    figure.ShowWalker(family, known ? Rgba.FromHex(info.ColorHex) : Rgba.White);
                }
                else
                {
                    figure.ShowCharacter(variant, Outfits?.Invoke(family));
                }

                _figures[index] = (figure, canvas, wanted);
            }

            canvas.alpha = alpha;
            figure.Rect.gameObject.SetActive(true);
            return figure.Rect;
        }
    }
}
