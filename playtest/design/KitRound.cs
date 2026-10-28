using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The round levels' pieces (spec 006 contracts/presentation.md), drawn from the kit as cached <see cref="UiRaster"/>
    /// pictures: material tiles without a variant icon (the material's symbol only in the symbols mode), the stream's
    /// water, the built bridge's plank and the link tube. Each marks its asset slots; the level screen only calls them.
    /// </summary>
    public static partial class Kit
    {
        private static readonly Dictionary<(TileStyle Style, TileState State, int Material, bool Symbol), (string, Func<int, int, byte[]>)> MaterialTiles =
            new Dictionary<(TileStyle, TileState, int, bool), (string, Func<int, int, byte[]>)>();

        private static readonly string[] WaterKeys = MakeWater(k => RoundBoardLayout.WaterSlot + "/" + k);

        private static readonly Func<int, int, byte[]>[] WaterPictures = MakeWater<Func<int, int, byte[]>>(k => (w, h) => UiRaster.Water(Math.Min(w, h), k));

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

        /// <summary>A built bridge (<c>board.bridge</c>): a light wooden plank across its site, turned to cross the stream.</summary>
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

        private static readonly string[] ClearedKeys = MakeMaterials(m => "tile.ground/round/" + RoundMaterial.All[m].Code);

        private static readonly Func<int, int, byte[]>[] ClearedPictures = MakeMaterials<Func<int, int, byte[]>>(m =>
        {
            Rgba color = RoundLook.ColorOf(m).Lighten(0.55f);
            return (w, h) => UiRaster.ClearedCell(w, h, color);
        });

        private static readonly Dictionary<(int Material, bool Waiting, bool Locked), (string, Func<int, int, byte[]>)> RoundPods =
            new Dictionary<(int, bool, bool), (string, Func<int, int, byte[]>)>();

        /// <summary>
        /// A round level's cleared cell filling <paramref name="full"/> (<c>tile.ground</c>, spec 006 T049): the finished
        /// picture as the game's pale flat cell in the material's colour, one cached picture a material.
        /// </summary>
        public static void ClearedCell(IPainter p, Box full, int material)
        {
            p.Mark("tile.ground");
            p.Picture(ClearedKeys[material], full, ClearedPictures[material]);
        }

        /// <summary>
        /// A round level's pod in <paramref name="chip"/> (spec 006 T049, contracts/presentation.md): the game's wooden pod
        /// tinted by the material (<see cref="GameplaySkin.Pod(int, int, Rgba?, bool, bool)"/>), the material's plain tile
        /// (its symbol only in the symbols mode) and its count; a hidden pod (<paramref name="material"/> null) shows the
        /// lilac "?" and no count until it reveals (FR-007). Returns the tile's box.
        /// </summary>
        public static Box RoundPod(IPainter p, PodChip chip, RoundMaterial? material, int count, PodLook look, bool waiting, bool symbolsMode)
        {
            p.Mark("pod.card");
            bool locked = look == PodLook.Locked;
            float sink = look == PodLook.Pressed ? chip.Frame.Height * 0.04f : 0f;
            Box frame = chip.Frame.Offset(0f, sink);
            (string key, Func<int, int, byte[]> render) = Recipe(RoundPods, (Material: material?.Index ?? -1, Waiting: waiting, Locked: locked), k =>
            {
                Rgba? color = k.Material >= 0 ? RoundLook.ColorOf(k.Material) : (Rgba?)null;
                string name = k.Material >= 0 ? RoundMaterial.All[k.Material].Code.ToString() : "mystery";
                return ("gameplay.pod.round/" + name + "/" + k.Waiting + "/" + k.Locked, (w, h) => GameplaySkin.Pod(w, h, color, k.Waiting, k.Locked));
            });
            p.Picture(key, frame, render);
            Box tile = chip.Tile.Offset(0f, sink);
            if (material == null)
            {
                p.Mark("pod.state.mystery");
                TextLook mystery = PodChip.CountLook(false);
                p.Text("?", tile.CenterX, tile.CenterY, GameplaySkin.PodCount, mystery.FillTop, tile.Width, tile.Height * 0.90f / p.U(GameplaySkin.PodCount.Size), mystery);
                return tile;
            }

            MaterialTile(p, tile, material, RoundLook.PodStyle, symbolsMode, waiting ? TileState.Dimmed : TileState.Normal);
            PodCount(p, chip.Count.Offset(0f, sink), count, waiting || locked);
            return tile;
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

        private static T[] MakeMaterials<T>(Func<int, T> make)
        {
            var items = new T[RoundMaterial.Codes.Length];
            for (int m = 0; m < items.Length; m++)
            {
                items[m] = make(m);
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
