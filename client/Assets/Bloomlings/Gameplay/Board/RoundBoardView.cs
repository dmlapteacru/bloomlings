using System;
using System.Collections.Generic;
using Bloomlings.Client.Art;
using Bloomlings.Client.Gameplay.Effects;
using Bloomlings.Client.Gameplay.Rounds;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;

namespace Bloomlings.Client.Gameplay.Board
{
    /// <summary>
    /// A round level's board in the Unity client (spec 006 T059, contracts/presentation.md; the playtest's
    /// <c>RoundPainter.Board</c>), laid out by the kit's <see cref="RoundBoardLayout"/> in screen pixels:
    /// <list type="bullet">
    /// <item><description>the owner's board of 2026-10-28: one fixed box on every level (<see cref="RoundBoardLayout.Outer"/>),
    /// its stone frame always the same thickness (<see cref="UiKit.StoneFrame"/>), each gate as the game's entry arch in the
    /// frame turned to its side (FR-040), and inside it one solid neutral surface (<see cref="RoundSprites.Surface"/>): no grid,
    /// no grass;</description></item>
    /// <item><description>on the surface: water on the stream (<see cref="RoundSprites.Water"/>), a stone wall where the level's
    /// overlay laid one (<see cref="RoundSprites.Wall"/>), both carried on to the frame where they close the ring
    /// (<see cref="RoundBoardLayout.StripPieces"/>), the built bridge's plank turned to its crossing (a buildable site glows
    /// toward its near bank and takes the tap, FR-009), and the cubes as the game's soft cubes in the material's colour with
    /// no icon (the symbol only in the symbols mode, FR-041, FR-047) at the cube size the picture fits the surface with;</description></item>
    /// <item><description>an eaten cube, or one the clearing style holds (<see cref="RoundFx.Holds"/>, the style drawing it eaten,
    /// FR-042), leaves the surface bare, no imprint; a cube Bloom Burst took swells and fades; while Bloom Burst waits for its
    /// target the cubes it can take carry a pulsing ring and a tap on the grid picks the material.</description></item>
    /// </list>
    /// Every cell is placed once relative to the grid, so a new screen size only moves the grid, its arches and planks. It
    /// reads the animator each frame (<see cref="Sync"/>) and changes only what changed. Presentation only.
    /// </summary>
    public sealed class RoundBoardView : MonoBehaviour
    {
        private readonly List<(Image Image, RoundGate Gate)> _arches = new List<(Image, RoundGate)>();
        private readonly List<(Image Plank, Image Glow, Button Touch, RoundBridgeSite Site)> _sites = new List<(Image, Image, Button, RoundBridgeSite)>();
        private RectTransform _root = null!;
        private RectTransform _frame = null!;
        private Image _surface = null!;
        private StoneBorderView _border = null!;
        private RectTransform _grid = null!;
        private RectTransform _cells = null!;
        private RectTransform _cubes = null!;
        private RectTransform _decor = null!;
        private RectTransform _over = null!;
        private Image _touch = null!;
        private RoundBoardLayout? _layout;
        private Image?[] _ground = Array.Empty<Image?>();
        private readonly List<(Image Image, RoundStripPiece Piece)> _pieces = new List<(Image, RoundStripPiece)>();
        private Image?[] _cube = Array.Empty<Image?>();
        private Image?[] _rings = Array.Empty<Image?>();
        private sbyte[] _shownCube = Array.Empty<sbyte>();
        private readonly List<int> _ringed = new List<int>();
        private readonly List<int> _puffs = new List<int>();
        private int _pixels;
        private bool _symbols;
        private bool _bursting;

        /// <summary>A buildable bridge site was tapped (its choice index).</summary>
        public event Action<int>? BridgeTapped;

        /// <summary>A cell was tapped while Bloom Burst waits for its target (the rules' cell index).</summary>
        public event Action<int>? CellTapped;

        /// <summary>The board's layout (screen pixels), once built.</summary>
        public RoundBoardLayout? Layout => _layout;

        /// <summary>The grid inside the frame (its children are placed relative to it).</summary>
        public RectTransform Grid => _grid;

        /// <summary>The layer over the cubes the clearing style's board items go to.</summary>
        public RectTransform Over => _over;

        public static RoundBoardView Create(RectTransform layer)
        {
            var view = layer.gameObject.AddComponent<RoundBoardView>();
            view._root = layer;

            // The fixed frame and the solid neutral surface under the grid (the owner's board of 2026-10-28).
            view._frame = UiFactory.CreateRect("RoundFrame", layer);
            view._border = UiKit.StoneFrame(view._frame, 0.05f);
            view._surface = UiFactory.CreateImage("Surface", view._frame, null, Color.white);
            view._surface.raycastTarget = false;
            UiFactory.Stretch(view._surface.rectTransform);
            view._grid = UiFactory.CreateRect("RoundGrid", layer);
            // The strips and the cubes batch on canvases of their own (up to 46 × 46 cells), so the walkers moving every
            // frame and a cube cleared now and then never make Unity batch the other layer again. Neither takes taps.
            view._cells = UiFactory.Stretch(UiFactory.CreateRect("Ground", view._grid));
            view._cells.gameObject.AddComponent<Canvas>();
            view._cubes = UiFactory.Stretch(UiFactory.CreateRect("Cubes", view._grid));
            view._cubes.gameObject.AddComponent<Canvas>();
            view._decor = UiFactory.Stretch(UiFactory.CreateRect("Decor", view._grid));
            view._over = UiFactory.Stretch(UiFactory.CreateRect("Over", view._grid));

            // Bloom Burst's target: a clear touch layer over the grid, live only while the booster waits.
            view._touch = UiFactory.CreateImage("BurstTouch", view._grid, null, Color.clear, raycast: false);
            UiFactory.Stretch(view._touch.rectTransform);
            view._touch.gameObject.AddComponent<BurstTouch>().Board = view;
            return view;
        }

        /// <summary>Builds the board of <paramref name="level"/> in <paramref name="layout"/> (screen pixels).</summary>
        public void Build(RoundLevel level, RoundBoardLayout layout, bool symbols)
        {
            int cells = level.CellCount;
            // Cells, cubes, arches, bridges and Bloom Burst's rings of an earlier build go.
            foreach (Transform child in new[] { _cells, _cubes, _decor })
            {
                for (int i = child.childCount - 1; i >= 0; i--)
                {
                    Destroy(child.GetChild(i).gameObject);
                }
            }

            _arches.Clear();
            _sites.Clear();
            _pieces.Clear();
            _ringed.Clear();
            _puffs.Clear();
            _layout = layout;
            _symbols = symbols;
            _ground = new Image?[cells];
            _cube = new Image?[cells];
            _rings = new Image?[cells];
            _shownCube = new sbyte[cells];
            PlaceGrid();
            _pixels = CellPixels();

            // Only the strips have ground pictures: the walkway ring, the picture's background and an eaten cube are the
            // surface itself.
            for (int y = 1; y < layout.Height - 1; y++)
            {
                for (int x = 1; x < layout.Width - 1; x++)
                {
                    int cell = (y * layout.Width) + x;
                    RoundCellKind kind = layout.KindOf(cell);
                    if (kind == RoundCellKind.Stream || kind == RoundCellKind.Wall || kind == RoundCellKind.BridgeSite)
                    {
                        Image ground = UiFactory.CreateImage("Strip", _cells, null, Color.white);
                        ground.raycastTarget = false;
                        PlaceInGrid(ground.rectTransform, layout.CellBox(cell));
                        _ground[cell] = ground;
                    }

                    if (kind == RoundCellKind.Material)
                    {
                        Image cube = UiFactory.CreateImage("Cube", _cubes, null, Color.white);
                        PlaceInGrid(cube.rectTransform, layout.CubeBox(cell));
                        _cube[cell] = cube;
                        _shownCube[cell] = sbyte.MinValue;
                    }
                }
            }

            foreach (RoundStripPiece piece in layout.StripPieces)
            {
                Image image = UiFactory.CreateImage("StripReach", _cells, null, Color.white);
                image.raycastTarget = false;
                PlaceInGrid(image.rectTransform, piece.Box);
                _pieces.Add((image, piece));
            }

            foreach (RoundBridgeSite site in layout.Bridges)
            {
                Image plank = UiFactory.CreateImage("Bridge", _decor, null, Color.white);
                Image glow = UiKit.RoundRing("BridgeGlow", _decor, UiTheme.Of(C.GardenGlow), b => Mathf.Min(b.Width, b.Height) * 0.25f, b => Mathf.Max(1.5f, Mathf.Min(b.Width, b.Height) * 0.1f));
                Image touchImage = UiFactory.CreateImage("BridgeTouch", _decor, null, Color.clear, raycast: true);
                Button touch = touchImage.gameObject.AddComponent<Button>();
                touch.transition = Selectable.Transition.None;
                touch.targetGraphic = touchImage;
                int choice = site.Index;
                touch.onClick.AddListener(() => BridgeTapped?.Invoke(choice));
                _sites.Add((plank, glow, touch, site));
            }

            foreach (RoundGate gate in layout.Gates)
            {
                Image arch = UiFactory.CreateImage("EntryArch", _decor, null, Color.white);
                arch.raycastTarget = false;
                _arches.Add((arch, gate));
            }

            PlaceDecor();
            ApplyPictures(force: true);
            _decor.SetAsLastSibling();
            _over.SetAsLastSibling();
            _touch.transform.SetAsLastSibling();
        }

        /// <summary>A new layout of the same board (a new screen size): the grid, the arches and the planks move.</summary>
        public void Relayout(RoundBoardLayout layout)
        {
            if (_layout == null || layout.Width != _layout.Width || layout.Height != _layout.Height)
            {
                return;
            }

            _layout = layout;
            PlaceGrid();
            foreach ((Image image, RoundStripPiece piece) in _pieces)
            {
                PlaceInGrid(image.rectTransform, piece.Box);
            }

            PlaceDecor();
            int pixels = CellPixels();
            if (Mathf.Abs(pixels - _pixels) > _pixels / 4)
            {
                _pixels = pixels;
                ApplyPictures(force: true);
            }
        }

        /// <summary>The screen box of cell <paramref name="cell"/> (the guided spotlights' holes).</summary>
        public Box CellBox(int cell) => _layout?.CellBox(cell) ?? default;

        /// <summary>The arches' screen boxes (the guided entry spotlight).</summary>
        public IEnumerable<Box> ArchBoxes()
        {
            if (_layout == null)
            {
                yield break;
            }

            foreach (RoundGate gate in _layout.Gates)
            {
                (Box box, float degrees) = gate.Arch;
                yield return Mathf.Abs(degrees % 180f) > 45f ? Box.FromCenter(box.CenterX, box.CenterY, box.Height, box.Width) : box;
            }
        }

        /// <summary>
        /// Shows the board as the animator holds it at display time <paramref name="clockUs"/>: cubes, cleared cells, the
        /// cubes the style holds, Bloom Burst's puffs, the bridge and its buildable sites, and Bloom Burst's targets.
        /// </summary>
        public void Sync(RoundLevelCore core, RoundFx fx, IReadOnlyList<int> buildable, float uiNow)
        {
            RoundBoardLayout? layout = _layout;
            if (layout == null)
            {
                return;
            }

            RoundAnimator a = core.Animator;
            if (core.Symbols != _symbols)
            {
                _symbols = core.Symbols;
                ApplyPictures(force: true);
            }

            long now = core.Play.ClockUs;
            for (int cell = 0; cell < _cube.Length; cell++)
            {
                Image? cube = _cube[cell];
                if (cube == null)
                {
                    continue;
                }

                int m = a.MaterialAt(cell);
                bool held = fx.Holds(cell);
                int original = Math.Max(0, a.OriginalMaterial(cell));
                long since = now - a.ClearedAt(cell);
                bool puff = m < 0 && a.Burst(cell) && a.ClearedAt(cell) >= 0 && since >= 0 && since < RoundAnimator.BurstUs;
                sbyte shown = (sbyte)(puff ? original : m >= 0 && !held ? m : -1);
                if (shown != _shownCube[cell])
                {
                    _shownCube[cell] = shown;
                    cube.enabled = shown >= 0;
                    if (shown >= 0)
                    {
                        cube.sprite = RoundSprites.MaterialTile(shown, RoundLook.CubeStyle, _symbols, _pixels);
                    }

                    if (!puff)
                    {
                        cube.color = Color.white;
                        cube.rectTransform.localScale = Vector3.one;
                    }
                }

                if (puff)
                {
                    float k = since / (float)RoundAnimator.BurstUs;
                    cube.color = new Color(1f, 1f, 1f, 1f - k);
                    cube.rectTransform.localScale = Vector3.one * (1f + (0.35f * k));
                    if (!_puffs.Contains(cell))
                    {
                        _puffs.Add(cell);
                    }
                }
            }

            // A puff that just ended goes back to an empty cell.
            for (int i = _puffs.Count - 1; i >= 0; i--)
            {
                int cell = _puffs[i];
                long since = now - a.ClearedAt(cell);
                if (since >= RoundAnimator.BurstUs || since < 0 || a.MaterialAt(cell) >= 0)
                {
                    _puffs.RemoveAt(i);
                    _shownCube[cell] = sbyte.MinValue;
                    Image cube = _cube[cell]!;
                    cube.color = Color.white;
                    cube.rectTransform.localScale = Vector3.one;
                    cube.enabled = false;
                }
            }

            SyncBridges(core, buildable, uiNow);
            SyncTargets(core, uiNow);
        }

        private void SyncBridges(RoundLevelCore core, IReadOnlyList<int> buildable, float uiNow)
        {
            RoundAnimator a = core.Animator;
            float glow = GardenLook.Glow(uiNow);
            bool live = core.HudLive && core.Intro == null && core.Guide == null && !core.Targeting.HasValue;
            foreach ((Image plank, Image ring, Button touch, RoundBridgeSite site) in _sites)
            {
                bool built = a.Bridge == site.Index;
                bool open = !built && a.Bridge < 0 && Contains(buildable, site.Index);
                plank.enabled = built || open;
                plank.color = new Color(1f, 1f, 1f, built ? 1f : 0.45f + (0.4f * glow));
                ring.gameObject.SetActive(open);
                if (open)
                {
                    Color c = UiTheme.Of(C.GardenGlow);
                    ring.color = new Color(c.r, c.g, c.b, 0.6f + (0.4f * glow));
                }

                touch.gameObject.SetActive(open && live);
            }
        }

        private void SyncTargets(RoundLevelCore core, float uiNow)
        {
            bool bursting = core.Targeting == BoosterKind.BloomBurst;
            _touch.raycastTarget = bursting;
            if (!bursting)
            {
                if (_bursting)
                {
                    foreach (int cell in _ringed)
                    {
                        _rings[cell]!.gameObject.SetActive(false);
                    }

                    _ringed.Clear();
                }

                _bursting = false;
                return;
            }

            _bursting = true;
            RoundAnimator a = core.Animator;
            float pulse = 0.5f + (0.5f * Mathf.Sin(uiNow * 6f));
            Color glow = UiTheme.Of(C.GardenGlow);
            var color = new Color(glow.r, glow.g, glow.b, 0.5f + (0.5f * pulse));
            _ringed.Clear();
            for (int cell = 0; cell < _cube.Length; cell++)
            {
                if (_cube[cell] == null)
                {
                    continue;
                }

                int m = a.MaterialAt(cell);
                bool target = m >= 0 && core.CanBurst(m);
                Image? ring = _rings[cell];
                if (!target)
                {
                    if (ring != null)
                    {
                        ring.gameObject.SetActive(false);
                    }

                    continue;
                }

                if (ring == null)
                {
                    ring = UiKit.RoundRing("BurstTarget", _decor, color, b => b.Width * 0.16f, b => Mathf.Max(1f, b.Width * 0.08f));
                    ring.raycastTarget = false;
                    PlaceInGrid(ring.rectTransform, _layout!.CellBox(cell).Inset(_layout.Cell * 0.06f));
                    _rings[cell] = ring;
                }

                ring.gameObject.SetActive(true);
                ring.color = color;
                _ringed.Add(cell);
            }
        }

        /// <summary>A tap on the grid while Bloom Burst waits: the cube under the finger picks the material.</summary>
        public void TapAt(Vector2 screenPosition)
        {
            if (!_bursting || _layout == null)
            {
                return;
            }

            (float w, float h, Insets _) = UiKit.ScreenFrame();
            float tx = screenPosition.x;
            float ty = h - screenPosition.y;
            RoundBoardLayout layout = _layout;
            int cell = layout.CellAt(tx, ty);
            if (cell >= 0)
            {
                CellTapped?.Invoke(cell);
            }
        }

        private static bool Contains(IReadOnlyList<int> list, int value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    return true;
                }
            }

            return false;
        }

        private int CellPixels() => _layout == null ? 64 : UiRaster.Quantize(Mathf.Max(8f, _layout.Cell));

        private void PlaceGrid()
        {
            if (_layout != null)
            {
                RoundBoardLayout layout = _layout;
                UiKit.PlaceScreen(_frame, layout.Surface);
                float side = Mathf.Max(1f, Mathf.Min(layout.Surface.Width, layout.Surface.Height));
                _border.SetUnitShare(layout.Unit / side);
                float radius = layout.Unit * RoundBoardLayout.SurfaceRadius / side;
                PictureFit.On(_surface, (w, h) => RoundSprites.Surface(w, h, radius));
                UiKit.PlaceScreen(_grid, layout.Grid);
            }
        }

        private void PlaceInGrid(RectTransform rect, Box box) => UiKit.PlaceBox(rect, box, _layout!.Grid);

        /// <summary>The arches and the bridge sites, which reach beyond their cells.</summary>
        private void PlaceDecor()
        {
            RoundBoardLayout layout = _layout!;
            foreach ((Image image, RoundGate gate) in _arches)
            {
                (Box arch, float degrees) = gate.Arch;
                PlaceInGrid(image.rectTransform, arch);
                image.transform.localEulerAngles = new Vector3(0f, 0f, -degrees);
                int pw = UiRaster.Quantize(arch.Width);
                int ph = UiRaster.Quantize(arch.Height);
                image.sprite = ProceduralSprites.Picture("board.entry.arch", pw, ph, UiRaster.EntryArch);
                image.type = Image.Type.Simple;
            }

            float min = DesignTokens.Size.TouchMin * DesignTokens.ScaleFor(UiKit.ScreenBox().Width, UiKit.ScreenBox().Height);
            foreach ((Image plank, Image ring, Button touch, RoundBridgeSite site) in _sites)
            {
                Box box = site.Plank;
                PlaceInGrid(plank.rectTransform, box);
                plank.transform.localEulerAngles = new Vector3(0f, 0f, -site.PlankDegrees);
                int seed = site.Cell;
                PictureFit.On(plank, (w, h) => ProceduralSprites.Plank(WoodTone.Light, w, h, 0.3f, 0.025f, seed), sliced: true);
                PlaceInGrid(ring.rectTransform, site.Glow.Inset(-layout.Cell * 0.08f));
                Box bounds = site.PlankBounds;
                PlaceInGrid((RectTransform)touch.transform, Box.FromCenter(bounds.CenterX, bounds.CenterY, Mathf.Max(bounds.Width, min), Mathf.Max(bounds.Height, min)));
            }
        }

        /// <summary>Every ground and cube picture again (a new size, the symbols mode switched).</summary>
        private void ApplyPictures(bool force)
        {
            RoundBoardLayout? layout = _layout;
            if (layout == null || !force)
            {
                return;
            }

            for (int cell = 0; cell < _ground.Length; cell++)
            {
                if (_ground[cell] != null)
                {
                    SetGround(cell, layout);
                }

                if (_cube[cell] != null)
                {
                    _shownCube[cell] = sbyte.MinValue;
                }
            }

            foreach ((Image image, RoundStripPiece piece) in _pieces)
            {
                image.sprite = layout.IsStone(piece.Cell) ? RoundSprites.Wall(piece.X, piece.Y, _pixels) : RoundSprites.Water(piece.X, _pixels);
                image.type = Image.Type.Simple;
            }
        }

        private void SetGround(int cell, RoundBoardLayout layout)
        {
            Image ground = _ground[cell]!;
            int x = cell % layout.Width;
            int y = cell / layout.Width;
            switch (layout.KindOf(cell))
            {
                case RoundCellKind.Stream:
                    ground.sprite = RoundSprites.Water(x, _pixels);
                    break;
                case RoundCellKind.Wall:
                    ground.sprite = RoundSprites.Wall(x, y, _pixels);
                    break;
                default:
                    ground.sprite = layout.IsStone(cell) ? RoundSprites.Wall(x, y, _pixels) : RoundSprites.Water(x, _pixels);
                    break;
            }

            ground.type = Image.Type.Simple;
        }

        /// <summary>The clear touch layer over the grid that hands Bloom Burst's taps to the board.</summary>
        private sealed class BurstTouch : MonoBehaviour, IPointerClickHandler
        {
            public RoundBoardView? Board { get; set; }

            public void OnPointerClick(PointerEventData eventData) => Board?.TapAt(eventData.position);
        }
    }
}
