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
    /// (contracts/painter.md, "Recording"). The kit's material pictures (spec 005 <c>UiRaster</c>) are cached as
    /// straight-alpha images, in a cache bounded by bytes as on the device (<see cref="PictureCache{T}"/>). The animated
    /// heroes' frames (spec 005 FR-028) take the device's path: decoded on first use into palette pictures in a cache
    /// bounded by bytes (<see cref="HeroFrameStore"/>), then expanded to premultiplied RGBA for the frames drawn.
    /// </summary>
    public sealed class SkiaPainter : PainterBase, IDisposable
    {
        private static readonly Dictionary<(MaskKind Kind, string Key, int Size), SKImage> Masks = new Dictionary<(MaskKind, string, int), SKImage>();
        private static readonly Dictionary<string, SKImage> Backdrops = new Dictionary<string, SKImage>(StringComparer.Ordinal);
        private static readonly PictureCache<SKImage> Pictures = new PictureCache<SKImage>(PictureCacheBytes, image => image.Dispose());
        private static readonly Dictionary<string, SKImage?> Sprites = new Dictionary<string, SKImage?>(StringComparer.Ordinal);
        private static readonly Dictionary<string, (byte[], int, int)?> Alphas = new Dictionary<string, (byte[], int, int)?>(StringComparer.Ordinal);
        private static readonly HeroFrameStore HeroFrames = new HeroFrameStore(typeof(SkiaPainter).Assembly, HeroFrameCacheBytes);
        private static readonly PictureCache<SKImage> HeroImages = new PictureCache<SKImage>(HeroDrawCacheBytes, image => image.Dispose());
        // Declared before the faces: static initializers run in order, and LoadFont reads it.
        private static readonly Dictionary<bool, SKTypeface?> Fonts = new Dictionary<bool, SKTypeface?>();
        private static readonly SKTypeface Bold = LoadFont(true) ?? SKTypeface.FromFamilyName("DejaVu Sans", SKFontStyle.Bold) ?? SKTypeface.Default;
        private static readonly SKTypeface Regular = LoadFont(false) ?? SKTypeface.FromFamilyName("DejaVu Sans", SKFontStyle.Normal) ?? SKTypeface.Default;

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

        /// <summary>Whether the bundled Nunito files were loaded (else the DejaVu fallback draws).</summary>
        public static bool BundledFonts => LoadFont(true) != null && LoadFont(false) != null;

        public override float Width => _width;

        public override float Height => _height;

        public SKCanvas Canvas => _surface.Canvas;

        /// <summary>Every slot id drawn or marked (shapes count as their slot).</summary>
        public HashSet<string> Slots { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Shape ids drawn that are not in the shape library (a check failure).</summary>
        public HashSet<string> UnknownShapes { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Character pictures asked for that are not embedded (a check failure).</summary>
        public HashSet<string> MissingSprites { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Every touch target of the frame, in screen pixels.</summary>
        public List<Box> Targets { get; } = new List<Box>();

        /// <summary>Every text drawn: its screen box and its text.</summary>
        public List<(Box Box, string Text)> Texts { get; } = new List<(Box, string)>();

        /// <summary>What a cached mask was made from.</summary>
        private enum MaskKind
        {
            Shape,
            Composite,
            Skin,
        }

        public override void BeginFrame()
        {
            lock (Pictures)
            {
                Pictures.NextFrame();
            }

            lock (HeroFrames)
            {
                HeroFrames.NextFrame();
                HeroImages.NextFrame();
            }

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

        protected override void ApplyTransform(float dx, float dy, float sx, float sy, float cx, float cy)
        {
            Canvas.Save();
            Canvas.Translate(dx, dy);
            Canvas.Scale(sx, sy, cx, cy);
        }

        /// <summary>A bundled Nunito face from the embedded resources (contracts/fonts.md), or null.</summary>
        private static SKTypeface? LoadFont(bool bold)
        {
            lock (Fonts)
            {
                if (!Fonts.TryGetValue(bold, out SKTypeface? face))
                {
                    using System.IO.Stream? stream = typeof(SkiaPainter).Assembly.GetManifestResourceStream(FontResource(bold));
                    face = stream != null ? SKTypeface.FromStream(stream) : null;
                    if (face == null)
                    {
                        Console.Error.WriteLine("preview: " + FontResource(bold) + " not found, using DejaVu Sans");
                    }

                    Fonts[bold] = face;
                }

                return face;
            }
        }

        protected override void RestoreTransform() => Canvas.Restore();

        protected override void ApplyRotation(float degrees, float cx, float cy)
        {
            Canvas.Save();
            Canvas.RotateDegrees(degrees, cx, cy);
        }

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
            DrawMask(MaskKind.Shape, id, null, box, color);
        }

        public override void ShapeOf(string key, Func<float, float, float> sdf, Box box, Rgba color) => DrawMask(MaskKind.Composite, key, sdf, box, color);

        private void DrawMask(MaskKind kind, string key, Func<float, float, float>? sdf, Box box, Rgba color)
        {
            int size = ShapeRaster.Quantize(Math.Max(box.Width, box.Height));
            if (!Masks.TryGetValue((kind, key, size), out SKImage? image))
            {
                Func<float, float, float> shape = kind switch
                {
                    MaskKind.Shape => ShapeLibrary.Get(key),
                    MaskKind.Skin => ShapeLibrary.SkinPattern(key),
                    _ => sdf!,
                };
                byte[] mask = ShapeRaster.Mask(shape, size, topDown: true);
                var info = new SKImageInfo(size, size, SKColorType.Alpha8, SKAlphaType.Premul);
                using var bitmap = new SKBitmap(info);
                System.Runtime.InteropServices.Marshal.Copy(mask, 0, bitmap.GetPixels(), mask.Length);
                image = SKImage.FromBitmap(bitmap);
                Masks[(kind, key, size)] = image;
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

        public override void Text(string text, float cx, float cy, TypeStyle style, Rgba color, float maxWidth = 0f, float sizeScale = 1f, TextLook? look = null) =>
            DrawText(text, cx, cy, style, color, maxWidth, sizeScale, centered: true, look);

        public override void TextLeft(string text, float x, float cy, TypeStyle style, Rgba color, float maxWidth = 0f, float sizeScale = 1f, TextLook? look = null) =>
            DrawText(text, x, cy, style, color, maxWidth, sizeScale, centered: false, look);

        private void DrawText(string text, float x, float cy, TypeStyle style, Rgba color, float maxWidth, float sizeScale, bool centered, TextLook? look)
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
            if (look != null)
            {
                DrawLook(shown, left, baseline, font, size, look, metrics);
                return;
            }

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

        /// <summary>A label with volume (contracts/painter-text.md): shadow, extrusion, outline, gradient fill.</summary>
        private void DrawLook(string shown, float left, float baseline, SKFont font, float size, TextLook look, SKFontMetrics metrics)
        {
            float stroke = look.OutlineEm * size * 2f;
            (int count, float step) = Extrusion(look, size);
            if (look.ShadowAlpha > 0f)
            {
                SKPaint shadow = Fill(DesignTokens.Colors.GardenShadow.WithAlpha(look.ShadowAlpha));
                shadow.Style = SKPaintStyle.StrokeAndFill;
                shadow.StrokeWidth = stroke;
                shadow.StrokeJoin = SKStrokeJoin.Round;
                using SKMaskFilter blur = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 0.06f * size);
                shadow.MaskFilter = blur;
                Canvas.DrawText(shown, left, baseline + ((look.ExtrudeEm + 0.05f) * size), font, shadow);
                shadow.MaskFilter = null;
            }

            if (look.Emboss.HasValue)
            {
                Canvas.DrawText(shown, left, baseline + (0.05f * size), font, Fill(look.Emboss.Value));
            }

            if (stroke > 0f)
            {
                SKPaint line = Fill(look.Outline);
                line.Style = SKPaintStyle.StrokeAndFill;
                line.StrokeWidth = stroke;
                line.StrokeJoin = SKStrokeJoin.Round;
                for (int k = count; k >= 0; k--)
                {
                    Canvas.DrawText(shown, left, baseline + (k * step), font, line);
                }
            }

            SKPaint fill = Fill(look.FillTop);
            float top = baseline + metrics.CapHeight * -1f;
            using SKShader shader = SKShader.CreateLinearGradient(new SKPoint(0, top), new SKPoint(0, baseline), new[] { Sk(Faded(look.FillTop, Alpha)), Sk(Faded(look.FillBottom, Alpha)) }, new[] { 0.3f, 1f }, SKShaderTileMode.Clamp);
            fill.Shader = shader;
            Canvas.DrawText(shown, left, baseline, font, fill);
            fill.Shader = null;
        }

        public override void Backdrop(Box box, BackdropColors colors, BackdropScene scene, string cacheKey)
        {
            float step = BackdropRaster.Downscale(scene);
            int w = Math.Max(32, (int)(box.Width / step));
            int h = Math.Max(32, (int)(box.Height / step));
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

        public override void Picture(string key, Box box, Func<int, int, byte[]> render)
        {
            if (box.Width <= 0f || box.Height <= 0f)
            {
                return;
            }

            int w = PictureSize(box.Width);
            int h = PictureSize(box.Height);
            SKImage image;
            lock (Pictures)
            {
                if (!Pictures.TryGet(key, w, h, out image))
                {
                    byte[] rgba = render(w, h);
                    if (rgba.Length != w * h * 4)
                    {
                        throw new ArgumentException("Picture " + UiRaster.CacheKey(key, w, h) + " rendered " + rgba.Length + " bytes, expected " + (w * h * 4) + ".");
                    }

                    // Straight alpha, as UiRaster renders it.
                    var info = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                    using var bitmap = new SKBitmap(info);
                    System.Runtime.InteropServices.Marshal.Copy(rgba, 0, bitmap.GetPixels(), rgba.Length);
                    image = SKImage.FromBitmap(bitmap);
                    Pictures.Add(key, w, h, image, rgba.Length);
                }
            }

            Canvas.DrawImage(image, Rect(box), new SKSamplingOptions(SKFilterMode.Linear), Fill(Rgba.White));
        }

        public override bool HasSprite(string name)
        {
            if (IsHeroFrame(name))
            {
                lock (HeroFrames)
                {
                    return HeroFrames.Has(name);
                }
            }

            return LoadSprite(name) != null;
        }

        /// <summary>How many hero frames were decoded so far, and the bytes of those held now (the preview's report).</summary>
        public static (int Decoded, long Bytes) HeroFrameStats
        {
            get
            {
                lock (HeroFrames)
                {
                    return (HeroFrames.Decoded, HeroFrames.Bytes);
                }
            }
        }

        public override (int Width, int Height)? SpriteSize(string name)
        {
            SKImage? image = LoadSprite(name);
            return image == null ? null : (image.Width, image.Height);
        }

        public override (byte[] Alpha, int Width, int Height)? SpriteAlpha(string name)
        {
            lock (Alphas)
            {
                if (!Alphas.TryGetValue(name, out (byte[], int, int)? alpha))
                {
                    SKImage? image = LoadSprite(name);
                    alpha = null;
                    if (image != null)
                    {
                        var info = new SKImageInfo(image.Width, image.Height, SKColorType.Alpha8, SKAlphaType.Premul);
                        var bytes = new byte[image.Width * image.Height];
                        var handle = System.Runtime.InteropServices.GCHandle.Alloc(bytes, System.Runtime.InteropServices.GCHandleType.Pinned);
                        try
                        {
                            if (image.ReadPixels(info, handle.AddrOfPinnedObject(), image.Width, 0, 0))
                            {
                                alpha = (bytes, image.Width, image.Height);
                            }
                        }
                        finally
                        {
                            handle.Free();
                        }
                    }

                    Alphas[name] = alpha;
                }

                return alpha;
            }
        }

        public override void Sprite(string name, Box box)
        {
            SKImage? image = LoadSprite(name);
            if (image == null)
            {
                MissingSprites.Add(name);
                return;
            }

            Canvas.DrawImage(image, Rect(Fit(box, image.Width, image.Height)), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), Fill(Rgba.White));
        }

        public override void SpriteSkin(string name, Box box, string skinShape, Rgba tint)
        {
            SKImage? image = LoadSprite(name);
            if (image == null)
            {
                MissingSprites.Add(name);
                return;
            }

            // A layer: the pattern, then the picture with destination-in, so the pattern stays only on the picture.
            SKRect rect = Rect(Fit(box, image.Width, image.Height));
            Canvas.SaveLayer(rect, null);
            DrawMask(MaskKind.Skin, skinShape, null, new Box(rect.Left, rect.Top, rect.Right, rect.Bottom), tint);
            using var mask = new SKPaint { BlendMode = SKBlendMode.DstIn };
            Canvas.DrawImage(image, rect, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), mask);
            Canvas.Restore();
        }

        /// <summary>
        /// A picture from the embedded resources (null when missing): an animated hero's frame from its palette picture,
        /// expanded for the frames that draw it (a frame that is not a palette PNG decodes as any picture), else decoded
        /// once.
        /// </summary>
        private static SKImage? LoadSprite(string name)
        {
            if (IsHeroFrame(name))
            {
                lock (HeroFrames)
                {
                    PalettePicture? frame = HeroFrames.Get(name);
                    if (frame != null)
                    {
                        if (!HeroImages.TryGet(name, frame.Width, frame.Height, out SKImage image))
                        {
                            var rgba = new byte[frame.Width * frame.Height * 4];
                            frame.Expand(rgba, frame.Width, premultiplied: true);
                            image = SKImage.FromPixelCopy(new SKImageInfo(frame.Width, frame.Height, SKColorType.Rgba8888, SKAlphaType.Premul), rgba);
                            HeroImages.Add(name, frame.Width, frame.Height, image, rgba.Length);
                        }

                        return image;
                    }

                    if (!HeroFrames.Has(name))
                    {
                        return null;
                    }
                }
            }

            lock (Sprites)
            {
                if (!Sprites.TryGetValue(name, out SKImage? image))
                {
                    string? grey = GreySource(name);
                    if (grey != null)
                    {
                        // A grey copy (a stuck slot's tile icon), made once from its picture.
                        SKImage? source = LoadSprite(grey);
                        image = source != null ? Greyed(source) : null;
                    }
                    else
                    {
                        using System.IO.Stream? stream = typeof(SkiaPainter).Assembly.GetManifestResourceStream(SpriteResource(name));
                        image = stream != null ? SKImage.FromEncodedData(stream)?.ToRasterImage(ensurePixelData: true) : null;
                    }

                    Sprites[name] = image;
                }

                return image;
            }
        }

        /// <summary>A grey copy of a picture (<see cref="OwnerPictures.GreyPixels"/>; the luma works on premultiplied pixels too).</summary>
        private static SKImage? Greyed(SKImage source)
        {
            var info = new SKImageInfo(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            var pixels = new byte[info.BytesSize];
            var handle = System.Runtime.InteropServices.GCHandle.Alloc(pixels, System.Runtime.InteropServices.GCHandleType.Pinned);
            try
            {
                if (!source.ReadPixels(info, handle.AddrOfPinnedObject(), info.RowBytes, 0, 0))
                {
                    return null;
                }

                OwnerPictures.GreyPixels(pixels);
                return SKImage.FromPixelCopy(info, handle.AddrOfPinnedObject(), info.RowBytes);
            }
            finally
            {
                handle.Free();
            }
        }

        public override void PushClip(Box box)
        {
            Canvas.Save();
            Canvas.ClipRect(Rect(box));
        }

        public override void PopClip() => Canvas.Restore();
    }
}
