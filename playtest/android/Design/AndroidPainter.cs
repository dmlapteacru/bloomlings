using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.Graphics;
using Bloomlings.Client.UI.Design;
using Bloomlings.Playtest.Design;
using Color = Android.Graphics.Color;

namespace Bloomlings.Playtest.Droid
{
    /// <summary>
    /// <see cref="IPainter"/> over <see cref="Canvas"/> (spec 002 research R3): the full playtest APK's drawing of the
    /// designed screens. Shape masks become cached ALPHA_8 bitmaps tinted by the paint color. The garden backdrop
    /// becomes a cached bitmap scaled with filtering. Text uses the bundled Nunito faces (spec 003 contracts/fonts.md),
    /// falling back to the system typeface, and labels with a look get their shadow, extrusion, outline and gradient
    /// fill (contracts/painter-text.md). The generated character pictures (spec 004) are embedded PNG files, decoded once
    /// with <see cref="BitmapFactory"/>. The kit's material pictures (spec 005 <c>UiRaster</c>) become premultiplied
    /// ARGB_8888 bitmaps drawn with filtering, in a cache bounded by bytes (<see cref="PictureCache{T}"/>). The gameplay
    /// lawn renders on a worker thread; until it is ready the lawn's flat gradient shows, then the view redraws.
    /// </summary>
    public sealed class AndroidPainter : PainterBase
    {
        /// <summary>How many vertical gradients are kept before they are all made again.</summary>
        private const int GradientCacheLimit = 256;

        private static readonly Dictionary<(MaskKind Kind, string Key, int Size), Bitmap> Masks = new Dictionary<(MaskKind, string, int), Bitmap>();
        private static readonly Dictionary<string, Bitmap> Backdrops = new Dictionary<string, Bitmap>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Task<byte[]>> PendingBackdrops = new Dictionary<string, Task<byte[]>>(StringComparer.Ordinal);
        private static readonly PictureCache<Bitmap> Pictures = new PictureCache<Bitmap>(PictureCacheBytes, bitmap => bitmap.Dispose());
        private static readonly Dictionary<(int Top, int Bottom, float Height), LinearGradient> Gradients = new Dictionary<(int, int, float), LinearGradient>();
        private static readonly Dictionary<string, Bitmap?> Sprites = new Dictionary<string, Bitmap?>(StringComparer.Ordinal);

        private readonly Paint _paint = new Paint(PaintFlags.AntiAlias | PaintFlags.FilterBitmap);
        private readonly Paint _text = new Paint(PaintFlags.AntiAlias);
        private static Typeface? s_bold;
        private static Typeface? s_regular;
        private readonly Typeface _bold = s_bold ??= LoadFont(true) ?? Typeface.Create(Typeface.Default, TypefaceStyle.Bold)!;
        private readonly Typeface _regular = s_regular ??= LoadFont(false) ?? Typeface.Default!;
        private readonly RectF _rect = new RectF();
        private readonly Rect _source = new Rect();
        private Canvas _canvas = null!;
        private float _width;
        private float _height;

        /// <summary>The <see cref="Redraw"/> of the painter that drew last: a lawn finished on a worker thread asks it for a frame.</summary>
        private static Action? s_redraw;

        public override float Width => _width;

        public override float Height => _height;

        /// <summary>
        /// Asks the host view for another frame, from any thread (<c>View.PostInvalidate</c>): a lawn rendered on a worker
        /// thread is ready.
        /// </summary>
        public Action? Redraw { get; set; }

        /// <summary>What a cached mask was made from.</summary>
        private enum MaskKind
        {
            Shape,
            Composite,
            Skin,
        }

        /// <summary>Starts a frame on a view's canvas.</summary>
        public void Begin(Canvas canvas, float width, float height, Client.UI.Design.Insets insets)
        {
            _canvas = canvas;
            _width = width;
            _height = height;
            Insets = insets;
            Volatile.Write(ref s_redraw, Redraw);
            Pictures.NextFrame();
            BeginFrame();
        }

        protected override void ApplyTransform(float dx, float dy, float sx, float sy, float cx, float cy)
        {
            _canvas.Save();
            _canvas.Translate(dx, dy);
            _canvas.Scale(sx, sy, cx, cy);
        }

        /// <summary>
        /// A bundled Nunito face: the embedded file is written to the cache folder once, then loaded from there. Null
        /// (logged once) keeps the system typeface, so text never disappears.
        /// </summary>
        private static Typeface? LoadFont(bool bold)
        {
            string resource = FontResource(bold);
            try
            {
                string folder = Android.App.Application.Context.CacheDir!.AbsolutePath;
                string path = System.IO.Path.Combine(folder, System.IO.Path.GetFileName(resource));
                using (System.IO.Stream? stream = typeof(AndroidPainter).Assembly.GetManifestResourceStream(resource))
                {
                    if (stream == null)
                    {
                        Android.Util.Log.Warn("Bloomlings", resource + " is not embedded; using the system font");
                        return null;
                    }

                    if (!System.IO.File.Exists(path) || new System.IO.FileInfo(path).Length != stream.Length)
                    {
                        using System.IO.FileStream file = System.IO.File.Create(path);
                        stream.CopyTo(file);
                    }
                }

                return Typeface.CreateFromFile(path);
            }
            catch (Exception e)
            {
                Android.Util.Log.Warn("Bloomlings", "Could not load " + resource + ": " + e.Message);
                return null;
            }
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
            if (box.Height <= 0f)
            {
                return;
            }

            Paint paint = Fill(top);
            // One gradient per pair of colors and height, from 0 down to the height; the box is drawn moved up to 0.
            Color from = ToColor(Faded(top, Alpha));
            Color to = ToColor(Faded(bottom, Alpha));
            (int, int, float) key = (from.ToArgb(), to.ToArgb(), box.Height);
            if (!Gradients.TryGetValue(key, out LinearGradient? shader))
            {
                if (Gradients.Count >= GradientCacheLimit)
                {
                    foreach (LinearGradient old in Gradients.Values)
                    {
                        old.Dispose();
                    }

                    Gradients.Clear();
                }

                shader = new LinearGradient(0f, 0f, 0f, box.Height, from, to, Shader.TileMode.Clamp!);
                Gradients[key] = shader;
            }

            paint.SetShader(shader);
            float r = Math.Min(radius, box.Height / 2f);
            _canvas.Save();
            _canvas.Translate(0f, box.Top);
            _rect.Set(box.Left, 0f, box.Right, box.Height);
            _canvas.DrawRoundRect(_rect, r, r, paint);
            _canvas.Restore();
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

        public override void Shape(string id, Box box, Rgba color) => DrawMask(MaskKind.Shape, id, null, box, color);

        public override void ShapeOf(string key, Func<float, float, float> sdf, Box box, Rgba color) => DrawMask(MaskKind.Composite, key, sdf, box, color);

        private void DrawMask(MaskKind kind, string key, Func<float, float, float>? sdf, Box box, Rgba color)
        {
            int size = ShapeRaster.Quantize(Math.Max(box.Width, box.Height));
            if (!Masks.TryGetValue((kind, key, size), out Bitmap? bitmap))
            {
                Func<float, float, float> shape = kind switch
                {
                    MaskKind.Shape => ShapeLibrary.Get(key),
                    MaskKind.Skin => ShapeLibrary.SkinPattern(key),
                    _ => sdf!,
                };
                byte[] mask = ShapeRaster.Mask(shape, size, topDown: true);
                bitmap = Bitmap.CreateBitmap(size, size, Bitmap.Config.Alpha8!)!;
                bitmap.CopyPixelsFromBuffer(Java.Nio.ByteBuffer.Wrap(mask));
                Masks[(kind, key, size)] = bitmap;
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
            float size = FitSize(shown, style, maxWidth, sizeScale, (t, s) => MeasureAt(t, style, s));
            _text.Reset();
            _text.AntiAlias = true;
            _text.SetTypeface(style.Bold ? _bold : _regular);
            _text.TextSize = size;
            float width = _text.MeasureText(shown);
            float left = centered ? x - (width / 2f) : x;
            float baseline = cy - ((_text.Descent() + _text.Ascent()) / 2f);
            if (look != null)
            {
                DrawLook(shown, left, baseline, size, look);
                return;
            }

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

        /// <summary>A label with volume (contracts/painter-text.md): shadow, extrusion, outline, gradient fill.</summary>
        private void DrawLook(string shown, float left, float baseline, float size, TextLook look)
        {
            float stroke = look.OutlineEm * size * 2f;
            (int count, float step) = Extrusion(look, size);
            _text.StrokeJoin = Paint.Join.Round;
            if (look.ShadowAlpha > 0f)
            {
                _text.SetStyle(Paint.Style.FillAndStroke);
                _text.StrokeWidth = stroke;
                _text.Color = ToColor(Faded(DesignTokens.Colors.GardenShadow.WithAlpha(look.ShadowAlpha), Alpha));
                using var blur = new BlurMaskFilter(Math.Max(1f, 0.06f * size), BlurMaskFilter.Blur.Normal!);
                _text.SetMaskFilter(blur);
                _canvas.DrawText(shown, left, baseline + ((look.ExtrudeEm + 0.05f) * size), _text);
                _text.SetMaskFilter(null);
            }

            if (look.Emboss.HasValue)
            {
                _text.SetStyle(Paint.Style.Fill);
                _text.Color = ToColor(Faded(look.Emboss.Value, Alpha));
                _canvas.DrawText(shown, left, baseline + (0.05f * size), _text);
            }

            if (stroke > 0f)
            {
                _text.SetStyle(Paint.Style.FillAndStroke);
                _text.StrokeWidth = stroke;
                _text.Color = ToColor(Faded(look.Outline, Alpha));
                for (int k = count; k >= 0; k--)
                {
                    _canvas.DrawText(shown, left, baseline + (k * step), _text);
                }
            }

            _text.SetStyle(Paint.Style.Fill);
            _text.Color = ToColor(Faded(look.FillTop, Alpha));
            float top = baseline - (size * 0.7f);
            using var shader = new LinearGradient(0f, top, 0f, baseline, new int[] { ToColor(Faded(look.FillTop, Alpha)).ToArgb(), ToColor(Faded(look.FillBottom, Alpha)).ToArgb() }, new[] { 0.3f, 1f }, Shader.TileMode.Clamp!);
            _text.SetShader(shader);
            _canvas.DrawText(shown, left, baseline, _text);
            _text.SetShader(null);
        }

        /// <summary>
        /// The garden backdrop, cached per theme and size. The Home and splash skies render at once. The gameplay lawn is
        /// heavier (a third of the screen's resolution), so it renders on a worker thread (<see cref="BackdropRaster.Render"/>
        /// is pure) while the lawn's flat gradient (<c>lawn.light</c> to <c>lawn.dark</c>) shows; its bitmap is made here,
        /// on the UI thread, in the first frame after it is ready (<see cref="Redraw"/> asks for that frame).
        /// </summary>
        public override void Backdrop(Box box, BackdropColors colors, BackdropScene scene, string cacheKey)
        {
            float step = BackdropRaster.Downscale(scene);
            int w = Math.Max(32, (int)(box.Width / step));
            int h = Math.Max(32, (int)(box.Height / step));
            string key = cacheKey + "@" + w + "x" + h;
            if (!Backdrops.TryGetValue(key, out Bitmap? bitmap))
            {
                byte[]? rgba = BackdropRaster.IsLawn(scene) ? RenderedAside(key, w, h, colors, scene) : BackdropRaster.Render(w, h, colors, scene);
                if (rgba == null)
                {
                    FillRoundGradient(box, 0f, DesignTokens.Colors.LawnLight, DesignTokens.Colors.LawnDark);
                    return;
                }

                bitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888!)!;
                bitmap.CopyPixelsFromBuffer(Java.Nio.ByteBuffer.Wrap(rgba));
                Backdrops[key] = bitmap;
            }

            _source.Set(0, 0, w, h);
            _canvas.DrawBitmap(bitmap, _source, R(box), Fill(Rgba.White));
        }

        /// <summary>
        /// A backdrop rendered on a worker thread: its pixels once they are ready, else null (the first call starts the
        /// render). A render that failed runs again here, so its error still shows.
        /// </summary>
        private byte[]? RenderedAside(string key, int w, int h, BackdropColors colors, BackdropScene scene)
        {
            if (!PendingBackdrops.TryGetValue(key, out Task<byte[]>? task))
            {
                task = Task.Run(() => BackdropRaster.Render(w, h, colors, scene));
                // The view drawing by then (a recreated activity's, too) shows it.
                task.ContinueWith(_ => Volatile.Read(ref s_redraw)?.Invoke(), TaskScheduler.Default);
                PendingBackdrops[key] = task;
            }

            if (!task.IsCompleted)
            {
                return null;
            }

            PendingBackdrops.Remove(key);
            if (task.Status == TaskStatus.RanToCompletion)
            {
                return task.Result;
            }

            Android.Util.Log.Warn("Bloomlings", "The lawn did not render aside (" + task.Exception?.GetBaseException().Message + "); rendering it here");
            return BackdropRaster.Render(w, h, colors, scene);
        }

        public override void Picture(string key, Box box, Func<int, int, byte[]> render)
        {
            if (box.Width <= 0f || box.Height <= 0f)
            {
                return;
            }

            int w = PictureSize(box.Width);
            int h = PictureSize(box.Height);
            if (!Pictures.TryGet(key, w, h, out Bitmap bitmap))
            {
                byte[] rgba = render(w, h);
                if (rgba.Length != w * h * 4)
                {
                    throw new ArgumentException("Picture " + UiRaster.CacheKey(key, w, h) + " rendered " + rgba.Length + " bytes, expected " + (w * h * 4) + ".");
                }

                // ARGB_8888 holds premultiplied RGBA bytes; UiRaster renders straight alpha.
                Premultiply(rgba);
                bitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888!)!;
                bitmap.CopyPixelsFromBuffer(Java.Nio.ByteBuffer.Wrap(rgba));
                // Dropped bitmaps are disposed, not recycled: a hardware canvas may still hold a recent frame's draws of them.
                Pictures.Add(key, w, h, bitmap, rgba.Length);
            }

            _source.Set(0, 0, w, h);
            _canvas.DrawBitmap(bitmap, _source, R(box), Fill(Rgba.White));
        }

        /// <summary>Straight to premultiplied alpha, in place.</summary>
        private static void Premultiply(byte[] rgba)
        {
            for (int i = 0; i < rgba.Length; i += 4)
            {
                int a = rgba[i + 3];
                if (a == 255)
                {
                    continue;
                }

                rgba[i] = (byte)(((rgba[i] * a) + 127) / 255);
                rgba[i + 1] = (byte)(((rgba[i + 1] * a) + 127) / 255);
                rgba[i + 2] = (byte)(((rgba[i + 2] * a) + 127) / 255);
            }
        }

        public override bool HasSprite(string name) => LoadSprite(name) != null;

        public override (int Width, int Height)? SpriteSize(string name)
        {
            Bitmap? bitmap = LoadSprite(name);
            return bitmap == null ? null : (bitmap.Width, bitmap.Height);
        }

        public override void Sprite(string name, Box box)
        {
            Bitmap? bitmap = LoadSprite(name);
            if (bitmap == null)
            {
                return;
            }

            _source.Set(0, 0, bitmap.Width, bitmap.Height);
            _canvas.DrawBitmap(bitmap, _source, R(Fit(box, bitmap.Width, bitmap.Height)), Fill(Rgba.White));
        }

        public override void SpriteSkin(string name, Box box, string skinShape, Rgba tint)
        {
            Bitmap? bitmap = LoadSprite(name);
            if (bitmap == null)
            {
                return;
            }

            // A layer: the pattern, then the picture with DST_IN, so the pattern stays only on the picture.
            Box fitted = Fit(box, bitmap.Width, bitmap.Height);
            int layer = _canvas.SaveLayer(R(fitted), null);
            DrawMask(MaskKind.Skin, skinShape, null, fitted, tint);
            using var mask = new Paint(PaintFlags.AntiAlias | PaintFlags.FilterBitmap);
            using var mode = new PorterDuffXfermode(PorterDuff.Mode.DstIn!);
            mask.SetXfermode(mode);
            _source.Set(0, 0, bitmap.Width, bitmap.Height);
            _canvas.DrawBitmap(bitmap, _source, R(fitted), mask);
            _canvas.RestoreToCount(layer);
        }

        /// <summary>An embedded picture (a character or an owner picture), decoded once; null (logged once) when it is missing.</summary>
        private static Bitmap? LoadSprite(string name)
        {
            if (!Sprites.TryGetValue(name, out Bitmap? bitmap))
            {
                string? grey = GreySource(name);
                if (grey != null)
                {
                    // A grey copy (a stuck slot's tile icon), made once from its picture.
                    Bitmap? source = LoadSprite(grey);
                    bitmap = source != null ? Greyed(source) : null;
                }
                else
                {
                    using System.IO.Stream? stream = typeof(AndroidPainter).Assembly.GetManifestResourceStream(SpriteResource(name));
                    bitmap = stream != null ? BitmapFactory.DecodeStream(stream) : null;
                    if (bitmap == null)
                    {
                        Android.Util.Log.Warn("Bloomlings", SpriteResource(name) + " is not embedded; drawing the stand-in");
                    }
                    else if (name.StartsWith(IconPrefix, StringComparison.Ordinal))
                    {
                        // The owner's icons are drawn far below their size (a 512 px icon on a 60 px tile): mipmaps keep
                        // them smooth.
                        bitmap.HasMipMap = true;
                    }
                }

                Sprites[name] = bitmap;
            }

            return bitmap;
        }

        /// <summary>A grey copy of a picture (<see cref="OwnerPictures.GreyPixels"/>), with mipmaps as its picture.</summary>
        private static Bitmap Greyed(Bitmap source)
        {
            int w = source.Width;
            int h = source.Height;
            var colors = new int[w * h];

            // GetPixels gives straight-alpha ARGB colors; CreateBitmap takes them the same way.
            source.GetPixels(colors, 0, w, 0, 0, w, h);
            var rgba = new byte[colors.Length * 4];
            for (int i = 0; i < colors.Length; i++)
            {
                int c = colors[i];
                rgba[(i * 4) + 0] = (byte)(c >> 16);
                rgba[(i * 4) + 1] = (byte)(c >> 8);
                rgba[(i * 4) + 2] = (byte)c;
                rgba[(i * 4) + 3] = (byte)(c >> 24);
            }

            OwnerPictures.GreyPixels(rgba);
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = (rgba[(i * 4) + 3] << 24) | (rgba[i * 4] << 16) | (rgba[(i * 4) + 1] << 8) | rgba[(i * 4) + 2];
            }

            Bitmap grey = Bitmap.CreateBitmap(colors, w, h, Bitmap.Config.Argb8888!)!;
            grey.HasMipMap = source.HasMipMap;
            return grey;
        }

        public override void PushClip(Box box)
        {
            _canvas.Save();
            _canvas.ClipRect(R(box));
        }

        public override void PopClip() => _canvas.Restore();
    }
}
