using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The round levels' pieces (spec 006 contracts/presentation.md), drawn from the kit as cached <see cref="UiRaster"/>
    /// pictures: the board's fixed stone frame and solid neutral surface, material tiles without a variant icon (the
    /// material's symbol only in the symbols mode), the stream's water, the built bridge's plank and the link tube. Each marks its asset slots; the level screen only calls them.
    /// </summary>
    public static partial class Kit
    {
        private static readonly Dictionary<(TileStyle Style, TileState State, int Material, bool Symbol), (string, Func<int, int, byte[]>)> MaterialTiles =
            new Dictionary<(TileStyle, TileState, int, bool), (string, Func<int, int, byte[]>)>();

        private static readonly string[] WaterKeys = MakeWater(k => RoundBoardLayout.WaterSlot + "/" + k);

        private static readonly Func<int, int, byte[]>[] WaterPictures = MakeWater<Func<int, int, byte[]>>(k => (w, h) => UiRaster.Water(w, h, k));

        private static readonly string[] WallKeys = MakeWall(k => RoundBoardLayout.WallSlot + "/" + k);

        private static readonly Func<int, int, byte[]>[] WallPictures = MakeWall<Func<int, int, byte[]>>(k => (w, h) => UiRaster.WallStone(w, h, k));

        /// <summary>
        /// A round material's tile filling the square in <paramref name="box"/> (spec 006 FR-040, FR-041, FR-047): a board
        /// cube (<see cref="RoundLook.CubeStyle"/>) or a pod's or place's tile (<see cref="RoundLook.PodStyle"/>) in the
        /// material's colour, with no variant icon, and its symbol when <paramref name="symbolsMode"/> is on.
        /// </summary>
        public static void MaterialTile(IPainter p, Box box, RoundMaterial material, TileStyle style, bool symbolsMode, TileState state = TileState.Normal)
        {
            bool symbol = symbolsMode && state != TileState.Mystery;
            p.Mark(style == TileStyle.Sticker ? "tile.candy.sticker" : "tile.candy");
            if (symbol)
            {
                p.Mark(RoundLook.SymbolOf(material));
            }

            (string key, Func<int, int, byte[]> render) = Recipe(MaterialTiles, (Style: style, State: state, Material: material.Index, Symbol: symbol), k =>
            {
                RoundMaterial m = RoundMaterial.All[k.Material];
                Rgba color = RoundLook.ColorOf(m);
                string? mark = k.Symbol ? RoundLook.SymbolOf(m) : null;
                return ("tile.round/" + k.Style + "/" + k.State + "/" + m.Code + (k.Symbol ? "/symbol" : string.Empty),
                    (w, h) => UiRaster.MaterialTile(Math.Min(w, h), color, mark, k.Style, k.State));
            });
            float s = Math.Min(box.Width, box.Height);
            if (s >= 1f)
            {
                p.Picture(key, Box.FromCenter(box.CenterX, box.CenterY, s, s), render);
            }
        }

        /// <summary>A stream cell filling <paramref name="full"/> (<c>board.water</c>), its picture by its column (<see cref="UiRaster.WaterSeed"/>).</summary>
        public static void Water(IPainter p, Box full, int x)
        {
            p.Mark(RoundBoardLayout.WaterSlot);
            int k = UiRaster.WaterSeed(x);
            p.Picture(WaterKeys[k], full, WaterPictures[k]);
        }

        /// <summary>
        /// A cell of a wall laid over the picture filling <paramref name="full"/> (<c>board.wall</c>, 17 §8 as amended on
        /// 2026-10-28), its picture by its place (<see cref="UiRaster.WallSeed"/>).
        /// </summary>
        public static void Wall(IPainter p, Box full, int x, int y)
        {
            p.Mark(RoundBoardLayout.WallSlot);
            int k = UiRaster.WallSeed(x, y);
            p.Picture(WallKeys[k], full, WallPictures[k]);
        }

        /// <summary>A built bridge (<c>board.bridge</c>): a light wooden plank across its site, turned to its crossing (up, down, left or right).</summary>
        public static void BridgePlank(IPainter p, RoundBridgeSite site)
        {
            p.Mark(RoundBoardLayout.BridgeSlot);
            bool turned = Math.Abs(site.PlankDegrees) > 0.5f;
            Box plank = site.Plank;
            if (turned)
            {
                p.PushRotate(site.PlankDegrees, plank.CenterX, plank.CenterY);
            }

            WoodPlank(p, plank, 0.3f, site.Cell);
            if (turned)
            {
                p.PopTransform();
            }
        }

        /// <summary>
        /// A link tube (<c>pod.link.tube</c>, spec 006 FR-043): its picture upright in its box, turned about its middle. Draw
        /// every tube before the pods and places it joins, so it never lies over a pod.
        /// </summary>
        public static void LinkTubePicture(IPainter p, LinkTubeShape tube)
        {
            p.Mark(LinkTube.Slot);
            if (tube.Length < 1f)
            {
                return;
            }

            bool turned = Math.Abs(tube.Degrees) > 0.5f;
            Box box = tube.Box;
            if (turned)
            {
                p.PushRotate(tube.Degrees, box.CenterX, box.CenterY);
            }

            Rgba start = tube.StartColor;
            Rgba end = tube.EndColor;
            p.Picture(tube.PictureKey, box, (w, h) => UiRaster.LinkTube(w, h, start, end));
            if (turned)
            {
                p.PopTransform();
            }
        }

        private static readonly Dictionary<(int Material, RoundPodLook Look, bool Symbol, bool Grey), (string, Func<int, int, byte[]>)> RoundPods =
            new Dictionary<(int, RoundPodLook, bool, bool), (string, Func<int, int, byte[]>)>();

        private static readonly Dictionary<(int Material, bool Symbol), (string, Func<int, int, byte[]>)> BoardCubes =
            new Dictionary<(int, bool), (string, Func<int, int, byte[]>)>();

        /// <summary>
        /// A round board's cube (spec 006 FR-064, the owner's "like Colony Flow" of 2026-10-28) in its picture box
        /// <paramref name="box"/> (<see cref="RoundBoardLayout.CubeBox"/>: a column wide, a row's pitch and the front face
        /// tall): the oblique cube in the material's colour (<see cref="UiRaster.BoardCube"/>), its symbol on its top in the
        /// symbols mode. Draw the far rows first: a nearer cube hides the front of the one behind it.
        /// </summary>
        public static void BoardCube(IPainter p, Box box, RoundMaterial material, bool symbolsMode)
        {
            p.Mark("tile.candy");
            if (symbolsMode)
            {
                p.Mark(RoundLook.SymbolOf(material));
            }

            (string key, Func<int, int, byte[]> render) = Recipe(BoardCubes, (Material: material.Index, Symbol: symbolsMode), k =>
            {
                RoundMaterial m = RoundMaterial.All[k.Material];
                Rgba color = RoundLook.ColorOf(m);
                string? mark = k.Symbol ? RoundLook.SymbolOf(m) : null;
                return (UiRaster.BoardCubeKey(m.Code, k.Symbol), (w, h) => UiRaster.BoardCube(w, h, color, mark));
            });
            p.Picture(key, box, render);
        }

        private static readonly Dictionary<(char Code, bool Left, int Top, bool Grey), (string, Func<int, int, byte[]>)> SideFaces =
            new Dictionary<(char, bool, int, bool), (string, Func<int, int, byte[]>)>();

        /// <summary>
        /// A cube's side face in <paramref name="box"/> (<c>pod.cube.side</c>, constitution 1.2.0 VII, <see cref="UiRaster.SideFace"/>):
        /// <paramref name="material"/> null is a hidden pod's; <paramref name="topShare"/> of it stands beside the top face.
        /// Draw it before its cube.
        /// </summary>
        public static void SideFace(IPainter p, Box box, RoundMaterial? material, bool left, float topShare, bool grey = false)
        {
            if (box.Width < 0.5f)
            {
                return;
            }

            p.Mark(RoundLook.SideSlot);
            int top = (int)Math.Round(topShare * 100f);
            (string key, Func<int, int, byte[]> render) = Recipe(SideFaces, (Code: material?.Code ?? '?', Left: left, Top: top, Grey: grey), k =>
            {
                Rgba color = k.Code == '?' ? DesignTokens.Colors.TileMystery : RoundLook.ColorOf(k.Code);
                if (k.Grey)
                {
                    color = color.Grey();
                }

                float share = k.Top / 100f;
                return (UiRaster.SideFaceKey(k.Code, k.Left, share, k.Grey), (w, h) => UiRaster.SideFace(w, h, color, k.Left, share));
            });
            p.Picture(key, box, render);
        }

        /// <summary>
        /// A round pod's cube picture filling the square in <paramref name="box"/> (<see cref="UiRaster.PodCube"/>,
        /// <c>pod.cube</c>): <paramref name="material"/> null is the hidden "?" cube; <paramref name="grey"/> a stuck place's.
        /// </summary>
        public static void PodCubePicture(IPainter p, Box box, RoundMaterial? material, RoundPodLook look, bool symbolsMode, bool grey = false, int sideStep = 0)
        {
            if (sideStep != 0)
            {
                float depth = look == RoundPodLook.Waiting ? RoundScreenLook.WaitingDepthShare : RoundScreenLook.CubeDepthShare;
                SideFace(p, RoundLook.SideBox(RoundLook.PodBody(box), sideStep), material, sideStep < 0, 1f - depth, grey);
            }

            p.Mark(RoundLook.PodCubeSlot);
            bool hidden = material == null;
            bool symbol = symbolsMode && !hidden;
            if (hidden)
            {
                p.Mark("pod.state.mystery");
            }
            else if (symbol)
            {
                p.Mark(RoundLook.SymbolOf(material!));
            }

            (string key, Func<int, int, byte[]> render) = Recipe(RoundPods, (Material: material?.Index ?? -1, Look: look, Symbol: symbol, Grey: grey), k =>
            {
                RoundMaterial m = RoundMaterial.All[Math.Max(0, k.Material)];
                Rgba color = k.Grey ? RoundLook.ColorOf(m).Grey() : RoundLook.ColorOf(m);
                string? mark = k.Symbol ? RoundLook.SymbolOf(m) : null;
                bool none = k.Material < 0;
                return (UiRaster.PodCubeKey(m.Code, none, k.Look, k.Symbol, k.Grey), (w, h) => UiRaster.PodCube(Math.Min(w, h), color, mark, none, k.Look));
            });
            float s = Math.Min(box.Width, box.Height);
            p.Picture(key, Box.FromCenter(box.CenterX, box.CenterY, s, s), render);
        }

        /// <summary>
        /// A round pod's cube in a place, in flight or on a card (spec 006 FR-064, Colony Flow's): the top row's cube without
        /// its outline filling the square in <paramref name="box"/>, its number big in the middle of its top when
        /// <paramref name="count"/> is given (no corner count), greyed while stuck.
        /// </summary>
        public static void PlacedCube(IPainter p, Box box, RoundMaterial material, int? count, bool stuck, bool symbolsMode, int sideStep = 0)
        {
            PodCubePicture(p, box, material, RoundPodLook.Placed, symbolsMode, stuck, sideStep);
            if (count.HasValue)
            {
                CubeCount(p, PodChip.Cube(box).Count, count.Value, false);
            }
        }

        /// <summary>
        /// A round board's solid neutral surface filling <paramref name="box"/> (<c>board.surface</c>, the owner's board of
        /// 2026-10-28): one cached picture a size (<see cref="UiRaster.BoardSurface"/>), corners rounded by
        /// <paramref name="radius"/> pixels. Nothing is drawn per cell under the picture.
        /// </summary>
        public static void BoardSurface(IPainter p, Box box, float radius)
        {
            p.Mark(RoundBoardLayout.SurfaceSlot);
            float share = (float)Math.Round(radius / Math.Max(1f, Math.Min(box.Width, box.Height)), 4);
            p.Picture(RoundBoardLayout.SurfaceSlot + "/" + share.ToString(System.Globalization.CultureInfo.InvariantCulture), box, (w, h) => UiRaster.BoardSurface(w, h, share));
        }

        /// <summary>
        /// A round level's fixed board (the owner's board of 2026-10-28): the stone frame round <see cref="RoundBoardLayout.Surface"/>
        /// at the frame's unit, so it is the same thickness on every level, then the neutral surface inside it.
        /// </summary>
        public static void RoundBoardFrame(IPainter p, RoundBoardLayout board)
        {
            StoneBorder(p, board.Surface, board.Unit);
            BoardSurface(p, board.Surface, board.Unit * RoundBoardLayout.SurfaceRadius);
        }

        /// <summary>
        /// A round level's pod in <paramref name="chip"/> (spec 006 FR-064, contracts/presentation.md; the owner's pods of
        /// 2026-10-28, as Colony Flow's): no frame or box round it, one oblique cube (<see cref="UiRaster.PodCube"/>,
        /// <c>pod.cube</c>) in the material's colour, the top row's in a white outline, a waiting pod's
        /// (<paramref name="waiting"/>, rows 2 and 3) barely lighter on a flatter base with its number half clear, the
        /// material's symbol small on its top in the symbols mode, and its number big in the middle of its top
        /// (<see cref="PodChip.Cube"/>, <see cref="PodChip.CubeCountLook"/>); a hidden pod (<paramref name="material"/> null)
        /// is the neutral "?" cube with no number until it reveals (FR-007). A pressed pod sinks a little. Returns its box.
        /// </summary>
        public static Box RoundPod(IPainter p, PodChip chip, RoundMaterial? material, int count, PodLook look, bool waiting, bool symbolsMode, int sideStep = 0)
        {
            float sink = look == PodLook.Pressed ? chip.Frame.Height * 0.04f : 0f;
            Box cube = chip.Frame.Offset(0f, sink);
            PodCubePicture(p, cube, material, waiting ? RoundPodLook.Waiting : RoundPodLook.Top, symbolsMode, false, sideStep);
            if (material != null)
            {
                CubeCount(p, chip.Count.Offset(0f, sink), count, waiting);
            }

            return cube;
        }

        /// <summary>A pod cube's count (<c>pod.count</c>, FR-064): white digits in a dark outline, filling <paramref name="area"/>'s height.</summary>
        public static void CubeCount(IPainter p, Box area, int count, bool waiting)
        {
            p.Mark("pod.count");
            p.Mark("font.shop");
            TextLook look = PodChip.CubeCountLook(waiting);
            p.Text(count.ToString(System.Globalization.CultureInfo.InvariantCulture), area.CenterX, area.CenterY, GameplaySkin.PodCount, look.FillTop, area.Width, area.Height / p.U(GameplaySkin.PodCount.Size), look);
        }

        /// <summary>
        /// A link tube (<see cref="LinkTubePicture"/>) whose length is rounded up to whole steps of 8 px about its middle, so
        /// a tube between sliding pods reuses its cached picture instead of rendering a new length every frame (its ends tuck
        /// under the pods, which hide the step).
        /// </summary>
        public static void LinkTubeStepped(IPainter p, LinkTubeShape tube)
        {
            float length = tube.Length;
            if (length < 1f)
            {
                p.Mark(LinkTube.Slot);
                return;
            }

            float stepped = (float)Math.Ceiling(length / 8f) * 8f;
            float k = stepped / length / 2f;
            (float mx, float my) = tube.Middle;
            float dx = (tube.End.X - tube.Start.X) * k;
            float dy = (tube.End.Y - tube.Start.Y) * k;
            LinkTubePicture(p, new LinkTubeShape((mx - dx, my - dy), tube.StartColor, (mx + dx, my + dy), tube.EndColor, (float)Math.Round(tube.Thickness)));
        }

        /// <summary>
        /// The glow round a pod that has just revealed (<c>pod.reveal.glow</c>, spec 006 FR-045): soft rings of the garden's
        /// glow round <paramref name="frame"/> at <paramref name="strength"/> (<see cref="RoundLook.RevealGlow"/>).
        /// </summary>
        public static void RevealGlow(IPainter p, Box frame, float strength)
        {
            if (strength <= 0f)
            {
                return;
            }

            p.Mark(RoundLook.RevealGlowSlot);
            float s = Math.Min(frame.Width, frame.Height);
            float radius = s * 0.22f;
            for (int ring = 3; ring >= 1; ring--)
            {
                float grow = s * 0.14f * ring / 3f;
                p.FillRound(frame.Inset(-grow), radius + grow, DesignTokens.Colors.GardenGlow.WithAlpha(0.3f * strength));
            }

            p.StrokeRound(frame.Inset(-s * 0.03f), radius + (s * 0.03f), s * 0.035f, DesignTokens.Colors.GardenGlow.WithAlpha(strength));
        }

        private static T[] MakeWall<T>(Func<int, T> make)
        {
            var items = new T[UiRaster.WallVariants];
            for (int k = 0; k < items.Length; k++)
            {
                items[k] = make(k);
            }

            return items;
        }

        private static T[] MakeWater<T>(Func<int, T> make)
        {
            var items = new T[UiRaster.WaterVariants];
            for (int k = 0; k < items.Length; k++)
            {
                items[k] = make(k);
            }

            return items;
        }
    }
}
