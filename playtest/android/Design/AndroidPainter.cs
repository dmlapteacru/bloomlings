using System;
using System.Collections.Generic;
using Android.Graphics;
using Bloomlings.Client.UI.Design;
using Bloomlings.Playtest.Design;
using Color = Android.Graphics.Color;

namespace Bloomlings.Playtest.Droid
{
    /// <summary>
    /// <see cref="IPainter"/> over <see cref="Canvas"/> (spec 002 research R3): the full playtest APK's drawing of the
    /// designed screens. Shape masks become cached ALPHA_8 bitmaps tinted by the paint color. The garden backdrop
    /// becomes a cached bitmap scaled with filtering. Text uses the bold or regular system typeface with an outline.
    /// </summary>
    public sealed class AndroidPainter : PainterBase
    {
        private static readonly Dictionary<string, Bitmap> Masks = new Dictionary<string, Bitmap>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Bitmap> Backdrops = new Dictionary<string, Bitmap>(StringComparer.Ordinal);

        private readonly Paint _paint = new Paint(PaintFlags.AntiAlias | PaintFlags.FilterBitmap);
        private readonly Paint _text = new Paint(PaintFlags.AntiAlias);
        private readonly Typeface _bold = Typeface.Create(Typeface.Default, TypefaceStyle.Bold)!;
        private readonly Typeface _regular = Typeface.Default!;
        private readonly RectF _rect = new RectF();
        private readonly Rect _source = new Rect();
        private Canvas _canvas = null!;
        private float _width;
        private float _height;

        public override float Width => _width;

        public override float Height => _height;

        /// <summary>Starts a frame on a view's canvas.</summary>
        public void Begin(Canvas canvas, float width, float height, Client.UI.Design.Insets insets)
        {
            _canvas = canvas;
            _width = width;
            _height = height;
            Insets = insets;
            BeginFrame();
        }

        protected override void ApplyTransform(float dx, float dy, float scale, float cx, float cy)
        {
            _canvas.Save();
            _canvas.Translate(dx, dy);
            _canvas.Scale(scale, scale, cx, cy);
        }

        protected override void RestoreTransform() => _canvas.Restore();

        private Paint Fill(Rgba color)
        {
            _paint.Reset();
            _paint.AntiAlias = true;
            _paint.FilterBitmap = true;
            _paint.SetStyle(Paint.Style.Fill);
            _paint.Color = ToColor(Faded(color, Alpha));
            return _paint;
        }

        private static Color ToColor(Rgba c) => Color.Argb(c.A, c.R, c.G, c.B);

        private RectF R(Box b)
        {
            _rect.Set(b.Left, b.Top, b.Right, b.Bottom);
            return _rect;
        }

        public override void FillRect(Box box, Rgba color) => _canvas.DrawRect(R(box), Fill(color));

        public override void FillRound(Box box, float radius, Rgba color)
        {
            float r = Math.Min(radius, box.Height / 2f);
            _canvas.DrawRoundRect(R(box), r, r, Fill(color));
        }

        public override void FillRoundGradient(Box box, float radius, Rgba top, Rgba bottom)
        {
            Paint paint = Fill(top);
            using var shader = new LinearGradient(0f, box.Top, 0f, box.Bottom, ToColor(Faded(top, Alpha)), ToColor(Faded(bottom, Alpha)), Shader.TileMode.Clamp!);
            paint.SetShader(shader);
            float r = Math.Min(radius, box.Height / 2f);
            _canvas.DrawRoundRect(R(box), r, r, paint);
            paint.SetShader(null);
        }

        public override void StrokeRound(Box box, float radius, float width, Rgba color, float dashOn = 0f, float dashOff = 0f)
        {
            Paint paint = Fill(color);
            paint.SetStyle(Paint.Style.Stroke);
            paint.StrokeWidth = width;
            using DashPathEffect? dash = dashOn > 0f ? new DashPathEffect(new[] { dashOn, dashOff }, 0f) : null;
            paint.SetPathEffect(dash);
            float r = Math.Min(radius, box.Height / 2f);
            _canvas.DrawRoundRect(R(box), r, r, paint);
            paint.SetPathEffect(null);
        }

        public override void FillCircle(float cx, float cy, float r, Rgba color) => _canvas.DrawCircle(cx, cy, r, Fill(color));

        public override void StrokeCircle(float cx, float cy, float r, float width, Rgba color)
        {
            Paint paint = Fill(color);
            paint.SetStyle(Paint.Style.Stroke);
            paint.StrokeWidth = width;
            _canvas.DrawCircle(cx, cy, r, paint);
        }

        public override void Line(float x0, float y0, float x1, float y1, float width, Rgba color)
        {
            Paint paint = Fill(color);
            paint.SetStyle(Paint.Style.Stroke);
            paint.StrokeWidth = width;
            paint.StrokeCap = Paint.Cap.Round;
            _canvas.DrawLine(x0, y0, x1, y1, paint);
        }

        public override void Shape(string id, Box box, Rgba color) => DrawMask(id, () => ShapeLibrary.Get(id), box, color);

        public override void ShapeOf(string key, Func<float, float, float> sdf, Box box, Rgba color) => DrawMask("composite/" + key, () => sdf, box, color);

        private void DrawMask(string key, Func<Func<float, float, float>> sdf, Box box, Rgba color)
        {
            int size = ShapeRaster.Quantize(Math.Max(box.Width, box.Height));
            string cacheKey = key + "@" + size;
            if (!Masks.TryGetValue(cacheKey, out Bitmap? bitmap))
            {
                byte[] mask = ShapeRaster.Mask(sdf(), size, topDown: true);
                bitmap = Bitmap.CreateBitmap(size, size, Bitmap.Config.Alpha8!)!;
                bitmap.CopyPixelsFromBuffer(Java.Nio.ByteBuffer.Wrap(mask));
                Masks[cacheKey] = bitmap;
            }

            _source.Set(0, 0, size, size);
            _canvas.DrawBitmap(bitmap, _source, R(box), Fill(color));
        }

        private float MeasureAt(string text, TypeStyle style, float size)
        {
            _text.SetTypeface(style.Bold ? _bold : _regular);
            _text.TextSize = size;
            return _text.MeasureText(text);
        }

        public override float MeasureText(string text, TypeStyle style, float sizeScale = 1f) =>
            MeasureAt(Cased(text, style), style, style.Size * Scale * sizeScale);

        public override void Text(string text, float cx, float cy, TypeStyle style, Rgba color, float maxWidth = 0f, float sizeScale = 1f) =>
            DrawText(text, cx, cy, style, color, maxWidth, sizeScale, centered: true);

        public override void TextLeft(string text, float x, float cy, TypeStyle style, Rgba color, float maxWidth = 0f, float sizeScale = 1f) =>
            DrawText(text, x, cy, style, color, maxWidth, sizeScale, centered: false);

        private void DrawText(string text, float x, float cy, TypeStyle style, Rgba color, float maxWidth, float sizeScale, bool centered)
        {
            if (text.Length == 0)
            {
                return;
            }

            string shown = Cased(text, style);
            float size = FitSize(shown, style, maxWidth, sizeScale, (t, s) => MeasureAt(t, style, s));
            _text.Reset();
            _text.AntiAlias = true;
            _text.SetTypeface(style.Bold ? _bold : _regular);
            _text.TextSize = size;
            float width = _text.MeasureText(shown);
            float left = centered ? x - (width / 2f) : x;
            float baseline = cy - ((_text.Descent() + _text.Ascent()) / 2f);
            if (style.Outline > 0f)
            {
                _text.SetStyle(Paint.Style.Stroke);
                _text.StrokeWidth = style.Outline * Scale * (size / (style.Size * Scale));
                _text.StrokeJoin = Paint.Join.Round;
                _text.Color = ToColor(Faded(DesignTokens.Colors.TextOutline, Alpha));
                _canvas.DrawText(shown, left, baseline, _text);
            }

            _text.SetStyle(Paint.Style.Fill);
            _text.Color = ToColor(Faded(color, Alpha));
            _canvas.DrawText(shown, left, baseline, _text);
        }

        public override void Backdrop(Box box, BackdropColors colors, BackdropScene scene, string cacheKey)
        {
            int w = Math.Max(32, (int)(box.Width / 5f));
            int h = Math.Max(32, (int)(box.Height / 5f));
            string key = cacheKey + "@" + w + "x" + h;
            if (!Backdrops.TryGetValue(key, out Bitmap? bitmap))
            {
                byte[] rgba = BackdropRaster.Render(w, h, colors, scene);
                bitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888!)!;
                bitmap.CopyPixelsFromBuffer(Java.Nio.ByteBuffer.Wrap(rgba));
                Backdrops[key] = bitmap;
            }

            _source.Set(0, 0, w, h);
            _canvas.DrawBitmap(bitmap, _source, R(box), Fill(Rgba.White));
        }

        public override void PushClip(Box box)
        {
            _canvas.Save();
            _canvas.ClipRect(R(box));
        }

        public override void PopClip() => _canvas.Restore();
    }
}
