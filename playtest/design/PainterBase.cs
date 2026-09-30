using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// The bookkeeping shared by the Android and the Skia painters:
    /// <list type="bullet">
    /// <item><description>the touch targets of the frame;</description></item>
    /// <item><description>the finger's position, for pressed looks;</description></item>
    /// <item><description>the alpha stack;</description></item>
    /// <item><description>the transform stack, which hit boxes follow;</description></item>
    /// <item><description>hooks the preview uses to record what was drawn.</description></item>
    /// </list>
    /// A frame starts with <see cref="BeginFrame"/>; a tap goes to <see cref="Dispatch"/>. Engine-free.
    /// </summary>
    public abstract class PainterBase : IPainter
    {
        private readonly List<(Box Box, Action Action)> _hits = new List<(Box, Action)>();
        private readonly List<float> _alpha = new List<float>();
        private readonly List<(float Dx, float Dy, float Scale, float Cx, float Cy)> _transforms = new List<(float, float, float, float, float)>();

        public abstract float Width { get; }

        public abstract float Height { get; }

        public float Scale => DesignTokens.ScaleFor(Width, Height);

        public Insets Insets { get; set; }

        /// <summary>Where a finger is down, or null.</summary>
        public (float X, float Y)? Finger { get; set; }

        public IReadOnlyList<(Box Box, Action Action)> Hits => _hits;

        /// <summary>The alpha everything is multiplied by now.</summary>
        protected float Alpha => _alpha.Count == 0 ? 1f : _alpha[_alpha.Count - 1];

        public virtual void BeginFrame()
        {
            _hits.Clear();
            _alpha.Clear();
            _transforms.Clear();
        }

        /// <summary>Runs the topmost target under a tap; false when none was hit.</summary>
        public bool Dispatch(float x, float y)
        {
            for (int i = _hits.Count - 1; i >= 0; i--)
            {
                if (_hits[i].Box.Contains(x, y))
                {
                    _hits[i].Action();
                    return true;
                }
            }

            return false;
        }

        public void Hit(Box box, Action action)
        {
            Box screen = ToScreen(box);
            _hits.Add((screen, action));
            OnHit(screen);
        }

        public bool Pressed(Box box) => Finger.HasValue && ToScreen(box).Contains(Finger.Value.X, Finger.Value.Y);

        public void PushAlpha(float alpha) => _alpha.Add(Alpha * Math.Max(0f, Math.Min(1f, alpha)));

        public void PopAlpha()
        {
            if (_alpha.Count > 0)
            {
                _alpha.RemoveAt(_alpha.Count - 1);
            }
        }

        public void PushTransform(float dx, float dy, float scale, float cx, float cy)
        {
            _transforms.Add((dx, dy, scale, cx, cy));
            ApplyTransform(dx, dy, scale, cx, cy);
        }

        public void PopTransform()
        {
            if (_transforms.Count > 0)
            {
                _transforms.RemoveAt(_transforms.Count - 1);
                RestoreTransform();
            }
        }

        public virtual void Mark(string slotId)
        {
        }

        /// <summary>A box in the current transform, in screen pixels.</summary>
        protected Box ToScreen(Box box)
        {
            float l = box.Left;
            float t = box.Top;
            float r = box.Right;
            float b = box.Bottom;
            for (int i = _transforms.Count - 1; i >= 0; i--)
            {
                (float dx, float dy, float s, float cx, float cy) = _transforms[i];
                l = ((l - cx) * s) + cx + dx;
                r = ((r - cx) * s) + cx + dx;
                t = ((t - cy) * s) + cy + dy;
                b = ((b - cy) * s) + cy + dy;
            }

            return new Box(l, t, r, b);
        }

        /// <summary>Called for each touch target (the preview checks sizes and overlaps).</summary>
        protected virtual void OnHit(Box screenBox)
        {
        }

        protected abstract void ApplyTransform(float dx, float dy, float scale, float cx, float cy);

        protected abstract void RestoreTransform();

        protected static Rgba Faded(Rgba color, float alpha) => alpha >= 1f ? color : color.WithAlpha(color.A / 255f * alpha);

        public abstract void FillRect(Box box, Rgba color);

        public abstract void FillRound(Box box, float radius, Rgba color);

        public abstract void FillRoundGradient(Box box, float radius, Rgba top, Rgba bottom);

        public abstract void StrokeRound(Box box, float radius, float width, Rgba color, float dashOn = 0f, float dashOff = 0f);

        public abstract void FillCircle(float cx, float cy, float r, Rgba color);

        public abstract void StrokeCircle(float cx, float cy, float r, float width, Rgba color);

        public abstract void Line(float x0, float y0, float x1, float y1, float width, Rgba color);

        public abstract void Shape(string id, Box box, Rgba color);

        public abstract void ShapeOf(string key, Func<float, float, float> sdf, Box box, Rgba color);

        public abstract void Text(string text, float cx, float cy, TypeStyle style, Rgba color, float maxWidth = 0f, float sizeScale = 1f);

        public abstract void TextLeft(string text, float x, float cy, TypeStyle style, Rgba color, float maxWidth = 0f, float sizeScale = 1f);

        public abstract float MeasureText(string text, TypeStyle style, float sizeScale = 1f);

        public abstract void Backdrop(Box box, BackdropColors colors, BackdropScene scene, string cacheKey);

        public abstract void PushClip(Box box);

        public abstract void PopClip();

        /// <summary>The text size in pixels, shrunk to fit <paramref name="maxWidth"/> down to the style's minimum.</summary>
        protected float FitSize(string text, TypeStyle style, float maxWidth, float sizeScale, Func<string, float, float> measureAt)
        {
            float size = style.Size * Scale * sizeScale;
            if (maxWidth <= 0f || text.Length == 0)
            {
                return size;
            }

            float width = measureAt(text, size);
            if (width <= maxWidth)
            {
                return size;
            }

            float min = style.Min * Scale * sizeScale;
            return Math.Max(min, size * maxWidth / width);
        }

        /// <summary>The text as drawn: uppercase when the style says so.</summary>
        protected static string Cased(string text, TypeStyle style) => style.Upper ? text.ToUpperInvariant() : text;
    }
}
