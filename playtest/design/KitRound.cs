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
