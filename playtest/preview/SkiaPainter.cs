using System;
using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Playtest.Design;
using SkiaSharp;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>
    /// <see cref="IPainter"/> over SkiaSharp, for PNG previews of the playtest's designed screens (spec 002 research R3).
    /// It also records every shape, asset slot and text drawn, and every touch target, so the preview can check them
    /// (contracts/painter.md, "Recording").
    /// </summary>
    public sealed class SkiaPainter : PainterBase, IDisposable
    {
        private static readonly Dictionary<string, SKImage> Masks = new Dictionary<string, SKImage>(StringComparer.Ordinal);
        private static readonly Dictionary<string, SKImage> Backdrops = new Dictionary<string, SKImage>(StringComparer.Ordinal);
        private static readonly SKTypeface Bold = SKTypeface.FromFamilyName("DejaVu Sans", SKFontStyle.Bold) ?? SKTypeface.Default;
        private static readonly SKTypeface Regular = SKTypeface.FromFamilyName("DejaVu Sans", SKFontStyle.Normal) ?? SKTypeface.Default;

        private readonly SKSurface _surface;
        private readonly SKPaint _paint = new SKPaint { IsAntialias = true };
        private readonly int _width;
        private readonly int _height;

        public SkiaPainter(int width, int height, Insets insets)
        {
            _width = width;
            _height = height;
            Insets = insets;
            _surface = SKSurface.Create(new SKImageInfo(width, height));
        }

        public override float Width => _width;

        public override float Height => _height;

        public SKCanvas Canvas => _surface.Canvas;

        /// <summary>Every slot id drawn or marked (shapes count as their slot).</summary>
        public HashSet<string> Slots { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Shape ids drawn that are not in the shape library (a check failure).</summary>
        public HashSet<string> UnknownShapes { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Every touch target of the frame, in screen pixels.</summary>
        public List<Box> Targets { get; } = new List<Box>();

        /// <summary>Every text drawn: its screen box and its text.</summary>
        public List<(Box Box, string Text)> Texts { get; } = new List<(Box, string)>();

        public override void BeginFrame()
        {
            base.BeginFrame();
            Targets.Clear();
            Texts.Clear();
            Canvas.Clear(SKColors.White);
        }

        public byte[] Png()
        {
            using SKImage image = _surface.Snapshot();
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        public SKImage Snapshot() => _surface.Snapshot();

        public void Dispose()
        {
            _paint.Dispose();
            _surface.Dispose();
        }

        public override void Mark(string slotId) => Slots.Add(slotId);

        protected override void OnHit(Box screenBox) => Targets.Add(screenBox);

        protected override void ApplyTransform(float dx, float dy, float scale, float cx, float cy)
        {
            Canvas.Save();
            Canvas.Translate(dx, dy);
            Canvas.Scale(scale, scale, cx, cy);
        }

        protected override void RestoreTransform() => Canvas.Restore();

        private SKPaint Fill(Rgba color)
        {
            _paint.Reset();
            _paint.IsAntialias = true;
            _paint.Style = SKPaintStyle.Fill;
            _paint.Color = Sk(Faded(color, Alpha));
            return _paint;
        }

        private static SKColor Sk(Rgba c) => new SKColor(c.R, c.G, c.B, c.A);

        private static SKRect Rect(Box b) => new SKRect(b.Left, b.Top, b.Right, b.Bottom);

        public override void FillRect(Box box, Rgba color) => Canvas.DrawRect(Rect(box), Fill(color));

        public override void FillRound(Box box, float radius, Rgba color) =>
            Canvas.DrawRoundRect(Rect(box), Math.Min(radius, box.Height / 2f), Math.Min(radius, box.Height / 2f), Fill(color));

        public override void FillRoundGradient(Box box, float radius, Rgba top, Rgba bottom)
        {
            SKPaint paint = Fill(top);
            using SKShader shader = SKShader.CreateLinearGradient(new SKPoint(0, box.Top), new SKPoint(0, box.Bottom), new[] { Sk(Faded(top, Alpha)), Sk(Faded(bottom, Alpha)) }, new[] { 0f, 1f }, SKShaderTileMode.Clamp);
            paint.Shader = shader;
            Canvas.DrawRoundRect(Rect(box), Math.Min(radius, box.Height / 2f), Math.Min(radius, box.Height / 2f), paint);
            paint.Shader = null;
        }

        public override void StrokeRound(Box box, float radius, float width, Rgba color, float dashOn = 0f, float dashOff = 0f)
        {
            SKPaint paint = Fill(color);
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = width;
            using SKPathEffect? dash = dashOn > 0f ? SKPathEffect.CreateDash(new[] { dashOn, dashOff }, 0f) : null;
            paint.PathEffect = dash;
            Canvas.DrawRoundRect(Rect(box), Math.Min(radius, box.Height / 2f), Math.Min(radius, box.Height / 2f), paint);
            paint.PathEffect = null;
        }

        public override void FillCircle(float cx, float cy, float r, Rgba color) => Canvas.DrawCircle(cx, cy, r, Fill(color));

        public override void StrokeCircle(float cx, float cy, float r, float width, Rgba color)
        {
            SKPaint paint = Fill(color);
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = width;
            Canvas.DrawCircle(cx, cy, r, paint);
        }

        public override void Line(float x0, float y0, float x1, float y1, float width, Rgba color)
        {
            SKPaint paint = Fill(color);
            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = width;
            paint.StrokeCap = SKStrokeCap.Round;
            Canvas.DrawLine(x0, y0, x1, y1, paint);
        }

        public override void Shape(string id, Box box, Rgba color)
        {
            if (!ShapeLibrary.Has(id))
            {
                UnknownShapes.Add(id);
            }

            Slots.Add(id);
            DrawMask(id, () => ShapeLibrary.Get(id), box, color);
        }

        public override void ShapeOf(string key, Func<float, float, float> sdf, Box box, Rgba color) => DrawMask("composite/" + key, () => sdf, box, color);

        private void DrawMask(string key, Func<Func<float, float, float>> sdf, Box box, Rgba color)
        {
            int size = ShapeRaster.Quantize(Math.Max(box.Width, box.Height));
            string cacheKey = key + "@" + size;
            if (!Masks.TryGetValue(cacheKey, out SKImage? image))
            {
                byte[] mask = ShapeRaster.Mask(sdf(), size, topDown: true);
                var info = new SKImageInfo(size, size, SKColorType.Alpha8, SKAlphaType.Premul);
                using var bitmap = new SKBitmap(info);
                System.Runtime.InteropServices.Marshal.Copy(mask, 0, bitmap.GetPixels(), mask.Length);
                image = SKImage.FromBitmap(bitmap);
                Masks[cacheKey] = image;
            }

            SKPaint paint = Fill(color);
            Canvas.DrawImage(image, Rect(box), new SKSamplingOptions(SKFilterMode.Linear), paint);
        }

        private SKFont Font(TypeStyle style, float size) => new SKFont(style.Bold ? Bold : Regular, size) { Subpixel = true, Edging = SKFontEdging.Antialias };

        private float MeasureAt(string text, TypeStyle style, float size)
        {
            using SKFont font = Font(style, size);
            return font.MeasureText(text);
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
            Slots.Add(style.Bold ? "font.display" : "font.body");
            float size = FitSize(shown, style, maxWidth, sizeScale, (t, s) => MeasureAt(t, style, s));
            using SKFont font = Font(style, size);
            float width = font.MeasureText(shown);
            float left = centered ? x - (width / 2f) : x;
            SKFontMetrics metrics = font.Metrics;
            float baseline = cy - ((metrics.Ascent + metrics.Descent) / 2f);
            Texts.Add((ToScreen(new Box(left, cy - (size / 2f), left + width, cy + (size / 2f))), shown));
            if (style.Outline > 0f)
            {
                SKPaint stroke = Fill(DesignTokens.Colors.TextOutline);
                stroke.Style = SKPaintStyle.Stroke;
                stroke.StrokeWidth = style.Outline * Scale * (size / (style.Size * Scale));
                stroke.StrokeJoin = SKStrokeJoin.Round;
                Canvas.DrawText(shown, left, baseline, font, stroke);
            }

            Canvas.DrawText(shown, left, baseline, font, Fill(color));
        }

        public override void Backdrop(Box box, BackdropColors colors, BackdropScene scene, string cacheKey)
        {
            int w = Math.Max(32, (int)(box.Width / 5f));
            int h = Math.Max(32, (int)(box.Height / 5f));
            string key = cacheKey + "@" + w + "x" + h;
            if (!Backdrops.TryGetValue(key, out SKImage? image))
            {
                byte[] rgba = BackdropRaster.Render(w, h, colors, scene);
                var info = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var bitmap = new SKBitmap(info);
                System.Runtime.InteropServices.Marshal.Copy(rgba, 0, bitmap.GetPixels(), rgba.Length);
                image = SKImage.FromBitmap(bitmap);
                Backdrops[key] = image;
            }

            Canvas.DrawImage(image, Rect(box), new SKSamplingOptions(SKFilterMode.Linear), Fill(Rgba.White));
        }

        public override void PushClip(Box box)
        {
            Canvas.Save();
            Canvas.ClipRect(Rect(box));
        }

        public override void PopClip() => Canvas.Restore();
    }
}
