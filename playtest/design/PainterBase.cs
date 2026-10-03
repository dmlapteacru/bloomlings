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
        private readonly List<(float Dx, float Dy, float Sx, float Sy, float Cx, float Cy, float Degrees)> _transforms = new List<(float, float, float, float, float, float, float)>();
        private (float X, float Y, float At)? _release;

        public abstract float Width { get; }

        public abstract float Height { get; }

        public float Scale => DesignTokens.ScaleFor(Width, Height);

        public Insets Insets { get; set; }

        /// <summary>Where a finger is down, or null.</summary>
        public (float X, float Y)? Finger { get; set; }

        /// <summary>The host's clock in seconds; set before each frame and each touch.</summary>
        public float Now { get; set; }

        /// <summary>Whether a press is still springing back (the host keeps drawing frames).</summary>
        public bool Springing => _release.HasValue && Now - _release.Value.At < DesignTokens.Motion.Press.Seconds;

        public IReadOnlyList<(Box Box, Action Action)> Hits => _hits;

        /// <summary>The alpha everything is multiplied by now.</summary>
        protected float Alpha => _alpha.Count == 0 ? 1f : _alpha[_alpha.Count - 1];

        public virtual void BeginFrame()
        {
            _hits.Clear();
            _alpha.Clear();
            _transforms.Clear();
        }

        /// <summary>Runs the topmost target under a tap; false when none was hit. The lift starts the spring-back.</summary>
        public bool Dispatch(float x, float y)
        {
            _release = (x, y, Now);
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

        public float Released(Box box)
        {
            if (!_release.HasValue || Finger.HasValue)
            {
                return -1f;
            }

            (float x, float y, float at) = _release.Value;
            return ToScreen(box).Contains(x, y) ? Math.Max(0f, Now - at) : -1f;
        }

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
            _transforms.Add((dx, dy, scale, scale, cx, cy, 0f));
            ApplyTransform(dx, dy, scale, scale, cx, cy);
        }

        public void PushSquash(float sx, float sy, float cx, float cy)
        {
            _transforms.Add((0f, 0f, sx, sy, cx, cy, 0f));
            ApplyTransform(0f, 0f, sx, sy, cx, cy);
        }

        public void PushRotate(float degrees, float cx, float cy)
        {
            _transforms.Add((0f, 0f, 1f, 1f, cx, cy, degrees));
            ApplyRotation(degrees, cx, cy);
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

        public abstract void Sprite(string name, Box box);

        public abstract bool HasSprite(string name);

        public abstract (int Width, int Height)? SpriteSize(string name);

        public abstract void SpriteSkin(string name, Box box, string skinShape, Rgba tint);

        /// <summary>
        /// The embedded resource name of a picture: <c>bg/{name}</c> is an owner background (embedded without its extension,
        /// so a PNG or a JPEG works), <c>brand/{name}</c> the
        /// logo, <c>icon/{name}</c> a booster icon, a variant icon or the lotus and <c>decor/{name}</c> a leaf decoration
        /// (spec 005 pictures.md B, C, D and G, <see cref="OwnerPictures"/>), <c>heromotion/{frame}</c> an animated hero's
        /// frame (spec 005 FR-028, embedded without its extension); every other name is a character picture (spec 004
        /// contracts/art-files.md "Loading"). A grey copy (<see cref="GreySuffix"/>) comes from its picture's resource.
        /// </summary>
        public static string SpriteResource(string name)
        {
            string? grey = GreySource(name);
            if (grey != null)
            {
                // A grey copy is made from its picture's resource.
                return SpriteResource(grey);
            }

            if (name.StartsWith(BackgroundPrefix, StringComparison.Ordinal))
            {
                return "backgrounds/" + name.Substring(BackgroundPrefix.Length);
            }

            if (name.StartsWith(BrandPrefix, StringComparison.Ordinal))
            {
                return "brand/" + name.Substring(BrandPrefix.Length) + ".png";
            }

            if (name.StartsWith(IconPrefix, StringComparison.Ordinal))
            {
                return "icons/" + name.Substring(IconPrefix.Length) + ".png";
            }

            if (name.StartsWith(DecorPrefix, StringComparison.Ordinal))
            {
                return "decor/" + name.Substring(DecorPrefix.Length) + ".png";
            }

            if (name.StartsWith(HeroMotionPrefix, StringComparison.Ordinal))
            {
                // Embedded without the extension, as the backgrounds.
                return name;
            }

            return "characters/" + name + ".png";
        }

        /// <summary>The name prefix of the owner's backgrounds (<c>bg/home</c>).</summary>
        public const string BackgroundPrefix = "bg/";

        /// <summary>The name prefix of the owner's logo pictures (<c>brand/logo</c>).</summary>
        public const string BrandPrefix = "brand/";

        /// <summary>The name prefix of the owner's icon pictures (<c>icon/booster-shuffle</c>, spec 005 pictures.md D).</summary>
        public const string IconPrefix = "icon/";

        /// <summary>The name prefix of the owner's leaf pictures (<c>decor/ivy</c>, spec 005 pictures.md D).</summary>
        public const string DecorPrefix = "decor/";

        /// <summary>
        /// The name prefix of the animated heroes' frames (<c>heromotion/sprig-idle-07</c>, spec 005 FR-028: the
        /// <see cref="HeroMotion.Folder"/> folder, lower case). The painters decode a frame when it is first drawn and keep
        /// it in a cache bounded by bytes (<see cref="HeroFrameStore"/>), never the whole set.
        /// </summary>
        public const string HeroMotionPrefix = "heromotion/";

        /// <summary>Whether a picture name is an animated hero's frame (<see cref="HeroMotionPrefix"/>).</summary>
        public static bool IsHeroFrame(string name) => name.StartsWith(HeroMotionPrefix, StringComparison.Ordinal);

        /// <summary>
        /// The suffix of a picture's grey copy (<c>icon/variant-leaf#grey</c>: a stuck slot's tile icon, spec 005
        /// pictures.md G): the painter makes it once from the picture (<see cref="OwnerPictures.GreyPixels"/>) and caches it.
        /// </summary>
        public const string GreySuffix = "#grey";

        /// <summary>The picture a grey copy is made from (<see cref="GreySuffix"/>), or null for any other name.</summary>
        public static string? GreySource(string name) =>
            name.EndsWith(GreySuffix, StringComparison.Ordinal) ? name.Substring(0, name.Length - GreySuffix.Length) : null;

        /// <summary>A picture of <paramref name="width"/> × <paramref name="height"/> fitted into a box: aspect kept, centered.</summary>
        public static Box Fit(Box box, float width, float height) => CharacterArt.FitBox(box, width, height);

        /// <summary>A box in the current transform, in screen pixels.</summary>
        protected Box ToScreen(Box box)
        {
            float l = box.Left;
            float t = box.Top;
            float r = box.Right;
            float b = box.Bottom;
            for (int i = _transforms.Count - 1; i >= 0; i--)
            {
                (float dx, float dy, float sx, float sy, float cx, float cy, float degrees) = _transforms[i];
                if (degrees != 0f)
                {
                    // A turn: the bounds of the turned corners (clockwise on screen, y down).
                    double a = degrees * Math.PI / 180.0;
                    float cos = (float)Math.Cos(a);
                    float sin = (float)Math.Sin(a);
                    float minX = float.MaxValue;
                    float minY = float.MaxValue;
                    float maxX = float.MinValue;
                    float maxY = float.MinValue;
                    foreach ((float x, float y) in new[] { (l, t), (r, t), (l, b), (r, b) })
                    {
                        float rx = cx + ((x - cx) * cos) - ((y - cy) * sin);
                        float ry = cy + ((x - cx) * sin) + ((y - cy) * cos);
                        minX = Math.Min(minX, rx);
                        minY = Math.Min(minY, ry);
                        maxX = Math.Max(maxX, rx);
                        maxY = Math.Max(maxY, ry);
                    }

                    l = minX;
                    t = minY;
                    r = maxX;
                    b = maxY;
                    continue;
                }

                float l2 = ((l - cx) * sx) + cx + dx;
                float r2 = ((r - cx) * sx) + cx + dx;
                float t2 = ((t - cy) * sy) + cy + dy;
                float b2 = ((b - cy) * sy) + cy + dy;

                // A mirroring transform (a negative scale, PushSquash(-1, 1, …)) swaps the edges.
                l = Math.Min(l2, r2);
                r = Math.Max(l2, r2);
                t = Math.Min(t2, b2);
                b = Math.Max(t2, b2);
            }

            return new Box(l, t, r, b);
        }

        /// <summary>Called for each touch target (the preview checks sizes and overlaps).</summary>
        protected virtual void OnHit(Box screenBox)
        {
        }

        protected abstract void ApplyTransform(float dx, float dy, float sx, float sy, float cx, float cy);

        /// <summary>Turns the canvas by <paramref name="degrees"/> clockwise about (cx, cy) after saving it (<see cref="RestoreTransform"/> undoes it).</summary>
        protected abstract void ApplyRotation(float degrees, float cx, float cy);

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

        public abstract void Text(string text, float cx, float cy, TypeStyle style, Rgba color, float maxWidth = 0f, float sizeScale = 1f, TextLook? look = null);

        public abstract void TextLeft(string text, float x, float cy, TypeStyle style, Rgba color, float maxWidth = 0f, float sizeScale = 1f, TextLook? look = null);

        public abstract float MeasureText(string text, TypeStyle style, float sizeScale = 1f);

        public abstract void Backdrop(Box box, BackdropColors colors, BackdropScene scene, string cacheKey);

        public abstract void Picture(string key, Box box, Func<int, int, byte[]> render);

        /// <summary>The cache size of a picture's side (<see cref="UiRaster.Quantize"/>).</summary>
        protected static int PictureSize(float pixels) => UiRaster.Quantize(pixels);

        /// <summary>
        /// How many bytes of pictures a painter keeps (<see cref="PictureCache{T}"/>): past it, the least recently used
        /// pictures not drawn in the last two frames are dropped and render again on demand.
        /// </summary>
        protected const long PictureCacheBytes = 12L * 1024 * 1024;

        /// <summary>
        /// How many bytes of decoded hero frames a painter keeps (<see cref="HeroFrameStore"/>): a frame is held as its
        /// palette picture, one byte a pixel, so the 576 frames (24 fps) would take about 70 MiB and Home's four idle loops
        /// take 47 MiB. The budget holds those loops and the last reaction or two (about 6 MiB each), never the whole set:
        /// past it the least recently drawn frames (an earlier hero's reaction first, as the idle loops come round every
        /// 4 s) are dropped and decode again when drawn next. It must stay above the idle loops, or Home would decode every
        /// frame it draws.
        /// </summary>
        protected const long HeroFrameCacheBytes = 60L * 1024 * 1024;

        /// <summary>
        /// How many bytes of hero frames a painter keeps expanded to RGBA for drawing (the frames on screen and the few
        /// before them): with <see cref="HeroFrameCacheBytes"/>, at most about 68 MiB of decoded frames.
        /// </summary>
        protected const long HeroDrawCacheBytes = 8L * 1024 * 1024;

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

        /// <summary>The bundled font file of a style (spec 003 contracts/fonts.md): ExtraBold for bold styles, else SemiBold.</summary>
        public static string FontResource(bool bold) => bold ? "fonts/Nunito-ExtraBold.ttf" : "fonts/Nunito-SemiBold.ttf";

        /// <summary>The steps of a label's extrusion (contracts/painter-text.md): 4 copies, each at least one pixel apart.</summary>
        protected static (int Count, float Step) Extrusion(TextLook look, float size)
        {
            if (look.ExtrudeEm <= 0f)
            {
                return (0, 0f);
            }

            const int count = 4;
            return (count, Math.Max(1f, look.ExtrudeEm * size / count));
        }
    }
}
