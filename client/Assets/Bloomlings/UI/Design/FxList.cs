using System;
using System.Collections.Generic;
using Bloomlings.Core.Variants;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>What one drawn item of a clearing style is (<see cref="FxList"/>).</summary>
    public enum FxKind
    {
        /// <summary>A variant's 2D character (the host's walker figure, in its outfit in Unity).</summary>
        Character,

        /// <summary>A variant's candy tile, as on the board.</summary>
        Tile,

        /// <summary>A filled circle or ellipse filling its box (<see cref="FxItem.W"/> × <see cref="FxItem.H"/>).</summary>
        Circle,

        /// <summary>A circle's outline (diameter <see cref="FxItem.W"/>, line <see cref="FxItem.Line"/>).</summary>
        Ring,

        /// <summary>A filled rounded box (corner <see cref="FxItem.Radius"/>).</summary>
        Round,

        /// <summary>A rounded box's outline (corner <see cref="FxItem.Radius"/>, line <see cref="FxItem.Line"/>).</summary>
        RoundRing,

        /// <summary>A shape of the shape library (<see cref="FxItem.Shape"/>), filled with the color.</summary>
        Shape,
    }

    /// <summary>Where an item is drawn: over the tiles (under the tray and slots), or over everything of the level.</summary>
    public enum FxLayer
    {
        Board,
        Over,
    }

    /// <summary>
    /// One drawn item, in cell units with y down (a cell is 1 × 1): a box of <see cref="W"/> × <see cref="H"/> centered
    /// on (<see cref="X"/>, <see cref="Y"/>), squashed by (<see cref="Sx"/>, <see cref="Sy"/>) along its own axes and
    /// then turned by <see cref="Turn"/> degrees clockwise on screen, both about its center; <see cref="Alpha"/> is its
    /// whole opacity. A host draws it by turning and squashing about its center and drawing the box.
    /// </summary>
    public readonly struct FxItem
    {
        public FxItem(FxKind kind, FxLayer layer, string slot, float x, float y, float w, float h, float sx, float sy, float turn, float alpha, Rgba color, VariantId variant, CharacterMood mood, string? shape, float radius, float line)
        {
            Kind = kind;
            Layer = layer;
            Slot = slot;
            X = x;
            Y = y;
            W = w;
            H = h;
            Sx = sx;
            Sy = sy;
            Turn = turn;
            Alpha = alpha;
            Color = color;
            Variant = variant;
            Mood = mood;
            Shape = shape;
            Radius = radius;
            Line = line;
        }

        public FxKind Kind { get; }

        public FxLayer Layer { get; }

        /// <summary>The registered asset slot the item stands for (<see cref="AssetSlots"/>).</summary>
        public string Slot { get; }

        public float X { get; }

        public float Y { get; }

        public float W { get; }

        public float H { get; }

        public float Sx { get; }

        public float Sy { get; }

        public float Turn { get; }

        public float Alpha { get; }

        public Rgba Color { get; }

        public VariantId Variant { get; }

        public CharacterMood Mood { get; }

        public string? Shape { get; }

        /// <summary>A rounded box's corner, in cell units.</summary>
        public float Radius { get; }

        /// <summary>An outline's width, in cell units.</summary>
        public float Line { get; }

        public Box Box => Box.FromCenter(X, Y, W, H);
    }

    /// <summary>
    /// The items a clearing style draws this frame (<see cref="ClearLook"/>), in the order to draw them, with a small
    /// transform stack like a painter's: turns, squashes and scales about a point, and an opacity. Each item keeps its
    /// composed transform as a turn and a squash about its own center, so a host needs neither shear nor nesting (the
    /// looks only squash along an item's own axes after turning it). Engine-free.
    /// </summary>
    public sealed class FxList
    {
        private readonly List<FxItem> _items = new List<FxItem>();
        private readonly List<(float A, float B, float C, float D, float Tx, float Ty, float Alpha)> _stack = new List<(float, float, float, float, float, float, float)>();

        // The current transform: x' = A x + B y + Tx, y' = C x + D y + Ty; and the opacity.
        private float _a = 1f;
        private float _b;
        private float _c;
        private float _d = 1f;
        private float _tx;
        private float _ty;
        private float _alpha = 1f;

        public IReadOnlyList<FxItem> Items => _items;

        /// <summary>The layer the next items go to.</summary>
        public FxLayer Layer { get; set; } = FxLayer.Board;

        /// <summary>The slot the next items stand for (<see cref="AssetSlots"/>).</summary>
        public string Slot { get; set; } = "fx.clear";

        public void Clear()
        {
            _items.Clear();
            _stack.Clear();
            _a = 1f;
            _b = 0f;
            _c = 0f;
            _d = 1f;
            _tx = 0f;
            _ty = 0f;
            _alpha = 1f;
            Layer = FxLayer.Board;
        }

        /// <summary>Turns what follows by <paramref name="degrees"/> clockwise about (<paramref name="px"/>, <paramref name="py"/>).</summary>
        public void PushTurn(float degrees, float px, float py)
        {
            double r = degrees * Math.PI / 180.0;
            float cos = (float)Math.Cos(r);
            float sin = (float)Math.Sin(r);
            Push(cos, -sin, sin, cos, px, py);
        }

        /// <summary>Squashes what follows by (<paramref name="sx"/>, <paramref name="sy"/>) about (<paramref name="px"/>, <paramref name="py"/>).</summary>
        public void PushSquash(float sx, float sy, float px, float py) => Push(sx, 0f, 0f, sy, px, py);

        public void PushScale(float scale, float px, float py) => Push(scale, 0f, 0f, scale, px, py);

        public void PushAlpha(float alpha)
        {
            Save();
            _alpha *= Clamp01(alpha);
        }

        /// <summary>Ends the last push.</summary>
        public void Pop()
        {
            if (_stack.Count == 0)
            {
                return;
            }

            (_a, _b, _c, _d, _tx, _ty, _alpha) = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
        }

        public void Character(Box box, VariantId variant, CharacterMood mood = CharacterMood.Happy) =>
            Add(FxKind.Character, box, Rgba.White, variant, mood, null, 0f, 0f);

        public void Tile(Box box, VariantId variant) => Add(FxKind.Tile, box, Rgba.White, variant, CharacterMood.Happy, null, 0f, 0f);

        /// <summary>The soft flat shadow under a figure standing in <paramref name="figure"/> (the walkers' own).</summary>
        public void Shadow(Box figure) =>
            Ellipse(Box.FromCenter(figure.CenterX, figure.Bottom - (figure.Height * 0.05f), figure.Width * 0.6f, figure.Width * 0.18f), Rgba.Black.WithAlpha(0.13f));

        public void Ellipse(Box box, Rgba color) => Add(FxKind.Circle, box, color, default, CharacterMood.Happy, null, 0f, 0f);

        public void Circle(float cx, float cy, float r, Rgba color) =>
            Add(FxKind.Circle, Box.FromCenter(cx, cy, r * 2f, r * 2f), color, default, CharacterMood.Happy, null, 0f, 0f);

        public void Ring(float cx, float cy, float r, float line, Rgba color) =>
            Add(FxKind.Ring, Box.FromCenter(cx, cy, r * 2f, r * 2f), color, default, CharacterMood.Happy, null, 0f, line);

        public void Round(Box box, float radius, Rgba color) => Add(FxKind.Round, box, color, default, CharacterMood.Happy, null, radius, 0f);

        public void RoundRing(Box box, float radius, float line, Rgba color) => Add(FxKind.RoundRing, box, color, default, CharacterMood.Happy, null, radius, line);

        public void Shape(string id, Box box, Rgba color) => Add(FxKind.Shape, box, color, default, CharacterMood.Happy, id, 0f, 0f);

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        private void Save() => _stack.Add((_a, _b, _c, _d, _tx, _ty, _alpha));

        // Composes the current transform with "translate to p, apply m, translate back".
        private void Push(float ma, float mb, float mc, float md, float px, float py)
        {
            Save();
            float ex = px - (ma * px) - (mb * py);
            float ey = py - (mc * px) - (md * py);
            float a = (_a * ma) + (_b * mc);
            float b = (_a * mb) + (_b * md);
            float c = (_c * ma) + (_d * mc);
            float d = (_c * mb) + (_d * md);
            float tx = (_a * ex) + (_b * ey) + _tx;
            float ty = (_c * ex) + (_d * ey) + _ty;
            _a = a;
            _b = b;
            _c = c;
            _d = d;
            _tx = tx;
            _ty = ty;
        }

        private void Add(FxKind kind, Box box, Rgba color, VariantId variant, CharacterMood mood, string? shape, float radius, float line)
        {
            if (_alpha <= 0.001f)
            {
                return;
            }

            float cx = box.CenterX;
            float cy = box.CenterY;
            float x = (_a * cx) + (_b * cy) + _tx;
            float y = (_c * cx) + (_d * cy) + _ty;

            // The linear part as a turn after a squash: its first column is the turned x axis.
            float sx = (float)Math.Sqrt((_a * _a) + (_c * _c));
            float turn = (float)Math.Atan2(_c, _a);
            float sy = (-(float)Math.Sin(turn) * _b) + ((float)Math.Cos(turn) * _d);
            if (sx < 1e-6f)
            {
                sx = 0f;
                turn = 0f;
                sy = _d;
            }

            _items.Add(new FxItem(kind, Layer, Slot, x, y, box.Width, box.Height, sx, sy, turn * 180f / (float)Math.PI, _alpha, color, variant, mood, shape, radius, line));
        }
    }
}
