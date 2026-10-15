using System;
using System.Collections.Generic;
using System.Threading;
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
    /// ARGB_8888 bitmaps drawn with filtering, in a cache bounded by bytes (<see cref="PictureCache{T}"/>). The animated
    /// heroes' frames (spec 005 FR-028) are decoded when first drawn into palette pictures kept in a cache bounded by bytes
    /// (<see cref="HeroFrameStore"/>), and the frames on screen are expanded into a few reused bitmaps
    /// (<see cref="HeroBitmaps"/>), never all 576 at once. The gameplay lawn renders on a worker thread; until it is ready
    /// the lawn's flat gradient shows, then the view redraws.
    /// </summary>
    public sealed class AndroidPainter : PainterBase, IPictureWarmer, ICachedIllustration, ITransientPictures
    {
        /// <summary>How many vertical gradients are kept before they are all made again.</summary>
        private const int GradientCacheLimit = 256;

        private static readonly Dictionary<(MaskKind Kind, string Key, int Size), Bitmap> Masks = new Dictionary<(MaskKind, string, int), Bitmap>();
        private static readonly Dictionary<string, Bitmap> Backdrops = new Dictionary<string, Bitmap>(StringComparer.Ordinal);
        private static readonly PictureCache<Bitmap> Pictures = new PictureCache<Bitmap>(PictureCacheBytes, bitmap => bitmap.Dispose());
        private static readonly Dictionary<(int Top, int Bottom, float Height), LinearGradient> Gradients = new Dictionary<(int, int, float), LinearGradient>();
        private static readonly PictureCache<Bitmap> Sprites = new PictureCache<Bitmap>(64L * 1024 * 1024, bitmap => bitmap.Dispose());
        private static readonly PictureCache<Bitmap> Illustrations = new PictureCache<Bitmap>(16L * 1024 * 1024, bitmap => bitmap.Dispose());
        private static readonly Dictionary<string, (byte[], int, int)?> Alphas = new Dictionary<string, (byte[], int, int)?>(StringComparer.Ordinal);
        private static readonly HeroFrameStore HeroFrames = new HeroFrameStore(HeroFrameCacheBytes);
        private static readonly HeroBitmaps HeroDraws = new HeroBitmaps(HeroDrawCacheBytes);

        private readonly Paint _paint = new Paint(PaintFlags.AntiAlias | PaintFlags.FilterBitmap);
        private readonly Paint _text = new Paint(PaintFlags.AntiAlias);
        private static Typeface? s_bold;
        private static Typeface? s_regular;
        private static Typeface? s_shop;
        private readonly Typeface _bold = s_bold ??= LoadFont(true) ?? Typeface.Create(Typeface.Default, TypefaceStyle.Bold)!;
        private readonly Typeface _regular = s_regular ??= LoadFont(false) ?? Typeface.Default!;
        private readonly Typeface _shop = s_shop ??= LoadFontResource("fonts/LilitaOne-Regular.ttf") ?? Typeface.Default!;
        private readonly RectF _rect = new RectF();
        private readonly Rect _source = new Rect();
        private Canvas _canvas = null!;
        private Canvas _output = null!;
        private Android.Graphics.Picture? _candidate;
        private readonly ReadyFrame<Android.Graphics.Picture> _frames = new ReadyFrame<Android.Graphics.Picture>(picture => picture.Dispose());
        private float _width;
        private float _height;

        public bool PreparingFrame => _frames.Waiting;
        private bool _transient;

        public void Transient(Action draw)
        {
            bool previous = _transient;
            _transient = true;
            try { draw(); }
            finally { _transient = previous; }
        }

        /// <summary>The <see cref="Redraw"/> of the painter that drew last: a lawn finished on a worker thread asks it for a frame.</summary>
        private static Action? s_redraw;

        public override float Width => _width;

        public override float Height => _height;

        /// <summary>
        /// Asks the host view for another frame, from any thread (<c>View.PostInvalidate</c>): a lawn rendered on a worker
        /// thread is ready.
        /// </summary>
        public Action? Redraw { get; set; }

        /// <summary>Called on the UI thread when Android needs memory; visible and recent images stay valid.</summary>
        public static void ReleaseUnused()
        {
            Pictures.TrimUnused();
            Sprites.TrimUnused();
            Illustrations.TrimUnused();
            HeroFrames.TrimUnused();
        }

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
            if (_width != width || _height != height) _frames.Dispose();
            _output = canvas;
            _width = width;
            _height = height;
            Insets = insets;
            Volatile.Write(ref s_redraw, Redraw);
            _frames.Begin();
            _candidate = new Android.Graphics.Picture();
            _canvas = _candidate.BeginRecording((int)width, (int)height)!;
            BeginFrame();
        }

        /// <summary>Retains the previous display list until every visible resource of this frame is ready.</summary>
        public void End()
        {
            _candidate!.EndRecording();
            bool published = _frames.Publish(_candidate);
            _candidate = null;
            _canvas = _output;
            if (_frames.Current != null) _canvas.DrawPicture(_frames.Current);
            else _canvas.DrawColor(new Color(236, 244, 222));
            if (published)
            {
                // A retained display list still references its bitmaps. Do not evict/reuse them while it is displayed.
                Pictures.NextFrame(); Sprites.NextFrame(); Illustrations.NextFrame();
                HeroFrames.NextFrame(); HeroDraws.NextFrame();
            }
            else
            {
                // This indicator has no assets of its own and cannot recursively wait on the picture worker.
                float radius = Math.Max(12f, _width * .021f), x = _width * .5f, y = _height * .45f;
                _paint.Reset(); _paint.AntiAlias = true; _paint.Color = new Color(255, 250, 230, 235);
                _canvas.DrawCircle(x, y, radius * 1.7f, _paint);
                _paint.SetStyle(Paint.Style.Stroke); _paint.StrokeWidth = radius * .24f;
                _paint.StrokeCap = Paint.Cap.Round; _paint.Color = new Color(66, 135, 75);
                _rect.Set(x - radius, y - radius, x + radius, y + radius);
                _canvas.DrawArc(_rect, (Android.OS.SystemClock.UptimeMillis() % 1000) * .36f, 270f, false, _paint);
            }
        }

        private void Missing(string key)
        {
            if (!_recording && !_transient && !s_pictureWork.Failed(key)) _frames.Missing();
        }

        protected override void ApplyTransform(float dx, float dy, float sx, float sy, float cx, float cy)
        {
            _canvas.Save();
            _canvas.Translate(dx, dy);
            _canvas.Scale(sx, sy, cx, cy);
        }

        protected override void ApplyRotation(float degrees, float cx, float cy)
        {
            _canvas.Save();
            _canvas.Rotate(degrees, cx, cy);
        }

        /// <summary>
        /// A bundled Nunito face: the embedded file is written to the cache folder once, then loaded from there. Null
        /// (logged once) keeps the system typeface, so text never disappears.
        /// </summary>
        private static Typeface? LoadFont(bool bold) => LoadFontResource(FontResource(bold));
        private static Typeface? LoadFontResource(string resource)
        {
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
                string workKey = "mask/" + kind + "/" + key + "/" + size;
                if (s_pictureWork.TryTake(workKey, out Bitmap ready))
                {
                    bitmap = ready;
                    Masks[(kind, key, size)] = bitmap;
                }
                else
                {
                    Func<float, float, float> shape = kind switch
                    {
                        MaskKind.Shape => ShapeLibrary.Get(key),
                        MaskKind.Skin => ShapeLibrary.SkinPattern(key),
                        _ => sdf!,
                    };
                    Missing(workKey);
                    s_pictureWork.Request(workKey, size * size, () =>
                        WarmBitmap(ShapeRaster.Mask(shape, size, topDown: true), size * size, size, size, Bitmap.Config.Alpha8!), urgent: !_recording);
                    return;
                }
            }
            if (_recording) return;

            _source.Set(0, 0, size, size);
            _canvas.DrawBitmap(bitmap, _source, R(box), Fill(color));
        }

        private float MeasureAt(string text, TypeStyle style, float size)
        {
            _text.SetTypeface(ShopBodyDesign.IsShopFont(style)?_shop:style.Bold ? _bold : _regular);
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
            if (_recording || text.Length == 0)
            {
                return;
            }

            string shown = Cased(text, style);
            float size = FitSize(shown, style, maxWidth, sizeScale, (t, s) => MeasureAt(t, style, s));
            _text.Reset();
            _text.AntiAlias = true;
            _text.SetTypeface(ShopBodyDesign.IsShopFont(style)?_shop:style.Bold ? _bold : _regular);
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
                _text.SetMaskFilter(Blur(Math.Max(1f, 0.06f * size)));
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

            // The fill's gradient runs from 0 down to the letters' height, kept per colors and height (as the rounded
            // boxes' gradients), and the letters are drawn moved up by its top: no shader is made for every label.
            float top = baseline - (size * 0.7f);
            _text.SetShader(TextGradient(ToColor(Faded(look.FillTop, Alpha)).ToArgb(), ToColor(Faded(look.FillBottom, Alpha)).ToArgb(), baseline - top));
            _canvas.Save();
            _canvas.Translate(0f, top);
            _canvas.DrawText(shown, left, baseline - top, _text);
            _canvas.Restore();
            _text.SetShader(null);
        }

        private static readonly Dictionary<(int Top, int Bottom, float Height), LinearGradient> TextGradients = new Dictionary<(int, int, float), LinearGradient>();
        private static readonly Dictionary<float, BlurMaskFilter> Blurs = new Dictionary<float, BlurMaskFilter>();

        /// <summary>A label fill's gradient from 0 to <paramref name="height"/> (30% of it in the top color), kept; all are made again past the limit.</summary>
        private static LinearGradient TextGradient(int top, int bottom, float height)
        {
            if (!TextGradients.TryGetValue((top, bottom, height), out LinearGradient? shader))
            {
                if (TextGradients.Count >= GradientCacheLimit)
                {
                    foreach (LinearGradient old in TextGradients.Values)
                    {
                        old.Dispose();
                    }

                    TextGradients.Clear();
                }

                shader = new LinearGradient(0f, 0f, 0f, height, new[] { top, bottom }, new[] { 0.3f, 1f }, Shader.TileMode.Clamp!);
                TextGradients[(top, bottom, height)] = shader;
            }

            return shader;
        }

        /// <summary>A label shadow's blur of <paramref name="radius"/> px, kept per radius (a few type sizes).</summary>
        private static BlurMaskFilter Blur(float radius)
        {
            if (!Blurs.TryGetValue(radius, out BlurMaskFilter? blur))
            {
                if (Blurs.Count >= GradientCacheLimit)
                {
                    foreach (BlurMaskFilter old in Blurs.Values)
                    {
                        old.Dispose();
                    }

                    Blurs.Clear();
                }

                blur = new BlurMaskFilter(radius, BlurMaskFilter.Blur.Normal!);
                Blurs[radius] = blur;
            }

            return blur;
        }

        /// <summary>Fallback garden scenes use the same producer as other graphics, including their bitmap upload.</summary>
        public override void Backdrop(Box box, BackdropColors colors, BackdropScene scene, string cacheKey)
        {
            float step = BackdropRaster.Downscale(scene);
            int w = Math.Max(32, (int)(box.Width / step));
            int h = Math.Max(32, (int)(box.Height / step));
            string key = cacheKey + "@" + w + "x" + h;
            if (!Backdrops.TryGetValue(key, out Bitmap? bitmap))
            {
                string workKey = "backdrop/" + key;
                if (s_pictureWork.TryTake(workKey, out Bitmap ready))
                {
                    bitmap = ready;
                    Backdrops[key] = bitmap;
                }
                else
                {
                    Missing(workKey);
                    s_pictureWork.Request(workKey, w * h * 4, () => WarmBitmap(BackdropRaster.Render(w, h, colors, scene), w * h * 4, w, h), urgent: !_recording);
                    if (!_recording)
                        FillRoundGradient(box, 0f, BackdropRaster.IsLawn(scene) ? DesignTokens.Colors.LawnLight : colors.SkyTop,
                            BackdropRaster.IsLawn(scene) ? DesignTokens.Colors.LawnDark : colors.SkyBottom);
                    return;
                }
            }
            if (_recording) return;
            _source.Set(0, 0, w, h);
            _canvas.DrawBitmap(bitmap, _source, R(box), Fill(Rgba.White));
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
                string cacheKey = UiRaster.CacheKey(key, w, h);
                int bytes = w * h * 4;
                if (s_pictureWork.TryTake(cacheKey, out Bitmap ready))
                {
                    bitmap = ready;
                    Pictures.Add(key, w, h, bitmap, bytes);
                }
                else
                {
                    Missing(cacheKey);
                    QueuePicture(key, w, h, render, urgent: !_recording);
                    return;
                }
            }
            if (_recording) return;

            _source.Set(0, 0, w, h);
            _canvas.DrawBitmap(bitmap, _source, R(box), Fill(Rgba.White));
        }

        public void Illustration(string key, Box bounds, Action draw)
        {
            if (_recording) { draw(); return; }
            int w = PictureSize(bounds.Width), h = PictureSize(bounds.Height);
            if (!Illustrations.TryGet(key, w, h, out Bitmap bitmap))
            {
                string workKey = "illustration/" + UiRaster.CacheKey(key, w, h);
                if (s_pictureWork.TryTake(workKey, out Bitmap ready))
                {
                    bitmap = ready;
                    Illustrations.Add(key, w, h, bitmap, bitmap.AllocationByteCount);
                }
                else
                {
                    if (s_pictureWork.Contains(workKey)) { draw(); return; }
                    // Record once for this frame; the same commands can be flattened off the UI thread when complete.
                    int requested = s_pictureWork.Requested;
                    var recording = new Android.Graphics.Picture();
                    Canvas original = _canvas;
                    var marks = FrameMarks();
                    bool complete = false;
                    try
                    {
                        _canvas = recording.BeginRecording(w, h)!;
                        _canvas.Scale(w / bounds.Width, h / bounds.Height);
                        _canvas.Translate(-bounds.Left, -bounds.Top);
                        draw();
                        complete = true;
                    }
                    finally
                    {
                        _canvas = original; recording.EndRecording(); ForgetSince(marks);
                        if (!complete) recording.Dispose();
                    }
                    _canvas.DrawPicture(recording, R(bounds));
                    // Newly requested components were absent in the recording, even if their jobs already finished.
                    if (s_pictureWork.Requested != requested || s_pictureWork.Progress < 1f) { recording.Dispose(); return; }
                    if (!s_pictureWork.Request(workKey, w * h * 4, () =>
                    {
                        try
                        {
                            Bitmap snapshot = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888!)!;
                            using (var canvas = new Canvas(snapshot)) canvas.DrawPicture(recording);
                            snapshot.PrepareToDraw();
                            return snapshot;
                        }
                        finally { recording.Dispose(); }
                    })) recording.Dispose();
                    return;
                }
            }
            _source.Set(0, 0, w, h);
            _canvas.DrawBitmap(bitmap, _source, R(bounds), Fill(Rgba.White));
        }

        // ---- Warming (IPictureWarmer) ----

        private static readonly PictureWorkQueue<Bitmap> s_pictureWork = new PictureWorkQueue<Bitmap>(
            32L * 1024 * 1024, bitmap => bitmap.Dispose(),
            () => Volatile.Read(ref s_redraw)?.Invoke(),
            error => Android.Util.Log.Warn("Bloomlings", "Picture preparation failed: " + error.Message),
            bitmap => bitmap.AllocationByteCount);
        private static Java.Nio.ByteBuffer? s_warmUpload;
        private static IntPtr s_warmUploadAddress;
        private static int s_warmUploadBytes;
        private static Bitmap? s_sinkBitmap;
        private static Canvas? s_sink;
        private bool _recording;
        public bool WarmsPictures => true;
        public float WarmProgress => s_pictureWork.Progress;

        private static void QueuePicture(string key, int w, int h, Func<int, int, byte[]> render, bool urgent)
        {
            int bytes = checked(w * h * 4);
            s_pictureWork.Request(UiRaster.CacheKey(key, w, h), bytes, () =>
            {
                // Disk reads, rasterization, premultiplication and upload all belong to the producer.
                byte[] rgba = s_store != null && s_store.Has(key, w, h) ? new byte[bytes] : Array.Empty<byte>();
                bool stored = rgba.Length == bytes && s_store!.TryRead(key, w, h, 4, rgba);
                if (!stored)
                {
                    rgba = render(w, h);
                    if (rgba.Length != bytes) throw new ArgumentException("Invalid picture size: " + key);
                    Premultiply(rgba);
                    s_store?.Write(key, w, h, rgba, bytes);
                }
                return WarmBitmap(rgba, bytes, w, h);
            }, urgent);
        }

        public void Warm(Action draw)
        {
            if (_recording) { draw(); return; }
            Canvas real = _canvas;
            var marks = FrameMarks();
            s_sink ??= new Canvas(s_sinkBitmap = Bitmap.CreateBitmap(1, 1, Bitmap.Config.Argb8888!)!);
            int save = s_sink.Save();
            _canvas = s_sink; _recording = true;
            try { draw(); }
            finally
            {
                _recording = false; s_sink.RestoreToCount(save); _canvas = real; ForgetSince(marks);
            }
        }

        /// <summary>A picture or mask bitmap, made on the producer through its reused direct upload buffer.</summary>
        private static Bitmap WarmBitmap(byte[] pixels, int bytes, int width, int height, Bitmap.Config? config = null)
        {
            Bitmap bitmap = Bitmap.CreateBitmap(width, height, config ?? Bitmap.Config.Argb8888!)!;
            if (s_warmUpload == null || s_warmUploadBytes < bytes)
            {
                s_warmUpload?.Dispose();
                s_warmUpload = Java.Nio.ByteBuffer.AllocateDirect(bytes)!;
                s_warmUploadAddress = Android.Runtime.JNIEnv.GetDirectBufferAddress(s_warmUpload.Handle);
                s_warmUploadBytes = bytes;
            }

            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, s_warmUploadAddress, bytes);
            s_warmUpload.Rewind();
            bitmap.CopyPixelsFromBuffer(s_warmUpload);

            // Asks the render thread to upload it to the GPU ahead too (from any thread, Android 7 and later).
            bitmap.PrepareToDraw();
            return bitmap;
        }

        /// <summary>
        /// How many bytes of made pictures the APK keeps on disk between launches (<see cref="PictureStore"/>): a few
        /// journeys through the screens and their level sizes.
        /// </summary>
        private const long StoreBytes = 160L * 1024 * 1024;

        private static PictureStore? s_store;

        /// <summary>
        /// Keeps the made pictures on disk between launches (<see cref="PictureStore"/>) in <paramref name="cacheFolder"/>'s
        /// <c>pictures</c> folder, one folder per build (<paramref name="fingerprint"/>): a screen seen in an earlier launch
        /// opens without making its pictures again. Called once by the view; without it pictures are only made.
        /// </summary>
        public static void OpenStore(string cacheFolder, string fingerprint)
        {
            if (s_store != null)
            {
                return;
            }

            s_store = new PictureStore(System.IO.Path.Combine(cacheFolder, "pictures"), fingerprint, StoreBytes)
            {
                Failed = message => Android.Util.Log.Warn("Bloomlings", message),
            };
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

        public override bool HasSprite(string name)
        {
            if (IsHeroFrame(name)) return HeroFrames.Has(name);
            if (!SpriteExists(name) || s_pictureWork.Failed("sprite/" + name)) return false;
            LoadSprite(name);
            return true;
        }

        private static bool SpriteExists(string name)
        {
            string? grey = GreySource(name);
            return grey != null ? SpriteExists(grey) : PlaytestFiles.Has(SpriteResource(name));
        }

        public override (int Width, int Height)? SpriteSize(string name)
        {
            if (IsHeroFrame(name))
            {
                PalettePicture? frame = HeroFrames.Get(name);
                return frame == null ? null : (frame.Width, frame.Height);
            }

            Bitmap? bitmap = LoadSprite(name);
            return bitmap == null ? null : (bitmap.Width, bitmap.Height);
        }

        public override (byte[] Alpha, int Width, int Height)? SpriteAlpha(string name)
        {
            if (!Alphas.TryGetValue(name, out (byte[], int, int)? alpha))
            {
                Bitmap? bitmap = LoadSprite(name);
                alpha = null;
                if (bitmap != null)
                {
                    int w = bitmap.Width;
                    int h = bitmap.Height;
                    var colors = new int[w * h];
                    bitmap.GetPixels(colors, 0, w, 0, 0, w, h);
                    var bytes = new byte[colors.Length];
                    for (int i = 0; i < colors.Length; i++)
                    {
                        bytes[i] = (byte)(colors[i] >> 24);
                    }

                    alpha = (bytes, w, h);
                }

                // A pending sprite has no alpha yet; do not cache that temporary absence.
                if (bitmap != null || !SpriteExists(name) || s_pictureWork.Failed("sprite/" + name)) Alphas[name] = alpha;
            }

            return alpha;
        }

        public override void Sprite(string name, Box box)
        {
            (Bitmap Bitmap, int Width, int Height)? picture = Drawable(name);
            if (picture == null)
            {
                return;
            }

            (Bitmap bitmap, int w, int h) = picture.Value;
            _source.Set(0, 0, w, h);
            _canvas.DrawBitmap(bitmap, _source, R(Fit(box, w, h)), Fill(Rgba.White));
        }

        public override void SpriteSkin(string name, Box box, string skinShape, Rgba tint)
        {
            (Bitmap Bitmap, int Width, int Height)? picture = Drawable(name);
            if (picture == null)
            {
                return;
            }

            // A layer: the pattern, then the picture with DST_IN, so the pattern stays only on the picture.
            (Bitmap bitmap, int w, int h) = picture.Value;
            Box fitted = Fit(box, w, h);
            int layer = _canvas.SaveLayer(R(fitted), null);
            DrawMask(MaskKind.Skin, skinShape, null, fitted, tint);
            using var mask = new Paint(PaintFlags.AntiAlias | PaintFlags.FilterBitmap);
            using var mode = new PorterDuffXfermode(PorterDuff.Mode.DstIn!);
            mask.SetXfermode(mode);
            _source.Set(0, 0, w, h);
            _canvas.DrawBitmap(bitmap, _source, R(fitted), mask);
            _canvas.RestoreToCount(layer);
        }

        public override void SpriteTint(string name,Box box,Rgba tint)
        {
            var picture=Drawable(name);
            if(!picture.HasValue) return;
            var (bitmap,w,h)=picture.Value;
            using var filter=new PorterDuffColorFilter(new Color(tint.R,tint.G,tint.B),PorterDuff.Mode.SrcIn!);
            using var paint=new Paint(PaintFlags.AntiAlias|PaintFlags.FilterBitmap);
            paint.SetColorFilter(filter);
            paint.Alpha=(int)(tint.A*Alpha);
            _source.Set(0,0,w,h);
            _canvas.DrawBitmap(bitmap,_source,R(Fit(box,w,h)),paint);
        }

        /// <summary>
        /// A picture to draw and the size of its part of the bitmap (from the top-left): an embedded picture decoded once,
        /// or an animated hero's frame from its palette picture in a reused bitmap (<see cref="HeroBitmaps"/>); a frame
        /// that is not a palette PNG decodes as any picture. Null when the picture is missing.
        /// </summary>
        private (Bitmap Bitmap, int Width, int Height)? Drawable(string name)
        {
            if (IsHeroFrame(name))
            {
                PalettePicture? frame = HeroFrames.Get(name);
                if (frame != null)
                {
                    return (HeroDraws.Get(name, frame), frame.Width, frame.Height);
                }

                if (!HeroFrames.Has(name))
                {
                    return null;
                }
            }

            Bitmap? bitmap = LoadSprite(name);
            return bitmap == null ? null : (bitmap, bitmap.Width, bitmap.Height);
        }

        /// <summary>A picture the APK carries (a character or an owner picture, <see cref="PlaytestFiles"/>), decoded once; null (logged once) when it is missing.</summary>
        private Bitmap? LoadSprite(string name)
        {
            if (Sprites.TryGet(name, 0, 0, out Bitmap cached)) return cached;
            if (!SpriteExists(name) || s_pictureWork.Failed("sprite/" + name)) return null;
            if (s_pictureWork.TryTake("sprite/" + name, out Bitmap ready))
            {
                Sprites.Add(name, 0, 0, ready, ready.AllocationByteCount);
                return ready;
            }
            Missing("sprite/" + name);
            s_pictureWork.Request("sprite/" + name, 0, () =>
            {
                Bitmap bitmap = DecodeSprite(name) ?? throw new InvalidOperationException("Could not decode " + name);
                bitmap.PrepareToDraw();
                return bitmap;
            }, urgent: !_recording);
            return null;
        }

        private static Bitmap? DecodeSprite(string name)
        {
            Bitmap? bitmap = null;
            {
                string? grey = GreySource(name);
                if (grey != null)
                {
                    // A grey copy (a stuck slot's tile icon), made once from its picture.
                    using Bitmap? source = DecodeSprite(grey);
                    bitmap = source != null ? Greyed(source) : null;
                }
                else if (name.StartsWith("gameplay/pod-", StringComparison.Ordinal))
                {
                    using Bitmap? atlas = DecodeSprite("gameplay/atlas");
                    var cell = GameplaySkin.PodCell(name.Substring(13));
                    if (atlas != null && cell.HasValue)
                    {
                        int w = atlas.Width / 4, h = atlas.Height / 3;
                        var crop = GameplaySkin.PodCrop(name.Substring(13), w, h);
                        bitmap = Bitmap.CreateBitmap(atlas, cell.Value.Column * w + crop.Left, cell.Value.Row * h + crop.Top, crop.Width, crop.Height);
                    }
                }
                else
                {
                    using System.IO.Stream? stream = PlaytestFiles.Open(SpriteResource(name));
                    bitmap = stream != null ? BitmapFactory.DecodeStream(stream) : null;
                    if(bitmap!=null&&(name==NavigationSkin.CreamBar||name==NavigationSkin.CreamSelected))
                    {
                        var c=NavigationSkin.CreamCrop(name,bitmap.Width,bitmap.Height);
                        Bitmap? cropped=Bitmap.CreateBitmap(bitmap,c.Left,c.Top,c.Width,c.Height);
                        if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();bitmap=cropped;
                    }
                    if(bitmap!=null&&(name.StartsWith("navigation/button-",StringComparison.Ordinal)||name=="navigation/plus-v2"))
                    {
                        var c=name=="navigation/plus-v2" ? NavigationSkin.PlusCrop(bitmap.Width,bitmap.Height) : NavigationSkin.ButtonCrop(name,bitmap.Width,bitmap.Height);
                        Bitmap? cropped=Bitmap.CreateBitmap(bitmap,c.Left,c.Top,c.Width,c.Height);
                        if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();bitmap=cropped;
                    }
                    if(bitmap!=null&&PopupDesign.SettingsIcon(name))
                    { var c=PopupDesign.SettingsCrop(name,bitmap.Width,bitmap.Height);var cropped=Bitmap.CreateBitmap(bitmap,c.Left,c.Top,c.Width,c.Height);if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();bitmap=cropped; }
                    if(bitmap!=null&&name==PopupDesign.Header)
                    { var c=PopupDesign.HeaderCrop(bitmap.Width,bitmap.Height);var cropped=Bitmap.CreateBitmap(bitmap,c.Left,c.Top,c.Width,c.Height);if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();bitmap=cropped; }
                    if(bitmap!=null&&name==PopupDesign.Frame)
                    {
                        var c=PopupDesign.Crop(bitmap.Width,bitmap.Height);var cropped=Bitmap.CreateBitmap(bitmap,c.Left,c.Top,c.Width,c.Height);
                        if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();bitmap=cropped;
                    }
                    if(bitmap!=null&&GameNavigationDesign.Owns(name))
                    {
                        var c=GameNavigationDesign.Crop(name,bitmap.Width,bitmap.Height);Bitmap cropped=Bitmap.CreateBitmap(bitmap,c.Left,c.Top,c.Width,c.Height);if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();bitmap=cropped;
                    }
                    if(bitmap!=null&&name.StartsWith("navigation/icon-",StringComparison.Ordinal))
                    {
                        var c=NavigationSkin.IconCrop(name,bitmap.Width,bitmap.Height);
                        Bitmap? cropped=Bitmap.CreateBitmap(bitmap,c.Left,c.Top,c.Width,c.Height);
                        if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose(); bitmap=cropped;
                    }
                    if(bitmap!=null&&name.StartsWith("shop/",StringComparison.Ordinal)&&name!="shop/background")
                    {
                        if(MetaScreenArt.Is(name))
                        {
                            var c=MetaScreenArt.Crop(name,bitmap.Width,bitmap.Height);
                            Bitmap cropped=Bitmap.CreateBitmap(bitmap,c.Left,c.Top,c.Width,c.Height);
                            if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();bitmap=cropped;
                        }
                        if(name.StartsWith("shop/profile-frame-",StringComparison.Ordinal))
                        {
                            var c=ProfileGameDesign.FrameCrop(name,bitmap.Width,bitmap.Height);
                            Bitmap cropped=Bitmap.CreateBitmap(bitmap,c.Left,c.Top,c.Width,c.Height);
                            if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();bitmap=cropped;
                        }
                        if(name==ProfileGameDesign.Gold)
                        {
                            var c=ProfileGameDesign.GoldCrop(bitmap.Width,bitmap.Height);
                            Bitmap cropped=Bitmap.CreateBitmap(bitmap,c.Left,c.Top,c.Width,c.Height);
                            if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();bitmap=cropped;
                        }
                        if(name.StartsWith("shop/accessory-",StringComparison.Ordinal))
                        {
                            var c=ShopCatalogDesign.AccessoryCrop(name,bitmap.Width,bitmap.Height);
                            Bitmap? cropped=Bitmap.CreateBitmap(bitmap,c.Left,c.Top,c.Width,c.Height);
                            if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();bitmap=cropped;
                        }
                        if(name.StartsWith("shop/catalog-",StringComparison.Ordinal))
                        {
                            var c=ShopCatalogDesign.FrameCrop(name,bitmap.Width,bitmap.Height);
                            Bitmap? cropped=Bitmap.CreateBitmap(bitmap,c.Left,c.Top,c.Width,c.Height);
                            if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();bitmap=cropped;
                        }
                        if(name=="shop/price-button")
                        {
                            var c=ShopBodyDesign.PriceCrop(bitmap.Width,bitmap.Height);
                            Bitmap? cropped=Bitmap.CreateBitmap(bitmap,c.Left,c.Top,c.Width,c.Height);
                            if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();
                            bitmap=cropped;
                        }
                        var booster=ShopBodyDesign.BoosterCell(name.Substring(5));
                        if(bitmap!=null&&booster.HasValue)
                        {
                            int w=bitmap.Width/2,h=bitmap.Height/2;
                            var crop=ShopBodyDesign.BoosterCrop(name.Substring(5),w,h);
                            Bitmap? cropped=Bitmap.CreateBitmap(bitmap,booster.Value.Column*w+crop.Left,booster.Value.Row*h+crop.Top,crop.Width,crop.Height);
                            if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();
                            bitmap=cropped;
                        }
                        var cell=ShopBodyDesign.AtlasCell(name.Substring(5));
                        if(bitmap!=null&&cell.HasValue)
                        {
                            int w=bitmap.Width/3,h=bitmap.Height/2;
                            var crop=ShopBodyDesign.AtlasCrop(name.Substring(5),w,h);
                            Bitmap? cropped=Bitmap.CreateBitmap(bitmap,cell.Value.Column*w+crop.Left,cell.Value.Row*h+crop.Top,crop.Width,crop.Height);
                            if(!ReferenceEquals(cropped,bitmap))bitmap.Dispose();
                            bitmap=cropped;
                        }
                    }
                    if (bitmap == null)
                    {
                        Android.Util.Log.Warn("Bloomlings", SpriteResource(name) + " is not in the APK; drawing the stand-in");
                    }
                    else if (name.StartsWith(IconPrefix, StringComparison.Ordinal) || name.StartsWith(AvatarPrefix, StringComparison.Ordinal))
                    {
                        // The owner's icons are drawn far below their size (a 512 px icon on a 60 px tile), and so are
                        // the avatars in the edit card's grid: mipmaps keep them smooth.
                        bitmap.HasMipMap = true;
                    }
                }


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

        public override void PushClipRound(Box box, float radius)
        {
            _canvas.Save();
            using var path = new Android.Graphics.Path();
            path.AddRoundRect(R(box), radius, radius, Android.Graphics.Path.Direction.Cw!);
            _canvas.ClipPath(path);
        }

        public override void PushClipOutsideCircle(float cx, float cy, float radius)
        {
            _canvas.Save();
            using var path = new Android.Graphics.Path();
            path.AddCircle(cx, cy, radius, Android.Graphics.Path.Direction.Cw!);
            _canvas.ClipOutPath(path);
        }

        public override void PopClip() => _canvas.Restore();
    }
}
